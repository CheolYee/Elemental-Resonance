using System.Collections.Generic;
using Battle.Enums;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Battle.UI
{
    public class CardDragHandler : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private RectTransform handAreaRect;
        [SerializeField] private int dragSortingOrder = 20;
        private List<RectTransform> _blockedDropAreas;

        private BattleTargetingController _targetingController;
        private CardView _cardView;
        private RectTransform _rectTransform;
        private Canvas _canvas;

        private bool _isDragging;
        private bool _isTargeting;

        public bool IsDragging => _isDragging;

        public void SetHandAreaRect(RectTransform rect) => handAreaRect = rect;
        public void SetBlockedAreas(List<RectTransform> areas) => _blockedDropAreas = areas;

        private void Awake()
        {
            _cardView = GetComponent<CardView>();
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            if (_targetingController == null)
                _targetingController = FindAnyObjectByType<BattleTargetingController>();
        }

        private void Update()
        {
            if (!_isDragging) return;

            if (Mouse.current.rightButton.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnToHand();
            }
        }

        public void HandleBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _isTargeting = false;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = dragSortingOrder;
            _cardView.CancelLayoutTween();
            battleEventChannel.RaiseEvent(new CardDragStartEvent(_cardView.CardInstance));
        }

        public void HandleDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            var parentRect = (RectTransform)_rectTransform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
            _rectTransform.anchoredPosition = localPos;

            bool isInsideHand = RectTransformUtility.RectangleContainsScreenPoint(
                handAreaRect, eventData.position, eventData.pressEventCamera);

            if (!_isTargeting && !isInsideHand)
            {
                _isTargeting = true;
                battleEventChannel.RaiseEvent(new CardTargetingStartEvent(_cardView.CardInstance));
            }
            else if (_isTargeting && isInsideHand)
            {
                _isTargeting = false;
                battleEventChannel.RaiseEvent(new CardTargetingEndEvent());
            }
        }

        private bool IsOverBlockedArea(PointerEventData eventData)
        {
            if (_blockedDropAreas == null) return false;
            foreach (var area in _blockedDropAreas)
            {
                if (area != null && RectTransformUtility.RectangleContainsScreenPoint(
                    area, eventData.position, eventData.pressEventCamera))
                    return true;
            }
            return false;
        }

        public void HandleEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            var targetType = _cardView.CardInstance?.data?.targetType ?? CardTargetType.None;
            bool isNoneCard = targetType == CardTargetType.None;
            bool isValidDrop = _isTargeting &&
                (isNoneCard ? !IsOverBlockedArea(eventData) : _targetingController.HasValidHoverTarget);

            if (isValidDrop)
            {
                var target = isNoneCard ? null : _targetingController.CurrentHoveredTarget;
                _isDragging = false;
                _isTargeting = false;
                _canvas.overrideSorting = false;
                battleEventChannel.RaiseEvent(new CardTargetingEndEvent());
                battleEventChannel.RaiseEvent(new CardDroppedOnTargetEvent(_cardView.CardInstance, target, _rectTransform.position));
            }
            else
            {
                ReturnToHand();
            }
        }

        public void ResetDragState()
        {
            _isDragging = false;
            _isTargeting = false;
            if (_canvas != null) _canvas.overrideSorting = false;
        }

        private void ReturnToHand()
        {
            _isDragging = false;

            if (_isTargeting)
            {
                _isTargeting = false;
                battleEventChannel.RaiseEvent(new CardTargetingEndEvent());
            }

            _canvas.overrideSorting = false;
            battleEventChannel.RaiseEvent(new CardReturnToHandEvent(_cardView.CardInstance));
        }
    }
}
