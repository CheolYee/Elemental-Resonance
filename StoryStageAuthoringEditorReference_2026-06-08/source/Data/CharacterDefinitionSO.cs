using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [Serializable]
    public sealed class CharacterExpressionSpriteData
    {
        [Tooltip("Enum expression used by preview/runtime. Legacy string keys remain as fallback.")]
        public StoryExpressionType expression = StoryExpressionType.Neutral;

        [HideInInspector]
        public string expressionKey = "";

        public Sprite fullBodySprite;
    }

    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Story/Character Definition")]
    public sealed class CharacterDefinitionSO : ScriptableObject
    {
        private static readonly Dictionary<string, StoryExpressionType> LegacyExpressionAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Surprise"] = StoryExpressionType.Surprised,
                ["Suprised"] = StoryExpressionType.Surprised,
                ["Shy"] = StoryExpressionType.Embarrassed
            };

        private static readonly HashSet<string> MissingExpressionWarnings = new();
        private static readonly HashSet<string> ConflictingExpressionWarnings = new();

        [Header("Identity")]
        [SerializeField] private string characterId;
        [SerializeField] private string displayName;

        [Header("Presentation")]
        [SerializeField] private Sprite logIcon;
        [SerializeField] private Color personalColor = Color.white;
        [HideInInspector]
        [SerializeField] private GameObject defaultActorPrefab;
        [HideInInspector]
        [SerializeField] private Sprite previewSprite;

        [Header("Shared Actor Visual")]
        [Tooltip("Default full-body sprite used by the shared actor view. Falls back to PreviewSprite/LogIcon.")]
        [SerializeField] private Sprite defaultFullBodySprite;
        [Tooltip("Full-body sprite variants keyed by expression enum.")]
        [SerializeField] private List<CharacterExpressionSpriteData> expressionSprites = new();
        [Tooltip("Default expression used when a line state has no explicit expression override.")]
        [SerializeField] private StoryExpressionType defaultExpression = StoryExpressionType.Neutral;
        [Tooltip("Character-wide uniform scale multiplier. Final scale = BaseScaleMultiplier * StoryActorStateData.scaleMultiplier.")]
        [SerializeField] private float baseScaleMultiplier = 1f;

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public Sprite LogIcon => logIcon;
        public Color PersonalColor => personalColor;
        public GameObject DefaultActorPrefab => defaultActorPrefab;
        public Sprite PreviewSprite => previewSprite != null ? previewSprite : logIcon;
        public Sprite DefaultFullBodySprite => defaultFullBodySprite != null ? defaultFullBodySprite : PreviewSprite;
        public IReadOnlyList<CharacterExpressionSpriteData> ExpressionSprites => expressionSprites;
        public StoryExpressionType DefaultExpression => defaultExpression;
        public float BaseScaleMultiplier => Mathf.Approximately(baseScaleMultiplier, 0f) ? 1f : baseScaleMultiplier;

        public Sprite ResolveExpressionSprite(StoryExpressionType expression) =>
            ResolveExpressionSprite(expression, string.Empty);

        public Sprite ResolveExpressionSprite(StoryExpressionType expression, string legacyExpressionKey)
        {
            if (expression == StoryExpressionType.Neutral)
            {
                if (TryGetExpressionSprite(StoryExpressionType.Neutral, out Sprite neutralSprite))
                    return neutralSprite;

                return DefaultFullBodySprite;
            }

            if (TryGetExpressionSprite(expression, out Sprite sprite))
                return sprite;

            if (TryResolveLegacyExpressionEnumSprite(legacyExpressionKey, out Sprite legacySprite))
                return legacySprite;

            if (TryResolveLegacyExpressionSprite(legacyExpressionKey, out legacySprite))
                return legacySprite;

            WarnMissingExpressionMapping(expression, legacyExpressionKey);
            return DefaultFullBodySprite;
        }

        public Sprite ResolveExpressionSprite(string expressionKey)
        {
            if (TryParseExpression(expressionKey, out StoryExpressionType expression))
                return ResolveExpressionSprite(expression, expressionKey);

            return TryResolveLegacyExpressionSprite(expressionKey, out Sprite sprite)
                ? sprite
                : DefaultFullBodySprite;
        }

        public bool TryGetExpressionSprite(StoryExpressionType expression, out Sprite sprite)
        {
            sprite = null;
            if (expressionSprites == null)
                return false;

            for (int i = 0; i < expressionSprites.Count; i++)
            {
                CharacterExpressionSpriteData entry = expressionSprites[i];
                if (entry == null || entry.fullBodySprite == null)
                    continue;

                WarnConflictingExpressionEntryIfNeeded(i, entry);
                if (entry.expression == expression)
                {
                    sprite = entry.fullBodySprite;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveLegacyExpressionSprite(string expressionKey, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(expressionKey) || expressionSprites == null)
                return false;

            for (int i = 0; i < expressionSprites.Count; i++)
            {
                CharacterExpressionSpriteData entry = expressionSprites[i];
                if (entry == null || entry.fullBodySprite == null)
                    continue;

                if (HasConflictingExpressionEntry(i, entry))
                    continue;

                if (string.Equals(entry.expressionKey, expressionKey, StringComparison.Ordinal))
                {
                    sprite = entry.fullBodySprite;
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveLegacyExpressionEnumSprite(string expressionKey, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(expressionKey))
                return false;

            return TryParseExpression(expressionKey, out StoryExpressionType legacyExpression)
                && TryGetExpressionSprite(legacyExpression, out sprite);
        }

        private bool HasConflictingExpressionEntry(int index, CharacterExpressionSpriteData entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.expressionKey))
                return false;

            if (!TryParseExpression(entry.expressionKey, out StoryExpressionType parsed))
                return false;

            if (parsed == entry.expression)
                return false;

            WarnConflictingExpressionEntry(index, entry, parsed);
            return true;
        }

        private void WarnConflictingExpressionEntryIfNeeded(int index, CharacterExpressionSpriteData entry)
        {
            HasConflictingExpressionEntry(index, entry);
        }

        private void WarnConflictingExpressionEntry(int index, CharacterExpressionSpriteData entry, StoryExpressionType parsedExpression)
        {
            string warningKey = $"{GetInstanceID()}::conflict::{index}::{entry.expression}::{entry.expressionKey}";
            if (!ConflictingExpressionWarnings.Add(warningKey))
                return;

            string spriteName = entry.fullBodySprite != null ? entry.fullBodySprite.name : "<null>";
            Debug.LogWarning(
                $"[{nameof(CharacterDefinitionSO)}] '{name}' has conflicting expression mapping at index {index}. " +
                $"Enum='{entry.expression}' is the source of truth, legacy key='{entry.expressionKey}' parses to '{parsedExpression}', " +
                $"sprite='{spriteName}'. The enum value will be used.",
                this);
        }

        private static bool TryParseExpression(string value, out StoryExpressionType expression)
        {
            if (Enum.TryParse(value, true, out expression))
                return true;

            string normalized = NormalizeExpressionKey(value);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (Enum.TryParse(normalized, true, out expression))
                return true;

            return LegacyExpressionAliases.TryGetValue(normalized, out expression);
        }

        private static string NormalizeExpressionKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        }

        private void WarnMissingExpressionMapping(StoryExpressionType expression, string legacyExpressionKey)
        {
            if (expression == StoryExpressionType.Neutral)
                return;

            string warningKey = $"{GetInstanceID()}::{expression}::{legacyExpressionKey}";
            if (!MissingExpressionWarnings.Add(warningKey))
                return;

            string fallbackSpriteName = DefaultFullBodySprite != null ? DefaultFullBodySprite.name : "<null>";
            Debug.LogWarning(
                $"[{nameof(CharacterDefinitionSO)}] '{name}' has no sprite mapped for expression '{expression}'. " +
                $"Legacy key='{legacyExpressionKey}', fallback sprite='{fallbackSpriteName}'. Falling back to DefaultFullBodySprite.",
                this);
        }

#if UNITY_EDITOR

        [ContextMenu("Validate Expression Mapping")]
        private void ValidateExpressionMapping()
        {
            if (expressionSprites == null || expressionSprites.Count == 0)
            {
                if (DefaultFullBodySprite == null)
                {
                    Debug.LogWarning($"[{nameof(CharacterDefinitionSO)}] '{name}' has no expression sprite entries and no DefaultFullBodySprite.", this);
                }
                return;
            }

            var seen = new Dictionary<StoryExpressionType, int>();
            for (int i = 0; i < expressionSprites.Count; i++)
            {
                CharacterExpressionSpriteData entry = expressionSprites[i];
                if (entry == null)
                {
                    Debug.LogWarning($"[{nameof(CharacterDefinitionSO)}] '{name}' has a null expression entry at index {i}.", this);
                    continue;
                }

                StoryExpressionType resolvedExpression = entry.expression;
                bool neutralEntry = resolvedExpression == StoryExpressionType.Neutral;
                if (entry.fullBodySprite == null && !neutralEntry)
                {
                    Debug.LogWarning(
                        $"[{nameof(CharacterDefinitionSO)}] '{name}' is missing a sprite for entry {i} " +
                        $"(enum='{entry.expression}', key='{entry.expressionKey}').",
                        this);
                }

                if (HasConflictingExpressionEntry(i, entry))
                {
                    // Warning is emitted once by HasConflictingExpressionEntry.
                }

                if (seen.TryGetValue(resolvedExpression, out int previousIndex))
                {
                    Debug.LogWarning(
                        $"[{nameof(CharacterDefinitionSO)}] '{name}' has duplicate expression mapping for '{resolvedExpression}' " +
                        $"at indices {previousIndex} and {i}.",
                        this);
                }
                else
                {
                    seen[resolvedExpression] = i;
                }
            }

            if (DefaultFullBodySprite == null && !seen.ContainsKey(StoryExpressionType.Neutral))
            {
                Debug.LogWarning(
                    $"[{nameof(CharacterDefinitionSO)}] '{name}' has no Neutral mapping and no DefaultFullBodySprite.",
                    this);
            }

            foreach (StoryExpressionType expression in Enum.GetValues(typeof(StoryExpressionType)))
            {
                if (expression == StoryExpressionType.Neutral)
                    continue;

                if (!seen.ContainsKey(expression))
                {
                    Debug.LogWarning(
                        $"[{nameof(CharacterDefinitionSO)}] '{name}' has no sprite mapping for expression '{expression}'.",
                        this);
                }
            }
        }
#endif
    }
}
