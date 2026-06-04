using _00._Work._Resources._02._Scripts.Agents;
using _00Work._Resources._02Scripts.Agents.Enemies;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public class EnemyEntryState : AgentEnemyState
    {
        public EnemyEntryState(Agent agent, int stateClipHash) : base(agent, stateClipHash) { }

        public override void Enter(float transitionDuration, int layerIndex = 0)
        {
            base.Enter(transitionDuration, layerIndex);
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
