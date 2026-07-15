using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class TurnEndButton : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private Button button;

        [Header("Slide")]
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(400f, 0f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;
        private bool _battleEnded;
        private bool _waveClearPending;
        private bool _skillQueueRunning;
        private bool _turnEndPending;
        private bool _inNodeContext;

        private void Awake()
        {
            if (slideTarget != null)
            {
                _slideInPos = slideTarget.anchoredPosition;
                slideTarget.anchoredPosition = _slideInPos + slideOutOffset;
            }
            SetButtonInteractable(false);
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<NodeContextEnteredEvent>(OnContextEntered);
            battleEventChannel.AddListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.AddListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.AddListener<PileDetailPanelOpenedEvent>(OnPileDetailOpened);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPileDetailClosed);
            battleEventChannel.AddListener<SkillQueueStartedEvent>(OnSkillQueueStarted);
            battleEventChannel.AddListener<SkillQueueCompletedEvent>(OnSkillQueueCompleted);
            battleEventChannel.AddListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<NodeContextEnteredEvent>(OnContextEntered);
            battleEventChannel.RemoveListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.RemoveListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.RemoveListener<PileDetailPanelOpenedEvent>(OnPileDetailOpened);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPileDetailClosed);
            battleEventChannel.RemoveListener<SkillQueueStartedEvent>(OnSkillQueueStarted);
            battleEventChannel.RemoveListener<SkillQueueCompletedEvent>(OnSkillQueueCompleted);
            battleEventChannel.RemoveListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        public void OnClick()
        {
            if (_battleEnded || _skillQueueRunning || _turnEndPending) return;
            _turnEndPending = true;
            SetButtonInteractable(false);
            battleEventChannel.RaiseEvent(new PlayerTurnEndRequestEvent());
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        private void OnSkillQueueStarted(SkillQueueStartedEvent _) { _skillQueueRunning = true; SlideOut(); }
        private void OnSkillQueueCompleted(SkillQueueCompletedEvent _) { _skillQueueRunning = false; if (!_battleEnded && !_waveClearPending) SlideIn(); }
        private void OnEnemyTurnStart(EnemyTurnStartEvent _) => SlideOut();

        private void OnPlayerTurnStart(PlayerTurnStartEvent _)
        {
            _turnEndPending = false;
            if (!_battleEnded && !_waveClearPending && !_skillQueueRunning)
                SetButtonInteractable(true);
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _waveClearPending = false; _skillQueueRunning = false; _turnEndPending = false; _inNodeContext = false; }
        private void OnContextEntered(NodeContextEnteredEvent _) { _inNodeContext = true; SlideOut(); }
        private void OnPileDetailOpened(PileDetailPanelOpenedEvent _) => SlideOut();
        private void OnPileDetailClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded && !_waveClearPending && !_skillQueueRunning && !_inNodeContext) SlideIn(); }
        private void OnBattleUIHidden(BattleUIHiddenEvent _) => SlideOut();
        private void OnBattleUIShown(BattleUIShownEvent _) { if (!_battleEnded && !_waveClearPending && !_skillQueueRunning && !_inNodeContext) SlideIn(); }
        private void OnCardDrawStart(CardDrawStartEvent _) { SlideOut(); SetButtonInteractable(false); }
        private void OnWaveClear(WaveClearEvent _) { _waveClearPending = true; SlideOut(); }
        private void OnCardDrawEnd(CardDrawEndEvent _) { _waveClearPending = false; if (!_battleEnded && !_skillQueueRunning && !_inNodeContext) SlideIn(); }
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; SlideOut(); }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; SlideOut(); }

        private void SlideOut()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos + slideOutOffset, slideDuration)
                .WithEase(slideEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(p => slideTarget.anchoredPosition = p);
        }

        private void SlideIn()
        {
            if (slideTarget == null) return;
            if (_slideHandle.IsActive()) _slideHandle.Cancel();
            _slideHandle = LMotion.Create(slideTarget.anchoredPosition, _slideInPos, slideDuration)
                .WithEase(slideEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => { if (!_turnEndPending) SetButtonInteractable(true); })
                .Bind(p => slideTarget.anchoredPosition = p);
        }
    }
}
