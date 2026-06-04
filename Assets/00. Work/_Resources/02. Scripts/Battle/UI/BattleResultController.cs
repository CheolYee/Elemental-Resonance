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
        private bool _pendingVictory;
        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillEnd);
            battleEventChannel.AddListener<EnemiesUpdatedEvent>(OnEnemiesUpdated);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillEnd);
            battleEventChannel.RemoveListener<EnemiesUpdatedEvent>(OnEnemiesUpdated);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDestroy()
        {
            foreach (var enemy in enemyRegistry.Enemies)
                if (enemy != null) enemy.OnDeathAnimationComplete -= OnEnemyDeathAnimationComplete;
        }

        private void OnEnemiesUpdated(EnemiesUpdatedEvent _)
        {
            _livingEnemyCount = 0;
            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (enemy == null) continue;
                enemy.OnDeathAnimationComplete += OnEnemyDeathAnimationComplete;
                _livingEnemyCount++;
            }
        }

        private void OnSkillStart(SkillExecutionStartEvent _) => _isExecuting = true;

        private void OnSkillEnd(SkillExecutionEndEvent _)
        {
            _isExecuting = false;
            if (_pendingVictory) ResolveVictory();
        }

        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        private void OnEnemyDeathAnimationComplete()
        {
            _livingEnemyCount--;
            if (_livingEnemyCount > 0 || _battleEnded) return;

            if (_isExecuting) _pendingVictory = true;
            else ResolveVictory();
        }

        private void ResolveVictory()
        {
            _battleEnded = true;
            battleEventChannel.RaiseEvent(new BattleVictoryEvent());
        }
    }
}
