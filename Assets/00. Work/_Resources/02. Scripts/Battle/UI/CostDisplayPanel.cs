using Battle.Data;
using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class CostDisplayPanel : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleCostModelSO costModel;
        [SerializeField] private TMP_Text costText;

        [Header("Slide")]
        [SerializeField] private RectTransform slideTarget;
        [SerializeField] private Vector2 slideOutOffset = new Vector2(-400f, 0f);
        [SerializeField] private float slideDuration = 0.3f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private Vector2 _slideInPos;
        private MotionHandle _slideHandle;
        private bool _battleEnded;
        private bool _waveClearPending;

        private void Awake()
        {
            if (slideTarget != null)
            {
                _slideInPos = slideTarget.anchoredPosition;
                slideTarget.anchoredPosition = _slideInPos + slideOutOffset;
            }
        }

        private void Start() => costText.text = $"{costModel.currentCost}/{costModel.baseCost}";

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<NodeContextEnteredEvent>(OnContextEntered);
            battleEventChannel.AddListener<CostChangedEvent>(OnCostChanged);
            battleEventChannel.AddListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.AddListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<PileDetailPanelOpenedEvent>(OnPileDetailOpened);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPileDetailClosed);
            battleEventChannel.AddListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<NodeContextEnteredEvent>(OnContextEntered);
            battleEventChannel.RemoveListener<CostChangedEvent>(OnCostChanged);
            battleEventChannel.RemoveListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.RemoveListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<PileDetailPanelOpenedEvent>(OnPileDetailOpened);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPileDetailClosed);
            battleEventChannel.RemoveListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
        }

        private void OnEnemyTurnStart(EnemyTurnStartEvent _) => SlideOut();
        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _waveClearPending = false; }
        private void OnContextEntered(NodeContextEnteredEvent _) => SlideOut();
        private void OnCostChanged(CostChangedEvent _) => costText.text = $"{costModel.currentCost}/{costModel.baseCost}";
        private void OnPileDetailOpened(PileDetailPanelOpenedEvent _) => SlideOut();
        private void OnPileDetailClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded && !_waveClearPending) SlideIn(); }
        private void OnBattleUIHidden(BattleUIHiddenEvent _) => SlideOut();
        private void OnBattleUIShown(BattleUIShownEvent _) { if (!_battleEnded && !_waveClearPending) SlideIn(); }
        private void OnCardDrawStart(CardDrawStartEvent _) => SlideOut();
        private void OnWaveClear(WaveClearEvent _) { _waveClearPending = true; SlideOut(); }
        private void OnCardDrawEnd(CardDrawEndEvent _) { _waveClearPending = false; if (!_battleEnded) SlideIn(); }
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
