using UnityEngine;
using UnityEngine.Serialization;

namespace Battle.Presentation
{
    [CreateAssetMenu(menuName = "Battle/Skill Presentation Data")]
    public class SkillPresentationDataSO : ScriptableObject
    {
        public bool hideBattleUIDuringSkill;

        [FormerlySerializedAs("normalTimeline")]
        public SkillPresentationTimeline timeline = new();

        public SkillPresentationTimeline GetTimeline() => timeline;

        public bool TryGetPlayableTimeline(out SkillPresentationTimeline result)
        {
            result = timeline;
            return result != null && !result.IsEmpty;
        }
    }
}
