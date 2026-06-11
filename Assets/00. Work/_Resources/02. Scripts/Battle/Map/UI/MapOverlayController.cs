using System;
using Battle.Map.Enums;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapOverlayController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private ScrollRect _scrollRect;

        private MapOverlayState _state = MapOverlayState.Hidden;
        private MotionHandle _fadeHandle;

        public MapOverlayState State => _state;
        public event Action<MapOverlayState> OnStateChanged;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OpenInspect()
        {
            if (_state != MapOverlayState.Hidden) return;
            ApplyState(MapOverlayState.InspectOnly);
            FadeTo(1f);
        }

        public void OpenForSelection()
        {
            ApplyState(MapOverlayState.SelectionPending);
            FadeTo(1f);
        }

        public bool TryClose()
        {
            if (_state != MapOverlayState.InspectOnly) return false;
            ApplyState(MapOverlayState.Hidden);
            FadeTo(0f);
            return true;
        }

        public void EnterTransitionInProgress()
        {
            ApplyState(MapOverlayState.TransitionInProgress);
        }

        public void CloseForTransition()
        {
            ApplyState(MapOverlayState.Hidden);
            FadeTo(0f);
        }

        private void ApplyState(MapOverlayState state)
        {
            _state = state;
            bool visible = state != MapOverlayState.Hidden;
            _canvasGroup.blocksRaycasts = visible;
            _canvasGroup.interactable = visible && state != MapOverlayState.TransitionInProgress;
            if (visible && _scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
            OnStateChanged?.Invoke(_state);
        }

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(_canvasGroup.alpha, target, 0.15f)
                .Bind(a => _canvasGroup.alpha = a);
        }
    }
}
