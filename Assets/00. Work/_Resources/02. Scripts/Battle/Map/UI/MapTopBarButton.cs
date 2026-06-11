using Battle.Map.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapTopBarButton : MonoBehaviour
    {
        [SerializeField] private MapFlowController    _mapFlowController;
        [SerializeField] private MapOverlayController _mapOverlayController;
        [SerializeField] private Button _button;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
            _mapOverlayController.OnStateChanged += RefreshInteractable;
            RefreshInteractable(_mapOverlayController.State);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            _mapOverlayController.OnStateChanged -= RefreshInteractable;
        }

        private void OnClick()
        {
            var state = _mapOverlayController.State;
            if (state == MapOverlayState.Hidden)
                _mapFlowController.OpenInspect();
            else if (state == MapOverlayState.InspectOnly)
                _mapOverlayController.TryClose();
        }

        private void RefreshInteractable(MapOverlayState state)
        {
            _button.interactable = state == MapOverlayState.Hidden
                                || state == MapOverlayState.InspectOnly;
        }
    }
}
