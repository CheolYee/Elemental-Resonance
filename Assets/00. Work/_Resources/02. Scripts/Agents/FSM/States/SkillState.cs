using _02._Scripts.CombatSystem.Skills;

namespace _00._Work._Resources._02._Scripts.Agents.FSM
{
    public class SkillState : AgentState
    {
        private readonly SkillModule _skillModule;

        public SkillState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _skillModule = agent.GetModule<SkillModule>();
        }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            SkillDataSO skill = _skillModule?.CurrentSkill;
            if (skill?.animParam != null)
                _renderer.PlayClip(skill.animParam.ParamHash, 0f, transitionDuration, layerIndex);

            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd += HandleAnimationEnd;
        }

        private void HandleAnimationEnd()
        {
            _agentTrigger.OnAnimationEnd -= HandleAnimationEnd;
            CompleteState();
        }

        public override void Exit()
        {
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd -= HandleAnimationEnd;
        }
    }
}
