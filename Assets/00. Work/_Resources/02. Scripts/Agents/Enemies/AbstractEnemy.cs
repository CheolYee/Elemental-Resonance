using System;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00Work._Resources._02Scripts.Agents.Enemies;
using Battle.Data;
using Agents.FSM;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public abstract class AbstractEnemy : Agent
    {
        [SerializeField] private StateListSO enemyStates;
        [SerializeField] private EnemyDataSO enemyData;

        public EnemyDataSO EnemyData => enemyData;

        private UniTaskCompletionSource _entryCompletion;

        private UniTaskCompletionSource GetOrCreateEntryCompletion()
        {
            _entryCompletion ??= new UniTaskCompletionSource();
            return _entryCompletion;
        }

        protected void InitializeEntry()
        {
            var tcs = GetOrCreateEntryCompletion();
            StateMachine.ChangeState((int)EnemyState.ENTRY, transitionDuration: 0);
            var entryState = StateMachine.CurrentState;
            void OnEntryComplete()
            {
                entryState.OnStateCompleted -= OnEntryComplete;
                ChangeState(EnemyState.IDLE);
                tcs.TrySetResult();
            }
            entryState.OnStateCompleted += OnEntryComplete;
        }

        public UniTask WaitForEntryComplete() => GetOrCreateEntryCompletion().Task;

        public event Action OnDeathStarted;
        public event Action OnDeathAnimationComplete;

        protected override void HandleDeath()
        {
            base.HandleDeath();
            OnDeathStarted?.Invoke();
            ChangeState(EnemyState.DEATH);
            var deathState = StateMachine.CurrentState;
            void OnDeathComplete()
            {
                deathState.OnStateCompleted -= OnDeathComplete;
                OnDeathAnimationComplete?.Invoke();
                GetModule<DissolveModule>()?.StartDissolve(() => Destroy(gameObject));
            }
            deathState.OnStateCompleted += OnDeathComplete;
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            StateMachine = new StateMachine(this, enemyStates.states);
        }

        protected override void HandleHitEvent()
        {
            if (StateMachine == null) return;
            ChangeState(EnemyState.HIT);
            var hitState = StateMachine.CurrentState;
            void OnHitComplete()
            {
                hitState.OnStateCompleted -= OnHitComplete;
                if (!Health.IsDead)
                    ChangeState(EnemyState.IDLE);
            }
            hitState.OnStateCompleted += OnHitComplete;
        }
        
        public void ChangeState(EnemyState state, float transitionDuration = 0.1f)
            => StateMachine.ChangeState((int)state, transitionDuration);
    }
}
