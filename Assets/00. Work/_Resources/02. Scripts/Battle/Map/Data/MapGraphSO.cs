using System.Collections.Generic;
using Battle.Map.Enums;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Battle.Map.Data
{
    [CreateAssetMenu(fileName = "New Map Graph", menuName = "Battle/Map/Map Graph")]
    public class MapGraphSO : ScriptableObject
    {
        public List<MapNodeDefinition> nodes = new();

        public MapNodeDefinition GetNode(string nodeId)
        {
            return nodes.Find(n => n.nodeId == nodeId);
        }

        public MapNodeDefinition GetStartNode()
        {
            return nodes.Find(n => n.nodeType == MapNodeType.Start);
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Generate Node IDs")]
        private void AutoGenerateNodeIds()
        {
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.nodeId))
                    node.nodeId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MapGraphSO] {nodes.Count}개 노드 ID 생성 완료.");
        }
#endif

        // Start 노드를 제외한 노드들의 최대 floorIndex (마지막 실질 층)
        public int GetMaxFloorIndex()
        {
            int max = 0;
            foreach (var node in nodes)
            {
                if (node.nodeType != MapNodeType.Start && node.floorIndex > max)
                    max = node.floorIndex;
            }
            return max;
        }
    }
}
