using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardDragStartEvent : GameEvent
    {
        public CardInstance CardInstance { get; }

        public CardDragStartEvent(CardInstance cardInstance)
        {
            CardInstance = cardInstance;
        }
    }
}
