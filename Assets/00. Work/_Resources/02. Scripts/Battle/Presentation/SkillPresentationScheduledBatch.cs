using System;
using System.Collections.Generic;

namespace Battle.Presentation
{
    public sealed class SkillPresentationScheduledBatch
    {
        public float TimeSeconds { get; }
        public IReadOnlyList<SkillKeyframeData> Keyframes { get; }

        public SkillPresentationScheduledBatch(float timeSeconds, IReadOnlyList<SkillKeyframeData> keyframes)
        {
            TimeSeconds = timeSeconds;
            Keyframes   = keyframes ?? Array.Empty<SkillKeyframeData>();
        }
    }
}
