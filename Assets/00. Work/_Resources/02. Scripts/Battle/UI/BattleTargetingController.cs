using System.Collections.Generic;
using System.Linq;
using Battle.Enums;
using Battle.Events;
using Battle.Modules;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Battle.UI
{
    public class BattleTargetingController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private LayerMask targetLayerMask;

        public bool HasValidHoverTarget => _hoveredTarget != null;
        public TargetingModule CurrentHoveredTarget => _hoveredTarget;

        private readonly List<TargetingModule> _registeredTargets = new();
        private List<TargetingModule> _validTargets = new();
        private TargetingModule _hoveredTarget;
        private bool _isTargeting;
        private CardTargetType _currentTargetType;

        private void OnEnable()
        {
            battleEventChannel.AddListener<TargetableRegisteredEvent>(OnTargetableRegistered);
            battleEventChannel.AddListener<TargetableDeregisteredEvent>(OnTargetableDeregistered);
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<TargetableRegisteredEvent>(OnTargetableRegistered);
            battleEventChannel.RemoveListener<TargetableDeregisteredEvent>(OnTargetableDeregistered);
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void Update()
        {
            if (!_isTargeting || _validTargets.Count == 0) return;

            var ray = battleCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            TargetingModule hit = null;
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, targetLayerMask))
                hit = _validTargets.FirstOrDefault(t => t.Collider == hitInfo.collider);

            if (_currentTargetType == CardTargetType.AllEnemies)
            {
                bool anyHit = hit != null;
                bool wasHovered = _hoveredTarget != null;
                if (anyHit == wasHovered) return;

                _hoveredTarget = anyHit ? hit : null;
                foreach (var t in _validTargets)
                {
                    if (anyHit) t.SetOutlineHovered();
                    else t.SetOutlineValid();
                }
                return;
            }

            if (hit == _hoveredTarget) return;
            _hoveredTarget?.SetOutlineValid();
            _hoveredTarget = hit;
            _hoveredTarget?.SetOutlineHovered();
        }

        private void OnTargetableRegistered(TargetableRegisteredEvent evt)
            => _registeredTargets.Add(evt.Module);

        private void OnTargetableDeregistered(TargetableDeregisteredEvent evt)
        {
            _registeredTargets.Remove(evt.Module);
            if (_hoveredTarget == evt.Module) _hoveredTarget = null;
        }

        private void OnTargetingStart(CardTargetingStartEvent evt)
        {
            _isTargeting = true;
            _hoveredTarget = null;
            _currentTargetType = evt.CardInstance.data.targetType;

            _validTargets = evt.CardInstance.data.targetType switch
            {
                CardTargetType.SingleEnemy => _registeredTargets
                    .Where(t => t.TargetGroup == TargetGroup.Enemy).ToList(),
                CardTargetType.SingleAlly => _registeredTargets
                    .Where(t => t.TargetGroup == TargetGroup.Ally).ToList(),
                CardTargetType.AllEnemies => _registeredTargets
                    .Where(t => t.TargetGroup == TargetGroup.Enemy).ToList(),
                CardTargetType.RandomEnemy => new List<TargetingModule>(),
                _ => new List<TargetingModule>()
            };

            foreach (var target in _validTargets)
                target.SetOutlineValid();
        }

        private void OnTargetingEnd(CardTargetingEndEvent evt)
        {
            _isTargeting = false;

            foreach (var target in _validTargets)
                target.RestoreOutline();

            _validTargets.Clear();
            _hoveredTarget = null;
        }
    }
}
