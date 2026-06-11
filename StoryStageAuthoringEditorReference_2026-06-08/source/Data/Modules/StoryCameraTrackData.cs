using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [Serializable]
    public sealed class StoryCameraTrackData
    {
        [Tooltip("Default camera state for this line when no camera key overrides it yet.")]
        public StoryCameraStateData defaultState = new();

        [Tooltip("Optional per-line camera keyframes.")]
        public List<StoryActorKeyframeData> keyframes = new();
    }
}
