using Gamelib.EventSystem;

namespace Battle.Events
{
    public class DrawPileRefillEvent : GameEvent
    {
        public int RefillCount { get; }
        public DrawPileRefillEvent(int refillCount) => RefillCount = refillCount;
    }
}
