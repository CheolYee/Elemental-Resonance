using System;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public interface ISkill
    {
        event Action OnSkillEnd;
        SkillDataSO SkillData { get; }
        bool IsUsing { get; }
        
        float NormalizedCooldown { get; } // 0~ 1로 표현되는 쿨다운 값. 1일때 사용가능
        
        void InitializeSkill(ISkillModule skillModule);
        bool CanUseSkill(GameObject target = null); //타겟팅 스킬의 경우를 대비해서 타겟도 받아야 해.
        void UseSkill(GameObject target = null);
        void StopSkill(); //강제 종료
    }
}