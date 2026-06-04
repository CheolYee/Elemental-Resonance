using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.FSM;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public abstract class AgentEnemyState : AgentState
    {
        protected readonly AbstractEnemy _enemy;

        public AgentEnemyState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _enemy = agent as AbstractEnemy;
            Debug.Assert(_enemy != null, $"{GetType().Name}은 AbstractEnemy 오너가 필요합니다.");
        }
    }
}
