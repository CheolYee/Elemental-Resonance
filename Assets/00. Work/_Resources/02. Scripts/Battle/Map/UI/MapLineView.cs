using MPUIKIT;
using UnityEngine;

namespace Battle.Map.UI
{
    public class MapLineView : MonoBehaviour
    {
        public enum LineState { Locked, Selectable, Visited }

        [SerializeField] private MPImage _lineImage;

        [Header("State Colors")]
        [SerializeField] private Color _visitedColor   = new Color(1f,  1f,  1f,  0.7f);
        [SerializeField] private Color _selectableColor = new Color(1f,  0.9f, 0.3f, 1f);
        [SerializeField] private Color _lockedColor    = new Color(0.4f, 0.4f, 0.4f, 0.4f);

        public void Setup(Vector2 fromPos, Vector2 toPos, LineState state)
        {
            var dir      = toPos - fromPos;
            var distance = dir.magnitude;
            var angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var rt = (RectTransform)transform;
            rt.anchoredPosition = (fromPos + toPos) * 0.5f;
            rt.sizeDelta        = new Vector2(distance, rt.sizeDelta.y);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

            _lineImage.color = state switch
            {
                LineState.Visited    => _visitedColor,
                LineState.Selectable => _selectableColor,
                _                    => _lockedColor
            };
        }
    }
}
