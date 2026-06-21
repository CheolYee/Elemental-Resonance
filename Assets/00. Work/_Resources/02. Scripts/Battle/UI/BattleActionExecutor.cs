using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using _00._Work._Resources._02._Scripts.Agents.Players;
using _02._Scripts.CombatSystem.Skills;
using Battle.Data;
using Battle.Effects;
using Battle.Enums;
using Battle.Events;
using Battle.Instances;
using Battle.Presentation;
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
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;

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
            costModel.currentCost += ResolveCostGain(evt.CardInstance);
            battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));
            _queue.Enqueue(evt);
            RaiseQueueChanged(currentCard: null);

            if (!_isRunning)
                RunQueueAsync().Forget();
        }

        // CostGainEffect는 연출(키프레임) 시점이 아니라 카드 적재 시점에 즉시 반영한다.
        // 큐 대기 중에는 currentCost가 미래 값을 미리 반영해야 동일 조건 카드의 연속 사용을 막을 수 있다.
        private static int ResolveCostGain(CardInstance card)
        {
            var data = card.data;
            if (data.effectSlots == null || data.presentationData == null) return 0;
            if (!data.presentationData.TryGetPlayableTimeline(out var timeline)) return 0;

            int total = 0;
            foreach (var slot in data.effectSlots)
            {
                if (slot?.effect is not CostGainEffect cge) continue;

                foreach (var keyframe in timeline.effectTrack.keyframes)
                {
                    if (keyframe.property != SkillKeyframeProperty.EffectSlot) continue;
                    if (keyframe.effectSlotId != slot.effectSlotId) continue;
                    total += CardEffectValueCalculator.Calculate(cge.BaseValue, keyframe.valueMultiplier);
                }
            }
            return total;
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

                List<Agent> allTargets = null;
                System.Func<Agent> randomTargetResolver = null;

                if (card.data.targetType == CardTargetType.AllEnemies && enemyRegistry != null)
                {
                    allTargets = new List<Agent>();
                    foreach (var enemy in enemyRegistry.Enemies)
                        if (enemy != null) allTargets.Add(enemy);
                }
                else if (card.data.targetType == CardTargetType.RandomEnemy && enemyRegistry != null)
                {
                    var reg = enemyRegistry;
                    randomTargetResolver = () =>
                    {
                        var alive = new List<AbstractEnemy>();
                        foreach (var e in reg.Enemies)
                            if (e != null && !e.Health.IsDead) alive.Add(e);
                        return alive.Count > 0 ? alive[UnityEngine.Random.Range(0, alive.Count)] : null;
                    };
                    targetGo = randomTargetResolver()?.gameObject;
                }

                RaiseQueueChanged(currentCard: card);
                battleEventChannel.RaiseEvent(new SkillExecutionStartEvent());
                var data = SkillUsageData.FromCard(card);
                var fizzleToken = new SkillFizzleToken();
                await _playerSkillModule.UseSkillAsync(data, targetGo, destroyCancellationToken, allTargets, randomTargetResolver, fizzleToken);

                if (fizzleToken.IsFizzled)
                {
                    costModel.currentCost += card.data.cost;
                    battleEventChannel.RaiseEvent(new CostChangedEvent(costModel.currentCost));
                    deckController.ForceDiscard(card);
                }
                else
                {
                    deckController.UseCard(card);
                }
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
