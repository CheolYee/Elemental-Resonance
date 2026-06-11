using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class BattleAnnouncementController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private BattleOverlayPanel overlayPanel;

        [Header("Texts")]
        [SerializeField] private string stageStartText = "STAGE START";
        [SerializeField] private string waveTextFormat = "WAVE {0}";
        [SerializeField] private string enemyTurnText = "ENEMY TURN";
        [SerializeField] private string playerTurnMainText = "YOUR TURN";
        [SerializeField] private string playerTurnSubFormat = "TURN {0}";
        [SerializeField] private string victoryText = "STAGE CLEAR";
        [SerializeField] private string defeatText = "STAGE FAILED";

        [Header("Hold Durations")]
        [SerializeField] private float stageStartHold = 1.5f;
        [SerializeField] private float waveHold = 1.2f;
        [SerializeField] private float enemyTurnHold = 1.0f;
        [SerializeField] private float playerTurnHold = 1.0f;
        [SerializeField] private float victoryHold = 2.5f;
        [SerializeField] private float defeatHold = 2.5f;

        [Header("Text Sequence Names")]
        [SerializeField] private string seqFadeIn = "FadeIn";
        [SerializeField] private string seqFadeOut = "FadeOut";
        [SerializeField] private string seqScalePop = "ScalePop";
        [SerializeField] private string seqSplitUp = "SplitUp";
        [SerializeField] private string seqSplitDown = "SplitDown";

        [Header("Background Sequence Names")]
        [SerializeField] private string seqStageStartBackground = "StageStartBg";
        [SerializeField] private string seqVictoryBackground = "VictoryBg";
        [SerializeField] private string seqDefeatBackground = "DefeatBg";

        private int _turnCount;
        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.AddListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
            battleEventChannel.AddListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnVictory);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnDefeat);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.RemoveListener<EnemyTurnStartEvent>(OnEnemyTurnStart);
            battleEventChannel.RemoveListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnVictory);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnDefeat);
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _turnCount = 0; }

        private void OnWaveStart(WaveStartEvent evt)
        {
            if (evt.WaveIndex == 0)
            {
                overlayPanel.Enqueue(new AnnouncementRequest
                {
                    PrimaryText = stageStartText,
                    PrimaryShow = seqScalePop,
                    PrimaryHide = seqFadeOut,
                    HoldDuration = stageStartHold,
                    BackgroundSequence = seqStageStartBackground
                });
            }

            overlayPanel.Enqueue(new AnnouncementRequest
            {
                PrimaryText = string.Format(waveTextFormat, evt.WaveIndex + 1),
                PrimaryShow = seqFadeIn,
                PrimaryHide = seqFadeOut,
                HoldDuration = waveHold
            });
        }

        private void OnEnemyTurnStart(EnemyTurnStartEvent _)
        {
            if (_battleEnded) return;
            overlayPanel.Enqueue(new AnnouncementRequest
            {
                PrimaryText = enemyTurnText,
                PrimaryShow = seqFadeIn,
                PrimaryHide = seqFadeOut,
                HoldDuration = enemyTurnHold
            });
        }

        private void OnPlayerTurnStart(PlayerTurnStartEvent _)
        {
            if (_battleEnded) return;
            _turnCount++;
            overlayPanel.Enqueue(new AnnouncementRequest
            {
                PrimaryText = playerTurnMainText,
                SecondaryText = string.Format(playerTurnSubFormat, _turnCount),
                PrimaryShow = seqFadeIn,
                PrimaryHide = seqSplitUp,
                SecondaryShow = seqFadeIn,
                SecondaryHide = seqSplitDown,
                HoldDuration = playerTurnHold
            });
        }

        private void OnVictory(BattleVictoryEvent _)
        {
            _battleEnded = true;
            overlayPanel.Enqueue(new AnnouncementRequest
            {
                PrimaryText = victoryText,
                PrimaryShow = seqScalePop,
                PrimaryHide = seqFadeOut,
                HoldDuration = victoryHold,
                BackgroundSequence = seqVictoryBackground,
                OnComplete = () => battleEventChannel.RaiseEvent(new BattleResultShownEvent())
            });
        }

        private void OnDefeat(BattleDefeatEvent _)
        {
            _battleEnded = true;
            overlayPanel.Enqueue(new AnnouncementRequest
            {
                PrimaryText = defeatText,
                PrimaryShow = seqScalePop,
                PrimaryHide = seqFadeOut,
                HoldDuration = defeatHold,
                BackgroundSequence = seqDefeatBackground,
                OnComplete = () => battleEventChannel.RaiseEvent(new BattleResultShownEvent())
            });
        }
    }
}
