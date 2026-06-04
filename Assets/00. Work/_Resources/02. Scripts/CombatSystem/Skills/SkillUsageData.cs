using System.Collections.Generic;
using Battle.Data;
using Battle.Effects;
using Battle.Instances;

namespace _02._Scripts.CombatSystem.Skills
{
    public class SkillUsageData
    {
        public SkillDataSO SkillData { get; }
        public int RepeatCount { get; }
        public List<CardEffectSO> Effects { get; }

        public SkillUsageData(SkillDataSO skillData, int repeatCount, List<CardEffectSO> effects)
        {
            SkillData = skillData;
            RepeatCount = repeatCount;
            Effects = effects ?? new List<CardEffectSO>();
        }

        public static SkillUsageData FromCard(CardInstance card)
            => new(card.data.skillData, (int)card.grade + 1, card.data.effects);

        public static SkillUsageData FromEnemyData(EnemyDataSO data)
            => new(data.attackSkill, 1, data.attackEffects);
    }
}
