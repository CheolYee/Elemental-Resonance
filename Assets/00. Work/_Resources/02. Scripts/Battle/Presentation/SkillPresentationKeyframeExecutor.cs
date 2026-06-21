using Cysharp.Threading.Tasks;

namespace Battle.Presentation
{
    public sealed class SkillPresentationKeyframeExecutor : ISkillPresentationKeyframeExecutor
    {
        private const float AnimationCrossFadeDuration = 0.1f;

        private readonly SkillEffectExecutionService _effectService;
        private readonly IBattleUIController         _battleUIController;

        public SkillPresentationKeyframeExecutor(
            SkillEffectExecutionService effectService,
            IBattleUIController         battleUIController)
        {
            _effectService      = effectService;
            _battleUIController = battleUIController;
        }

        public async UniTask ExecuteAsync(SkillPresentationPlaybackContext context, SkillKeyframeData keyframe)
        {
            if (keyframe == null) return;

            switch (keyframe.property)
            {
                case SkillKeyframeProperty.AnimParam:
                    if (keyframe.animParam != null && context?.Caster?.Renderer != null)
                        context.Caster.Renderer.PlayClip(keyframe.animParam.ParamHash, 0f, AnimationCrossFadeDuration);
                    break;

                case SkillKeyframeProperty.EffectSlot:
                    if (_effectService != null)
                    {
                        bool success = await _effectService.ExecuteAsync(context, keyframe);
                        if (!success) context.FizzleToken.RequestFizzle();
                    }
                    break;

                case SkillKeyframeProperty.UiAction:
                    switch (keyframe.uiAction)
                    {
                        case SkillUiAction.HideBattleUI: _battleUIController?.HideBattleUI(); break;
                        case SkillUiAction.ShowBattleUI: _battleUIController?.ShowBattleUI(); break;
                    }
                    break;

                case SkillKeyframeProperty.SfxId:
                    _battleUIController?.PlaySfx(keyframe.sfxSound);
                    break;
            }
        }
    }
}
