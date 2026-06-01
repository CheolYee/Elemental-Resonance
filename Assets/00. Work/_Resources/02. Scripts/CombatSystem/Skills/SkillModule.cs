using System;
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public class SkillModule : MonoBehaviour, IModule, ISkillModule
    {
        [SerializeField] private SkillDataSO[] skills;
        [SerializeField] private int skillStateIndex;
        [SerializeField] private int idleStateIndex;

        public ModuleOwner Owner { get; private set; }
        public SkillDataSO CurrentSkill { get; private set; }

        public event Action OnCurrentSkillEnd;

        private AgentState _trackedState;

        public void Initialize(ModuleOwner owner) => Owner = owner;

        public bool CanUseSkill(int skillIndex, GameObject target = null)
            => skillIndex >= 0 && skillIndex < skills.Length;

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (!CanUseSkill(skillIndex)) return;
            if (Owner is not Agent agent) return;

            CurrentSkill = skills[skillIndex];

            if (_trackedState != null)
            {
                _trackedState.OnStateCompleted -= HandleStateCompleted;
                _trackedState = null;
            }

            agent.StateMachine.ChangeState(skillStateIndex);

            _trackedState = agent.StateMachine.CurrentState;
            _trackedState.OnStateCompleted += HandleStateCompleted;
        }

        public void InvokeSkillEnd() => OnCurrentSkillEnd?.Invoke();

        public void StopSkillIfNotFinished()
        {
            if (_trackedState == null) return;
            _trackedState.OnStateCompleted -= HandleStateCompleted;
            _trackedState = null;
        }

        private void HandleStateCompleted()
        {
            if (_trackedState != null)
            {
                _trackedState.OnStateCompleted -= HandleStateCompleted;
                _trackedState = null;
            }

            // Phase 4: 시네머신 카메라 전환, 파티클, 데미지 타이밍 처리 예정
            if (Owner is Agent agent)
                agent.StateMachine.ChangeState(idleStateIndex);

            InvokeSkillEnd();
        }
    }
}
