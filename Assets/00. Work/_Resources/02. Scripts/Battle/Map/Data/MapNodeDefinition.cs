using System;
using System.Collections.Generic;
using Battle.Data;
using Battle.Map.Enums;
using UnityEngine;

namespace Battle.Map.Data
{
    [Serializable]
    public class MapNodeDefinition
    {
        [Header("ID (읽기 전용 — Auto Generate로 생성)")][SerializeField] public string nodeId;
        public int floorIndex;
        public float xOffset;
        public MapNodeType nodeType;
        public List<string> nextNodeIds = new();

        public BattleStageSO stageRef;
        public RestContentSO restContent;
        public ShopContentSO shopContent;
    }
}
