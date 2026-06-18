using System.Collections.Generic;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using _02._Scripts.CombatSystem.Skills;
using Battle.Enums;
using Battle.Instances;

namespace Battle.Presentation
{
    public sealed class SkillPresentationPlaybackContext
    {
        public SkillPresentationDataSO PresentationData { get; }
        public CardGrade Grade { get; }
        public SkillUsageData UsageData { get; }
        public CardInstance CardInstance { get; }
        public Agent Caster { get; }
        public Agent Target { get; }
        public IReadOnlyList<Agent> Targets { get; }
        public CancellationToken CancellationToken { get; }
        public SkillPresentationTimeline Timeline { get; }

        public SkillPresentationPlaybackContext(
            SkillPresentationDataSO presentationData,
            CardGrade grade,
            SkillUsageData usageData,
            CardInstance cardInstance,
            Agent caster,
            Agent target,
            CancellationToken cancellationToken,
            SkillPresentationTimeline timeline = null,
            IReadOnlyList<Agent> targets = null)
        {
            PresentationData = presentationData;
            Grade = grade;
            UsageData = usageData;
            CardInstance = cardInstance;
            Caster = caster;
            Target = target;
            Targets = targets ?? (target != null ? new List<Agent> { target } : new List<Agent>());
            CancellationToken = cancellationToken;
            Timeline = timeline;
        }

        public SkillPresentationPlaybackContext WithTimeline(SkillPresentationTimeline timeline)
            => new(
                PresentationData,
                Grade,
                UsageData,
                CardInstance,
                Caster,
                Target,
                CancellationToken,
                timeline,
                Targets);
    }
}
