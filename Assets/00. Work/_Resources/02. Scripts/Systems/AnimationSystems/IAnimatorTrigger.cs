using System;

namespace _00._Work._Resources._02._Scripts.Systems.AnimationSystems
{
    public interface IAnimatorTrigger
    {
        event Action OnAnimationEnd;
        event Action OnAttackTrigger;
        
        event Action<bool> OnCounterStateChange;
    }
}