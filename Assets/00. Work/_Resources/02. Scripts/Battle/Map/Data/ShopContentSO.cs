using UnityEngine;

namespace Battle.Map.Data
{
    [CreateAssetMenu(fileName = "New Shop Content", menuName = "Battle/Map/Shop Content")]
    public class ShopContentSO : ScriptableObject
    {
        public string displayName;
        public string descriptionText;
    }
}
