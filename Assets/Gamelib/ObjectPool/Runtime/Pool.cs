using System.Collections.Generic;
using UnityEngine;

namespace Gamelib.ObjectPool.Runtime
{
    public class Pool
    {
        private readonly Stack<IPoolable> _pool;
        private readonly Transform _parent;
        private readonly GameObject _prefab;

        public Pool(PoolItemSo poolItemSo, Transform parent, int initCount)
        {
            _parent = parent;
            _prefab = poolItemSo.prefab;
            _pool = new Stack<IPoolable>(initCount);

            for (int i = 0; i < initCount; i++)
            {
                GameObject obj = Object.Instantiate(_prefab, _parent);
                obj.SetActive(false);
                IPoolable poolable = obj.GetComponent<IPoolable>();
                Debug.Assert(poolable != null, $"Poolable component is missing on prefab {_prefab.name}");
                
                _pool.Push(poolable);
            }
        }

        public IPoolable Pop()
        {
            IPoolable item;
            if (_pool.Count == 0)
            {
                GameObject obj = Object.Instantiate(_prefab, _parent);
                item = obj.GetComponent<IPoolable>();
            }
            else
            {
                item = _pool.Pop();
                item.GameObject.SetActive(true);
            }
            item.ResetItem();
            return item;
        }
        
        public void Push(IPoolable item)
        {
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}