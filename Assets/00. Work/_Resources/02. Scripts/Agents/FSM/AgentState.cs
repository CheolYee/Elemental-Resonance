using System;
using Agents;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;

namespace _00._Work._Resources._02._Scripts.Agents.FSM
{
    public abstract class AgentState
    {
        protected readonly Agent _agent;
        protected readonly int _stateClipHash;
        protected readonly IRenderer _renderer;
        protected readonly AgentTrigger _agentTrigger;

        public event Action OnStateCompleted;

        public AgentState(Agent agent, int stateClipHash)
        {
            _agent = agent;
            _stateClipHash = stateClipHash;
            _renderer = agent.Renderer;
            _agentTrigger = agent.GetModule<AgentTrigger>();
        }

        public virtual void Enter(float transitionDuration, int layerIndex = 0)
        {
            _renderer.PlayClip(_stateClipHash, 0f, transitionDuration, layerIndex);
        }

        public virtual void Update() {}
        public virtual void Exit() {}

        protected void CompleteState() => OnStateCompleted?.Invoke();
    }
}