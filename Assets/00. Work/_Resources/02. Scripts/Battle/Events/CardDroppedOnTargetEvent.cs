using Battle.Instances;
using Battle.Modules;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardDroppedOnTargetEvent : GameEvent
    {
        public CardInstance CardInstance { get; }
        public TargetingModule Target { get; }

        public CardDroppedOnTargetEvent(CardInstance cardInstance, TargetingModule target)
        {
            CardInstance = cardInstance;
            Target = target;
        }
    }
}
