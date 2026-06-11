using System;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public abstract class CardEffect
    {
        public bool isRepeat = true;
        public abstract int BaseValue { get; }
        public abstract void Apply(GameObject source, GameObject target, int finalValue);
    }
}
