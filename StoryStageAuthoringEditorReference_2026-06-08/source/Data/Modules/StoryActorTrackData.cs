using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// Optional per-actor keyframe track for intra-line motion within one story line.
    /// Keyed by actorInstanceKey (must match StoryActorStateData.actorInstanceKey).
    /// Does not replace the StoryActorStateData snapshot in StoryStageLayoutModuleSO.actors;
    /// the snapshot remains the authoritative absolute state at line entry.
    /// </summary>
    [Serializable]
    public sealed class StoryActorTrackData
    {
        [Tooltip("Must match StoryActorStateData.actorInstanceKey for the target actor.")]
        public string actorInstanceKey = "";

        [Tooltip("Keyframes sorted by normalizedTime ascending (0 = line start, 1 = line end).")]
        public List<StoryActorKeyframeData> keyframes = new();
    }
}
