using _00._Work._Resources._02._Scripts.Modules;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class HitSoundModule : MonoBehaviour, IModule
    {
        [SerializeField] private SfxSounds[] hitSounds;
        [SerializeField] private SfxSounds[] blockSounds;
        [SerializeField] private EventChannelSO soundChannel;

        private HealthModule _health;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            if (_health != null)
                _health.OnDamageTaken += OnDamageTaken;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDamageTaken -= OnDamageTaken;
        }

        private void OnDamageTaken(int damageToHp, int damageToBlock)
        {
            if (soundChannel == null) return;

            SfxSounds[] pool = damageToHp > 0 ? hitSounds : blockSounds;
            if (pool == null || pool.Length == 0) return;

            var sound = pool[Random.Range(0, pool.Length)];
            soundChannel.RaiseEvent(new PlaySoundEvent(sound, transform.position));
        }
    }
}
