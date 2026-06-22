using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "RewardConfig", menuName = "Battle/Reward Config")]
    public class RewardConfigSO : ScriptableObject
    {
        [Header("Gold Formula")]
        public int baseGold       = 25;
        public int perFloorBonus  = 6;
        public int randomVariance = 8;

        [Header("Grade Weights by Floor")]
        public List<FloorGradeWeight> gradeWeightTable = new()
        {
            new FloorGradeWeight { floorFrom = 0, floorTo = 2,  normalWeight = 60, rareWeight = 30, epicWeight = 8,  legendaryWeight = 2  },
            new FloorGradeWeight { floorFrom = 3, floorTo = 5,  normalWeight = 40, rareWeight = 40, epicWeight = 15, legendaryWeight = 5  },
            new FloorGradeWeight { floorFrom = 6, floorTo = 8,  normalWeight = 20, rareWeight = 35, epicWeight = 30, legendaryWeight = 15 },
            new FloorGradeWeight { floorFrom = 9, floorTo = 99, normalWeight = 10, rareWeight = 25, epicWeight = 35, legendaryWeight = 30 },
        };

        [Header("Elite Bonus")]
        public float eliteGoldMultiplier = 2f;

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

        public int CalculateGold(int floorIndex, int goldBonus = 0, RewardProfile profile = RewardProfile.Normal)
        {
            int variance = randomVariance > 0
                ? UnityEngine.Random.Range(-randomVariance, randomVariance + 1)
                : 0;
            int raw = Mathf.Max(0, baseGold + floorIndex * perFloorBonus + goldBonus + variance);
            if (profile == RewardProfile.Elite)
                raw = Mathf.RoundToInt(raw * eliteGoldMultiplier);
            return raw;
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
