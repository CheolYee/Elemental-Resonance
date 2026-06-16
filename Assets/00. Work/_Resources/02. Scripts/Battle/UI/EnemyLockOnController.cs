using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class EnemyLockOnController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private EnemyLockOnView viewPrefab;
        [SerializeField] private RectTransform viewParent;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private int poolSize = 3;

        private readonly List<EnemyLockOnView> _pool = new();
        private readonly Dictionary<AbstractEnemy, EnemyLockOnView> _active = new();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var view = Instantiate(viewPrefab, viewParent);
                view.gameObject.SetActive(false);
                _pool.Add(view);
            }
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<EnemyHoverEvent>(OnEnemyHover);
            battleEventChannel.AddListener<EnemyHoverExitEvent>(OnEnemyHoverExit);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<EnemyHoverEvent>(OnEnemyHover);
            battleEventChannel.RemoveListener<EnemyHoverExitEvent>(OnEnemyHoverExit);
        }

        private void OnEnemyHover(EnemyHoverEvent evt) => Show(new List<AbstractEnemy> { evt.Enemy });
        private void OnEnemyHoverExit(EnemyHoverExitEvent _) => HideAll();

        public void Show(List<AbstractEnemy> enemies)
        {
            var toRemove = new List<AbstractEnemy>();
            foreach (var kvp in _active)
                if (!enemies.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);

            foreach (var enemy in toRemove)
            {
                _active[enemy].Hide();
                _active.Remove(enemy);
            }

            foreach (var enemy in enemies)
            {
                if (_active.ContainsKey(enemy)) continue;
                var view = GetAvailableView();
                if (view == null) continue;
                view.Show(enemy, battleCamera);
                _active[enemy] = view;
            }
        }

        public void HideAll()
        {
            foreach (var view in _active.Values)
                view.Hide();
            _active.Clear();
        }

        private EnemyLockOnView GetAvailableView()
        {
            foreach (var view in _pool)
                if (!_active.ContainsValue(view))
                    return view;
            return null;
        }
    }
}
