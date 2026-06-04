using System.Collections.Generic;
using _02._Scripts.CombatSystem.Skills;
using Battle.Effects;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "New Enemy Data", menuName = "Battle/Enemy Data", order = 0)]
    public class EnemyDataSO : ScriptableObject
    {
        public string enemyId;
        public string enemyName;
        public GameObject enemyPrefab;
        public int maxHp;
        public SkillDataSO attackSkill;
        public List<CardEffectSO> attackEffects;
    }
}
