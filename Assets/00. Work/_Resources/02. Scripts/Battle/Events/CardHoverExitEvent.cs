using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardHoverExitEvent : GameEvent
    {
        public CardInstance CardInstance { get; }
        public CardHoverExitEvent(CardInstance cardInstance) { CardInstance = cardInstance; }
    }
}
