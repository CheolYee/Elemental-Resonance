using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "BattleCostModel", menuName = "Battle/Battle Cost Model", order = 3)]
    public class BattleCostModelSO : ScriptableObject
    {
        public int maxCost = 3;
        public int currentCost = 3;
    }
}
