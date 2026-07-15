using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class PlayerHealthBarView : MonoBehaviour, IModule
    {
        [SerializeField] private TMP_Text       hpText;
        [SerializeField] private EventChannelSO _battleEventChannel;

        [Header("Heal Feedback")]
        [SerializeField] private Color _healColor       = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private float _colorDuration   = 0.5f;
        [SerializeField] private float _popScale        = 1.3f;
        [SerializeField] private float _popDuration     = 0.2f;

        private HealthModule _health;
        private Color        _defaultColor;
        private Vector3      _baseScale;

        private MotionHandle _colorMotion;
        private MotionHandle _scaleMotion;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            _health.OnHpChanged += UpdateDisplay;
        }

        private void Awake()
        {
            if (hpText != null)
            {
                _defaultColor = hpText.color;
                _baseScale    = hpText.transform.localScale;
            }
        }

        private void Start()
        {
            UpdateDisplay(_health.CurrentHp, _health.MaxHp);
        }

        private void OnEnable()
        {
            _battleEventChannel?.AddListener<PlayerHpChangedEvent>(OnPlayerHpChanged);
        }

        private void OnDisable()
        {
            _battleEventChannel?.RemoveListener<PlayerHpChangedEvent>(OnPlayerHpChanged);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHpChanged -= UpdateDisplay;
            if (_colorMotion.IsActive()) _colorMotion.Cancel();
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
        }

        private void OnPlayerHpChanged(PlayerHpChangedEvent evt)
        {
            UpdateDisplay(evt.NewHp, evt.MaxHp);
            if (evt.IsHeal) PlayHealFeedback();
        }

        private void UpdateDisplay(int currentHp, int maxHp)
        {
            if (hpText != null)
                hpText.text = maxHp > 0 ? $"{currentHp}/{maxHp}" : "--/--";
        }

        private void PlayHealFeedback()
        {
            if (hpText == null) return;

            // 초록 깜박임
            if (_colorMotion.IsActive()) _colorMotion.Cancel();
            hpText.color = _healColor;
            _colorMotion = LMotion.Create(0f, 1f, _colorDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(t => { if (hpText != null) hpText.color = Color.Lerp(_healColor, _defaultColor, t); });

            // Scale Pop
            if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
            hpText.transform.localScale = _baseScale;
            _scaleMotion = LMotion.Create(_popScale, 1f, _popDuration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (hpText != null) hpText.transform.localScale = _baseScale * s; });
        }
    }
}
