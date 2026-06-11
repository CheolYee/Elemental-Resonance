using Cysharp.Threading.Tasks;

namespace Battle.Presentation
{
    public interface ISkillPresentationKeyframeExecutor
    {
        UniTask ExecuteAsync(SkillPresentationPlaybackContext context, SkillKeyframeData keyframe);
    }
}
