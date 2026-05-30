using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.AnimationSystems
{
    public interface IRenderer
    {
        float FacingDirection { get; }
        void SetAnimator(RuntimeAnimatorController animator);
        void PlayClip(int clipHash, int layer = -1, float normalizedTime = 0);
        void SetBool(AnimParamSO param, bool value);
        void SetFloat(AnimParamSO param, float value);
        void SetInt(AnimParamSO param, int value);
        void SetTrigger(AnimParamSO param);
        void FlipController(float xMoveDirection);
        void Flip();
    }
}