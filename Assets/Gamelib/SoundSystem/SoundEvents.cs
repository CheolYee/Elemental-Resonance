using Gamelib.EventSystem;
using UnityEngine;

namespace Gamelib.SoundSystem
{
    public enum SoundChannelId
    {
        None = 0,
        Bgm = 1,
        StorySfx = 2,
    }

    public readonly struct SoundRef
    {
        public AudioTypes AudioType { get; }
        public int Index { get; }

        public SoundRef(AudioTypes audioType, int index)
        {
            AudioType = audioType;
            Index = index;
        }

        //사용자 정의 자동 형변환으로 이넘들을 처리하기
        public static implicit operator SoundRef(SfxSounds sfx)
            => new SoundRef(AudioTypes.SFX, (int)sfx);

        public static implicit operator SoundRef(BgmSounds bgm)
            => new SoundRef(AudioTypes.MUSIC, (int)bgm);
    }

    public sealed class PlaySoundEvent : GameEvent
    {
        public SoundRef Sound { get; }
        public Vector3 Position { get; }
        public SoundChannelId ChannelId { get; }

        public PlaySoundEvent(SoundRef sound, Vector3 position, SoundChannelId channelId = SoundChannelId.None)
        {
            Sound = sound;
            Position = position;
            ChannelId = channelId;
        }

        //생성자 요약문법 this를 이용하여 중복코드 제거하기
        public PlaySoundEvent(SfxSounds sfxSound, Vector3 position, SoundChannelId channelId = SoundChannelId.None)
            : this((SoundRef)sfxSound, position, channelId)
        {
        }

        public PlaySoundEvent(BgmSounds bgmSound, Vector3 position, SoundChannelId channelId = SoundChannelId.Bgm)
            : this((SoundRef)bgmSound, position, channelId)
        {
        }
    }

    public sealed class StopSoundEvent : GameEvent
    {
        public SoundChannelId ChannelId { get; }

        public StopSoundEvent(SoundChannelId channelId)
        {
            ChannelId = channelId;
        }
    }

    public sealed class PlayManagedSoundEvent : GameEvent
    {
        public SoundRef Sound { get; }
        public Vector3 Position { get; }
        public SoundChannelId ChannelId { get; }
        public float FadeInDuration { get; }
        public float FadeOutDuration { get; }
        public bool CrossfadeExisting { get; }

        public PlayManagedSoundEvent(
            SoundRef sound,
            Vector3 position,
            SoundChannelId channelId,
            float fadeInDuration,
            float fadeOutDuration,
            bool crossfadeExisting = false)
        {
            Sound = sound;
            Position = position;
            ChannelId = channelId;
            FadeInDuration = fadeInDuration;
            FadeOutDuration = fadeOutDuration;
            CrossfadeExisting = crossfadeExisting;
        }

        public PlayManagedSoundEvent(
            SfxSounds sfxSound,
            Vector3 position,
            SoundChannelId channelId,
            float fadeInDuration,
            float fadeOutDuration)
            : this((SoundRef)sfxSound, position, channelId, fadeInDuration, fadeOutDuration)
        {
        }

        public PlayManagedSoundEvent(
            BgmSounds bgmSound,
            Vector3 position,
            SoundChannelId channelId,
            float fadeInDuration,
            float fadeOutDuration,
            bool crossfadeExisting)
            : this((SoundRef)bgmSound, position, channelId, fadeInDuration, fadeOutDuration, crossfadeExisting)
        {
        }
    }

    public sealed class StopManagedSoundEvent : GameEvent
    {
        public SoundChannelId ChannelId { get; }
        public float FadeOutDuration { get; }

        public StopManagedSoundEvent(SoundChannelId channelId, float fadeOutDuration)
        {
            ChannelId = channelId;
            FadeOutDuration = fadeOutDuration;
        }
    }

    public sealed class PauseSoundEvent : GameEvent
    {
        public SoundChannelId ChannelId { get; }
        public PauseSoundEvent(SoundChannelId channelId) => ChannelId = channelId;
    }

    public sealed class ResumeSoundEvent : GameEvent
    {
        public SoundChannelId ChannelId { get; }
        public ResumeSoundEvent(SoundChannelId channelId) => ChannelId = channelId;
    }
}
