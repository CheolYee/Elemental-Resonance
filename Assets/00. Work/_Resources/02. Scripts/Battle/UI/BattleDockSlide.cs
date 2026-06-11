using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class BattleDockSlide : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(0f, -200f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;
        private bool _battleEnded;
        private bool _waveClearPending;

        private void Awake()
        {
            if (slideTarget != null)
                _slideInPos = slideTarget.anchoredPosition;
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnUnlock);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnUnlock);
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _waveClearPending = false; }
        private void OnLock(SkillExecutionStartEvent _) => SlideOut();
        private void OnLock(CardDrawStartEvent _) => SlideOut();
        private void OnWaveClear(WaveClearEvent _) { _waveClearPending = true; SlideOut(); }
        private void OnUnlock(SkillExecutionEndEvent _) { if (!_battleEnded && !_waveClearPending) SlideIn(); }
        private void OnUnlock(CardDrawEndEvent _) { _waveClearPending = false; if (!_battleEnded) SlideIn(); }
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; SlideOut(); }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; SlideOut(); }

        private void SlideOut()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos + slideOutOffset, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        private void SlideIn()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos, slideDuration)
                .WithEase(slideEase)
                .Bind(p => slideTarget.anchoredPosition = p);
        }
    }
}
