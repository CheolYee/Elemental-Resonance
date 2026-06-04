using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CostChangedEvent : GameEvent
    {
        public int CurrentCost { get; }

        public CostChangedEvent(int currentCost)
        {
            CurrentCost = currentCost;
        }
    }
}
