using System;
using Battle.Enums;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public abstract class CardEffect
    {
        public bool isRepeat = true;
        public abstract int BaseValue { get; }
        public virtual EffectTargetType EffectTarget => EffectTargetType.Target;
        public abstract void Apply(GameObject source, GameObject target, int finalValue);
    }
}
