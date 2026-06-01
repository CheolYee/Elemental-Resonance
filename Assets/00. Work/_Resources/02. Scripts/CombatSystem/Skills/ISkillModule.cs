using System;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public interface ISkillModule
    {
        ModuleOwner Owner { get; }

        event Action OnCurrentSkillEnd;
        bool CanUseSkill(int skillIndex, GameObject target = null);
        void UseSkill(int skillIndex, GameObject target = null);
        void InvokeSkillEnd();
        void StopSkillIfNotFinished();
    }
}