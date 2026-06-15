using Battle.Enums;
using Battle.Events;
using Battle.Map.Enums;
using Battle.Map.UI;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class CardPileButton : MonoBehaviour
    {
        [SerializeField] private EventChannelSO       battleEventChannel;
        [SerializeField] private MapOverlayController mapOverlayController;
        [SerializeField] private PileDisplayTarget    pileTarget;

        private Button _button;
        private bool   _battleEnded;
        private bool   _isMapOpen;

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.AddListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.AddListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            if (mapOverlayController != null)
                mapOverlayController.OnStateChanged += OnMapStateChanged;
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.RemoveListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.RemoveListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            if (mapOverlayController != null)
                mapOverlayController.OnStateChanged -= OnMapStateChanged;
        }

        private void OnClick()
        {
            _button.interactable = false;
            battleEventChannel.RaiseEvent(new PileDetailPanelOpenedEvent(pileTarget));
        }

        private void OnMapStateChanged(MapOverlayState state)
        {
            _isMapOpen = state != MapOverlayState.Hidden;
            if (_isMapOpen) _button.interactable = false;
            else if (!_battleEnded) _button.interactable = true;
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _isMapOpen = false; }
        private void OnPanelClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnBattleUIHidden(BattleUIHiddenEvent _) => _button.interactable = false;
        private void OnBattleUIShown(BattleUIShownEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnCardDrawStart(CardDrawStartEvent _) => _button.interactable = false;
        private void OnCardDrawEnd(CardDrawEndEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnWaveClear(WaveClearEvent _) => _button.interactable = false;
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; _button.interactable = false; }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; _button.interactable = false; }
    }
}
