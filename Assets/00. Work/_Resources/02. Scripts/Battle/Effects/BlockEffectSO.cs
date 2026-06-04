using _00._Work._Resources._02._Scripts.Agents;
using UnityEngine;

namespace Battle.Effects
{
    [CreateAssetMenu(fileName = "New Block Effect", menuName = "Battle/Effects/Block", order = 1)]
    public class BlockEffectSO : CardEffectSO
    {
        public int block;

        public override void Apply(GameObject source, GameObject target)
        {
            if (source == null) return;
            var agent = source.GetComponent<Agent>()
                ?? source.GetComponentInParent<Agent>();
            agent?.AddBlock(block);
        }
    }
}
