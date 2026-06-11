using Gamelib.EventSystem;

namespace Battle.Map.Events
{
    public class MapNodeSelectedEvent : GameEvent
    {
        public string NodeId;

        public MapNodeSelectedEvent(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
