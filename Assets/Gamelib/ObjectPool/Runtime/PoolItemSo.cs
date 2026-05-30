using System;
using UnityEngine;

namespace Gamelib.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "Pool Item", menuName = "Object Pool/Pool Item", order = 10)]
    public class PoolItemSo : ScriptableObject
    {
        public string poolingName;
        public GameObject prefab;
        public int initCount;

        private void OnValidate()
        {
            if (prefab != null && !prefab.TryGetComponent(out IPoolable poolable))
            {
                Debug.LogWarning("풀링 프리팹에는 IPoolable을 구현한 스크립트가 적어도 1개 이상 있어야 합니다.");
                prefab = null;
            }
        }
    }
}