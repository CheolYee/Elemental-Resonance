using Battle.Enums;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class CardPileButton : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private PileDisplayTarget pileTarget;

        private Button _button;
        private bool _battleEnded;

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnUnlock);
            battleEventChannel.AddListener<WaveClearEvent>(OnLock);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnUnlock);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnLock);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnClick()
        {
            _button.interactable = false;
            battleEventChannel.RaiseEvent(new PileDetailPanelOpenedEvent(pileTarget));
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _button.interactable = true; }
        private void OnPanelClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded) _button.interactable = true; }
        private void OnLock(SkillExecutionStartEvent _) => _button.interactable = false;
        private void OnUnlock(SkillExecutionEndEvent _) { if (!_battleEnded) _button.interactable = true; }
        private void OnLock(CardDrawStartEvent _) => _button.interactable = false;
        private void OnUnlock(CardDrawEndEvent _) { if (!_battleEnded) _button.interactable = true; }
        private void OnLock(WaveClearEvent _) => _button.interactable = false;
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; _button.interactable = false; }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; _button.interactable = false; }
    }
}
