using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CostGainRevealedEvent : GameEvent
    {
        public int Amount { get; }

        public CostGainRevealedEvent(int amount)
        {
            Amount = amount;
        }
    }
}
