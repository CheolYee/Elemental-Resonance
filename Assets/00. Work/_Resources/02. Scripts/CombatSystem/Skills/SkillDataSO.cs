using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    [CreateAssetMenu(fileName = "Skill data", menuName = "Agent/Skill data", order = 25)]
    public class SkillDataSO : ScriptableObject
    {
        public AnimParamSO animParam;
    }
}