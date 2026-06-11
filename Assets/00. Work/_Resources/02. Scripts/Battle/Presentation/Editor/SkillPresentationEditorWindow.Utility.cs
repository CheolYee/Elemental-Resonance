using System.Collections.Generic;
using UnityEngine;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
    {
        // ── time ↔ pixel 변환 ─────────────────────────────────────────────────

        internal float TimeToPixel(float timeSeconds) =>
            timeSeconds * PixelsPerSecond;

        internal float PixelToTime(float pixel) =>
            pixel / Mathf.Max(1f, PixelsPerSecond);

        // ── Keyframe 정렬 ─────────────────────────────────────────────────────

        internal static void SortByTime(List<SkillKeyframeData> keys)
        {
            if (keys == null || keys.Count < 2) return;
            keys.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
        }

        internal static void SortVfxObjects(List<SkillVfxObjectData> objects)
        {
            if (objects == null || objects.Count < 2) return;
            objects.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        }

        // ── Timeline 유효 Duration 조회 ───────────────────────────────────────

        internal float GetDisplayDuration()
        {
            var tl = GetEditableTimeline();
            if (tl == null) return 1f;
            return Mathf.Max(tl.GetEffectiveDuration(), 0.5f);
        }
    }
}
