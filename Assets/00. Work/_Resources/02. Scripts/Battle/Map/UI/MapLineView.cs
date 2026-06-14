using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapLineView : MonoBehaviour
    {
        public enum LineState { Locked, Selectable, Visited }

        [SerializeField] private Sprite _dashSprite;     // null이면 흰 사각형, 둥근 모서리 원하면 연결
        [SerializeField] private float  _dashLength    = 15f;
        [SerializeField] private float  _dashGap       = 8f;
        [SerializeField] private float  _lineThickness = 3f;

        private static readonly Color VisitedColor    = new Color(1f, 1f, 1f, 1.0f);
        private static readonly Color SelectableColor = new Color(1f, 1f, 1f, 0.6f);
        private static readonly Color LockedColor     = new Color(1f, 1f, 1f, 0.25f);

        public void Setup(Vector2 fromPos, Vector2 toPos, LineState state)
        {
            var dir      = toPos - fromPos;
            var distance = dir.magnitude;
            var angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var rt = (RectTransform)transform;
            rt.anchoredPosition = (fromPos + toPos) * 0.5f;
            rt.sizeDelta        = new Vector2(distance, _lineThickness);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

            SpawnDashes(distance, StateToColor(state));
        }

        private void SpawnDashes(float distance, Color color)
        {
            float period = _dashLength + _dashGap;
            if (period <= 0f || distance <= 0f) return;

            int   count     = Mathf.Max(1, Mathf.FloorToInt(distance / period));
            float halfTotal = (count - 1) * period * 0.5f;

            for (int i = 0; i < count; i++)
                CreateDash(-halfTotal + i * period, color);
        }

        private void CreateDash(float localX, Color color)
        {
            var go = new GameObject("d", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(localX, 0f);
            rt.sizeDelta        = new Vector2(_dashLength, _lineThickness);

            var img    = go.GetComponent<Image>();
            img.sprite = _dashSprite;
            img.color  = color;
        }

        private static Color StateToColor(LineState state) => state switch
        {
            LineState.Visited    => VisitedColor,
            LineState.Selectable => SelectableColor,
            _                    => LockedColor
        };
    }
}
