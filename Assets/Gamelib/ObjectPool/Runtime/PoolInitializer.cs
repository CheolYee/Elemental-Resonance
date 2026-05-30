using System;
using UnityEngine;

namespace Gamelib.ObjectPool.Runtime
{
    public class PoolInitializer : MonoBehaviour
    {
        [field: SerializeField] public PoolManagerSo PoolManager { get; private set; }

        private void Awake()
        {
            PoolManager.InitializePool(transform);

            PoolInitializer[] initializers = FindObjectsByType<PoolInitializer>(FindObjectsSortMode.None);
            Debug.Assert(initializers.Length == 1, $"씬에 PoolInitializer는 하나만 존재해야 합니다. 현재 개수 : {initializers.Length}");
        }
        
        public T Pop<T>(PoolItemSo type) where T : IPoolable => PoolManager.Pop<T>(type);
        public void Push(IPoolable item) => PoolManager.Push(item);
    }
}