using System;
using System.Collections.Generic;
using Battle.Enums;
using Battle.Events;
using Battle.Fusion;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using Reflex.Attributes;
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
        [SerializeField] private FusionProbabilityTooltip fusionProbabilityTooltip;
        [SerializeField] private RectTransform drawPileRect;

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

        [Inject] private FusionRecipeService _fusionRecipeService;

        public float SlideDuration => slideDuration;
        public float TweenDuration => tweenDuration;
        public IReadOnlyList<CardView> HandCards => _handCards;

        private Pool _cardPool;
        private readonly List<CardView> _handCards = new();
        private CardView _draggedCard;
        private int _draggedCardIndex;
        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;
        private CardView _fusionHoverTarget;

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
            battleEventChannel.AddListener<CardFusionRequestedEvent>(OnFusionRequested);
            battleEventChannel.AddListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.AddListener<BattleUIShownEvent>(OnBattleUIShown);
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
            battleEventChannel.RemoveListener<CardFusionRequestedEvent>(OnFusionRequested);
            battleEventChannel.RemoveListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.RemoveListener<BattleUIShownEvent>(OnBattleUIShown);
        }

        private void OnBattleUIHidden(BattleUIHiddenEvent _) => SlideOut();
        private void OnBattleUIShown(BattleUIShownEvent _) { if (!_battleEnded) SlideIn(); }

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
            UpdateFusionHighlights(evt.CardInstance);
        }

        private void UpdateFusionHighlights(CardInstance draggedCard)
        {
            if (_fusionRecipeService == null) return;
            var draggedElement = draggedCard.data.elementType;
            foreach (var card in _handCards)
            {
                bool eligible = _fusionRecipeService.CanFuse(draggedElement, card.CardInstance.data.elementType);
                var glowColor = eligible ? CardColorUtility.GetElementColor(card.CardInstance.data.elementType) : Color.clear;
                card.SetFusionState(eligible, glowColor);
            }
        }

        public void SetFusionHoverTarget(CardView view)
        {
            if (_fusionHoverTarget == view) return;

            _fusionHoverTarget?.SetFusionHoverState(false);
            _fusionHoverTarget = null;
            fusionProbabilityTooltip?.Hide();

            if (view == null || _fusionRecipeService == null || _draggedCard == null) return;

            if (!_fusionRecipeService.TryGetResult(
                    _draggedCard.CardInstance.data.elementType,
                    view.CardInstance.data.elementType,
                    out var resultElement))
                return;

            _fusionHoverTarget = view;
            _fusionHoverTarget.SetFusionHoverState(true);

            var maxGrade = (CardGrade)Mathf.Max(
                (int)_draggedCard.CardInstance.data.grade,
                (int)view.CardInstance.data.grade);
            fusionProbabilityTooltip?.Show(resultElement, maxGrade, (RectTransform)view.transform);
        }

        private void ClearFusionHighlights()
        {
            _fusionHoverTarget?.SetFusionHoverState(false);
            _fusionHoverTarget = null;
            fusionProbabilityTooltip?.Hide();
            foreach (var card in _handCards)
                card.ClearFusionState();
            _draggedCard?.ClearFusionState();
        }

        private void OnFusionRequested(CardFusionRequestedEvent _) => ClearFusionHighlights();

        private void OnCardReturnToHand(CardReturnToHandEvent evt)
        {
            if (_draggedCard == null) return;
            ClearFusionHighlights();
            int insertIndex = Mathf.Min(_draggedCardIndex, _handCards.Count);
            _handCards.Insert(insertIndex, _draggedCard);
            _draggedCard = null;
            RefreshLayoutWithReturn(insertIndex);
        }

        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            ClearFusionHighlights();
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

        public void SetAllCardsInteractable(bool interactable)
        {
            foreach (var view in _handCards)
                view.SetInteractable(interactable);
        }

        // 드래그 중인 카드를 강제로 손패로 복귀 — 턴 종료 버림 전 호출
        public void ForceReturnDraggedCard()
        {
            if (_draggedCard == null) return;
            ClearFusionHighlights();
            int insertIndex = Mathf.Min(_draggedCardIndex, _handCards.Count);
            _handCards.Insert(insertIndex, _draggedCard);
            _draggedCard = null;
        }

        // 딜 시작 위치 — drawPileRect가 연결되어 있으면 그 로컬 좌표, 없으면 기본값
        private Vector2 GetDealStartPos()
        {
            if (drawPileRect == null) return new Vector2(0f, -300f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)cardContainer,
                drawPileRect.position,
                null,
                out Vector2 localPos);
            return localPos;
        }

        // 합성 재료 뷰를 손패에서 분리해 참조 반환 (풀 반환은 하지 않음). 성공 시 true.
        public bool TryDetachFusionMaterials(
            CardInstance draggedCard, CardInstance targetCard,
            out CardView draggedView, out CardView targetView,
            out Vector2 draggedPos,  out Vector2 targetPos)
        {
            draggedView = null;
            targetView  = null;
            draggedPos  = Vector2.zero;
            targetPos   = Vector2.zero;

            if (_draggedCard == null || _draggedCard.CardInstance != draggedCard) return false;
            var tv = _handCards.Find(v => v.CardInstance == targetCard);
            if (tv == null) return false;

            draggedPos  = _draggedCard.transform.position;
            targetPos   = tv.transform.position;

            draggedView  = _draggedCard;
            _draggedCard = null;
            draggedView.transform.SetParent(poolRoot, true);

            _handCards.Remove(tv);
            targetView = tv;
            tv.transform.SetParent(poolRoot, true);

            RefreshLayout();
            return true;
        }

        public void ReturnViewToPool(CardView view)
        {
            view.transform.SetParent(poolRoot, false);
            view.transform.localScale = Vector3.one;
            _cardPool.Push(view);
        }

        // 손패에서 CardView를 분리해 반환. 호출자가 위치/부모를 직접 제어함.
        // skipRefresh=true 사용 시 레이아웃 갱신 생략 (배치 분리 후 마지막에 직접 호출 필요).
        public CardView DetachCard(CardInstance instance, Transform newParent = null, bool skipRefresh = false)
        {
            var view = _handCards.Find(v => v.CardInstance == instance);
            if (view == null) return null;
            _handCards.Remove(view);
            view.CancelLayoutTween(); // reparent 전 진행 중인 트윈 취소
            view.ForceExitHover(tweenBack: false);
            view.transform.SetParent(newParent != null ? newParent : poolRoot, true);
            if (!skipRefresh) RefreshLayout();
            return view;
        }

        // DetachCard로 분리된 CardView를 손패 끝에 재삽입.
        public void ReattachCard(CardView view)
        {
            view.transform.SetParent(cardContainer, true);
            _handCards.Add(view);
            RefreshLayout();
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
            dragHandler?.SetHandLayoutController(this);

            view.Setup(instance);
            _handCards.Add(view);

            // 진입 전 목표 회전값을 미리 스냅 → 위치 트윈만 동작
            int count = _handCards.Count;
            float t = (count - 1) - (count - 1) / 2f;
            ((RectTransform)view.transform).anchoredPosition = GetDealStartPos();
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

            float effectiveHeightOffset = heightOffset * (effectiveSpacing / cardSpacing);

            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var targetPos = new Vector2(t * effectiveSpacing, -t * t * effectiveHeightOffset);
                float targetRotZ = -t * rotationPerCard;
                _handCards[i].TweenToLayout(targetPos, targetRotZ, tweenDuration, tweenEase);
                _handCards[i].SetLayoutScale(effectiveScale, tweenDuration, tweenEase);
            }
        }
    }
}
