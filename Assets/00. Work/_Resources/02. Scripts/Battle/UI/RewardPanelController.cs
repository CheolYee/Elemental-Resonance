using System.Collections.Generic;
using Battle.Data;
using Battle.Enums;
using Battle.Events;
using Battle.Services;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class RewardPanelController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO        _battleEventChannel;
        [SerializeField] private PlayerRunStateSO      _playerRunState;
        [SerializeField] private RewardConfigSO        _rewardConfig;
        [SerializeField] private RewardCardPoolService _cardPoolService;

        [SerializeField] private CardFlyAnimator  _cardFlyAnimator;
        [SerializeField] private DeckController   _deckController;

        [SerializeField] private CanvasGroup     _backgroundCanvasGroup;
        [SerializeField] private CanvasGroup     _panelCanvasGroup;
        [SerializeField] private Transform       _contentRoot;
        [SerializeField] private Button          _closeButton;
        [SerializeField] private CardRewardPanel _cardRewardPanel;

        [SerializeField] private GameObject _rewardButtonPrefab;
        [SerializeField] private Sprite     _goldIcon;
        [SerializeField] private Sprite     _cardIcon;

        [Header("Reward Order")]
        [SerializeField] private List<RewardType> _rewardOrder = new() { RewardType.Gold, RewardType.Card };

        [SerializeField] private float _showDuration = 0.35f;
        [SerializeField] private Ease  _showEase     = Ease.OutBack;

        private readonly List<GameObject> _spawnedButtons = new();
        private int _pendingClaimsCount;

        private void Awake()
        {
            if (_backgroundCanvasGroup != null)
            {
                _backgroundCanvasGroup.alpha          = 0f;
                _backgroundCanvasGroup.blocksRaycasts = false;
            }
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha          = 0f;
                _panelCanvasGroup.blocksRaycasts = false;
                _panelCanvasGroup.interactable   = false;
            }
        }

        private void OnEnable()  => _battleEventChannel.AddListener<BattleResultShownEvent>(OnBattleResultShown);
        private void OnDisable() => _battleEventChannel.RemoveListener<BattleResultShownEvent>(OnBattleResultShown);

        private void OnBattleResultShown(BattleResultShownEvent _) => ShowAsync().Forget();

        private async UniTaskVoid ShowAsync()
        {
            var ct = destroyCancellationToken;

            foreach (var go in _spawnedButtons) Destroy(go);
            _spawnedButtons.Clear();

            int  gold  = _rewardConfig.CalculateGold(_playerRunState.CurrentFloorIndex);
            var  cards = _cardPoolService.DrawCards(3, _playerRunState.CurrentFloorIndex);

            _pendingClaimsCount = _rewardOrder.Count;

            if (_closeButton != null)
            {
                _closeButton.interactable = false;
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnClose);
            }

            foreach (var type in _rewardOrder)
            {
                switch (type)
                {
                    case RewardType.Gold: SpawnGoldButton(gold);  break;
                    case RewardType.Card: SpawnCardButton(cards); break;
                }
            }

            if (_backgroundCanvasGroup != null)
            {
                _backgroundCanvasGroup.alpha          = 0f;
                _backgroundCanvasGroup.blocksRaycasts = true;
            }
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha          = 0f;
                _panelCanvasGroup.blocksRaycasts = true;
                _panelCanvasGroup.interactable   = true;
            }
            transform.localScale = Vector3.one * 0.85f;

            await UniTask.WhenAll(
                _backgroundCanvasGroup != null
                    ? LMotion.Create(0f, 1f, _showDuration)
                        .Bind(a => _backgroundCanvasGroup.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask,
                _panelCanvasGroup != null
                    ? LMotion.Create(0f, 1f, _showDuration).WithEase(_showEase)
                        .Bind(a => _panelCanvasGroup.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask,
                LMotion.Create(0.85f, 1f, _showDuration).WithEase(_showEase)
                    .Bind(s => transform.localScale = new Vector3(s, s, 1f)).ToUniTask(ct));
        }

        private void SpawnGoldButton(int gold)
        {
            if (_rewardButtonPrefab == null) return;
            var go  = Instantiate(_rewardButtonPrefab, _contentRoot);
            var btn = go.GetComponent<RewardItemButtonView>();
            if (btn == null) return;
            btn.Setup($"골드  +{gold} G", _goldIcon);
            btn.OnClaimed = () =>
            {
                int old = _playerRunState.AddGold(gold);
                _battleEventChannel.RaiseEvent(new GoldChangedEvent(old, _playerRunState.Gold));
                OnRewardClaimed();
            };
            _spawnedButtons.Add(go);
        }

        private void SpawnCardButton(List<CardDataSO> cards)
        {
            if (_rewardButtonPrefab == null) return;
            var go  = Instantiate(_rewardButtonPrefab, _contentRoot);
            var btn = go.GetComponent<RewardItemButtonView>();
            if (btn == null) return;
            btn.Setup("카드 선택", _cardIcon);
            btn.OnClaimed = () =>
            {
                if (_cardRewardPanel != null)
                {
                    _cardRewardPanel.OnCardSelected = (card, screenPos) =>
                    {
                        _playerRunState.AddCard(card);
                        _deckController?.AddRewardCard(card);
                        _cardFlyAnimator?.FlyToCurrentDeck(screenPos);
                        OnRewardClaimed();
                    };
                    _cardRewardPanel.Show(cards);
                }
                else
                {
                    OnRewardClaimed();
                }
            };
            _spawnedButtons.Add(go);
        }

        private void OnRewardClaimed()
        {
            _pendingClaimsCount--;
            if (_pendingClaimsCount <= 0 && _closeButton != null)
                _closeButton.interactable = true;
        }

        private void OnClose() => CloseAsync().Forget();

        private async UniTaskVoid CloseAsync()
        {
            var ct = destroyCancellationToken;

            if (_backgroundCanvasGroup != null)
                _backgroundCanvasGroup.blocksRaycasts = false;
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.blocksRaycasts = false;
                _panelCanvasGroup.interactable   = false;
            }

            await UniTask.WhenAll(
                _backgroundCanvasGroup != null
                    ? LMotion.Create(_backgroundCanvasGroup.alpha, 0f, _showDuration)
                        .Bind(a => _backgroundCanvasGroup.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask,
                _panelCanvasGroup != null
                    ? LMotion.Create(_panelCanvasGroup.alpha, 0f, _showDuration)
                        .Bind(a => _panelCanvasGroup.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask);

            _battleEventChannel.RaiseEvent(new RewardPanelClosedEvent());
        }
    }
}
