using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class TurnEndButton : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;

        [Header("Slide")]
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(400f, 0f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;
        private bool _battleEnded;

        private void Awake()
        {
            if (slideTarget != null)
            {
                _slideInPos = slideTarget.anchoredPosition;
                slideTarget.anchoredPosition = _slideInPos + slideOutOffset;
            }
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnUnlock);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillExecutionStartEvent>(OnLock);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnLock);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<SkillExecutionEndEvent>(OnUnlock);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnUnlock);
        }

        public void OnClick()
        {
            if (_battleEnded) return;
            battleEventChannel.RaiseEvent(new PlayerTurnEndRequestEvent());
        }

        private void OnLock(SkillExecutionStartEvent _) => SlideOut();
        private void OnLock(CardDrawStartEvent _) => SlideOut();
        private void OnUnlock(SkillExecutionEndEvent _) { if (!_battleEnded) SlideIn(); }
        private void OnUnlock(CardDrawEndEvent _) { if (!_battleEnded) SlideIn(); }
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
