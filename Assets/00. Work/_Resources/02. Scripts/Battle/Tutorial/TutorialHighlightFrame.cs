using UnityEngine;

namespace Battle.Tutorial
{
    // Sort Order 100 캔버스에 부착. 대상 RectTransform의 화면 위치/크기를 읽어
    // 발광 테두리 프레임을 동일 위치에 배치한다. 대상 오브젝트는 원래 캔버스에 유지.
    [RequireComponent(typeof(RectTransform))]
    public class TutorialHighlightFrame : MonoBehaviour
    {
        [SerializeField] private float padding = 8f;

        private RectTransform _rect;
        private Canvas _rootCanvas;
        private RectTransform _target;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<Canvas>();
            gameObject.SetActive(false);
        }

        public void SetTarget(RectTransform target)
        {
            _target = target;
            gameObject.SetActive(target != null);
            if (target != null) UpdateFrame();
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy) return;
            UpdateFrame();
        }

        private void UpdateFrame()
        {
            var targetCanvas = _target.GetComponentInParent<Canvas>();
            Camera targetCam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera : null;
            Camera overlayCam = _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _rootCanvas.worldCamera : null;

            var corners = new Vector3[4];
            _target.GetWorldCorners(corners);

            // corners[0]=bottom-left, corners[2]=top-right
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(targetCam, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(targetCam, corners[2]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform, screenMin, overlayCam, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform, screenMax, overlayCam, out var localMax);

            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = (localMin + localMax) * 0.5f;
            _rect.sizeDelta = new Vector2(
                Mathf.Abs(localMax.x - localMin.x) + padding * 2f,
                Mathf.Abs(localMax.y - localMin.y) + padding * 2f);
        }
    }
}
