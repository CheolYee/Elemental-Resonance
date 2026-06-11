using System;
using _02._Scripts.CombatSystem;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class DamageEffect : CardEffect
    {
        public int damage;

        public override int BaseValue => damage;

        public override void Apply(GameObject source, GameObject target, int finalValue)
        {
            if (target == null) return;
            var damageable = target.GetComponent<IDamageable>()
                ?? target.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(new DamageData(finalValue, Vector3.zero, Vector3.zero, null, false));
        }
    }
}
