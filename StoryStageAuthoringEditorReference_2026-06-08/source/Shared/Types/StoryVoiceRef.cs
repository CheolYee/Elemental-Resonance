using System;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types
{
    [Serializable]
    public sealed class StoryVoiceRef //혹시 추가하고싶긴하지만 안될것같지만 아무튼 희망사항
    {
        // 스토리 대사에 음성 정보가 포함될 때 사용하는 참조 클래스
        public string voiceKey;
        public VoicePlaybackPolicy playbackPolicy = VoicePlaybackPolicy.None;
        public bool allowReplayInLog = true;
    }
}