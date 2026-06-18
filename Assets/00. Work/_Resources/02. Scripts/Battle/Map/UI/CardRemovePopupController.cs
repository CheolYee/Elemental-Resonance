using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class CardRemovePopupController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup        _popupGroup;
        [SerializeField] private Button             _closeButton;
        [SerializeField] private Transform          _cardListRoot;
        [SerializeField] private RewardCardView     _cardViewPrefab;
        [SerializeField] private TextMeshProUGUI    _costText;

        [SerializeField] private PlayerRunStateSO   _playerRunState;
        [SerializeField] private EventChannelSO     _battleEventChannel;
        [SerializeField] private DeckController     _deckController;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.2f;

        [Header("Colors")]
        [SerializeField] private Color _normalCostColor      = Color.white;
        [SerializeField] private Color _insufficientColor    = new Color(1f, 0.3f, 0.3f);

        public event Action<CardDataSO> OnCardRemoved;

        private readonly List<RewardCardView> _cardViews = new();
        private CancellationTokenSource       _animCts;
        private int                           _currentCost;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(() => HideAsync().Forget());
            SetHidden();
        }

        private void OnDestroy()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
        }

        public void Show(int cost)
        {
            _currentCost = cost;
            ShowAsync().Forget();
        }

        public void UpdateCost(int cost)
        {
            _currentCost = cost;
            RefreshCostText();
        }

        private void RefreshCostText()
        {
            if (_costText == null) return;
            _costText.text  = _currentCost.ToString();
            _costText.color = _playerRunState.CanAfford(_currentCost)
                ? _normalCostColor
                : _insufficientColor;
        }

        private void BuildCardList()
        {
            foreach (var v in _cardViews)
                if (v != null) Destroy(v.gameObject);
            _cardViews.Clear();

            // 시작 덱 + 런 중 획득 카드 전체 표시 (pilePanel과 동일한 범위)
            var allCards = _deckController != null
                ? _deckController.CurrentDeckCards
                : (IReadOnlyList<CardDataSO>)_playerRunState.CurrentPile;

            foreach (var card in allCards)
            {
                var view = Instantiate(_cardViewPrefab, _cardListRoot);
                view.Setup(card);

                var captured = card;
                view.OnClicked = () => OnCardClicked(captured, view).Forget();
                _cardViews.Add(view);
            }
        }

        private async UniTaskVoid OnCardClicked(CardDataSO card, RewardCardView view)
        {
            if (!_playerRunState.CanAfford(_currentCost))
            {
                _battleEventChannel?.RaiseEvent(new GoldInsufficientEvent());
                return;
            }

            // 선택 연출
            await view.PlaySelectAsync(destroyCancellationToken);

            int oldGold = _playerRunState.SpendGold(_currentCost);
            _deckController?.RemoveCardFromDeck(card);  // CurrentPile → deckProvider 순으로 탐색 후 제거 + UI 갱신
            _battleEventChannel?.RaiseEvent(new GoldChangedEvent(oldGold, _playerRunState.Gold));

            OnCardRemoved?.Invoke(card);
            await HideAsync();
        }

        private async UniTask ShowAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            _popupGroup.alpha          = 0f;
            _popupGroup.interactable   = false;
            _popupGroup.blocksRaycasts = true;

            // SetActive 이후 BuildCardList 호출해야 LayoutGroup이 정상 계산됨
            gameObject.SetActive(true);
            RefreshCostText();
            BuildCardList();

            await LMotion.Create(0f, 1f, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .Bind(a => _popupGroup.alpha = a)
                .ToUniTask(ct);

            _popupGroup.interactable = true;
        }

        private async UniTask HideAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            _popupGroup.interactable   = false;
            _popupGroup.blocksRaycasts = false;

            await LMotion.Create(1f, 0f, _fadeDuration)
                .WithEase(Ease.InCubic)
                .Bind(a => _popupGroup.alpha = a)
                .ToUniTask(ct);

            SetHidden();
        }

        private void SetHidden()
        {
            _popupGroup.alpha          = 0f;
            _popupGroup.interactable   = false;
            _popupGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
