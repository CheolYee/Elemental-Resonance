using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "EnemyRegistry", menuName = "Battle/Enemy Registry")]
    public class RuntimeEnemyRegistrySO : ScriptableObject
    {
        private readonly List<AbstractEnemy> _enemies = new();

        public IReadOnlyList<AbstractEnemy> Enemies => _enemies;

        public void Register(AbstractEnemy enemy) => _enemies.Add(enemy);
        public void Unregister(AbstractEnemy enemy) => _enemies.Remove(enemy);
        public void Clear() => _enemies.Clear();
    }
}
