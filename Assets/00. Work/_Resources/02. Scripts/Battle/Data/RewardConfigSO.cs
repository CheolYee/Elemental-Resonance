using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "RewardConfig", menuName = "Battle/Reward Config")]
    public class RewardConfigSO : ScriptableObject
    {
        [Header("Gold Formula")]
        public int baseGold       = 50;
        public int perFloorBonus  = 10;
        public int randomVariance = 15;

        [Header("Grade Weights by Floor")]
        public List<FloorGradeWeight> gradeWeightTable = new()
        {
            new FloorGradeWeight { floorFrom = 0, floorTo = 2,  normalWeight = 60, rareWeight = 30, epicWeight = 8,  legendaryWeight = 2  },
            new FloorGradeWeight { floorFrom = 3, floorTo = 5,  normalWeight = 40, rareWeight = 40, epicWeight = 15, legendaryWeight = 5  },
            new FloorGradeWeight { floorFrom = 6, floorTo = 8,  normalWeight = 20, rareWeight = 35, epicWeight = 30, legendaryWeight = 15 },
            new FloorGradeWeight { floorFrom = 9, floorTo = 99, normalWeight = 10, rareWeight = 25, epicWeight = 35, legendaryWeight = 30 },
        };

        [Header("Rest Node")]
        [Range(0f, 1f)]
        public float restHealPercent = 0.3f;

        public int[] GetGradeWeights(int floorIndex)
        {
            foreach (var entry in gradeWeightTable)
            {
                if (floorIndex >= entry.floorFrom && floorIndex <= entry.floorTo)
                    return new[] { entry.normalWeight, entry.rareWeight, entry.epicWeight, entry.legendaryWeight };
            }
            if (gradeWeightTable.Count > 0)
            {
                var last = gradeWeightTable[^1];
                return new[] { last.normalWeight, last.rareWeight, last.epicWeight, last.legendaryWeight };
            }
            return new[] { 60, 30, 8, 2 };
        }

        public int CalculateGold(int floorIndex)
        {
            int variance = randomVariance > 0
                ? UnityEngine.Random.Range(-randomVariance, randomVariance + 1)
                : 0;
            return Mathf.Max(0, baseGold + floorIndex * perFloorBonus + variance);
        }
    }

    [Serializable]
    public struct FloorGradeWeight
    {
        public int floorFrom;
        public int floorTo;
        public int normalWeight;
        public int rareWeight;
        public int epicWeight;
        public int legendaryWeight;
    }
}
