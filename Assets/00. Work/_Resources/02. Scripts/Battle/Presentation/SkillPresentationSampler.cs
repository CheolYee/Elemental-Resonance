using System.Collections.Generic;
using UnityEngine;

namespace Battle.Presentation
{
    public sealed class SkillPresentationSampler : ISkillPresentationSampler
    {
        private const float BatchEpsilon = 0.0001f;

        public IReadOnlyList<SkillPresentationScheduledBatch> CreateSchedule(SkillPresentationTimeline timeline)
        {
            if (timeline == null) return System.Array.Empty<SkillPresentationScheduledBatch>();

            var entries = new List<ScheduledEntry>();
            AddEntries(timeline.animationTrack, 0, entries);
            AddEntries(timeline.effectTrack,    1, entries);
            // VFX 스폰: SkillPresentationPlayer.RunVfxAsync() 에서 별도 처리
            // Camera:   SkillPresentationPlayer.RunCameraAsync() 에서 별도 처리
            AddEntries(timeline.uiTrack,  5, entries);
            AddEntries(timeline.sfxTrack, 6, entries);

            if (entries.Count == 0) return System.Array.Empty<SkillPresentationScheduledBatch>();

            entries.Sort(CompareEntries);

            var batches     = new List<SkillPresentationScheduledBatch>();
            List<SkillKeyframeData> currentKeys = null;
            float currentTime = 0f;

            foreach (var entry in entries)
            {
                if (currentKeys == null ||
                    !Mathf.Approximately(currentTime, entry.TimeSeconds) &&
                    Mathf.Abs(currentTime - entry.TimeSeconds) > BatchEpsilon)
                {
                    currentTime = entry.TimeSeconds;
                    currentKeys = new List<SkillKeyframeData>();
                    batches.Add(new SkillPresentationScheduledBatch(currentTime, currentKeys));
                }
                currentKeys.Add(entry.Keyframe);
            }

            return batches;
        }

        private static void AddEntries(SkillSingleTrackData track, int rowOrder, List<ScheduledEntry> entries)
        {
            if (track?.keyframes == null) return;
            for (int i = 0; i < track.keyframes.Count; i++)
            {
                var key = track.keyframes[i];
                if (key == null) continue;
                entries.Add(new ScheduledEntry(key.timeSeconds, rowOrder, i, key));
            }
        }

        private static int CompareEntries(ScheduledEntry a, ScheduledEntry b)
        {
            int timeCompare = a.TimeSeconds.CompareTo(b.TimeSeconds);
            if (timeCompare != 0) return timeCompare;
            int rowCompare  = a.RowOrder.CompareTo(b.RowOrder);
            if (rowCompare  != 0) return rowCompare;
            return a.RowIndex.CompareTo(b.RowIndex);
        }

        private readonly struct ScheduledEntry
        {
            public ScheduledEntry(float timeSeconds, int rowOrder, int rowIndex, SkillKeyframeData keyframe)
            {
                TimeSeconds = timeSeconds;
                RowOrder    = rowOrder;
                RowIndex    = rowIndex;
                Keyframe    = keyframe;
            }
            public float             TimeSeconds { get; }
            public int               RowOrder    { get; }
            public int               RowIndex    { get; }
            public SkillKeyframeData Keyframe    { get; }
        }
    }
}
