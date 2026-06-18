using _00._Work._Resources._02._Scripts.Agents.Players;
using _02._Scripts.CombatSystem.Skills;
using Battle.Data;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class BattleTurnController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;

        [Inject] private Player _player;

        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEnd);
            battleEventChannel.AddListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEnd);
            battleEventChannel.RemoveListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnSessionStart(BattleSessionStartEvent _) => _battleEnded = false;
        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        private void OnPlayerTurnStart(PlayerTurnStartEvent _)
        {
            _player.ResetBlock();
            costModel.currentCost = costModel.baseCost;
            battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));

            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (enemy != null && !enemy.Health.IsDead)
                    enemy.SelectNextAttackCard();
            }
        }

        private void OnPlayerTurnEnd(PlayerTurnEndRequestEvent _)
        {
            if (_battleEnded) return;
            RunEnemyTurnAsync().Forget();
        }

        private async UniTaskVoid RunEnemyTurnAsync()
        {
            foreach (var enemy in enemyRegistry.Enemies)
                enemy?.ResetBlock();

            battleEventChannel.RaiseEvent(new EnemyTurnStartEvent());
            battleEventChannel.RaiseEvent(new SkillExecutionStartEvent());

            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (_player.Health.IsDead) break;
                if (enemy == null || enemy.Health.IsDead) continue;

                var skillModule = enemy.GetModule<SkillModule>();
                if (skillModule == null) continue;

                var data = SkillUsageData.FromEnemyCard(enemy.NextAttackCard);
                if (data == null) continue;
                await skillModule.UseSkillAsync(data, _player.gameObject, destroyCancellationToken);
            }

            if (_player.Health.IsDead) return;

            battleEventChannel.RaiseEvent(new SkillExecutionEndEvent());
            battleEventChannel.RaiseEvent(new PlayerTurnStartEvent());
        }
    }
}
