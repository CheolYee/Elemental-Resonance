using UnityEngine;

namespace Battle.Effects
{
    [CreateAssetMenu(fileName = "New Damage Effect", menuName = "Battle/Effects/Damage", order = 0)]
    public class DamageEffectSO : CardEffectSO
    {
        public int damage;
    }
}
