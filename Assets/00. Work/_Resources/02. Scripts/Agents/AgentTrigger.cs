using System;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace Agents
{
    public class AgentTrigger : MonoBehaviour, IModule
    {
        public event Action OnAnimationEnd;
        public event Action OnDamageCast;
        
        public void Initialize(ModuleOwner owner)
        {
            //당장 할게 없다.    
        }
        
        private void AnimationEndTrigger() => OnAnimationEnd?.Invoke();
        private void DamageCastTrigger() => OnDamageCast?.Invoke();
    }
}