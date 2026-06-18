using System.Collections.Generic;
using Battle.Data;
using Battle.Enums;
using Battle.Instances;
using DeckBuilding;
using UnityEngine;

namespace Battle.Fusion
{
    public class FusionResultPicker
    {
        private readonly CardDatabaseSO _database;
        private readonly GradeWeightTableSO _weightTable;

        public FusionResultPicker(CardDatabaseSO database, GradeWeightTableSO weightTable)
        {
            _database = database;
            _weightTable = weightTable;
        }

        // 드래그 호버 시 확률 표시용 — 가중치 배열 반환 [Normal, Rare, Epic, Legendary]
        public int[] GetEffectiveWeights(CardGrade maxMaterialGrade, ElementType resultElement)
        {
            var pool = BuildPool(resultElement);
            int[] weights = _weightTable.GetWeights(maxMaterialGrade);

            // 해당 원소 카드가 없으면 등급 필터 없이 기본 가중치 그대로 반환
            if (pool.Count == 0)
                return weights;

            var available = new HashSet<CardGrade>();
            foreach (var c in pool) available.Add(c.grade);

            var grades = new[] { CardGrade.Normal, CardGrade.Rare, CardGrade.Epic, CardGrade.Legendary };
            var result = new int[4];
            for (int i = 0; i < grades.Length; i++)
                result[i] = available.Contains(grades[i]) ? weights[i] : 0;
            return result;
        }

        public CardInstance Pick(ElementType element, CardGrade maxMaterialGrade)
        {
            var pool = BuildPool(element);
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[FusionResultPicker] 원소 {element}에 해당하는 카드가 없습니다.");
                return null;
            }

            var grade = DrawGrade(maxMaterialGrade, pool);
            var candidates = pool.FindAll(c => c.grade == grade);
            if (candidates.Count == 0) candidates = pool;

            var selected = candidates[Random.Range(0, candidates.Count)];
            return new CardInstance(selected, selected.grade);
        }

        private List<CardDataSO> BuildPool(ElementType element)
        {
            var result = new List<CardDataSO>();
            foreach (var card in _database.allCards)
            {
                if (card != null && card.elementType == element)
                    result.Add(card);
            }
            return result;
        }

        private CardGrade DrawGrade(CardGrade basedOnGrade, List<CardDataSO> pool)
        {
            var available = new HashSet<CardGrade>();
            foreach (var c in pool) available.Add(c.grade);

            var grades = new[] { CardGrade.Normal, CardGrade.Rare, CardGrade.Epic, CardGrade.Legendary };
            int[] weights = _weightTable.GetWeights(basedOnGrade);

            int total = 0;
            for (int i = 0; i < grades.Length; i++)
            {
                if (available.Contains(grades[i])) total += weights[i];
            }

            if (total == 0) return CardGrade.Normal;

            int roll = Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < grades.Length; i++)
            {
                if (!available.Contains(grades[i])) continue;
                acc += weights[i];
                if (roll < acc) return grades[i];
            }

            return CardGrade.Normal;
        }
    }
}
