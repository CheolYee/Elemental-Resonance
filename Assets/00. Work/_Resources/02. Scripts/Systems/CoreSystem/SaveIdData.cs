using _00._Work._Resources._02._Scripts.Systems.Database;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.CoreSystem
{
    [CreateAssetMenu(fileName = "Save ID", menuName = "System/Save ID", order = 0)]
    public class SaveIdData : IndexedAsset
    {
        public int Id { get => AssetIndex; private set => AssetIndex = value; }
        [SerializeField, TextArea] private string description;
    }
}