using UnityEngine;

namespace Battle.Effects
{
    public abstract class CardEffectSO : ScriptableObject
    {
        public bool isRepeat = true;
        public abstract void Apply(GameObject source, GameObject target);
    }
}
