using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Tutorial
{
    // Sort Order 100 캔버스에 부착. 말풍선 텍스트 표시 + 자동 위치 배치.
    // ClickToContinue 단계: tapHintText로 "화면을 탭하여 계속" 힌트 표시.
    // 별도 버튼 없음 — 오버레이 패널 클릭으로 진행 (TutorialOverlayView 처리).
    public class TutorialTooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private TMP_Text tooltipText;
        [SerializeField] private TMP_Text tapHintText;
        [SerializeField] private float anchorGap = 24f;
        [SerializeField] private float canvasPadding = 16f;

        private Canvas _rootCanvas;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            gameObject.SetActive(false);
        }

        public void Show(TutorialStepSO step, RectTransform target)
        {
            tooltipText.text = step.tooltipText;

            if (tapHintText != null)
                tapHintText.gameObject.SetActive(step.completionType == TutorialCompletionType.ClickToContinue);

            gameObject.SetActive(true);

            // 레이아웃 갱신 후 크기 확정 → 위치 계산 → 경계 클램핑
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            PositionTooltip(step, target);
            ClampToCanvas();
        }

        public void Hide() => gameObject.SetActive(false);

        private void ClampToCanvas()
        {
            var canvasHalf = ((RectTransform)_rootCanvas.transform).rect.size * 0.5f;
            var half       = tooltipRect.rect.size * 0.5f;
            var pos        = tooltipRect.anchoredPosition;

            pos.x = Mathf.Clamp(pos.x, -canvasHalf.x + half.x + canvasPadding, canvasHalf.x - half.x - canvasPadding);
            pos.y = Mathf.Clamp(pos.y, -canvasHalf.y + half.y + canvasPadding, canvasHalf.y - half.y - canvasPadding);

            tooltipRect.anchoredPosition = pos;
        }

        private void PositionTooltip(TutorialStepSO step, RectTransform target)
        {
            if (target == null)
            {
                tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.anchoredPosition = step.anchorOffset;
                return;
            }

            var targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            Camera overlayCam = _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _rootCanvas.worldCamera : null;

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(targetCam, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(targetCam, corners[2]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform, screenMin, overlayCam, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform, screenMax, overlayCam, out var localMax);

            Vector2 targetCenter    = (localMin + localMax) * 0.5f;
            Vector2 targetHalfSize  = (localMax - localMin) * 0.5f;
            Vector2 tooltipHalfSize = tooltipRect.rect.size * 0.5f;

            Vector2 basePosition = step.preferredAnchor switch
            {
                TooltipAnchor.Top    => targetCenter + new Vector2(0f,   targetHalfSize.y + anchorGap + tooltipHalfSize.y),
                TooltipAnchor.Bottom => targetCenter + new Vector2(0f, -(targetHalfSize.y + anchorGap + tooltipHalfSize.y)),
                TooltipAnchor.Left   => targetCenter + new Vector2(-(targetHalfSize.x + anchorGap + tooltipHalfSize.x), 0f),
                TooltipAnchor.Right  => targetCenter + new Vector2(  targetHalfSize.x + anchorGap + tooltipHalfSize.x, 0f),
                _                    => targetCenter
            };

            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.anchoredPosition = basePosition + step.anchorOffset;
        }
    }
}
