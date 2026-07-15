using System.Threading;
using Battle.Data;
using Battle.Effects;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using Gamelib.SoundSystem;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DelayType = Cysharp.Threading.Tasks.DelayType;

namespace Battle.UI
{
    public class CardView : PoolableMono, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image labelImage;
        [SerializeField] private Image costImage;
        [SerializeField] private ElementIconTableSO elementIconTable;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMPEffect nameTextEffect;
        [SerializeField] private TMPEffect typeTextEffect;


        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds      hoverSound;

        [Header("Hover")]
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverDuration = 0.2f;
        [SerializeField] private Ease hoverEase = Ease.OutCubic;
        [SerializeField] private int hoverSortingOrder = 5;

        [Header("Usability")]
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private Image usabilityOverlay;
        [SerializeField] private float overlayTargetAlpha = 0.5f;
        [SerializeField] private float overlayFadeDuration = 0.2f;
        [SerializeField] private float shakeStepDuration = 0.04f;
        [SerializeField] private float costPopScale = 1.35f;
        [SerializeField] private float costPopDuration = 0.1f;

        [Header("Fusion")]
        [SerializeField] private Image fusionGlowImage;
        [SerializeField] private Image fusionDimOverlay;
        [SerializeField] private Color fusionDimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private float fusionFadeDuration = 0.15f;
        [SerializeField] private float fusionPulseMin = 0.65f;
        [SerializeField] private float fusionPulseMax = 1.0f;
        [SerializeField] private float fusionPulseDuration = 0.4f;
        [SerializeField] private float fusionFloatHeight   = 6f;
        [SerializeField] private float fusionFloatDuration = 0.5f;

        [Header("Fusion Hover")]
        [SerializeField] private float fusionHoverScale       = 1.2f;
        [SerializeField] private float fusionHoverDuration    = 0.15f;
        [SerializeField] private int   fusionHoverSortingOrder = 3;

        public CardInstance CardInstance { get; private set; }

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private MotionHandle _posHandle;
        private MotionHandle _rotHandle;
        private MotionHandle _scaleHandle;
        private MotionHandle _overlayHandle;
        private MotionHandle _costFlashHandle;
        private MotionHandle _costPopHandle;
        private MotionHandle _fusionGlowHandle;
        private MotionHandle _fusionDimHandle;
        private MotionHandle _fusionFloatHandle;
        private MotionHandle _fusionHoverScaleHandle;
        private MotionHandle _fusionHoverRotHandle;
        private bool    _isFusionHovered;
        private Vector2 _fusionBasePos;
        private CancellationTokenSource _fusionFloatCts;

