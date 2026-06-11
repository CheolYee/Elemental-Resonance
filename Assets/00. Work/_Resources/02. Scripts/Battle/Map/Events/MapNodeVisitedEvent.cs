using Gamelib.EventSystem;

namespace Battle.Map.Events
{
    public class MapNodeVisitedEvent : GameEvent
    {
        public string NodeId;

        public MapNodeVisitedEvent(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
