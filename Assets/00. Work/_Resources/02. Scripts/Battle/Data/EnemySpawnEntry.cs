using System;
using UnityEngine;

namespace Battle.Data
{
    [Serializable]
    public class EnemySpawnEntry
    {
        public EnemyDataSO enemyData;
        public int slotIndex;
        public Vector3 localOffset;
        public bool isLargeEnemy;
    }
}
