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

        [Header("Slide")]
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(0f, -400f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        [Header("Deck")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private int initialDrawCount = 5;

        [Header("Layout")]
        [SerializeField] private float rotationPerCard = 5f;
        [SerializeField] private float cardSpacing = 150f;
        [SerializeField] private float heightOffset = 20f;

        [Header("Tween")]
        [SerializeField] private float tweenDuration = 0.3f;
        [SerializeField] private Ease tweenEase = Ease.OutBack;
        [SerializeField] private float dealStaggerDelay = 0.12f;
        [SerializeField] private float entryRotationOffset = 15f;

        [Header("Return Tween")]
        [SerializeField] private float returnDuration = 0.2f;
        [SerializeField] private Ease returnEase = Ease.OutCubic;

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
            battleEventChannel.AddListener<CardDragStartEvent>(OnCardDragStart);
            battleEventChannel.AddListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.AddListener<CardReturnToHandEvent>(OnCardReturnToHand);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<BattleReadyEvent>(OnBattleReady);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardDragStartEvent>(OnCardDragStart);
            battleEventChannel.RemoveListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.RemoveListener<CardReturnToHandEvent>(OnCardReturnToHand);
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<BattleReadyEvent>(OnBattleReady);
        }

        private void OnCardDrawStart(CardDrawStartEvent _) { SetAllCardsInteractable(false); SlideOut(); }
        private void OnCardDrawEnd(CardDrawEndEvent _) { if (!_battleEnded) { SetAllCardsInteractable(true); SlideIn(); } }
        private void OnBattleEnded(BattleVictoryEvent _) => LockBattle();
        private void OnBattleEnded(BattleDefeatEvent _) => LockBattle();

        private void LockBattle()
        {
            _battleEnded = true;
            SetAllCardsInteractable(false);
            SlideOut();
        }

        private void SlideOut()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos + slideOutOffset, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        private void SlideIn()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        private void OnBattleReady(BattleReadyEvent _) => DealStartingCardsAsync().Forget();

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
                _draggedCard.transform.SetParent(poolRoot, false);
                _cardPool.Push(_draggedCard);
                _draggedCard = null;
                RefreshLayout();
                return;
            }
            RemoveCard(evt.CardInstance);
        }

        private void OnSkillExecutionStart(SkillExecutionStartEvent _) { SetAllCardsInteractable(false); SlideOut(); }
        private void OnSkillExecutionEnd(SkillExecutionEndEvent _) { if (!_battleEnded) { SetAllCardsInteractable(true); SlideIn(); } }

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

        private async UniTaskVoid DealStartingCardsAsync()
        {
            battleEventChannel.RaiseEvent(new CardDrawStartEvent());

            var dealtViews = new List<CardView>();
            for (int i = 0; i < initialDrawCount; i++)
            {
                var cardInstance = deckController.DrawCard();
                if (cardInstance == null) break;

                var view = AddCard(cardInstance);
                view.SetInteractable(false);
                dealtViews.Add(view);
                await UniTask.Delay(TimeSpan.FromSeconds(dealStaggerDelay), cancellationToken: destroyCancellationToken);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(tweenDuration), cancellationToken: destroyCancellationToken);

            foreach (var view in dealtViews)
                view.SetInteractable(true);

            battleEventChannel.RaiseEvent(new CardDrawEndEvent());
        }

        public CardView AddCard(CardInstance instance)
        {
            var view = (CardView)_cardPool.Pop();
            view.transform.SetParent(cardContainer, false);
            view.transform.localScale = Vector3.one;

            var dragHandler = view.GetComponent<CardDragHandler>();
            dragHandler?.SetHandAreaRect((RectTransform)transform);

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

public void RemoveCard(CardInstance instance, System.Action onComplete = null)
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
            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var targetPos = new Vector2(t * cardSpacing, -t * t * heightOffset);
                float targetRotZ = -t * rotationPerCard;
                _handCards[i].TweenToLayout(targetPos, targetRotZ, tweenDuration, tweenEase);
            }
        }
    }
}
