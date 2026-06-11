using Gamelib.EventSystem;

namespace Battle.Events
{
    public class PileCountChangedEvent : GameEvent
    {
        public int DrawCount { get; }
        public int HandCount { get; }
        public int DiscardCount { get; }
        public int GraveCount { get; }
        public int CurrentDeckCount { get; }

        public PileCountChangedEvent(int drawCount, int handCount, int discardCount, int graveCount, int currentDeckCount)
        {
            DrawCount = drawCount;
            HandCount = handCount;
            DiscardCount = discardCount;
            GraveCount = graveCount;
            CurrentDeckCount = currentDeckCount;
        }
    }
}
