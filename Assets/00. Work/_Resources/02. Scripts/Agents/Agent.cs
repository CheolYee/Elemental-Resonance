using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using _02._Scripts.CombatSystem;
using UnityEngine.Events;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public abstract class Agent : ModuleOwner, IDamageable
    {
        public UnityEvent onHit;
        public UnityEvent onDeath;
        public HealthModule Health { get; private set; }
        public ActionDataModule ActionData { get; private set; }
        public IRenderer Renderer { get; private set; }
        public StateMachine StateMachine { get; protected set; }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            Health = GetModule<HealthModule>();
            ActionData = GetModule<ActionDataModule>();
            Renderer = GetModule<IRenderer>();
        }
        protected override void AfterInitComponents()
        {
            base.AfterInitComponents();
            Health.OnDeath += HandleDeath;
            onHit.AddListener(HandleHitEvent);
        }

        protected virtual void OnDestroy()
        {
            Health.OnDeath -= HandleDeath;
            onHit.RemoveListener(HandleHitEvent);
        }

        protected virtual void HandleHitEvent() { }

        private void HandleDeath()
        {
            onDeath.Invoke();
        }

        public void TakeDamage(DamageData data)
        {
            if (Health.IsDead) return;
            if (ActionData != null)
            {
                ActionData.LastHitPoint = data.HitPoint;
                ActionData.LastHitNormal = data.HitNormal;
                ActionData.Attacker = data.Attacker;
            }
            
            onHit?.Invoke();
            
            Health?.TakeDamage(data.DamageAmount);
        }

        public void AddBlock(int amount) => Health.AddBlock(amount);
        public void ResetBlock() => Health.ResetBlock();
        public void Reinitialize() => Health.Reinitialize();
    }
}