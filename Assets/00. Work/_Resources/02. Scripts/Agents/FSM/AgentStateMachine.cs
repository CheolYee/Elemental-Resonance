using System;
using System.Collections.Generic;
using Agents;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.FSM
{
    public class AgentStateMachine
    {
        public AgentState CurrentState { get; private set; }

        private Dictionary<int, AgentState> _stateDict;

        public AgentStateMachine(Agent owner, StateSO[] stateList)
        {
            _stateDict = new Dictionary<int, AgentState>();

            foreach (StateSO state in stateList)
            {
                Type type = Type.GetType(state.className); //해당 클래스 이름을 기반으로 타입정보를 가져온다.
                Debug.Assert(type != null, $"State class not found: {state.className}");
                
                AgentState agentState = Activator.CreateInstance(type, owner, state.stateParam) as AgentState;
                
                _stateDict.Add(state.stateIndex, agentState);
            }
        }

        public void ChangeState(int nextStateIndex)
        {
            CurrentState?.Exit();
            AgentState nextState = _stateDict.GetValueOrDefault(nextStateIndex);
            Debug.Assert(nextState != null, $"State not found: {nextStateIndex}");
            
            CurrentState = nextState;
            CurrentState.Enter();
        }
        
        public void UpdateMachine() => CurrentState?.Update();
    }
}