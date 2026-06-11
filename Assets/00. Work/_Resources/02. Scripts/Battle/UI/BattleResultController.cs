using Battle.Data;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;

        private int _livingEnemyCount;
        private bool _isExecuting;
        private bool _pendingWaveClear;
        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillEnd);
            battleEventChannel.AddListener<EnemiesUpdatedEvent>(OnEnemiesUpdated);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillEnd);
            battleEventChannel.RemoveListener<EnemiesUpdatedEvent>(OnEnemiesUpdated);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
        }

        private void OnDestroy()
        {
            foreach (var enemy in enemyRegistry.Enemies)
                if (enemy != null) enemy.OnDeathStarted -= OnEnemyDeathStarted;
        }

        private void OnEnemiesUpdated(EnemiesUpdatedEvent _)
        {
            _livingEnemyCount = 0;
            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (enemy == null) continue;
                enemy.OnDeathStarted += OnEnemyDeathStarted;
                _livingEnemyCount++;
            }
        }

        private void OnSkillStart(SkillExecutionStartEvent _) => _isExecuting = true;

        private void OnSkillEnd(SkillExecutionEndEvent _)
        {
            _isExecuting = false;
            if (_pendingWaveClear) ResolveWaveClear();
        }

        private void OnSessionStart(BattleSessionStartEvent _)
        {
            _battleEnded = false;
            _isExecuting = false;
            _pendingWaveClear = false;
            _livingEnemyCount = 0;
        }

        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;

        private void OnEnemyDeathStarted()
        {
            _livingEnemyCount--;
            if (_livingEnemyCount > 0 || _battleEnded) return;

            if (_isExecuting) _pendingWaveClear = true;
            else ResolveWaveClear();
        }

        private void ResolveWaveClear()
        {
            _pendingWaveClear = false;
            battleEventChannel.RaiseEvent(new WaveClearEvent());
        }
    }
}
