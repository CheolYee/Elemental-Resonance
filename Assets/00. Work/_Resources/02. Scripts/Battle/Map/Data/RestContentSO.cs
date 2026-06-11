using UnityEngine;

namespace Battle.Map.Data
{
    [CreateAssetMenu(fileName = "New Rest Content", menuName = "Battle/Map/Rest Content")]
    public class RestContentSO : ScriptableObject
    {
        public string displayName;
        public string descriptionText;
    }
}
