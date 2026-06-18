using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Battle.Map.UI
{
    public class InteractableObject : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Collider _collider;

        [Header("Outline")]
        [SerializeField] private float _defaultOutlineWidth = 4f;
        [SerializeField] private float _hoverOutlineWidth = 8f;
        [SerializeField] private Color _outlineColor = Color.white;

        public event Action OnClicked;

        private static readonly int OutlineWidthId     = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColorId     = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlinePureColorId = Shader.PropertyToID("_OutlinePureColor");
        private const uint ForegroundLayerBit = 1u << 1;

        private Renderer[] _renderers;
        private bool _isHovered;
        private bool _isInteractable;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            if (interactable)
            {
                _isHovered = false;
                SetOutlineDefault();
            }
            else
            {
                _isHovered = false;
                SetOutlineOff();
            }
        }

        private void Update()
        {
            if (!_isInteractable) return;

            var cam = _camera != null ? _camera : Camera.main;
            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, _layerMask)
                       && hitInfo.collider == _collider;

            if (hit != _isHovered)
            {
                _isHovered = hit;
                if (_isHovered) SetOutlineOn();
                else SetOutlineDefault();
            }

            if (_isHovered && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _isInteractable = false;
                _isHovered = false;
                SetOutlineOff();
                OnClicked?.Invoke();
            }
        }

        private void SetOutlineDefault()
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.material.SetFloat(OutlineWidthId, _defaultOutlineWidth);
                r.material.SetColor(OutlineColorId, _outlineColor);
                r.material.SetFloat(OutlinePureColorId, 1f);
                r.renderingLayerMask |= ForegroundLayerBit;
            }
        }

        private void SetOutlineOn()
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.material.SetFloat(OutlineWidthId, _hoverOutlineWidth);
                r.material.SetColor(OutlineColorId, _outlineColor);
                r.material.SetFloat(OutlinePureColorId, 1f);
                r.renderingLayerMask |= ForegroundLayerBit;
            }
        }

        private void SetOutlineOff()
        {
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.material.SetFloat(OutlineWidthId, 0f);
                r.material.SetFloat(OutlinePureColorId, 0f);
                r.renderingLayerMask &= ~ForegroundLayerBit;
            }
        }
    }
}