        private float _currentRotZ;
        private Vector2 _layoutPos;
        private float _layoutRotZ;
        private float _layoutScale = 1f;
        private bool _isHovered;
        private CancellationTokenSource _exitDebounce;
        private CardDragHandler _dragHandler;
        private bool _canAfford = true;
        private bool _conditionMet = true;
        private bool _isInteractable = true;
        private Color _originalCostColor;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponent<Canvas>();
            _originalCostColor = costText.color;
            _dragHandler = GetComponent<CardDragHandler>();
            InitializeUsabilityOverlay();
        }

        private void OnEnable() => battleEventChannel.AddListener<CostChangedEvent>(OnCostChanged);
        private void OnDisable() => battleEventChannel.RemoveListener<CostChangedEvent>(OnCostChanged);

        public void Setup(CardInstance instance)
        {
            CardInstance = instance;
            nameText.text = instance.data.cardName;
            descText.text = instance.data.description;
            costText.text = instance.data.cost.ToString();
            artworkImage.sprite = instance.data.artwork;

            if (typeText != null) typeText.text = CardColorUtility.GetTypeName(instance.data.cardType);
            CardColorUtility.Apply(frameImage, labelImage, instance.data);
            CardColorUtility.ApplyTextEffects(nameTextEffect, typeTextEffect, instance.data.grade);
            CardColorUtility.ApplyCostImage(costImage, elementIconTable, instance.data.elementType);

            RefreshUsability(costModel.currentCost);
        }

        private void OnCostChanged(CostChangedEvent evt) => RefreshUsability(evt.CurrentCost);

        private void RefreshUsability(int currentCost)
        {
            if (CardInstance == null) return;
            _canAfford = CardInstance.data.CanAfford(currentCost);
            _conditionMet = CardInstance.data.IsConditionMet(currentCost);
            bool canUse = _canAfford && _conditionMet;
            TweenOverlayAlpha(canUse ? 0f : overlayTargetAlpha);
            costText.color = canUse ? _originalCostColor : Color.red;
        }

        private void TweenOverlayAlpha(float targetAlpha)
        {
            if (usabilityOverlay == null) return;
            if (!usabilityOverlay.gameObject.activeSelf)
                usabilityOverlay.gameObject.SetActive(true);
            if (_overlayHandle.IsActive()) _overlayHandle.Cancel();
            var c = usabilityOverlay.color;
            _overlayHandle = LMotion.Create(c.a, targetAlpha, overlayFadeDuration)
                .Bind(a => usabilityOverlay.color = new Color(c.r, c.g, c.b, a));
        }

        private void InitializeUsabilityOverlay()
        {
            if (usabilityOverlay == null) return;

            usabilityOverlay.gameObject.SetActive(true);
            usabilityOverlay.raycastTarget = false;

            var c = usabilityOverlay.color;
            usabilityOverlay.color = new Color(c.r, c.g, c.b, 0f);
        }

        public void SnapRotation(float rotZ)
        {
            _currentRotZ = rotZ;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        public void TweenToLayout(Vector2 targetPos, float targetRotZ, float duration, Ease ease)
        {
            _layoutPos = targetPos;
            _layoutRotZ = targetRotZ;

            if (_isHovered) return;
            if (_dragHandler != null && _dragHandler.IsDragging) return;

            ApplyLayoutTween(targetPos, targetRotZ, duration, ease);
        }

        // --- IBeginDragHandler / IDragHandler ---

        public void SetInteractable(bool interactable) => _isInteractable = interactable;

        public void SetFusionState(bool eligible, Color glowColor)
        {
            if (fusionGlowImage != null)
            {
                if (_fusionGlowHandle.IsActive()) _fusionGlowHandle.Cancel();

                if (eligible)
                {
                    // 원소 색상 세팅 후 맥박 Yoyo 루프
                    fusionGlowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, fusionPulseMin);
                    _fusionGlowHandle = LMotion.Create(fusionPulseMin, fusionPulseMax, fusionPulseDuration)
                        .WithLoops(-1, LoopType.Yoyo)
                        .WithEase(Ease.InOutSine)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(a => { if (fusionGlowImage != null) fusionGlowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, a); });
                }
                else
                {
                    var c = fusionGlowImage.color;
                    _fusionGlowHandle = LMotion.Create(c.a, 0f, fusionFadeDuration)
                        .Bind(a => { if (fusionGlowImage != null) fusionGlowImage.color = new Color(c.r, c.g, c.b, a); });
                }
            }

            if (fusionDimOverlay != null)
            {
                float targetAlpha = eligible ? 0f : fusionDimColor.a;
                if (_fusionDimHandle.IsActive()) _fusionDimHandle.Cancel();
                _fusionDimHandle = LMotion.Create(fusionDimOverlay.color.a, targetAlpha, fusionFadeDuration)
                    .Bind(a => { if (fusionDimOverlay != null) fusionDimOverlay.color = new Color(fusionDimColor.r, fusionDimColor.g, fusionDimColor.b, a); });
            }

            // 부유 모션
            _fusionFloatCts?.Cancel();
            _fusionFloatCts?.Dispose();
            _fusionFloatCts = null;
            if (_fusionFloatHandle.IsActive()) _fusionFloatHandle.Cancel();

            if (eligible)
            {
                _fusionFloatCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                StartFusionFloatAsync(_fusionFloatCts.Token).Forget();
            }
            else
            {
                _rectTransform.anchoredPosition = _layoutPos;
            }
        }

        private async UniTaskVoid StartFusionFloatAsync(CancellationToken ct)
        {
            // 진행 중인 레이아웃 트윈이 끝날 때까지 대기 (RefreshLayout 직후 호출 시 튀는 현상 방지)
            await UniTask.WaitUntil(() => !_posHandle.IsActive(), cancellationToken: ct);
            if (ct.IsCancellationRequested) return;

            _fusionBasePos = _layoutPos;
            _fusionFloatHandle = LMotion.Create(0f, fusionFloatHeight, fusionFloatDuration)
                .WithLoops(-1, LoopType.Yoyo)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(y => { if (this != null) _rectTransform.anchoredPosition = new Vector2(_fusionBasePos.x, _fusionBasePos.y + y); });
        }

        public void SetFusionHoverState(bool active)
        {
            if (_isFusionHovered == active) return;
            _isFusionHovered = active;

            if (_fusionHoverScaleHandle.IsActive()) _fusionHoverScaleHandle.Cancel();
            if (_fusionHoverRotHandle.IsActive())   _fusionHoverRotHandle.Cancel();

            // 소팅 오더 — 드래그 카드(20) 뒤, 일반 카드들 앞
            if (_canvas != null)
            {
                _canvas.overrideSorting = active;
                if (active)
                {
                    int rootOrder = _canvas.rootCanvas != null ? _canvas.rootCanvas.sortingOrder : 0;
                    _canvas.sortingOrder = rootOrder + fusionHoverSortingOrder;
                }
            }

            float targetScale = active ? fusionHoverScale * _layoutScale : _layoutScale;
            float targetRotZ  = active ? 0f : _layoutRotZ;
            var   ease        = active ? Ease.OutBack : Ease.OutCubic;

            _fusionHoverScaleHandle = LMotion.Create(transform.localScale.x, targetScale, fusionHoverDuration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f));

            _fusionHoverRotHandle = LMotion.Create(_currentRotZ, targetRotZ, fusionHoverDuration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(z =>
                {
                    _currentRotZ = z;
                    _rectTransform.localRotation = Quaternion.Euler(0f, 0f, z);
                });
        }

        public async UniTask PlayFusionDepartureAsync(CancellationToken ct)
        {
            float startScale = transform.localScale.x;
            await LMotion.Create(startScale, startScale * 1.25f, 0.1f)
                .WithEase(Ease.OutQuad)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);

            await LMotion.Create(transform.localScale.x, 0f, 0.15f)
                .WithEase(Ease.InBack)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);
        }

        public void ClearFusionState()
        {
            SetFusionHoverState(false);
            if (fusionGlowImage != null)
            {
                if (_fusionGlowHandle.IsActive()) _fusionGlowHandle.Cancel();
                var c = fusionGlowImage.color;
                _fusionGlowHandle = LMotion.Create(c.a, 0f, fusionFadeDuration)
                    .Bind(a => { if (fusionGlowImage != null) fusionGlowImage.color = new Color(c.r, c.g, c.b, a); });
            }

            if (fusionDimOverlay != null)
            {
                if (_fusionDimHandle.IsActive()) _fusionDimHandle.Cancel();
                _fusionDimHandle = LMotion.Create(fusionDimOverlay.color.a, 0f, fusionFadeDuration)
                    .Bind(a => { if (fusionDimOverlay != null) fusionDimOverlay.color = new Color(0f, 0f, 0f, a); });
            }

            _fusionFloatCts?.Cancel();
            _fusionFloatCts?.Dispose();
            _fusionFloatCts = null;
            if (_fusionFloatHandle.IsActive()) _fusionFloatHandle.Cancel();
            _rectTransform.anchoredPosition = _layoutPos;
        }

        public void CancelLayoutTween()
        {
            if (_posHandle.IsActive()) _posHandle.Cancel();
            if (_rotHandle.IsActive()) _rotHandle.Cancel();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            ForceExitHover(tweenBack: false);
            _dragHandler.HandleBeginDrag(eventData);
        }

        private int GetRequiredDiscardCount()
        {
            var slots = CardInstance?.data?.effectSlots;
            if (slots == null) return 0;
            int total = 0;
            foreach (var slot in slots)
                if (slot?.effect is DiscardEffect de) total += de.discardCount;
            return total;
        }

        public void OnDrag(PointerEventData eventData) => _dragHandler.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            // 타겟팅 경로(스킬 사용)일 때만 코스트/조건 체크. 합성 경로는 항상 허용.
            if (_dragHandler != null && _dragHandler.IsTargeting)
            {
                if (!_canAfford)
                {
                    PlayShakeAsync(destroyCancellationToken).Forget();
                    FlashCostRed();
                    PopCostText();
                    battleEventChannel.RaiseEvent(new InsufficientCostEvent());
                    _dragHandler.ForceReturnToHand();
                    return;
                }
                if (!_conditionMet)
                {
                    PlayShakeAsync(destroyCancellationToken).Forget();
                    _dragHandler.ForceReturnToHand();
                    return;
                }
                // 드래그 시작 시 카드는 이미 손패에서 제거됨 → HandCardCount에 자신 미포함
                int discardRequired = GetRequiredDiscardCount();
                if (discardRequired > 0 && _dragHandler.HandCardCount < discardRequired)
                {
                    PlayShakeAsync(destroyCancellationToken).Forget();
                    _dragHandler.ForceReturnToHand();
                    return;
                }
            }
            _dragHandler.HandleEndDrag(eventData);
        }

        // --- IPointerEnterHandler ---

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            if (_dragHandler != null && _dragHandler.IsDragging) return;
            _exitDebounce?.Cancel();
            if (_isHovered) return;
            _isHovered = true;

            soundChannel?.RaiseEvent(new PlaySoundEvent(hoverSound, Vector3.zero));

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                int rootOrder = _canvas.rootCanvas != null ? _canvas.rootCanvas.sortingOrder : 0;
                _canvas.sortingOrder = rootOrder + hoverSortingOrder;
            }

            TweenHoverIn();
            battleEventChannel.RaiseEvent(new CardHoverEvent(CardInstance));
        }

        // --- IPointerExitHandler ---

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.IsDragging)
            {
                ForceExitHover(tweenBack: false);
                return;
            }
            _exitDebounce?.Cancel();
            _exitDebounce = new CancellationTokenSource();
            DebounceExitAsync(_exitDebounce.Token).Forget();
        }

        private async UniTaskVoid DebounceExitAsync(CancellationToken ct)
        {
            await UniTask.Delay(System.TimeSpan.FromMilliseconds(50), DelayType.UnscaledDeltaTime, cancellationToken: ct);
            ForceExitHover(tweenBack: true);
        }

        public void SetLayoutScale(float scale, float duration, Ease ease)
        {
            _layoutScale = scale;
            if (_isHovered || (_dragHandler != null && _dragHandler.IsDragging)) return;
            TweenScaleTo(new Vector3(scale, scale, 1f), duration, ease);
        }

        public void ForceExitHover(bool tweenBack = true)
        {
            _exitDebounce?.Cancel();
            if (!_isHovered) return;
            _isHovered = false;

            if (_canvas != null) _canvas.overrideSorting = false;

            if (tweenBack)
                ApplyLayoutTween(_layoutPos, _layoutRotZ, hoverDuration, hoverEase);

            TweenScaleTo(new Vector3(_layoutScale, _layoutScale, 1f), hoverDuration, hoverEase);
            battleEventChannel.RaiseEvent(new CardHoverExitEvent(CardInstance));
        }

        // --- 내부 트윈 ---

        private void TweenHoverIn()
        {
            float snapY = CalculateSnapToBottomY();
            ApplyLayoutTween(new Vector2(_layoutPos.x, snapY), 0f, hoverDuration, hoverEase);
            TweenScaleTo(new Vector3(hoverScale, hoverScale, 1f), hoverDuration, hoverEase);
        }

        // 카드 밑면이 화면 바닥에 맞닿는 Y 좌표를 CardContainer 로컬 기준으로 계산
        private float CalculateSnapToBottomY()
        {
            var cardContainer = (RectTransform)transform.parent;
            Canvas rootCanvas = _canvas != null ? _canvas.rootCanvas : null;
            if (rootCanvas == null) return _layoutPos.y;

            var rootRT = (RectTransform)rootCanvas.transform;
            Vector3 bottomWorld = rootRT.TransformPoint(new Vector3(0f, rootRT.rect.yMin, 0f));
            Vector2 bottomLocal = cardContainer.InverseTransformPoint(bottomWorld);

            float cardHalfHeight = _rectTransform.rect.height * hoverScale / 2f;
            return bottomLocal.y + cardHalfHeight;
        }

        private void ApplyLayoutTween(Vector2 targetPos, float targetRotZ, float duration, Ease ease)
        {
            if (_posHandle.IsActive()) _posHandle.Cancel();
            if (_rotHandle.IsActive()) _rotHandle.Cancel();

            _posHandle = LMotion.Create(_rectTransform.anchoredPosition, targetPos, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(pos => _rectTransform.anchoredPosition = pos);

            _rotHandle = LMotion.Create(_currentRotZ, targetRotZ, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(z =>
                {
                    _currentRotZ = z;
                    _rectTransform.localRotation = Quaternion.Euler(0f, 0f, z);
                });
        }

        private void TweenScaleTo(Vector3 targetScale, float duration, Ease ease)
        {
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();

            _scaleHandle = LMotion.Create(transform.localScale, targetScale, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => transform.localScale = s);
        }

        private async UniTaskVoid PlayShakeAsync(CancellationToken ct)
        {
            if (_posHandle.IsActive()) _posHandle.Cancel();

            float[] xOffsets = { 18f, -18f, 12f, -12f, 6f, -6f, 0f };
            foreach (var offset in xOffsets)
            {
                if (ct.IsCancellationRequested) break;
                var target = new Vector2(_layoutPos.x + offset, _layoutPos.y);
                await LMotion.Create(_rectTransform.anchoredPosition, target, shakeStepDuration)
                    .WithEase(Ease.OutQuad)
                    .Bind(pos => _rectTransform.anchoredPosition = pos)
                    .ToUniTask(cancellationToken: ct);
            }
        }

        private void FlashCostRed()
        {
            if (_costFlashHandle.IsActive()) _costFlashHandle.Cancel();
            _costFlashHandle = LMotion.Create(Color.red, Color.white, 0.15f)
                .WithLoops(2, LoopType.Yoyo)
                .Bind(c => costText.color = c);
        }

        private void PopCostText()
        {
            if (_costPopHandle.IsActive()) _costPopHandle.Cancel();
            costText.transform.localScale = Vector3.one;
            _costPopHandle = LMotion.Create(1f, costPopScale, costPopDuration)
                .WithLoops(2, LoopType.Yoyo)
                .WithEase(Ease.OutQuad)
                .Bind(s => costText.transform.localScale = new Vector3(s, s, 1f));
        }

        public override void ResetItem()
        {
            if (_posHandle.IsActive()) _posHandle.Cancel();
            if (_rotHandle.IsActive()) _rotHandle.Cancel();
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
            if (_overlayHandle.IsActive()) _overlayHandle.Cancel();
            if (_costFlashHandle.IsActive()) _costFlashHandle.Cancel();
            if (_costPopHandle.IsActive()) _costPopHandle.Cancel();
            if (_fusionGlowHandle.IsActive()) _fusionGlowHandle.Cancel();
            if (_fusionDimHandle.IsActive()) _fusionDimHandle.Cancel();
            _fusionFloatCts?.Cancel();
            _fusionFloatCts?.Dispose();
            _fusionFloatCts = null;
            if (_fusionFloatHandle.IsActive()) _fusionFloatHandle.Cancel();
            if (_fusionHoverScaleHandle.IsActive()) _fusionHoverScaleHandle.Cancel();
            if (_fusionHoverRotHandle.IsActive())   _fusionHoverRotHandle.Cancel();
            _isFusionHovered = false;

            if (fusionGlowImage != null) fusionGlowImage.color = new Color(fusionGlowImage.color.r, fusionGlowImage.color.g, fusionGlowImage.color.b, 0f);
            if (fusionDimOverlay != null) fusionDimOverlay.color = new Color(0f, 0f, 0f, 0f);
            costText.transform.localScale = Vector3.one;

            _isHovered = false;
            _layoutScale = 1f;
            _canAfford = true;
            _conditionMet = true;
            _isInteractable = true;
            _dragHandler?.ResetDragState();
            _currentRotZ = 0f;
            _rectTransform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (_canvas != null)
                _canvas.overrideSorting = false;

            if (usabilityOverlay != null)
            {
                var c = usabilityOverlay.color;
                usabilityOverlay.color = new Color(c.r, c.g, c.b, 0f);
            }

            if (costText != null) costText.color = _originalCostColor;

            CardInstance = null;
            if (artworkImage != null) artworkImage.sprite = null;
            CardColorUtility.Reset(frameImage, labelImage);
            CardColorUtility.ResetTextEffects(nameTextEffect, typeTextEffect);
            if (typeText != null) typeText.text = string.Empty;
            if (nameText != null) nameText.text = string.Empty;
            if (descText != null) descText.text = string.Empty;
            if (costText != null) costText.text = string.Empty;
        }
    }
}
