using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.FSM
{
    [CreateAssetMenu(fileName = "State data", menuName = "FSM/State data", order = 0)]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string className;
        public int stateIndex;
        public AnimParamSO stateParam;
    }
}