using _00._Work._Resources._02._Scripts.Agents.Players;
using _02._Scripts.CombatSystem.Skills;
using Battle.Data;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleTurnController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private Player player;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;

        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEnd);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEnd);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        private void OnPlayerTurnEnd(PlayerTurnEndRequestEvent _)
        {
            if (_battleEnded) return;
            RunEnemyTurnAsync().Forget();
        }

        private async UniTaskVoid RunEnemyTurnAsync()
        {
            battleEventChannel.RaiseEvent(new SkillExecutionStartEvent());

            foreach (var enemy in enemyRegistry.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;

                var skillModule = enemy.GetModule<SkillModule>();
                if (skillModule == null) continue;

                var data = SkillUsageData.FromEnemyData(enemy.EnemyData);
                await skillModule.UseSkillAsync(data, player.gameObject, destroyCancellationToken);
            }

            costModel.currentCost = costModel.maxCost;
            battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));
            battleEventChannel.RaiseEvent(new PlayerTurnStartEvent());
            battleEventChannel.RaiseEvent(new SkillExecutionEndEvent());
        }
    }
}
