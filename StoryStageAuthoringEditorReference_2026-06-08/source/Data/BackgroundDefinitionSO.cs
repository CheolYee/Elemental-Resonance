using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [CreateAssetMenu(fileName = "BackgroundDefinition", menuName = "Story/Background Definition")]
    public sealed class BackgroundDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string backgroundId;
        [SerializeField] private string displayName;

        [Header("Presentation")]
        [SerializeField] private Sprite previewSprite;

        [Header("Stage Defaults")]
        [Tooltip("Base uniform scale multiplier. finalScale = baseScaleMultiplier * StoryBackgroundStateData.scaleMultiplier.")]
        [SerializeField] private float baseScaleMultiplier = 1f;
        [Tooltip("Reserved for parallax. 0=fixed relative to StageRoot, 1=follows camera fully.")]
        [SerializeField] [Range(0f, 1f)] private float parallaxFactor = 0f;
        [SerializeField] private Color defaultTint = Color.white;
        [SerializeField] private int defaultSortOrder = -100;

        public string BackgroundId => backgroundId;
        public string DisplayName => displayName;
        public Sprite PreviewSprite => previewSprite;
        public Sprite DefaultSprite => previewSprite;
        public float BaseScaleMultiplier => Mathf.Max(0.001f, baseScaleMultiplier);
        public float ParallaxFactor => Mathf.Clamp01(parallaxFactor);
        public Color DefaultTint => defaultTint;
        public int DefaultSortOrder => defaultSortOrder;
    }
}
