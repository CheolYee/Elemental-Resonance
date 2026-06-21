using System;
using System.Collections.Generic;
using _02._Scripts.CombatSystem.Skills;
using Battle.Effects;
using Battle.Enums;
using Battle.Presentation;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "New Card Data", menuName = "Battle/Card Data", order = 2)]
    public class CardDataSO : ScriptableObject
    {
        public string cardId;
        public string cardName;
        public int cost;
        public Sprite artwork;
        [TextArea] public string description;
        public CardTargetType targetType;
        public CardDisposePolicy disposePolicy;
        public ElementType elementType;
        public CardGrade grade;
        public CardType cardType;
        public List<CardEffectSlot> effectSlots;
        public SkillPresentationDataSO presentationData;

        private void OnValidate()
        {
            EnsureEffectSlotIds();
        }

        public bool EnsureEffectSlotIds()
        {
            if (effectSlots == null) return false;

            bool changed = false;
            var seen = new HashSet<string>();

            foreach (var slot in effectSlots)
            {
                if (slot == null) continue;
                // 빈 ID이거나 이미 등록된 중복 ID면 재생성 (첫 번째 것은 유지, 이후 중복만 교체)
                if (string.IsNullOrEmpty(slot.effectSlotId) || !seen.Add(slot.effectSlotId))
                {
                    slot.effectSlotId = Guid.NewGuid().ToString();
                    seen.Add(slot.effectSlotId);
                    changed = true;
                }
            }

            return changed;
        }

        public bool CanAfford(int currentCost) => currentCost >= cost;

        public bool IsConditionMet(int currentCost)
        {
            if (effectSlots == null) return true;
            foreach (var slot in effectSlots)
            {
                if (slot.effect is CostGainEffect cge &&
                    cge.condition == CostGainConditionType.CurrentCostLessOrEqual &&
                    currentCost > cge.conditionValue)
                    return false;
            }
            return true;
        }
    }
}
