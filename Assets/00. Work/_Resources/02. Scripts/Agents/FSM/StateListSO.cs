using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.FSM
{
    [CreateAssetMenu(fileName = "FSM state manager", menuName = "FSM/State list", order = 10)]
    public class StateListSO : ScriptableObject
    {
        [HideInInspector] public string generatePath;
        public string enumName;
        public StateSO[] states;
    }
}