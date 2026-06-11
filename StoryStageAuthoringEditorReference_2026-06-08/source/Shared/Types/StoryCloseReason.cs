namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types
{
    /// 스토리 종료 이유를 나타내는 열거형입니다.
    public enum StoryCloseReason
    {
        Completed = 0,
        UserClosed = 1,
        Skipped = 2,
        Aborted = 3,
        ExternalRequest = 4,
    }
}