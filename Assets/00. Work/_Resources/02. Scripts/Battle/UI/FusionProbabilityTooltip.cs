using System;
using System.Threading;
using Battle.Enums;
using Battle.Fusion;
using Cysharp.Threading.Tasks;
using LitMotion;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class FusionProbabilityTooltip : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI normalText;
        [SerializeField] private TextMeshProUGUI rareText;
        [SerializeField] private TextMeshProUGUI epicText;
        [SerializeField] private TextMeshProUGUI legendaryText;
        [SerializeField] private float yOffset = 100f;
        [SerializeField] private float showDelay = 0.2f;
        [SerializeField] private float fadeDuration = 0.12f;

        [Inject] private FusionResultPicker _fusionResultPicker;

        private CancellationTokenSource _showCts;
        private MotionHandle _fadeHandle;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        }

        public void Show(ElementType resultElement, CardGrade maxGrade, RectTransform targetCard)
        {
            ResetCts();
            ShowDelayedAsync(resultElement, maxGrade, targetCard, _showCts.Token).Forget();
        }

        public void Hide()
        {
            ResetCts();
            FadeOut();
        }

        private void ResetCts()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private async UniTaskVoid ShowDelayedAsync(ElementType resultElement, CardGrade maxGrade, RectTransform targetCard, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(showDelay), cancellationToken: ct);

            var weights = _fusionResultPicker.GetEffectiveWeights(maxGrade, resultElement);
            int total = 0;
            foreach (var w in weights) total += w;

            SetLine(normalText,    weights[0], total, "일반");
            SetLine(rareText,      weights[1], total, "레어");
            SetLine(epicText,      weights[2], total, "에픽");
            SetLine(legendaryText, weights[3], total, "전설");

            PositionAbove(targetCard);

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, 1f, fadeDuration)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => canvasGroup.alpha = a);
        }

        private void FadeOut()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, 0f, fadeDuration)
                .WithEase(Ease.OutQuad)
                .Bind(a => canvasGroup.alpha = a);
        }

        private void SetLine(TextMeshProUGUI tmp, int weight, int total, string label)
        {
            if (tmp == null) return;
            float pct = total > 0 ? weight * 100f / total : 0f;
            tmp.text = $"{label}: {pct:0}%";
            tmp.gameObject.SetActive(weight > 0);
        }

        private void PositionAbove(RectTransform targetCard)
        {
            var corners = new Vector3[4];
            targetCard.GetWorldCorners(corners); // 0=BL 1=TL 2=TR 3=BR
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            transform.position = topCenter + Vector3.up * yOffset;
        }
    }
}
