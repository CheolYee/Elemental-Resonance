using _00Work._Resources._02Scripts.Agents.Enemies;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Enemies
{
    public class BaseEnemy : AbstractEnemy
    {
        private void Start()
        {
            InitializeEntry();
        }

        private void Update()
        {
            StateMachine.UpdateMachine();
        }
    }
}
