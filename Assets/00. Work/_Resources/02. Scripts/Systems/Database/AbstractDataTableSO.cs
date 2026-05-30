using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.Database
{
    public abstract class AbstractDataTableSO : ScriptableObject
    {
        [field: SerializeField] public string TableName { get; private set; }
        [field: SerializeField] public IndexedAsset[] AssetList { get; private set; } 
    }
}