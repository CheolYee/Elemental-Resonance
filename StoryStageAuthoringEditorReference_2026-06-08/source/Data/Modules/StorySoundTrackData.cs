using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    public enum StoryBgmKeyOperation
    {
        Play,
        Stop,
    }

    public enum StoryBgmTransitionMode
    {
        HardReplace,
        Crossfade,
    }

    [Serializable]
    public sealed class StorySoundSettingsData
    {
        [Tooltip("SoundManager가 구독하는 EventChannelSO. 비어 있으면 이 라인의 사운드 키는 실행되지 않는다.")]
        public EventChannelSO soundChannel;

        [Tooltip("BGM key의 재생/정지 시 사용되는 페이드 시간.")]
        [Min(0f)]
        public float bgmFadeDuration = 0.25f;

        [Tooltip("SFX key의 인스턴스 envelope 및 line 종료 정리 시 사용되는 페이드 시간.")]
        [Min(0f)]
        public float sfxFadeDuration = 0.08f;

        public StorySoundSettingsData ShallowClone() => (StorySoundSettingsData)MemberwiseClone();

        public void CopyFrom(StorySoundSettingsData source)
        {
            if (source == null)
            {
                soundChannel = null;
                bgmFadeDuration = 0.25f;
                sfxFadeDuration = 0.08f;
                return;
            }

            soundChannel = source.soundChannel;
            bgmFadeDuration = source.bgmFadeDuration;
            sfxFadeDuration = source.sfxFadeDuration;
        }
    }

    [Serializable]
    public sealed class StoryBgmKeyframeData
    {
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Min(0f)]
        public float timeSeconds = 0f;

        public StoryBgmKeyOperation operation = StoryBgmKeyOperation.Play;

        public BgmSounds bgmSound;

        [Tooltip("HardReplace = 이전 BGM을 정리한 뒤 다음 BGM 시작, Crossfade = 이전 BGM fade-out과 새 BGM fade-in을 동시에 시작.")]
        public StoryBgmTransitionMode transitionMode = StoryBgmTransitionMode.HardReplace;

        public StoryBgmKeyframeData ShallowClone() => (StoryBgmKeyframeData)MemberwiseClone();
    }

    [Serializable]
    public sealed class StorySfxKeyframeData
    {
        [Range(0f, 1f)]
        public float normalizedTime = 0f;

        [Min(0f)]
        public float timeSeconds = 0f;

        public SfxSounds sfxSound;

        public StorySfxKeyframeData ShallowClone() => (StorySfxKeyframeData)MemberwiseClone();
    }

    [Serializable]
    public sealed class StorySoundTrackData
    {
        [Tooltip("Line 내부 BGM keyframes. 한 시점에는 한 key만 유지한다.")]
        public List<StoryBgmKeyframeData> bgmKeyframes = new();

        [Tooltip("Line 내부 SFX keyframes. 한 시점에는 한 key만 유지한다.")]
        public List<StorySfxKeyframeData> sfxKeyframes = new();

        public bool HasAnyKeyframes =>
            (bgmKeyframes?.Count ?? 0) > 0
            || (sfxKeyframes?.Count ?? 0) > 0;
    }

    public static class StorySoundSettingsUtility
    {
        private static readonly StorySoundSettingsData DefaultSettings = new();

        public static StorySoundSettingsData ResolveEpisodeDefaults(StoryEpisodeSO episode)
        {
            if (episode == null)
                return DefaultSettings;

            return episode.DefaultSoundSettings ?? DefaultSettings;
        }

        public static StorySoundSettingsData ResolveEffectiveLineSettings(StoryEpisodeSO episode, StoryStageLayoutModuleSO layout)
        {
            return ResolveEpisodeDefaults(episode);
        }
    }
}
