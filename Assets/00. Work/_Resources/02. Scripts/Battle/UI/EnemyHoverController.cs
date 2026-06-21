using System.Collections.Generic;
using System.Linq;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using Battle.Events;
using Battle.Modules;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Battle.UI
{
    public class EnemyHoverController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private LayerMask targetLayerMask;

        private readonly List<TargetingModule> _registeredModules = new();
        private AbstractEnemy _hoveredEnemy;
        private bool _isReady;

        private void OnEnable()
        {
            battleEventChannel.AddListener<TargetableRegisteredEvent>(OnRegistered);
            battleEventChannel.AddListener<TargetableDeregisteredEvent>(OnDeregistered);
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.AddListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
            battleEventChannel.AddListener<SkillQueueStartedEvent>(OnSkillQueueStarted);
            battleEventChannel.AddListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.AddListener<SkillQueueCompletedEvent>(OnSkillQueueCompleted);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<TargetableRegisteredEvent>(OnRegistered);
            battleEventChannel.RemoveListener<TargetableDeregisteredEvent>(OnDeregistered);
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.RemoveListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
            battleEventChannel.RemoveListener<SkillQueueStartedEvent>(OnSkillQueueStarted);
            battleEventChannel.RemoveListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.RemoveListener<SkillQueueCompletedEvent>(OnSkillQueueCompleted);
        }

        private void OnSessionStart(BattleSessionStartEvent _) => SetReady(false);
        private void OnWaveStart(WaveStartEvent _) => SetReady(false);
        private void OnEnemyTurnStart(EnemyTurnStartEvent _) => SetReady(false);
        private void OnSkillQueueStarted(SkillQueueStartedEvent _) => SetReady(false);
        private void OnPlayerTurnStart(PlayerTurnStartEvent _) => _isReady = true;
        private void OnSkillQueueCompleted(SkillQueueCompletedEvent _) => _isReady = true;

        private void SetReady(bool ready)
        {
            _isReady = ready;
            if (!ready && _hoveredEnemy != null)
            {
                _hoveredEnemy = null;
                battleEventChannel.RaiseEvent(new EnemyHoverExitEvent());
            }
        }

        private void OnRegistered(TargetableRegisteredEvent evt)
        {
            if (evt.Module.TargetGroup == Battle.Enums.TargetGroup.Enemy)
                _registeredModules.Add(evt.Module);
        }

        private void OnDeregistered(TargetableDeregisteredEvent evt)
        {
            _registeredModules.Remove(evt.Module);
            if (_hoveredEnemy != null && evt.Module.GetComponentInParent<AbstractEnemy>() == _hoveredEnemy)
            {
                _hoveredEnemy = null;
                battleEventChannel.RaiseEvent(new EnemyHoverExitEvent());
            }
        }

        private void Update()
        {
            if (!_isReady || _registeredModules.Count == 0) return;

            var ray = battleCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            AbstractEnemy hit = null;

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, targetLayerMask))
            {
                var module = _registeredModules.FirstOrDefault(m => m.Collider == hitInfo.collider);
                if (module != null)
                    hit = module.GetComponentInParent<AbstractEnemy>();
            }

            if (hit == _hoveredEnemy) return;

            _hoveredEnemy = hit;

            if (_hoveredEnemy != null)
                battleEventChannel.RaiseEvent(new EnemyHoverEvent(_hoveredEnemy));
            else
                battleEventChannel.RaiseEvent(new EnemyHoverExitEvent());
        }
    }
}
