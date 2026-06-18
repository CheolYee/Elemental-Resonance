using Gamelib.EventSystem;

namespace Battle.Events
{
    public class GoldChangedEvent : GameEvent
    {
        public int OldAmount { get; }
        public int NewAmount { get; }

        public GoldChangedEvent(int oldAmount, int newAmount)
        {
            OldAmount = oldAmount;
            NewAmount = newAmount;
        }
    }
}
