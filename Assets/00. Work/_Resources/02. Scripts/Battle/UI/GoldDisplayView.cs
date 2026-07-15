using Battle.Data;
using Battle.Events;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class GoldDisplayView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO _battleEventChannel;
        [SerializeField] private TMP_Text _coinText;
        [SerializeField] private PlayerRunStateSO _playerRunState;
        [SerializeField] private float _countDuration = 0.4f;

        [Header("Sound")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private SfxSounds      _goldGainSound;

        [Header("Insufficient Feedback")]
        [SerializeField] private float _shakeStrength = 12f;
        [SerializeField] private float _shakeDuration = 0.4f;
        [SerializeField] private Color _insufficientColor = Color.red;

        private MotionHandle _currentMotion;
        private MotionHandle _shakeMotion;
        private MotionHandle _colorMotion;
        private Color _defaultColor;

        private void Awake()
        {
            if (_coinText == null)
                _coinText = GetComponentInChildren<TMP_Text>(true);
            _defaultColor = _coinText != null ? _coinText.color : Color.white;
        }

        private void OnEnable()
        {
            _battleEventChannel.AddListener<GoldChangedEvent>(OnGoldChanged);
            _battleEventChannel.AddListener<GoldInsufficientEvent>(OnGoldInsufficient);
            RefreshDisplay(_playerRunState != null ? _playerRunState.Gold : 0);
        }

        private void OnDisable()
        {
            _battleEventChannel.RemoveListener<GoldChangedEvent>(OnGoldChanged);
            _battleEventChannel.RemoveListener<GoldInsufficientEvent>(OnGoldInsufficient);
            if (_currentMotion.IsActive()) _currentMotion.Cancel();
            if (_shakeMotion.IsActive()) _shakeMotion.Cancel();
            if (_colorMotion.IsActive()) _colorMotion.Cancel();
        }

        private void OnGoldChanged(GoldChangedEvent evt)
        {
            if (evt.NewAmount > evt.OldAmount)
                _soundChannel?.RaiseEvent(new PlaySoundEvent(_goldGainSound, Vector3.zero));

            if (_currentMotion.IsActive()) _currentMotion.Cancel();
            _currentMotion = LMotion.Create((float)evt.OldAmount, (float)evt.NewAmount, _countDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v => _coinText.text = Mathf.RoundToInt(v).ToString());
        }

        private void OnGoldInsufficient(GoldInsufficientEvent _)
        {
            if (_coinText == null) return;

            if (_shakeMotion.IsActive()) _shakeMotion.Cancel();
            if (_colorMotion.IsActive()) _colorMotion.Cancel();

            float baseX = _coinText.rectTransform.anchoredPosition.x;
            float baseY = _coinText.rectTransform.anchoredPosition.y;
            _shakeMotion = LMotion.Shake.Create(baseX, _shakeStrength, _shakeDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(x => { if (_coinText != null) _coinText.rectTransform.anchoredPosition = new Vector2(x, baseY); });

            _coinText.color = _insufficientColor;
            _colorMotion = LMotion.Create(0f, 1f, _shakeDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(t => { if (_coinText != null) _coinText.color = Color.Lerp(_insufficientColor, _defaultColor, t); });
        }

        private void RefreshDisplay(int amount)
        {
            if (_coinText != null)
                _coinText.text = amount.ToString();
        }
    }
}

