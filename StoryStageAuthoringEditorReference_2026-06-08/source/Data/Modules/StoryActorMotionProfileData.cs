using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryStageMoveMotionType
    {
        Instant,
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        SmoothStep,
        SmootherStep,
        None,
    }

    [Serializable]
    public sealed class StoryActorMotionProfileData
    {
        [SerializeField] private StoryEnterMotionType enterMotion = StoryEnterMotionType.FadeIn;
        [SerializeField] [Range(0f, 3f)] private float enterDuration = 0.35f;
        [SerializeField] private StoryStageMoveMotionType moveMotion = StoryStageMoveMotionType.Linear;
        [SerializeField] [Range(0f, 3f)] private float moveDuration = 0.2f;
        [SerializeField] private StoryEnterMotionType exitMotion = StoryEnterMotionType.FadeIn;
        [SerializeField] [Range(0f, 3f)] private float exitDuration = 0.25f;

        public StoryEnterMotionType EnterMotion => enterMotion;
        public float EnterDuration => enterDuration;
        public StoryStageMoveMotionType MoveMotion => moveMotion;
        public float MoveDuration => moveDuration;
        public StoryEnterMotionType ExitMotion => exitMotion;
        public float ExitDuration => exitDuration;
    }
}
