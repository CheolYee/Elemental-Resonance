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

            int damageToHp = amount - Block;
            Block = Mathf.Max(0, Block - amount);

            if (damageToHp > 0)
                CurrentHp = Mathf.Max(0, CurrentHp - damageToHp);

            if (CurrentHp == 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }

        public void AddBlock(int amount)
        {
            Block += amount;
        }

        public void ResetBlock()
        {
            Block = 0;
        }

        public void Reinitialize()
        {
            MaxHp = (int)_statModule.GetStat(hpStatSO.AssetIndex).Value;
            CurrentHp = MaxHp;
            Block = 0;
            IsDead = false;
        }
    }
}
