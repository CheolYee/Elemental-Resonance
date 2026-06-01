using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.EnumSystem
{
    [CreateAssetMenu(fileName = "New Enum List", menuName = "Enum/Enum List", order = 0)]
    public class EnumListSO : ScriptableObject
    {
        public string enumName;
        public string namespaceName;
        public string[] valueNames;
        [HideInInspector] public string generatePath;
    }
}
