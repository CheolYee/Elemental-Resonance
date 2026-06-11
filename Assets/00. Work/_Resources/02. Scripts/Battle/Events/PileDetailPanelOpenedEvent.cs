using Battle.Enums;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class PileDetailPanelOpenedEvent : GameEvent
    {
        public PileDisplayTarget Target { get; }

        public PileDetailPanelOpenedEvent(PileDisplayTarget target)
        {
            Target = target;
        }
    }
}
