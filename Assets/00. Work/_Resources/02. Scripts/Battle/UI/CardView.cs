using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.UI
{
    public class CardView : PoolableMono, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;

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

        public CardInstance CardInstance { get; private set; }

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private MotionHandle _posHandle;
        private MotionHandle _rotHandle;
        private MotionHandle _scaleHandle;
        private MotionHandle _overlayHandle;
        private MotionHandle _costFlashHandle;
        private MotionHandle _costPopHandle;

        private float _currentRotZ;
        private Vector2 _layoutPos;
        private float _layoutRotZ;
        private bool _isHovered;
        private CardDragHandler _dragHandler;
        private bool _canUse = true;
        private bool _isInteractable = true;
        private Color _originalCostColor;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponent<Canvas>();
            _originalCostColor = costText.color;
            _dragHandler = GetComponent<CardDragHandler>();
        }

        private void OnEnable() => battleEventChannel.AddListener<CostChangedEvent>(OnCostChanged);
        private void OnDisable() => battleEventChannel.RemoveListener<CostChangedEvent>(OnCostChanged);

        public void Setup(CardInstance instance)
        {
            CardInstance = instance;
            nameText.text = instance.data.cardName;
            costText.text = instance.data.cost.ToString();
            artworkImage.sprite = instance.data.artwork;

            SetUsability(costModel.currentCost >= instance.data.cost);
        }

        private void OnCostChanged(CostChangedEvent evt) => SetUsability(evt.CurrentCost >= CardInstance.data.cost);

        private void SetUsability(bool canUse)
        {
            _canUse = canUse;
            TweenOverlayAlpha(canUse ? 0f : overlayTargetAlpha);
            costText.color = canUse ? _originalCostColor : Color.red;
        }

        private void TweenOverlayAlpha(float targetAlpha)
        {
            if (usabilityOverlay == null) return;
            if (_overlayHandle.IsActive()) _overlayHandle.Cancel();
            var c = usabilityOverlay.color;
            _overlayHandle = LMotion.Create(c.a, targetAlpha, overlayFadeDuration)
                .Bind(a => usabilityOverlay.color = new Color(c.r, c.g, c.b, a));
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

        public void CancelLayoutTween()
        {
            if (_posHandle.IsActive()) _posHandle.Cancel();
            if (_rotHandle.IsActive()) _rotHandle.Cancel();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            if (!_canUse)
            {
                PlayShakeAsync(destroyCancellationToken).Forget();
                FlashCostRed();
                PopCostText();
                battleEventChannel.RaiseEvent(new InsufficientCostEvent());
                return;
            }

            ForceExitHover(tweenBack: false);
            _dragHandler.HandleBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData) => _dragHandler.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => _dragHandler.HandleEndDrag(eventData);

        // --- IPointerEnterHandler ---

public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            if (_dragHandler != null && _dragHandler.IsDragging) return;
            _isHovered = true;

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
            bool isDragging = _dragHandler != null && _dragHandler.IsDragging;
            ForceExitHover(tweenBack: !isDragging);
        }

        public void ForceExitHover(bool tweenBack = true)
        {
            if (!_isHovered) return;
            _isHovered = false;

            if (_canvas != null)
                _canvas.overrideSorting = false;

            if (tweenBack)
                ApplyLayoutTween(_layoutPos, _layoutRotZ, hoverDuration, hoverEase);

            TweenScaleTo(Vector3.one, hoverDuration, hoverEase);
            battleEventChannel.RaiseEvent(new CardHoverExitEvent());
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
                .Bind(pos => _rectTransform.anchoredPosition = pos);

            _rotHandle = LMotion.Create(_currentRotZ, targetRotZ, duration)
                .WithEase(ease)
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
            costText.transform.localScale = Vector3.one;

            _isHovered = false;
            _canUse = true;
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
            if (nameText != null) nameText.text = string.Empty;
            if (costText != null) costText.text = string.Empty;
        }
    }
}
