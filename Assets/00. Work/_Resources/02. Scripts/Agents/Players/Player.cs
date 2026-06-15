using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00Work._Resources._02Scripts.Agents.Players;
using Agents.FSM;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Systems;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Players
{
    public class Player : Agent
    {
        [Header("Player values")]
        [field: SerializeField] public EventChannelSO PlayerEventChannel { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO playerStates;
        [SerializeField] private EventChannelSO battleEventChannel;

        private UniTaskCompletionSource _entryCompletion;
        private bool _isSkillPresenting;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            StateMachine = new StateMachine(this, playerStates.states);
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
        }

        private void Start()
        {
            StateMachine.ChangeState((int)PlayerState.IDLE, transitionDuration: 0);
        }

        public UniTask BeginEntryAsync()
        {
            _entryCompletion = new UniTaskCompletionSource();
            StateMachine.ChangeState((int)PlayerState.ENTRY, transitionDuration: 0);
            var entryState = StateMachine.CurrentState;
            void OnEntryComplete()
            {
                entryState.OnStateCompleted -= OnEntryComplete;
                StateMachine.ChangeState((int)PlayerState.IDLE);
                _entryCompletion.TrySetResult();
            }
            entryState.OnStateCompleted += OnEntryComplete;
            return _entryCompletion.Task;
        }

        protected override void HandleDeath()
        {
            base.HandleDeath();
            StateMachine.ChangeState((int)PlayerState.DEATH);
            var deathState = StateMachine.CurrentState;
            void OnDeathComplete()
            {
                deathState.OnStateCompleted -= OnDeathComplete;
                battleEventChannel.RaiseEvent(new BattleDefeatEvent());
            }
            deathState.OnStateCompleted += OnDeathComplete;
        }

        private void Update()
        {
            StateMachine.UpdateMachine();
        }

        protected override void HandleHitEvent()
        {
            if (StateMachine == null) return;
            StateMachine.ChangeState((int)PlayerState.HIT);
            var hitState = StateMachine.CurrentState;
            void OnHitComplete()
            {
                hitState.OnStateCompleted -= OnHitComplete;
                if (!Health.IsDead)
                    StateMachine.ChangeState((int)PlayerState.IDLE);
            }
            hitState.OnStateCompleted += OnHitComplete;
        }

        private void OnSkillExecutionStart(SkillExecutionStartEvent _) => _isSkillPresenting = true;
        private void OnSkillExecutionEnd(SkillExecutionEndEvent _) => _isSkillPresenting = false;

        private void OnTargetingStart(CardTargetingStartEvent _)
        {
            if (_isSkillPresenting) return;
            StateMachine.ChangeState((int)PlayerState.TARGETING);
        }

        private void OnTargetingEnd(CardTargetingEndEvent _)
        {
            if (_isSkillPresenting) return;
            StateMachine.ChangeState((int)PlayerState.IDLE);
        }
        
        public void ChangeState(PlayerState newStateIndex, float transitionDuration = 0.1f)
            => StateMachine.ChangeState((int)newStateIndex, transitionDuration);
    }
}