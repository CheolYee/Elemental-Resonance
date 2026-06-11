using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// Optional per-line background timeline. The line snapshot remains
    /// StoryBackgroundStateData; these keys only describe intra-line changes.
    /// </summary>
    [Serializable]
    public sealed class StoryBackgroundTrackData
    {
        [Tooltip("Background Cut / Position / Scale keyframes for this line.")]
        public List<StoryActorKeyframeData> keyframes = new();
    }
}
