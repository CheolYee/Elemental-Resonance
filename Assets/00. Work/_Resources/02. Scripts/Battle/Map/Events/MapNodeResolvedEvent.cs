using Gamelib.EventSystem;

namespace Battle.Map.Events
{
    public class MapNodeResolvedEvent : GameEvent
    {
        public string NodeId;

        public MapNodeResolvedEvent(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
