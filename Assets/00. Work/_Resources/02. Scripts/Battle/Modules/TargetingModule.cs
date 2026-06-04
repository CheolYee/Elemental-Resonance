using _00._Work._Resources._02._Scripts.Agents.Enemies;
using Battle.Enums;
using Battle.Events;
using Gamelib.EventSystem;
using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace Battle.Modules
{
    public class TargetingModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private TargetGroup targetGroup;
        [SerializeField] private Collider targetCollider;

        [Header("Outline — Valid Target")]
        [SerializeField] private float validOutlineWidth = 4f;
        [SerializeField] private Color validOutlineColor = Color.white;

        [Header("Outline — Hovered Target")]
        [SerializeField] private float hoveredOutlineWidth = 8f;
        [SerializeField] private Color hoveredOutlineColor = Color.white;

        public TargetGroup TargetGroup => targetGroup;
        public string DisplayName
        {
            get
            {
                var enemy = GetComponentInParent<AbstractEnemy>();
                if (enemy != null && enemy.EnemyData != null)
                    return enemy.EnemyData.enemyName;
                return gameObject.name;
            }
        }
        public Collider Collider => targetCollider;

        // Rendering Layer 2 (bit 1) — RenderObjects 피처가 이 비트를 기준으로 필터링
        private const uint ForegroundLayerBit = 1u << 1;

        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlinePureColorId = Shader.PropertyToID("_OutlinePureColor");

        private Renderer[] _renderers;
        private float[] _originalWidths;
        private Color[] _originalColors;
        private uint[] _originalRenderingLayers;
        private bool _initialized;
        private bool _hasStarted;

        public void Initialize(ModuleOwner owner)
        {
            _renderers = owner.GetComponentsInChildren<Renderer>();
            _originalWidths = new float[_renderers.Length];
            _originalColors = new Color[_renderers.Length];
            _originalRenderingLayers = new uint[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _originalWidths[i] = mat != null && mat.HasFloat("_OutlineWidth")
                    ? mat.GetFloat("_OutlineWidth") : 1f;
                _originalColors[i] = mat != null && mat.HasColor("_OutlineColor")
                    ? mat.GetColor("_OutlineColor") : Color.gray;
                _originalRenderingLayers[i] = _renderers[i].renderingLayerMask;
            }
            _initialized = true;
        }

        private void Start()
        {
            _hasStarted = true;
            Register();
        }

        private void OnEnable()
        {
            if (_hasStarted) Register();
        }

        private void OnDisable()
        {
            if (battleEventChannel == null) return;
            battleEventChannel.RaiseEvent(new TargetableDeregisteredEvent(this));
        }

        private void Register()
        {
            if (battleEventChannel == null)
            {
                Debug.LogError($"[TargetingModule] battleEventChannel이 {gameObject.name}에 연결되지 않았습니다.", this);
                return;
            }
            battleEventChannel.RaiseEvent(new TargetableRegisteredEvent(this));
        }

        public void SetOutlineNone()
        {
            if (!_initialized) return;
            foreach (var r in _renderers)
                r.material.SetFloat(OutlineWidthId, 0f);
        }

        public void RestoreOutline()
        {
            if (!_initialized) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.SetFloat(OutlineWidthId, _originalWidths[i]);
                _renderers[i].material.SetColor(OutlineColorId, _originalColors[i]);
                _renderers[i].material.SetFloat(OutlinePureColorId, 0f);
                _renderers[i].renderingLayerMask = _originalRenderingLayers[i];
            }
        }

        public void SetOutlineValid()
        {
            if (!_initialized) return;
            foreach (var r in _renderers)
            {
                r.material.SetFloat(OutlineWidthId, validOutlineWidth);
                r.material.SetColor(OutlineColorId, validOutlineColor);
                r.material.SetFloat(OutlinePureColorId, 1f);
                r.renderingLayerMask |= ForegroundLayerBit;
            }
        }

        public void SetOutlineHovered()
        {
            if (!_initialized) return;
            foreach (var r in _renderers)
            {
                r.material.SetFloat(OutlineWidthId, hoveredOutlineWidth);
                r.material.SetColor(OutlineColorId, hoveredOutlineColor);
                r.material.SetFloat(OutlinePureColorId, 1f);
                r.renderingLayerMask |= ForegroundLayerBit;
            }
        }
    }
}
