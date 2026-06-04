using _02._Scripts.CombatSystem.Skills;
using Battle.Data;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleActionExecutor : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private SkillModule playerSkillModule;
        [SerializeField] private DeckController deckController;

        private bool _isExecuting;

        private void OnEnable()
            => battleEventChannel.AddListener<CardDroppedOnTargetEvent>(OnCardDropped);

        private void OnDisable()
            => battleEventChannel.RemoveListener<CardDroppedOnTargetEvent>(OnCardDropped);

        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            if (_isExecuting) return;
            ExecuteCardAsync(evt).Forget();
        }

        private async UniTaskVoid ExecuteCardAsync(CardDroppedOnTargetEvent evt)
        {
            _isExecuting = true;

            var card = evt.CardInstance;
            var targetGo = evt.Target?.gameObject;

            costModel.currentCost -= card.data.cost;
            battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));
            battleEventChannel.RaiseEvent(new SkillExecutionStartEvent());

            var data = SkillUsageData.FromCard(card);
            await playerSkillModule.UseSkillAsync(data, targetGo, destroyCancellationToken);

            deckController.Discard(card);
            battleEventChannel.RaiseEvent(new SkillExecutionEndEvent());
            _isExecuting = false;
        }
    }
}
