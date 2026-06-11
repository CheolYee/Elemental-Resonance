using Battle.Enums;
using UnityEngine;

namespace Battle.Presentation
{
    [CreateAssetMenu(menuName = "Battle/Skill Presentation Data")]
    public class SkillPresentationDataSO : ScriptableObject
    {
        public bool hideBattleUIDuringSkill;

        public SkillPresentationTimeline normalTimeline = new();
        public SkillPresentationTimeline rareTimeline = new();
        public SkillPresentationTimeline epicTimeline = new();
        public SkillPresentationTimeline legendaryTimeline = new();

        public SkillPresentationTimeline GetTimeline(CardGrade grade)
        {
            return grade switch
            {
                CardGrade.Legendary => FirstNonEmpty(legendaryTimeline, epicTimeline, rareTimeline, normalTimeline),
                CardGrade.Epic      => FirstNonEmpty(epicTimeline, rareTimeline, normalTimeline),
                CardGrade.Rare      => FirstNonEmpty(rareTimeline, normalTimeline),
                _                   => normalTimeline
            };
        }

        public bool TryGetPlayableTimeline(CardGrade grade, out SkillPresentationTimeline timeline)
        {
            timeline = GetTimeline(grade);
            return timeline != null && !timeline.IsEmpty;
        }

        private static SkillPresentationTimeline FirstNonEmpty(params SkillPresentationTimeline[] timelines)
        {
            foreach (var t in timelines)
                if (t != null && !t.IsEmpty) return t;
            return timelines[^1];
        }
    }
}
