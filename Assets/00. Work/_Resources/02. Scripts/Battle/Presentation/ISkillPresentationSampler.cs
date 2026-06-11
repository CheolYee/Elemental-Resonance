using System.Collections.Generic;

namespace Battle.Presentation
{
    public interface ISkillPresentationSampler
    {
        IReadOnlyList<SkillPresentationScheduledBatch> CreateSchedule(SkillPresentationTimeline timeline);
    }
}
