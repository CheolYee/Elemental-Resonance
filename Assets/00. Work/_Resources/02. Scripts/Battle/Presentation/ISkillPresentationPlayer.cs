using Cysharp.Threading.Tasks;

namespace Battle.Presentation
{
    public interface ISkillPresentationPlayer
    {
        UniTask PlayAsync(SkillPresentationPlaybackContext context);
    }
}
