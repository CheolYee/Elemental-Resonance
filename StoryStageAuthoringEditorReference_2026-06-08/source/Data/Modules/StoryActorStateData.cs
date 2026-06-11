using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// Absolute actor state at a story line. StoryStageLayoutModuleSO owns these
    /// values as sub-asset-authored source data through StoryLineSO.modules.
    /// </summary>
    [Serializable]
    public sealed class StoryActorStateData
    {
        [Tooltip("Character definition used for preview visuals and runtime prefab lookup.")]
        public CharacterDefinitionSO actor;

        [Tooltip("Stable actor key. Falls back to CharacterDefinitionSO.CharacterId when empty.")]
        public string actorKey = "";

        [Tooltip("Unique stage actor instance key. Multiple entries may reference the same CharacterDefinitionSO.")]
        public string actorInstanceKey = "";

        [Tooltip("Stage local world position. (0,0)=StageRoot center. (2,0)=2 world units right. Unclamped.")]
        public Vector2 stageLocalPosition = Vector2.zero;

        [Tooltip("Per-line uniform scale multiplier. Final scale = CharacterDefinitionSO.BaseScaleMultiplier * scaleMultiplier.")]
        public float scaleMultiplier = 1f;

        [Tooltip("Whether this actor is visible at this line state.")]
        public bool visible = true;

        [Tooltip("Whether this actor is focused. Unfocused actors can be dimmed by preview/runtime.")]
        public bool focused = true;

        [NonSerialized]
        public float focusVisualAlpha = -1f;

        [Tooltip("Stage sorting priority. Larger values are rendered in front.")]
        public int sortOrder = 0;

        [Tooltip("Pose key for sprite/animation lookup.")]
        public string poseKey = "";

        [Tooltip("Expression enum for full-body sprite lookup.")]
        public StoryExpressionType expression = StoryExpressionType.Neutral;

        [HideInInspector]
        public string expressionKey = "";

        [Tooltip("Optional motion profile key for shared presets in later authoring steps.")]
        public string motionProfileKey = "";

        public string ResolvedActorKey =>
            !string.IsNullOrWhiteSpace(actorInstanceKey)
                ? actorInstanceKey
                : ResolvedCharacterKey;

        public string ResolvedCharacterKey =>
            !string.IsNullOrWhiteSpace(actorKey)
                ? actorKey
                : ResolveActorKey(actor);

        public StoryExpressionType ResolvedExpression => expression;

        public float EffectiveFocusAlpha =>
            focusVisualAlpha >= 0f ? Mathf.Clamp01(focusVisualAlpha) : focused ? 1f : 0.65f;

        public void SyncActorKey()
        {
            if (string.IsNullOrWhiteSpace(actorKey))
                actorKey = ResolveActorKey(actor);
        }

        public void EnsureActorInstanceKey(string fallbackKey)
        {
            if (!string.IsNullOrWhiteSpace(actorInstanceKey))
                return;

            actorInstanceKey = !string.IsNullOrWhiteSpace(fallbackKey)
                ? fallbackKey
                : ResolvedCharacterKey;
        }

        public bool MatchesActor(CharacterDefinitionSO target)
        {
            if (target == null)
                return false;

            if (actor == target)
                return true;

            string targetKey = ResolveActorKey(target);
            return !string.IsNullOrWhiteSpace(targetKey) && ResolvedCharacterKey == targetKey;
        }

        public bool MatchesActorInstance(string instanceKey) =>
            !string.IsNullOrWhiteSpace(instanceKey) && ResolvedActorKey == instanceKey;

        public static string ResolveActorKey(CharacterDefinitionSO actorDefinition) =>
            actorDefinition != null
                ? !string.IsNullOrWhiteSpace(actorDefinition.CharacterId)
                    ? actorDefinition.CharacterId
                    : actorDefinition.name
                : string.Empty;

        public StoryActorStateData ShallowClone() => (StoryActorStateData)MemberwiseClone();
    }
}
