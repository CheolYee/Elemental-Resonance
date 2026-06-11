using _00._Work._Resources._02._Scripts.Agents.Players;
using _02._Scripts.CombatSystem.Skills;
using Battle.Data;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class BattleActionExecutor : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private DeckController deckController;

        [Inject] private Player _player;

        private SkillModule _playerSkillModule;
        private bool _isExecuting;
        private bool _battleEnded;

        private void Start()
        {
            _playerSkillModule = _player.GetModule<SkillModule>();
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _isExecuting = false; }
        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            if (_isExecuting || _battleEnded) return;
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
            await _playerSkillModule.UseSkillAsync(data, targetGo, destroyCancellationToken);

            deckController.UseCard(card);
            battleEventChannel.RaiseEvent(new SkillExecutionEndEvent());
            _isExecuting = false;
        }
    }
}
