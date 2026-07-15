using System;
using System.Collections.Generic;

namespace Battle.Data
{
    [Serializable]
    public class RunSaveData
    {
        public int gold;
        public int currentHp;
        public int maxHp;
        public int floorIndex;
        public List<string> startingCardIds = new();
        public List<string> cardIds         = new();
        public int    seed;
        public string graphId;
        public string currentNodeId;
        public List<string> visitedNodeIds  = new();
        public List<string> resolvedNodeIds = new();
    }
}
