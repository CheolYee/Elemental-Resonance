using System;
using UnityEngine;

namespace Agents.StatSystem
{
    [Serializable]
    public class StatOverride
    {
        [field: SerializeField] public StatSO StatData { get; private set; }
        [SerializeField] private bool isUseOverride;
        [SerializeField] private float overrideValue;

        public StatOverride(StatSO stat) => this.StatData = stat; //생성자

        public StatSO CreateStat()
        {
            StatSO newStat = StatData.Clone() as StatSO;

            Debug.Assert(newStat != null, $"StatSO Clone Error : check {StatData.StatName}");
            
            if (isUseOverride)
                newStat.BaseValue = overrideValue;
            return newStat;
        }
    }
}