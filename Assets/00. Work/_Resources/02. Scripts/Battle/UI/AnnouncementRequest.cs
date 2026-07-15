using System;

namespace Battle.UI
{
    public struct AnnouncementRequest
    {
        public string PrimaryText;
        public string SecondaryText;
        public string PrimaryShow;
        public string PrimaryHide;
        public string SecondaryShow;
        public string SecondaryHide;
        public string BackgroundSequence;  // null이면 배경 없음, 지정 시 fire-and-forget으로 독립 실행
        public float HoldDuration;
        public Action OnShow;
        public Action OnComplete;
    }
}
