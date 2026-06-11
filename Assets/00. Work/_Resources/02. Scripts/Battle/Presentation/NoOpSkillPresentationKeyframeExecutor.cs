using Cysharp.Threading.Tasks;

namespace Battle.Presentation
{
    public sealed class NoOpSkillPresentationKeyframeExecutor : ISkillPresentationKeyframeExecutor
    {
        public UniTask ExecuteAsync(SkillPresentationPlaybackContext context, SkillKeyframeData keyframe)
            => UniTask.CompletedTask;
    }
}
