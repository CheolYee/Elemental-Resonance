using Gamelib.SoundSystem;

namespace Battle.Presentation
{
    public interface IBattleUIController
    {
        void HideBattleUI();
        void ShowBattleUI();
        void PlaySfx(SfxSounds sound);
    }
}
