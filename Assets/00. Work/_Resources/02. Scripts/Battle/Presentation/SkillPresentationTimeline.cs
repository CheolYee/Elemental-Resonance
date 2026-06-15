using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.Presentation
{
    [Serializable]
    public class SkillPresentationTimeline
    {
        public SkillSingleTrackData    animationTrack = new();
        public SkillSingleTrackData    effectTrack    = new();
        public List<SkillVfxObjectData> vfxObjects    = new();
        public SkillSingleTrackData    cameraTrack    = new();
        public SkillSingleTrackData    casterTrack    = new();
        public SkillSingleTrackData    uiTrack        = new();
        public SkillSingleTrackData    sfxTrack       = new();
        public List<SkillObjectKind>   addedObjects        = new();
        public SkillKeyframeData       endMarkerKeyframe;

        public bool IsEmpty =>
            (animationTrack?.keyframes == null || animationTrack.keyframes.Count == 0) &&
            (effectTrack?.keyframes    == null || effectTrack.keyframes.Count    == 0) &&
            (vfxObjects                == null || vfxObjects.Count               == 0) &&
            (cameraTrack?.keyframes    == null || cameraTrack.keyframes.Count    == 0) &&
            (casterTrack?.keyframes    == null || casterTrack.keyframes.Count    == 0) &&
            (uiTrack?.keyframes        == null || uiTrack.keyframes.Count        == 0) &&
            (sfxTrack?.keyframes       == null || sfxTrack.keyframes.Count       == 0);

        // endMarkerKeyframe이 있으면 그 시각을 사용. 없으면 마지막 키프레임 + 0.5s.
        public float GetEffectiveDuration()
        {
            if (endMarkerKeyframe != null) return endMarkerKeyframe.timeSeconds;

            float max = 0f;
            CollectMax(animationTrack, ref max);
            CollectMax(effectTrack,    ref max);
            CollectMax(cameraTrack,    ref max);
            CollectMax(casterTrack,    ref max);
            CollectMax(uiTrack,        ref max);
            CollectMax(sfxTrack,       ref max);

            if (vfxObjects != null)
                foreach (var v in vfxObjects)
                    if (v != null) CollectMax(new SkillSingleTrackData { keyframes = v.keyframes }, ref max);

            return max > 0f ? max + 0.5f : 1f;
        }

        private static void CollectMax(SkillSingleTrackData track, ref float max)
        {
            if (track?.keyframes == null) return;
            foreach (var k in track.keyframes)
                if (k != null && k.timeSeconds > max) max = k.timeSeconds;
        }
    }
}
