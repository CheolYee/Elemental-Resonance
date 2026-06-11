using System.Collections.Generic;
using Battle.Map.Data;
using Battle.Map.Enums;

namespace Battle.Map.Runtime
{
    public class MapRouteRuleService
    {
        // Start 노드 기준 첫 selectable 계산
        public List<string> GetInitialSelectableNodeIds(MapGraphSO graph)
        {
            var startNode = graph.GetStartNode();
            if (startNode == null) return new List<string>();
            return new List<string>(startNode.nextNodeIds);
        }

        // 현재 노드 기준 다음 selectable 계산 (미해결 outgoing 노드)
        public List<string> GetSelectableNodeIds(MapGraphSO graph, RunMapState state)
        {
            if (string.IsNullOrEmpty(state.currentNodeId))
                return GetInitialSelectableNodeIds(graph);

            var currentNode = graph.GetNode(state.currentNodeId);
            if (currentNode == null) return new List<string>();

            var selectable = new List<string>();
            foreach (var nextId in currentNode.nextNodeIds)
            {
                if (!state.IsResolved(nextId))
                    selectable.Add(nextId);
            }
            return selectable;
        }

        public bool IsLastFloorNode(MapGraphSO graph, string nodeId)
        {
            var node = graph.GetNode(nodeId);
            if (node == null) return false;
            return node.floorIndex == graph.GetMaxFloorIndex();
        }

        // 현재 노드가 마지막 층이고 Resolved 상태면 런 클리어 조건 충족
        public bool IsRunComplete(MapGraphSO graph, RunMapState state)
        {
            if (string.IsNullOrEmpty(state.currentNodeId)) return false;
            return IsLastFloorNode(graph, state.currentNodeId)
                   && state.IsResolved(state.currentNodeId);
        }
    }
}
