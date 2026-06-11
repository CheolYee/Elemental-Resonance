using System;
using _00._Work._Resources._02._Scripts.Agents;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class BlockEffect : CardEffect
    {
        public int block;

        public override int BaseValue => block;

        public override void Apply(GameObject source, GameObject target, int finalValue)
        {
            if (source == null) return;
            var agent = source.GetComponent<Agent>()
                ?? source.GetComponentInParent<Agent>();
            agent?.AddBlock(finalValue);
        }
    }
}
