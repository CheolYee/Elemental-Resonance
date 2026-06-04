using _02._Scripts.CombatSystem;
using UnityEngine;

namespace Battle.Effects
{
    [CreateAssetMenu(fileName = "New Damage Effect", menuName = "Battle/Effects/Damage", order = 0)]
    public class DamageEffectSO : CardEffectSO
    {
        public int damage;

        public override void Apply(GameObject source, GameObject target)
        {
            if (target == null) return;
            var damageable = target.GetComponent<IDamageable>()
                ?? target.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(new DamageData(damage, Vector3.zero, Vector3.zero, null, false));
        }
    }
}
