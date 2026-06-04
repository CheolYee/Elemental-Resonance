using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardReturnToHandEvent : GameEvent
    {
        public CardInstance CardInstance { get; }

        public CardReturnToHandEvent(CardInstance cardInstance)
        {
            CardInstance = cardInstance;
        }
    }
}
