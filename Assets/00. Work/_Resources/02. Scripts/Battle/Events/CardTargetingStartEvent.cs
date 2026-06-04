using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardTargetingStartEvent : GameEvent
    {
        public CardInstance CardInstance { get; }

        public CardTargetingStartEvent(CardInstance cardInstance)
        {
            CardInstance = cardInstance;
        }
    }
}
