using System.Collections.Generic;
using System.Linq;
using _00._Work._Resources._02._Scripts.Modules;
using Agents.StatSystem;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.StatSystem
{
    public class StatModule : MonoBehaviour, IModule, IStatModule
    {
        [SerializeField] private StatOverride[] statOverrides;
        
        private ModuleOwner _owner;
        private Dictionary<int, StatSO> _statDict; //진짜 스탯이 저장되는 딕셔너리
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            //새로운 스탯 복제본들로 딕셔너리를 만들어준다.
            _statDict = statOverrides.ToDictionary(stat => stat.StatData.AssetIndex, stat => stat.CreateStat());
        }

        public StatSO[] GetAllStats() => _statDict.Values.ToArray();

        public StatSO GetStat(int index) => _statDict.GetValueOrDefault(index);

        public bool TryGetStat(int index, out StatSO outStat) => _statDict.TryGetValue(index, out outStat);

        public void AddModifier(int index, object key, float value)
        {
            if (_statDict.TryGetValue(index, out StatSO stat))
            {
                stat.AddModifier(key, value);
                return;
            }
            Debug.LogError($"Stat with index {index} not found");
        }

        public void RemoveModifier(int index, object key)
        {
            if (_statDict.TryGetValue(index, out StatSO stat))
            {
                stat.RemoveModifier(key);
                return;
            }
            Debug.LogError($"Stat with index {index} not found");
        }

        public float SubscribeStat(int index, StatSO.ValueChangeHandler handler, float defaultValue)
        {
            if (_statDict.TryGetValue(index, out StatSO stat))
            {
                stat.OnValueChanged += handler;
                return stat.Value;
            }

            return defaultValue;
        }
        
        public void UnSubscribeStat(int index, StatSO.ValueChangeHandler handler)
        {
            if (_statDict.TryGetValue(index, out StatSO stat))
            {
                stat.OnValueChanged -= handler;
            }
        }
    }
}