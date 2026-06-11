using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect
{
    [CreateAssetMenu(
        fileName = "StoryAspectSettings",
        menuName = "Story/Story Aspect Settings")]
    public sealed class StoryAspectSettingsSO : ScriptableObject
    {
        [Range(0.1f, 1f)]
        [SerializeField] private float visibleWidthRatio = 0.625f;
        [SerializeField] private Sprite leftLetterboxSprite;
        [SerializeField] private Sprite rightLetterboxSprite;
        [SerializeField] private Color letterboxColor = Color.black;
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float titleHoldDuration = 1.2f;

        public float VisibleWidthRatio => Mathf.Clamp(visibleWidthRatio, 0.1f, 1f);
        public Sprite LeftLetterboxSprite => leftLetterboxSprite;
        public Sprite RightLetterboxSprite => rightLetterboxSprite;
        public Color LetterboxColor => letterboxColor;
        public float FadeDuration => Mathf.Max(0f, fadeDuration);
        public float TitleHoldDuration => Mathf.Max(0f, titleHoldDuration);
    }
}
