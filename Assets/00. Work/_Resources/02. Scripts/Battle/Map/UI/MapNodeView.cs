using System;
using Battle.Map.Enums;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _pulseRingImage;
        [SerializeField] private Image _currentRingImage;
        [SerializeField] private Button _button;

        [Header("State Colors")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _visitedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color _lockedColor  = new Color(0.35f, 0.35f, 0.35f, 1f);

        private string _nodeId;
        private MotionHandle _pulseHandle;

        public string NodeId => _nodeId;

        public event Action<string> OnHoverEnter;
        public event Action<string> OnHoverExit;
        public event Action<string> OnClicked;

        private void Awake()
        {
            _pulseRingImage.gameObject.SetActive(false);
            _currentRingImage.gameObject.SetActive(false);
            _button.onClick.AddListener(() => OnClicked?.Invoke(_nodeId));
        }

        private void OnDestroy()
        {
            if (_pulseHandle.IsActive()) _pulseHandle.Cancel();
        }

        public void Setup(string nodeId, Sprite icon, MapNodeVisualState visualState, bool isInteractable)
        {
            _nodeId = nodeId;
            _iconImage.sprite = icon;
            _button.interactable = isInteractable;
            ApplyVisualState(visualState);
        }

        public void SetInteractable(bool interactable) => _button.interactable = interactable;

        public void MarkTransitionSelected() => ApplyVisualState(MapNodeVisualState.TransitionSelected);

        private void ApplyVisualState(MapNodeVisualState state)
        {
            if (_pulseHandle.IsActive()) _pulseHandle.Cancel();
            transform.localScale = Vector3.one;
            _pulseRingImage.gameObject.SetActive(false);
            _currentRingImage.gameObject.SetActive(false);

            switch (state)
            {
                case MapNodeVisualState.Locked:
                    _iconImage.color = _lockedColor;
                    break;

                case MapNodeVisualState.Selectable:
                    _iconImage.color = _defaultColor;
                    _pulseRingImage.gameObject.SetActive(true);
                    _pulseHandle = LMotion.Create(1f, 1.18f, 0.7f)
                        .WithLoops(-1, LoopType.Yoyo)
                        .Bind(s => transform.localScale = new Vector3(s, s, 1f));
                    break;

                case MapNodeVisualState.Visited:
                    _iconImage.color = _visitedColor;
                    break;

                case MapNodeVisualState.Current:
                    _iconImage.color = _defaultColor;
                    _currentRingImage.gameObject.SetActive(true);
                    break;

                case MapNodeVisualState.TransitionSelected:
                    _iconImage.color = _defaultColor;
                    _currentRingImage.gameObject.SetActive(true);
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter?.Invoke(_nodeId);
        public void OnPointerExit(PointerEventData eventData)  => OnHoverExit?.Invoke(_nodeId);
    }
}
