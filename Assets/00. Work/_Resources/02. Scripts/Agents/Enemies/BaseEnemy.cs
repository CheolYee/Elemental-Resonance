using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public class BaseEnemy : AbstractEnemy
    {
        private void Start()
        {
            StateMachine.ChangeState(0, transitionDuration: 0);
        }

        private void Update()
        {
            StateMachine.UpdateMachine();
        }
    }
}
