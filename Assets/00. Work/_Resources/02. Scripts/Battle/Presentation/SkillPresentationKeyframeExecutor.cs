using Cysharp.Threading.Tasks;

namespace Battle.Presentation
{
    public sealed class SkillPresentationKeyframeExecutor : ISkillPresentationKeyframeExecutor
    {
        private const float AnimationCrossFadeDuration = 0.1f;

        private readonly SkillEffectExecutionService _effectService;

        public SkillPresentationKeyframeExecutor(SkillEffectExecutionService effectService)
        {
            _effectService = effectService;
        }

        public UniTask ExecuteAsync(SkillPresentationPlaybackContext context, SkillKeyframeData keyframe)
        {
            if (keyframe == null) return UniTask.CompletedTask;

            switch (keyframe.property)
            {
                case SkillKeyframeProperty.AnimParam:
                    if (keyframe.animParam != null && context?.Caster?.Renderer != null)
                        context.Caster.Renderer.PlayClip(keyframe.animParam.ParamHash, 0f, AnimationCrossFadeDuration);
                    break;

                case SkillKeyframeProperty.EffectSlot:
                    _effectService?.Execute(context, keyframe);
                    break;
            }

            return UniTask.CompletedTask;
        }
    }
}
