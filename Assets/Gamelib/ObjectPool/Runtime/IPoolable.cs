using UnityEngine;

namespace Gamelib.ObjectPool.Runtime
{
    public interface IPoolable
    {
        public PoolItemSo PoolItem { get; set; }
        public GameObject GameObject { get; }
        public void ResetItem();
    }
}