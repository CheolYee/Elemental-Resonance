using UnityEngine;

namespace Battle.Effects
{
    [CreateAssetMenu(fileName = "New Block Effect", menuName = "Battle/Effects/Block", order = 1)]
    public class BlockEffectSO : CardEffectSO
    {
        public int block;
    }
}
