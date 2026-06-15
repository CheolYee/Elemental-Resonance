using System;
using System.Collections.Generic;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class HandLayoutController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private PoolItemSo cardPoolItem;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private List<RectTransform> blockedDropAreas;

        [Header("Slide")]
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(0f, -400f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        [Header("Layout")]
        [SerializeField] private float rotationPerCard = 5f;
        [SerializeField] private float cardSpacing = 150f;
        [SerializeField] private float heightOffset = 20f;
        [SerializeField] private float handAreaWidth = 1200f;
        [SerializeField] private float minCardSpacing = 60f;
        [SerializeField] private float minCardScale = 0.6f;

        [Header("Tween")]
        [SerializeField] private float tweenDuration = 0.3f;
        [SerializeField] private Ease tweenEase = Ease.OutBack;
        [SerializeField] private float entryRotationOffset = 15f;

        [Header("Return Tween")]
        [SerializeField] private float returnDuration = 0.2f;
        [SerializeField] private Ease returnEase = Ease.OutCubic;

        [Header("Card Shrink")]
        [SerializeField] private float cardShrinkDuration = 0.2f;
        [SerializeField] private Ease cardShrinkEase = Ease.InBack;

        public float SlideDuration => slideDuration;
        public float TweenDuration => tweenDuration;
        public IReadOnlyList<CardView> HandCards => _handCards;

        private Pool _cardPool;
        private readonly List<CardView> _handCards = new();
        private CardView _draggedCard;
        private int _draggedCardIndex;
        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;

        private void Awake()
        {
            _cardPool = new Pool(cardPoolItem, poolRoot, cardPoolItem.initCount);
            if (slideTarget != null)
            {
                _slideInPos = slideTarget.anchoredPosition;
                slideTarget.anchoredPosition = _slideInPos + slideOutOffset;
            }
        }

        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<CardDragStartEvent>(OnCardDragStart);
            battleEventChannel.AddListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.AddListener<CardReturnToHandEvent>(OnCardReturnToHand);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<PileDetailPanelOpenedEvent>(OnPileDetailPanelOpened);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPileDetailPanelClosed);
            battleEventChannel.AddListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<CardDragStartEvent>(OnCardDragStart);
            battleEventChannel.RemoveListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.RemoveListener<CardReturnToHandEvent>(OnCardReturnToHand);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<PileDetailPanelOpenedEvent>(OnPileDetailPanelOpened);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPileDetailPanelClosed);
            battleEventChannel.RemoveListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; }
        private void OnCardDrawStart(CardDrawStartEvent _) { SetAllCardsInteractable(false); SlideIn(); }
        private void OnCardDrawEnd(CardDrawEndEvent _) { if (!_battleEnded) { SetAllCardsInteractable(true); SlideIn(); } }
        private void OnBattleEnded(BattleVictoryEvent _) => LockBattle();
        private void OnBattleEnded(BattleDefeatEvent _) => LockBattle();
        private void OnPileDetailPanelOpened(PileDetailPanelOpenedEvent _) => SetAllCardsInteractable(false);
        private void OnPileDetailPanelClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded) SetAllCardsInteractable(true); }

        private void LockBattle()
        {
            _battleEnded = true;
            SetAllCardsInteractable(false);
            SlideOut();
        }

        public void SlideOut()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos + slideOutOffset, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        public void SlideIn()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        public void ReturnAllToPool()
        {
            for (int i = _handCards.Count - 1; i >= 0; i--)
            {
                var view = _handCards[i];
                view.transform.SetParent(poolRoot, false);
                _cardPool.Push(view);
            }
            _handCards.Clear();
        }

        private void OnCardDragStart(CardDragStartEvent evt)
        {
            var view = _handCards.Find(v => v.CardInstance == evt.CardInstance);
            if (view == null) return;
            _draggedCardIndex = _handCards.IndexOf(view);
            _draggedCard = view;
            _handCards.Remove(view);
            RefreshLayout();
        }

        private void OnCardReturnToHand(CardReturnToHandEvent evt)
        {
            if (_draggedCard == null) return;
            int insertIndex = Mathf.Min(_draggedCardIndex, _handCards.Count);
            _handCards.Insert(insertIndex, _draggedCard);
            _draggedCard = null;
            RefreshLayoutWithReturn(insertIndex);
        }

        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            if (_draggedCard != null && _draggedCard.CardInstance == evt.CardInstance)
            {
                ShrinkAndReturnAsync(_draggedCard).Forget();
                _draggedCard = null;
                RefreshLayout();
                return;
            }
            RemoveCard(evt.CardInstance);
        }

        private async UniTaskVoid ShrinkAndReturnAsync(CardView view)
        {
            // slideTarget 계층에서 분리 — worldPositionStays=true로 화면 위치 유지
            // slideTarget이 슬라이드 아웃될 때 카드가 같이 내려가는 현상 방지
            view.transform.SetParent(poolRoot, true);
            await LMotion.Create(Vector3.one, Vector3.zero, cardShrinkDuration)
                .WithEase(cardShrinkEase)
                .Bind(s => { if (view != null) view.transform.localScale = s; })
                .ToUniTask(cancellationToken: destroyCancellationToken);
            if (view == null) return;
            view.transform.localScale = Vector3.one;
            _cardPool.Push(view);
        }

        private void OnEnemyTurnStart(EnemyTurnStartEvent _) { SetAllCardsInteractable(false); SlideOut(); }
        private void OnWaveClear(WaveClearEvent _) { SetAllCardsInteractable(false); SlideOut(); }

        private void SetAllCardsInteractable(bool interactable)
        {
            foreach (var view in _handCards)
                view.SetInteractable(interactable);
        }



        private void RefreshLayoutWithReturn(int returnIndex)
        {
            int count = _handCards.Count;
            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var targetPos = new Vector2(t * cardSpacing, -t * t * heightOffset);
                float targetRotZ = -t * rotationPerCard;
                bool isReturning = i == returnIndex;
                _handCards[i].TweenToLayout(
                    targetPos, targetRotZ,
                    isReturning ? returnDuration : tweenDuration,
                    isReturning ? returnEase : tweenEase);
            }
        }

        public CardView AddCard(CardInstance instance)
        {
            var view = (CardView)_cardPool.Pop();
            view.transform.SetParent(cardContainer, false);
            view.transform.localScale = Vector3.one;

            var dragHandler = view.GetComponent<CardDragHandler>();
            dragHandler?.SetHandAreaRect((RectTransform)transform);
            dragHandler?.SetBlockedAreas(blockedDropAreas);

            view.Setup(instance);
            _handCards.Add(view);

            // 진입 전 목표 회전값을 미리 스냅 → 위치 트윈만 동작
            int count = _handCards.Count;
            float t = (count - 1) - (count - 1) / 2f;
            ((RectTransform)view.transform).anchoredPosition = new Vector2(0f, -300f);
            view.SnapRotation(-t * rotationPerCard + entryRotationOffset);

            RefreshLayout();
            return view;
        }

        public void RemoveCard(CardInstance instance, Action onComplete = null)
        {
            var view = _handCards.Find(v => v.CardInstance == instance);
            if (view == null)
            {
                onComplete?.Invoke();
                return;
            }

            _handCards.Remove(view);
            view.transform.SetParent(poolRoot, false);
            _cardPool.Push(view);
            RefreshLayout();
            onComplete?.Invoke();
        }

        private void RefreshLayout()
        {
            int count = _handCards.Count;
            if (count == 0) return;

            float effectiveSpacing = cardSpacing;
            float effectiveScale = 1f;

            if (count > 1)
            {
                float totalWidth = (count - 1) * cardSpacing;
                if (totalWidth > handAreaWidth)
                {
                    effectiveSpacing = handAreaWidth / (count - 1);
                    if (effectiveSpacing < minCardSpacing)
                    {
                        effectiveSpacing = minCardSpacing;
                        float reducedWidth = (count - 1) * minCardSpacing;
                        if (reducedWidth > handAreaWidth)
                            effectiveScale = Mathf.Max(minCardScale, handAreaWidth / reducedWidth);
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var targetPos = new Vector2(t * effectiveSpacing, -t * t * heightOffset);
                float targetRotZ = -t * rotationPerCard;
                _handCards[i].TweenToLayout(targetPos, targetRotZ, tweenDuration, tweenEase);
                _handCards[i].SetLayoutScale(effectiveScale, tweenDuration, tweenEase);
            }
        }
    }
}
