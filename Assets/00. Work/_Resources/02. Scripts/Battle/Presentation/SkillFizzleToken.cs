using System.Threading;

namespace Battle.Presentation
{
    public sealed class SkillFizzleToken
    {
        private readonly CancellationTokenSource _cts = new();

        public bool IsFizzled => _cts.IsCancellationRequested;
        public CancellationToken CancellationToken => _cts.Token;

        public void RequestFizzle() => _cts.Cancel();
    }
}
