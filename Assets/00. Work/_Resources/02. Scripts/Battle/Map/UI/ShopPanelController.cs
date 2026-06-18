using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.Map.Data;
using Battle.Services;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using Reflex.Attributes;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class ShopPanelController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _areaCamera;
        [SerializeField] private CinemachineCamera _interactCamera;
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private CanvasGroup _exitPanelGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private InteractableObject _interactable;

        [Header("Shop")]
        [SerializeField] private ShopConfigSO      _shopConfig;
        [SerializeField] private PlayerRunStateSO  _playerRunState;
        [SerializeField] private EventChannelSO    _battleEventChannel;
        [SerializeField] private CardFlyAnimator   _cardFlyAnimator;
        [SerializeField] private DeckController    _deckController;
        [SerializeField] private List<ShopCardSlotView> _cardSlots;
        [SerializeField] private Button                    _rerollButton;
        [SerializeField] private TextMeshProUGUI           _rerollCostText;
        [SerializeField] private Button                    _removeCardButton;
        [SerializeField] private TextMeshProUGUI           _removeCardCostText;
        [SerializeField] private CardRemovePopupController _cardRemovePopup;

        [Header("Slide Animation")]
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private float _slideOffsetX = 1200f;
        [SerializeField] private Ease _slideInEase = Ease.OutCubic;
        [SerializeField] private Ease _slideOutEase = Ease.InCubic;

        [Inject] private RewardCardPoolService _cardPoolService;

        public event Action OnExited;

        private CancellationTokenSource _animCts;
        private ShopContentSO _currentContent;
        private int _rerollCount = 0;
        private int _removeCount = 0;

        private void Awake()
        {
            _exitButton.onClick.AddListener(() => OnExited?.Invoke());
            _closeButton?.onClick.AddListener(() => CloseInteractAsync().Forget());
            if (_interactable != null)
                _interactable.OnClicked += () => OpenInteractAsync().Forget();

            _rerollButton?.onClick.AddListener(OnRerollClicked);
            _removeCardButton?.onClick.AddListener(OnRemoveCardClicked);

            if (_cardRemovePopup != null)
                _cardRemovePopup.OnCardRemoved += OnCardRemovedFromPopup;

            SetPanelHidden();
            SetExitPanel(false);
            SetAreaCamera(false);
            SetInteractCamera(false);
        }

        private void OnDestroy()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            if (_cardRemovePopup != null)
                _cardRemovePopup.OnCardRemoved -= OnCardRemovedFromPopup;
        }

        // 노드 진입 시 MapFlowController가 호출 — Area 카메라 + Exit 패널 활성화, 메인 UI 없음
        public void EnterNode(ShopContentSO content)
        {
            _currentContent = content;
            if (_titleText != null && content != null)
                _titleText.text = content.displayName;

            _rerollCount = 0;
            _removeCount = 0;

            SetupCardSlots();
            SetAreaCamera(true);
            SetExitPanel(true);
            _interactable?.SetInteractable(true);
        }

        private readonly List<Action> _slotBuyHandlers = new();

        private void SetupCardSlots()
        {
            if (_cardSlots == null || _cardPoolService == null || _shopConfig == null) return;

            // 이전 구독 해제
            for (int i = 0; i < _cardSlots.Count && i < _slotBuyHandlers.Count; i++)
                _cardSlots[i].OnBuyClicked -= _slotBuyHandlers[i];
            _slotBuyHandlers.Clear();

            var cards = _cardPoolService.DrawCards(_shopConfig.cardSlotCount,
                _playerRunState != null ? _playerRunState.CurrentFloorIndex : 0);

            for (int i = 0; i < _cardSlots.Count; i++)
            {
                var slot = _cardSlots[i];
                if (i < cards.Count)
                {
                    var card  = cards[i];
                    var price = _shopConfig.GetCardPrice(card.grade);
                    slot.Setup(card, price);
                    slot.SetPriceInsufficient(!_playerRunState.CanAfford(price));

                    var captured = slot;
                    Action handler = () => OnSlotBuyClicked(captured);
                    _slotBuyHandlers.Add(handler);
                    slot.OnBuyClicked += handler;
                }
                else
                {
                    _slotBuyHandlers.Add(null);
                    slot.gameObject.SetActive(false);
                }
            }

            RefreshServiceButtons();
        }

        private void OnSlotBuyClicked(ShopCardSlotView slot)
        {
            if (!_playerRunState.CanAfford(slot.Price))
            {
                slot.SetPriceInsufficient(true);
                _battleEventChannel?.RaiseEvent(new GoldInsufficientEvent());
                return;
            }

            Vector2 cardScreenPos = slot.CardScreenPosition;
            int oldGold = _playerRunState.SpendGold(slot.Price);
            _playerRunState.AddCard(slot.Card);
            _deckController?.RefreshCurrentDeckCount();
            _battleEventChannel?.RaiseEvent(new GoldChangedEvent(oldGold, _playerRunState.Gold));
            slot.SetPurchased();
            _cardFlyAnimator?.FlyToCurrentDeck(cardScreenPos);
        }

        // NPC 클릭 시 — Interact 카메라 + UI 슬라이드 인
        public async UniTask OpenInteractAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            SetInteractCamera(true);

            _panelGroup.alpha = 1f;
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;
            _panelRect.anchoredPosition = new Vector2(_slideOffsetX, _panelRect.anchoredPosition.y);

            await LMotion.Create(_slideOffsetX, 0f, _slideDuration)
                .WithEase(_slideInEase)
                .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                .ToUniTask(ct);

            _panelGroup.interactable = true;
            _panelGroup.blocksRaycasts = true;
        }

        // 닫기 버튼 — UI 슬라이드 아웃 + Interact 카메라 비활성화 → Area 카메라 복귀
        public async UniTask CloseInteractAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;

            await LMotion.Create(0f, _slideOffsetX, _slideDuration)
                .WithEase(_slideOutEase)
                .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                .ToUniTask(ct);

            SetPanelHidden();
            SetInteractCamera(false);
            _interactable?.SetInteractable(true);
        }

        // 나가기 버튼 처리 후 MapFlowController가 호출 — 모든 카메라 비활성화
        public async UniTask ExitAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            if (_panelGroup.interactable)
            {
                _panelGroup.interactable = false;
                _panelGroup.blocksRaycasts = false;

                await LMotion.Create(0f, _slideOffsetX, _slideDuration)
                    .WithEase(_slideOutEase)
                    .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                    .ToUniTask(ct);
            }

            SetPanelHidden();
            SetExitPanel(false);
            SetInteractCamera(false);
            SetAreaCamera(false);
            _interactable?.SetInteractable(false);
        }

        private void SetExitPanel(bool active)
        {
            if (_exitPanelGroup == null) return;
            _exitPanelGroup.alpha          = active ? 1f : 0f;
            _exitPanelGroup.interactable   = active;
            _exitPanelGroup.blocksRaycasts = active; // 3D Physics 레이캐스트를 막지 않음
        }

        private void SetAreaCamera(bool active)
        {
            if (_areaCamera != null) _areaCamera.Priority = active ? 20 : -5;
        }

        private void SetInteractCamera(bool active)
        {
            if (_interactCamera != null) _interactCamera.Priority = active ? 25 : -5;
        }

        private void SetPanelHidden()
        {
            _panelGroup.alpha = 0f;
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;
            if (_panelRect != null)
                _panelRect.anchoredPosition = new Vector2(_slideOffsetX, _panelRect.anchoredPosition.y);
        }

        // ── 서비스 버튼 (리롤 / 카드 버리기) ────────────────────────────────

        private int GetCurrentRerollCost() =>
            _shopConfig == null ? 0
            : Mathf.RoundToInt(_shopConfig.rerollCost * Mathf.Pow(_shopConfig.rerollCostMultiplier, _rerollCount));

        private int GetCurrentRemoveCost() =>
            _shopConfig == null ? 0
            : Mathf.RoundToInt(_shopConfig.removeCardCost * Mathf.Pow(_shopConfig.removeCardCostMultiplier, _removeCount));

        private void RefreshServiceButtons()
        {
            if (_playerRunState == null || _shopConfig == null) return;

            int rerollCost = GetCurrentRerollCost();
            bool canReroll = _playerRunState.CanAfford(rerollCost);
            if (_rerollButton != null)   _rerollButton.interactable = canReroll;
            if (_rerollCostText != null) _rerollCostText.text = rerollCost.ToString();

            int removeCost = GetCurrentRemoveCost();
            int totalCards = _deckController != null ? _deckController.CurrentDeckCards.Count : _playerRunState.CurrentPile.Count;
            bool canRemove = totalCards > 0 && _playerRunState.CanAfford(removeCost);
            if (_removeCardButton != null)   _removeCardButton.interactable = canRemove;
            if (_removeCardCostText != null) _removeCardCostText.text = removeCost.ToString();
        }

        private void OnRerollClicked()
        {
            int cost = GetCurrentRerollCost();
            if (!_playerRunState.CanAfford(cost))
            {
                _battleEventChannel?.RaiseEvent(new GoldInsufficientEvent());
                return;
            }

            int oldGold = _playerRunState.SpendGold(cost);
            _battleEventChannel?.RaiseEvent(new GoldChangedEvent(oldGold, _playerRunState.Gold));
            _rerollCount++;

            // 미구매 슬롯만 새 카드로 교체
            var newCards = _cardPoolService.DrawCards(_shopConfig.cardSlotCount,
                _playerRunState != null ? _playerRunState.CurrentFloorIndex : 0);

            int newIdx = 0;
            for (int i = 0; i < _cardSlots.Count; i++)
            {
                var slot = _cardSlots[i];
                if (!slot.gameObject.activeSelf) continue;
                if (slot.IsPurchased) continue;

                if (newIdx < newCards.Count)
                {
                    if (i < _slotBuyHandlers.Count && _slotBuyHandlers[i] != null)
                        slot.OnBuyClicked -= _slotBuyHandlers[i];

                    var card  = newCards[newIdx++];
                    var price = _shopConfig.GetCardPrice(card.grade);
                    slot.Setup(card, price);
                    slot.SetPriceInsufficient(!_playerRunState.CanAfford(price));

                    var captured = slot;
                    Action handler = () => OnSlotBuyClicked(captured);
                    if (i < _slotBuyHandlers.Count) _slotBuyHandlers[i] = handler;
                    slot.OnBuyClicked += handler;
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            RefreshServiceButtons();
        }

        private void OnRemoveCardClicked()
        {
            if (_cardRemovePopup == null) return;
            int cost = GetCurrentRemoveCost();
            if (!_playerRunState.CanAfford(cost))
            {
                _battleEventChannel?.RaiseEvent(new GoldInsufficientEvent());
                return;
            }
            _cardRemovePopup.Show(cost);
        }

        private void OnCardRemovedFromPopup(CardDataSO _)
        {
            _removeCount++;
            RefreshServiceButtons();
        }
    }
}
