using System;
using Battle.Enums;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class CostGainEffect : CardEffect
    {
        public int costGain;
        public CostGainConditionType condition;
        public int conditionValue;

        public override int BaseValue => costGain;

        public override void Apply(GameObject source, GameObject target, int finalValue)
        {
            // SP-3에서 구현
        }
    }
}
