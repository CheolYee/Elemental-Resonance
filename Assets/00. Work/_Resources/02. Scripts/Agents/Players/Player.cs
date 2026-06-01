using _00._Work._Resources._02._Scripts.Agents.FSM;
using Agents.FSM;
using Gamelib.EventSystem;
using Systems;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Players
{
    public class Player : Agent
    {
        [Header("Player values")] 
        [field: SerializeField] public EventChannelSO PlayerEventChannel { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO playerStates;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            StateMachine = new StateMachine(this, playerStates.states);
        }

        private void Start()
        {
            StateMachine.ChangeState(0, transitionDuration: 0);
        }

        private void Update()
        {
            StateMachine.UpdateMachine();
        }
        
        /*public void ChangeState(PlayerState newStateIndex, float transitionDuration)
            => _stateMachine.ChangeState((int)newStateIndex, transitionDuration);*/
    }
}