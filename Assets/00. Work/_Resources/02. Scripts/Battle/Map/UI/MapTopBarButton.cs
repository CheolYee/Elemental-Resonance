using Battle.Events;
using Battle.Map.Enums;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class MapTopBarButton : MonoBehaviour
    {
        [SerializeField] private MapFlowController    _mapFlowController;
        [SerializeField] private MapOverlayController _mapOverlayController;
        [SerializeField] private EventChannelSO       _battleEventChannel;
        [SerializeField] private Button _button;

        [Header("Sound")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private SfxSounds      _mapOpenSound;
        [SerializeField] private SfxSounds      _mapCloseSound;

        private bool _isPileOpen;
        private bool _isSessionStarting;

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
            _mapOverlayController.OnStateChanged += OnMapStateChanged;
            _battleEventChannel.AddListener<PileDetailPanelOpenedEvent>(OnPileOpened);
            _battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPileClosed);
            _battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            _battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            Refresh();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            _mapOverlayController.OnStateChanged -= OnMapStateChanged;
            _battleEventChannel.RemoveListener<PileDetailPanelOpenedEvent>(OnPileOpened);
            _battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPileClosed);
            _battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            _battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
        }

        private void OnClick()
        {
            var state = _mapOverlayController.State;
            if (state == MapOverlayState.Hidden)
            {
                _soundChannel?.RaiseEvent(new PlaySoundEvent(_mapOpenSound, Vector3.zero));
                _mapFlowController.OpenInspect();
            }
            else if (state == MapOverlayState.InspectOnly)
            {
                _soundChannel?.RaiseEvent(new PlaySoundEvent(_mapCloseSound, Vector3.zero));
                _mapOverlayController.TryClose();
            }
        }

        private void OnMapStateChanged(MapOverlayState _) => Refresh();
        private void OnPileOpened(PileDetailPanelOpenedEvent _) { _isPileOpen = true;  Refresh(); }
        private void OnPileClosed(PileDetailPanelClosedEvent _) { _isPileOpen = false; Refresh(); }
        private void OnSessionStart(BattleSessionStartEvent _)  { _isSessionStarting = true;  Refresh(); }
        private void OnCardDrawEnd(CardDrawEndEvent _)           { _isSessionStarting = false; Refresh(); }

        private void Refresh()
        {
            var state = _mapOverlayController.State;
            bool mapOk = state == MapOverlayState.Hidden || state == MapOverlayState.InspectOnly;
            _button.interactable = mapOk && !_isPileOpen && !_isSessionStarting;
        }
    }
}
