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
        [Tooltip("적 최대 HP에 곱할 배수. 1이면 기본값.")]
        public float hpMultiplier = 1f;
    }
}
