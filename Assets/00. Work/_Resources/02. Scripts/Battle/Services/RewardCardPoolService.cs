using System.Collections.Generic;
using System.Linq;
using Battle.Data;
using Battle.Enums;
using DeckBuilding;
using UnityEngine;

namespace Battle.Services
{
    public class RewardCardPoolService : MonoBehaviour
    {
        [SerializeField] private CardDatabaseSO _database;
        [SerializeField] private RewardConfigSO _rewardConfig;

        private static readonly HashSet<ElementType> FusionResultElements = new()
        {
            ElementType.Steam, ElementType.Storm, ElementType.Twilight,
            ElementType.Poison, ElementType.Holy
        };

        public List<CardDataSO> DrawCards(int count, int floorIndex)
        {
            var pool = _database.allCards
                .Where(c => !FusionResultElements.Contains(c.elementType))
                .ToList();

            int[] weights = _rewardConfig.GetGradeWeights(floorIndex);
            var result = new List<CardDataSO>();
            var drawn = new HashSet<CardDataSO>();

            for (int i = 0; i < count; i++)
            {
                var grade = PickGrade(weights);
                var candidates = pool.Where(c => c.grade == grade && !drawn.Contains(c)).ToList();
                if (candidates.Count == 0) continue;
                var card = candidates[Random.Range(0, candidates.Count)];
                drawn.Add(card);
                result.Add(card);
            }

            return result;
        }

        private static CardGrade PickGrade(int[] weights)
        {
            int total = 0;
            foreach (var w in weights) total += w;
            int roll = Random.Range(0, total);
            int acc = 0;
            var grades = new[] { CardGrade.Normal, CardGrade.Rare, CardGrade.Epic, CardGrade.Legendary };
            for (int i = 0; i < weights.Length && i < grades.Length; i++)
            {
                acc += weights[i];
                if (roll < acc) return grades[i];
            }
            return CardGrade.Normal;
        }
    }
}
