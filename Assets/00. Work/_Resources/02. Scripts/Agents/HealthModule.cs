using System;
using _00._Work._Resources._02._Scripts.Agents.StatSystem;
using _00._Work._Resources._02._Scripts.Modules;
using Agents.StatSystem;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class HealthModule : MonoBehaviour, IModule, IAfterInitModule
    {
        public event Action OnDeath;
        public event Action<int, int> OnHpChanged;
        public event Action<int, int> OnDamageTaken; // (damageToHp, damageToBlock)
        public event Action<int> OnBlockChanged;      // (currentBlock)
        
        [SerializeField] private StatSO hpStatSO;

        private StatModule _statModule;

        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public int Block { get; private set; }
        public bool IsDead { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _statModule = owner.GetModule<StatModule>();
        }

        public void AfterInit()
        {
            MaxHp = (int)_statModule.GetStat(hpStatSO.AssetIndex).Value;
            CurrentHp = MaxHp;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            int damageToBlock = Mathf.Min(amount, Block);
            int damageToHp = Mathf.Max(0, amount - Block);
            Block = Mathf.Max(0, Block - amount);

            if (damageToHp > 0)
                CurrentHp = Mathf.Max(0, CurrentHp - damageToHp);

            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnDamageTaken?.Invoke(damageToHp, damageToBlock);
            if (damageToBlock > 0)
                OnBlockChanged?.Invoke(Block);

            if (CurrentHp == 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public void AddBlock(int amount)
        {
            Block += amount;
            OnBlockChanged?.Invoke(Block);
        }

        public void ResetBlock()
        {
            if (Block == 0) return;
            Block = 0;
            OnBlockChanged?.Invoke(Block);
        }

        public void Reinitialize()
        {
            MaxHp = (int)_statModule.GetStat(hpStatSO.AssetIndex).Value;
            CurrentHp = MaxHp;
            Block = 0;
            IsDead = false;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void InitializeHp(int currentHp, int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = Mathf.Clamp(currentHp, 0, maxHp);
            Block = 0;
            IsDead = CurrentHp <= 0;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
