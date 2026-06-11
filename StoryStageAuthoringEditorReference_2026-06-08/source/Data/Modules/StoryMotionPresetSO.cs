using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [CreateAssetMenu(menuName = "Story/Motion Preset", fileName = "StoryMotionPreset")]
    public sealed class StoryMotionPresetSO : ScriptableObject
    {
        [Tooltip("Optional stable id for designer-facing preset organization.")]
        public string presetId = "";

        [Tooltip("Primary/default property row. Individual keyframes keep their own property, so one preset can contain Position and Scale together.")]
        public StoryActorKeyframeProperty property = StoryActorKeyframeProperty.Position;

        [Tooltip("Keyframes stored relative to the first selected key time. Supports mixed Position/Scale rows.")]
        public List<StoryActorKeyframeData> keyframes = new();

        public int KeyCount => keyframes?.Count ?? 0;

        public bool ContainsProperty(StoryActorKeyframeProperty target)
        {
            if (keyframes == null)
                return false;

            foreach (StoryActorKeyframeData keyframe in keyframes)
            {
                if (keyframe != null && keyframe.property == target)
                    return true;
            }

            return false;
        }
    }
}
