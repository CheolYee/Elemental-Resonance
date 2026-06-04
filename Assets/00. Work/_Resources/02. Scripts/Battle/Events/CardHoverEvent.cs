using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardHoverEvent : GameEvent
    {
        public CardInstance CardInstance { get; }

        public CardHoverEvent(CardInstance cardInstance)
        {
            CardInstance = cardInstance;
        }
    }
}
