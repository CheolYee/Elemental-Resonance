using Battle.Events;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class BattleSlowMotionController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private float          slowMotionTimeScale = 0.2f;

        [Header("배속")]
        [SerializeField] private TMP_Text      _speedLabel;
        [SerializeField] private RectTransform _buttonRect;

        [Header("Sound")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private SfxSounds      _clickSound;

        [Header("Scale Pop")]
        [SerializeField] private float _popScale    = 1.2f;
        [SerializeField] private float _popDuration = 0.12f;
        [SerializeField] private Ease  _popEase     = Ease.OutBack;

        private MotionHandle _popHandle;

        private static readonly float[] SpeedSteps = { 1f, 2f, 3f };
        private int   _speedIndex      = 0;
        private float _speedMultiplier = 1f;

        private bool _isSkillPresenting;
        private bool _isTargeting;

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnSkillExecutionStart);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnSkillExecutionEnd);
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            Time.timeScale = 1f;
        }

        public void CycleSpeed()
        {
            _speedIndex      = (_speedIndex + 1) % SpeedSteps.Length;
            _speedMultiplier = SpeedSteps[_speedIndex];
            UpdateSpeedLabel();
            UpdateTimeScale();
            PlayPopAndSound();
        }

        private void PlayPopAndSound()
        {
            _soundChannel?.RaiseEvent(new PlaySoundEvent(_clickSound, Vector3.zero));

            if (_buttonRect == null) return;
            if (_popHandle.IsActive()) _popHandle.Cancel();
            _buttonRect.localScale = Vector3.one * _popScale;
            _popHandle = LMotion.Create(_popScale, 1f, _popDuration)
                .WithEase(_popEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (_buttonRect != null) _buttonRect.localScale = new Vector3(s, s, 1f); });
        }

        private void OnSkillExecutionStart(SkillExecutionStartEvent _) { _isSkillPresenting = true;  UpdateTimeScale(); }
        private void OnSkillExecutionEnd(SkillExecutionEndEvent _)     { _isSkillPresenting = false; UpdateTimeScale(); }
        private void OnTargetingStart(CardTargetingStartEvent _)       { _isTargeting = true;        UpdateTimeScale(); }
        private void OnTargetingEnd(CardTargetingEndEvent _)           { _isTargeting = false;       UpdateTimeScale(); }
        private void OnBattleEnded(BattleVictoryEvent _)               => ResetAll();
        private void OnBattleEnded(BattleDefeatEvent _)                => ResetAll();
        private void OnSessionStart(BattleSessionStartEvent _)         => ResetAll();

        private void UpdateTimeScale()
        {
            if (_isSkillPresenting && _isTargeting)
                Time.timeScale = slowMotionTimeScale;
            else if (_isSkillPresenting)
                Time.timeScale = _speedMultiplier;
            else
                Time.timeScale = 1f;
        }

        private void ResetAll()
        {
            _isSkillPresenting = false;
            _isTargeting       = false;
            Time.timeScale     = 1f;
        }

        private void UpdateSpeedLabel()
        {
            if (_speedLabel != null)
                _speedLabel.text = $"{_speedMultiplier:0}x";
        }
    }
}
