using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleSlowMotionController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private float slowMotionTimeScale = 0.2f;

        private bool _isSkillPresenting;
        private bool _isTargeting;

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            Time.timeScale = 1f;
        }

        private void OnSkillExecutionStart(SkillExecutionStartEvent _) { _isSkillPresenting = true;  UpdateTimeScale(); }
        private void OnSkillExecutionEnd(SkillExecutionEndEvent _)     { _isSkillPresenting = false; UpdateTimeScale(); }
        private void OnTargetingStart(CardTargetingStartEvent _)       { _isTargeting = true;        UpdateTimeScale(); }
        private void OnTargetingEnd(CardTargetingEndEvent _)           { _isTargeting = false;       UpdateTimeScale(); }
        private void OnBattleEnded(BattleVictoryEvent _)               => ResetTimeScale();
        private void OnBattleEnded(BattleDefeatEvent _)                => ResetTimeScale();

        private void UpdateTimeScale()
            => Time.timeScale = (_isSkillPresenting && _isTargeting) ? slowMotionTimeScale : 1f;

        private void ResetTimeScale()
        {
            _isSkillPresenting = false;
            _isTargeting = false;
            Time.timeScale = 1f;
        }
    }
}
