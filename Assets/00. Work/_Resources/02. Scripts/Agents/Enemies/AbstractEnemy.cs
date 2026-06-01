using _00._Work._Resources._02._Scripts.Agents.FSM;
using Agents.FSM;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public abstract class AbstractEnemy : Agent
    {
        [SerializeField] private StateListSO enemyStates;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            StateMachine = new StateMachine(this, enemyStates.states);
        }
    }
}
