using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryActorKeyframeProperty
    {
        Position,
        Scale,
        Easing,
        Expression,
        BackgroundCut,
        BackgroundPosition,
        BackgroundScale,
        CameraTarget,
        CameraOffset,
        CameraZoom,
        CameraShake,
        SoundBgm,
        SoundSfx,
    }

    /// <summary>
    /// A single keyframe in a per-actor timeline track within one story line.
    /// normalizedTime 0 = line start, 1 = line end.
    /// Keyframes are evaluated relative to the actor's accumulated entry state from
    /// StoryActorStateData and do not replace the StoryLineSO.modules source of truth.
    /// </summary>
    [Serializable]
    public sealed class StoryActorKeyframeData
    {
        [Tooltip("Which property row this key belongs to.")]
        public StoryActorKeyframeProperty property = StoryActorKeyframeProperty.Position;

        [Tooltip("Position of this keyframe in the line: 0 = line start, 1 = line end.")]
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Tooltip("Absolute key time in seconds from the start of this line. Preferred by the timeline editor.")]
        [Min(0f)]
        public float timeSeconds = 0f;

        [Tooltip("Stage local world position at this keyframe. (0,0)=StageRoot center. (2,0)=2 world units right.")]
        public Vector2 stageLocalPosition;

        [Tooltip("Scale multiplier at this keyframe.")]
        public Vector2 scale = Vector2.one;

        [Tooltip("Legacy horizontal scale/flip at this keyframe.")]
        public float scaleX = 1f;

        [Tooltip("Legacy visibility value. Hidden in the MVP timeline UI.")]
        public bool visible = true;

        [Tooltip("Easing for the segment arriving at this keyframe (from previous key or line start state). None = hold previous value, snap at key time.")]
        public StoryStageMoveMotionType easing = StoryStageMoveMotionType.None;

        [Tooltip("Discrete full-body expression value used by Expression keys.")]
        public StoryExpressionType expression = StoryExpressionType.Neutral;

        [Tooltip("Discrete background definition used by Background Cut keys.")]
        public BackgroundDefinitionSO background;

        [Tooltip("Stable background key fallback used by Background Cut keys.")]
        public string backgroundKey = "";

        [Tooltip("Discrete camera target actorInstanceKey used by Camera Target keys.")]
        public string cameraTargetActorKey = "";

        [Tooltip("Camera follow mode used by Camera Target keys.")]
        public StoryCameraFollowMode cameraFollowMode = StoryCameraFollowMode.FollowActor;

        [Tooltip("Camera stage local world position used by Camera Position keys. (0,0)=StageRoot center.")]
        public Vector2 cameraStageLocalPosition = Vector2.zero;

        [Tooltip("Camera zoom multiplier used by Camera Zoom keys.")]
        [Min(0.01f)]
        public float cameraZoom = 1f;

        [Tooltip("Stage-local actor pivot position captured at key-authoring time when CameraTarget uses SnapshotPosition. (0,0) = stage root center.")]
        public Vector2 cameraSnapshotNormalizedPosition = Vector2.zero;

        [Tooltip("Camera shake strength used by Camera Shake keys.")]
        [Min(0f)]
        public float cameraShakeStrength = 1f;

        [Tooltip("Camera shake duration used by Camera Shake keys.")]
        [Min(0f)]
        public float cameraShakeDuration = 0.25f;

        [Tooltip("Camera shake frequency used by Camera Shake keys.")]
        [Min(0f)]
        public float cameraShakeFrequency = 2f;

        [Tooltip("Which visual scope the camera shake should affect.")]
        public StoryCameraShakeTargetMode cameraShakeTargetMode = StoryCameraShakeTargetMode.All;

        public StoryActorKeyframeData ShallowClone() => (StoryActorKeyframeData)MemberwiseClone();
    }
}
