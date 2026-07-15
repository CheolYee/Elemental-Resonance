using _00._Work._Resources._02._Scripts.Agents.Players;
using Battle.Data;
using Battle.Events;
using Gamelib.EventSystem;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class BattleResultController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;
        [SerializeField] private PlayerRunStateSO playerRunState;

        [Inject] private Player _player;

        private int _livingEnemyCount;
        private bool _isExecuting;
        private bool _pendingWaveClear;
        private bool _battleEnded;

        private void OnEnable()
        {
            //이벤트 채널로 구독하여 발행자를 모르고도 구독할 수 있어 의존이 분리된다
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillEnd);
            battleEventChannel.AddListener<EnemiesUpdatedEvent>(OnEnemiesUpdated);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            //씬 전환이나 오브젝트가 꺼질 때 해제하지 않으면 이벤트가 죽은 객체를 참조하게 된다
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
            //보류해뒀던 클리어가 있으면 연출이 끝난 이 시점에 처리한다
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

        private void OnBattleEnded(BattleVictoryEvent _)
        {
            _battleEnded = true;
            if (playerRunState != null && _player != null)
                playerRunState.SetHp(_player.Health.CurrentHp, _player.Health.MaxHp);
        }

        private void OnEnemyDeathStarted()
        {
            _livingEnemyCount--;
            if (_livingEnemyCount > 0 || _battleEnded) return;

            //스킬 연출 중에 클리어를 처리하면 UI가 겹쳐서 보류했다가 연출이 끝나면 처리한다
            if (_isExecuting) _pendingWaveClear = true;
            else ResolveWaveClear();
        }

        private void ResolveWaveClear()
        {
            _pendingWaveClear = false;
            //WaveClearEvent만 발행하고 다음 웨이브인지 클리어인지는 StageBootstrapper가 판단한다
            battleEventChannel.RaiseEvent(new WaveClearEvent());
        }
    }
}
