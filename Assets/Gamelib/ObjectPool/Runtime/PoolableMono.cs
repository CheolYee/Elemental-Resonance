using UnityEngine;

namespace Gamelib.ObjectPool.Runtime
{
    public abstract class PoolableMono : MonoBehaviour, IPoolable
    {
        [field: SerializeField] public PoolItemSo PoolItem { get; set; }
        public GameObject GameObject => this != null ? gameObject : null;
        public virtual void ResetItem() { }
    }
}