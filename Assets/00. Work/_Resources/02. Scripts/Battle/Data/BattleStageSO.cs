using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    public enum RewardProfile { Normal = 0, Elite = 1, Boss = 2 }

    [CreateAssetMenu(fileName = "New Battle Stage", menuName = "Battle/Stage", order = 1)]
    public class BattleStageSO : ScriptableObject
    {
        public string stageId;
        public List<WaveData> waves;
        [Tooltip("엘리트: 45, 보스: 90, 일반: 0")]
        public int goldBonus;
        public RewardProfile rewardProfile;
    }
}
