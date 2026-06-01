using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "New Battle Stage", menuName = "Battle/Stage", order = 1)]
    public class BattleStageSO : ScriptableObject
    {
        public string stageId;
        public List<EnemySpawnEntry> enemies;
    }
}
