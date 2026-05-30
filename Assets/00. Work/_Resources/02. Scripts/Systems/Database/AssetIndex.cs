using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.Database
{
    public abstract class IndexedAsset : ScriptableObject
    {
        [field: SerializeField] public int AssetIndex { get; set; }
    }
}