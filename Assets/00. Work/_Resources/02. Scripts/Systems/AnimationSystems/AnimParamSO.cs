using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.AnimationSystems
{
    [CreateAssetMenu(fileName = "Anim parameter", menuName = "Animator/Param", order = 5)]
    public class AnimParamSO : ScriptableObject
    {
        [field: SerializeField] public string ParamName { get; private set; }
        [field: SerializeField] public int ParamHash { get; private set; }

        private void OnValidate()
        {
            ParamHash = Animator.StringToHash(ParamName);
        }
    }
}