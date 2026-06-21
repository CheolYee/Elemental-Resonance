using System.Collections.Generic;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class DiscardSelectRequestEvent : GameEvent
    {
        public int DiscardCount { get; }
        public UniTaskCompletionSource<List<CardInstance>> Tcs { get; }

        public DiscardSelectRequestEvent(int discardCount, UniTaskCompletionSource<List<CardInstance>> tcs)
        {
            DiscardCount = discardCount;
            Tcs = tcs;
        }
    }
}
