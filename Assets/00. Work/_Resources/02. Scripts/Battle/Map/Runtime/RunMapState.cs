using System;
using System.Collections.Generic;
using Battle.Map.Enums;

namespace Battle.Map.Runtime
{
    [Serializable]
    public class RunMapState
    {
        public string graphId;
        public string currentNodeId;
        public string pendingNodeId;
        public List<string> visitedNodeIds = new();
        public List<string> resolvedNodeIds = new();
        public MapOverlayState overlayState = MapOverlayState.Hidden;

        public bool IsVisited(string nodeId) => visitedNodeIds.Contains(nodeId);
        public bool IsResolved(string nodeId) => resolvedNodeIds.Contains(nodeId);

        public void MarkVisited(string nodeId)
        {
            if (!visitedNodeIds.Contains(nodeId))
                visitedNodeIds.Add(nodeId);
        }

        public void MarkResolved(string nodeId)
        {
            if (!resolvedNodeIds.Contains(nodeId))
                resolvedNodeIds.Add(nodeId);
        }
    }
}
