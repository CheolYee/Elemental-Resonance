using System.Collections.Generic;
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
        private readonly Queue<CardDroppedOnTargetEvent> _queue = new();
        private bool _isRunning;
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

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _queue.Clear(); _isRunning = false; }
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; _queue.Clear(); }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; _queue.Clear(); }

        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            if (_battleEnded) return;

            costModel.currentCost -= evt.CardInstance.data.cost;
            battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));
            _queue.Enqueue(evt);
            RaiseQueueChanged(currentCard: null);

            if (!_isRunning)
                RunQueueAsync().Forget();
        }

        private async UniTaskVoid RunQueueAsync()
        {
            _isRunning = true;
            battleEventChannel.RaiseEvent(new SkillQueueStartedEvent());

            while (_queue.Count > 0 && !_battleEnded)
            {
                var evt      = _queue.Dequeue();
                var card     = evt.CardInstance;
                var targetGo = evt.Target?.gameObject;

                RaiseQueueChanged(currentCard: card);
                battleEventChannel.RaiseEvent(new SkillExecutionStartEvent());
                var data = SkillUsageData.FromCard(card);
                await _playerSkillModule.UseSkillAsync(data, targetGo, destroyCancellationToken);
                deckController.UseCard(card);
                battleEventChannel.RaiseEvent(new SkillExecutionEndEvent());
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), DelayType.DeltaTime, PlayerLoopTiming.Update, destroyCancellationToken);
            }

            _isRunning = false;
            RaiseQueueChanged(currentCard: null);
            battleEventChannel.RaiseEvent(new SkillQueueCompletedEvent());
        }

        private void RaiseQueueChanged(CardInstance currentCard)
        {
            var pending = new List<CardInstance>();
            foreach (var q in _queue)
                pending.Add(q.CardInstance);
            battleEventChannel.RaiseEvent(new SkillQueueChangedEvent(currentCard, pending));
        }
    }
}
