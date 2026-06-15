using Battle.Events;
using Battle.Presentation;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleUIController : MonoBehaviour, IBattleUIController
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private EventChannelSO soundChannel;

        public void HideBattleUI()
            => battleEventChannel.RaiseEvent(new BattleUIHiddenEvent());

        public void ShowBattleUI()
            => battleEventChannel.RaiseEvent(new BattleUIShownEvent());

        public void PlaySfx(SfxSounds sound)
            => soundChannel.RaiseEvent(new PlaySoundEvent(sound, Vector3.zero));
    }
}
