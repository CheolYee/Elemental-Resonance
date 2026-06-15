using System.Collections.Generic;
using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class SkillQueueChangedEvent : GameEvent
    {
        public CardInstance Current  { get; }
        public IReadOnlyList<CardInstance> Pending { get; }

        public SkillQueueChangedEvent(CardInstance current, IReadOnlyList<CardInstance> pending)
        {
            Current = current;
            Pending = pending;
        }
    }
}
