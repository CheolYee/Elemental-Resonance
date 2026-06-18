using System.Collections.Generic;
using Battle.Data;
using Battle.Effects;
using Battle.Enums;
using Battle.Instances;
using Battle.Presentation;

namespace _02._Scripts.CombatSystem.Skills
{
    public class SkillUsageData
    {
        public SkillDataSO SkillData { get; }
        public int RepeatCount { get; }
        public List<CardEffect> Effects { get; }
        public CardInstance CardInstance { get; }
        public SkillPresentationDataSO PresentationData { get; }
        public CardGrade Grade => CardInstance?.grade ?? CardGrade.Normal;

        public SkillUsageData(
            SkillDataSO skillData,
            int repeatCount,
            List<CardEffect> effects,
            CardInstance cardInstance = null,
            SkillPresentationDataSO presentationData = null)
        {
            SkillData = skillData;
            RepeatCount = repeatCount;
            Effects = effects ?? new List<CardEffect>();
            CardInstance = cardInstance;
            PresentationData = presentationData;
        }

        public static SkillUsageData FromCard(CardInstance card)
        {
            card?.data?.EnsureEffectSlotIds();
            return new(
                card.data.skillData,
                1,
                card.data.effectSlots?.ConvertAll(s => s.effect),
                card,
                card.data.presentationData);
        }

        public static SkillUsageData FromEnemyCard(CardDataSO card)
        {
            if (card == null) return null;
            card.EnsureEffectSlotIds();
            return new(
                card.skillData,
                1,
                card.effectSlots?.ConvertAll(s => s.effect),
                new Battle.Instances.CardInstance(card, Battle.Enums.CardGrade.Normal),
                card.presentationData);
        }
    }
}
