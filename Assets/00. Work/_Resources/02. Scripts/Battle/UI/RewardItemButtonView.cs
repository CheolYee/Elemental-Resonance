using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.UI
{
    public class RewardItemButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image    background;
        [SerializeField] private Image    iconImage;
        [SerializeField] private TMP_Text labelText;

        [SerializeField] private CanvasGroup    bracketContainer;
        [SerializeField] private RectTransform  bracketTL;
        [SerializeField] private RectTransform  bracketTR;
        [SerializeField] private RectTransform  bracketBL;
        [SerializeField] private RectTransform  bracketBR;

        [Header("Hover")]
        [SerializeField] private float bracketSpread  = 10f;
        [SerializeField] private float bracketPulse   = 3f;
        [SerializeField] private float spreadDuration = 0.15f;
        [SerializeField] private float pulseDuration  = 0.45f;
        [SerializeField] private float brightAmount   = 0.15f;

        [Header("Claim")]
        [SerializeField] private float claimDuration = 0.25f;

        public Action OnClaimed;

        private Vector2 _tlOrigin, _trOrigin, _blOrigin, _brOrigin;
        private Color   _baseBgColor;

        private readonly MotionHandle[] _pulseHandles = new MotionHandle[4];
        private CancellationTokenSource _hoverCts;
        private bool _claimed;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (bracketTL != null) _tlOrigin = bracketTL.anchoredPosition;
            if (bracketTR != null) _trOrigin = bracketTR.anchoredPosition;
            if (bracketBL != null) _blOrigin = bracketBL.anchoredPosition;
            if (bracketBR != null) _brOrigin = bracketBR.anchoredPosition;

            if (background != null) _baseBgColor = background.color;

            if (bracketContainer != null) bracketContainer.alpha = 0f;
        }

        private void OnDestroy()
        {
            StopPulse();
            _hoverCts?.Cancel();
            _hoverCts?.Dispose();
        }

        public void Setup(string label, Sprite icon = null)
        {
            _claimed = false;
            if (labelText != null) labelText.text = label;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(icon != null);
            }
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (_claimed) return;
            ResetHoverCts();
            HoverInAsync(_hoverCts.Token).Forget();
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (_claimed) return;
            ResetHoverCts();
            HoverOutAsync(_hoverCts.Token).Forget();
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (_claimed) return;
            _claimed = true;
            ResetHoverCts();
            ClaimAsync(destroyCancellationToken).Forget();
        }

        private void ResetHoverCts()
        {
            _hoverCts?.Cancel();
            _hoverCts?.Dispose();
            _hoverCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private async UniTaskVoid HoverInAsync(CancellationToken ct)
        {
            StopPulse();

            float currentAlpha = bracketContainer != null ? bracketContainer.alpha : 0f;

            await UniTask.WhenAll(
                bracketContainer != null
                    ? LMotion.Create(currentAlpha, 1f, spreadDuration)
                        .Bind(a => bracketContainer.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask,
                MoveBracketAsync(bracketTL, _tlOrigin + new Vector2(-bracketSpread,  bracketSpread), ct),
                MoveBracketAsync(bracketTR, _trOrigin + new Vector2( bracketSpread,  bracketSpread), ct),
                MoveBracketAsync(bracketBL, _blOrigin + new Vector2(-bracketSpread, -bracketSpread), ct),
                MoveBracketAsync(bracketBR, _brOrigin + new Vector2( bracketSpread, -bracketSpread), ct),
                TweenBgAsync(true, ct));

            if (ct.IsCancellationRequested) return;
            StartPulse();
        }

        private async UniTaskVoid HoverOutAsync(CancellationToken ct)
        {
            StopPulse();

            float currentAlpha = bracketContainer != null ? bracketContainer.alpha : 1f;

            await UniTask.WhenAll(
                bracketContainer != null
                    ? LMotion.Create(currentAlpha, 0f, spreadDuration)
                        .Bind(a => bracketContainer.alpha = a).ToUniTask(ct)
                    : UniTask.CompletedTask,
                MoveBracketAsync(bracketTL, _tlOrigin, ct),
                MoveBracketAsync(bracketTR, _trOrigin, ct),
                MoveBracketAsync(bracketBL, _blOrigin, ct),
                MoveBracketAsync(bracketBR, _brOrigin, ct),
                TweenBgAsync(false, ct));
        }

        private async UniTask MoveBracketAsync(RectTransform rt, Vector2 target, CancellationToken ct)
        {
            if (rt == null) return;
            await LMotion.Create(rt.anchoredPosition, target, spreadDuration)
                .WithEase(Ease.OutCubic)
                .Bind(p => rt.anchoredPosition = p)
                .ToUniTask(ct);
        }

        private async UniTask TweenBgAsync(bool brighter, CancellationToken ct)
        {
            if (background == null) return;
            Color target = brighter
                ? new Color(
                    Mathf.Min(_baseBgColor.r + brightAmount, 1f),
                    Mathf.Min(_baseBgColor.g + brightAmount, 1f),
                    Mathf.Min(_baseBgColor.b + brightAmount, 1f),
                    _baseBgColor.a)
                : _baseBgColor;
            await LMotion.Create(background.color, target, spreadDuration)
                .Bind(c => background.color = c)
                .ToUniTask(ct);
        }

        private void StartPulse()
        {
            float p = bracketPulse;
            _pulseHandles[0] = PulseOne(bracketTL, new Vector2(-p,  p));
            _pulseHandles[1] = PulseOne(bracketTR, new Vector2( p,  p));
            _pulseHandles[2] = PulseOne(bracketBL, new Vector2(-p, -p));
            _pulseHandles[3] = PulseOne(bracketBR, new Vector2( p, -p));
        }

        private MotionHandle PulseOne(RectTransform rt, Vector2 delta)
        {
            if (rt == null) return default;
            Vector2 from = rt.anchoredPosition;
            return LMotion.Create(from, from + delta, pulseDuration)
                .WithLoops(-1, LoopType.Yoyo)
                .WithEase(Ease.InOutSine)
                .Bind(p => rt.anchoredPosition = p);
        }

        private void StopPulse()
        {
            for (int i = 0; i < _pulseHandles.Length; i++)
            {
                if (_pulseHandles[i].IsActive()) _pulseHandles[i].Cancel();
            }
        }

        private async UniTaskVoid ClaimAsync(CancellationToken ct)
        {
            StopPulse();

            Vector3 startScale = transform.localScale;
            float   startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;

            await UniTask.WhenAll(
                LMotion.Create(startScale, Vector3.zero, claimDuration)
                    .WithEase(Ease.InBack)
                    .Bind(s => { if (this != null) transform.localScale = s; })
                    .ToUniTask(ct),
                _canvasGroup != null
                    ? LMotion.Create(startAlpha, 0f, claimDuration)
                        .WithEase(Ease.InQuad)
                        .Bind(a => { if (_canvasGroup != null) _canvasGroup.alpha = a; })
                        .ToUniTask(ct)
                    : UniTask.CompletedTask);

            OnClaimed?.Invoke();
            if (!ct.IsCancellationRequested)
                Destroy(gameObject);
        }
    }
}
