using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.Map.Data;
using Battle.Map.Enums;
using Battle.Map.Events;
using Battle.Map.Runtime;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UI;
using Unity.Cinemachine;
using UnityEngine;
using MapOverlayOpenedEvent = Battle.Events.MapOverlayOpenedEvent;

namespace Battle.Map.UI
{
    public class MapFlowController : MonoBehaviour
    {
        [SerializeField] private MapGraphSO           _mapGraph;
        [SerializeField] private MapGraphGeneratorSO  _generator;
        [SerializeField] private MapOverlayController _mapOverlayController;
        [SerializeField] private MapScreenPresenter _mapScreenPresenter;
        [SerializeField] private EventChannelSO _battleEventChannel;
        [SerializeField] private StageBootstrapper _stageBootstrapper;
        [SerializeField] private RestPanelController _restPanel;
        [SerializeField] private ShopPanelController _shopPanel;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private EnvironmentController _skyboxController;
        [SerializeField] private CinemachineBrain _cinemachineBrain;
        [SerializeField] private PlayerRunStateSO _playerRunState;
        [SerializeField] private RunClearPanel  _runClearPanel;
        [SerializeField] private GameOverPanel  _gameOverPanel;
        [SerializeField] private SaveController _saveController;
        [SerializeField] private DeckController _deckController;

        [Header("BGM")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private float          _bgmFadeIn  = 1f;
        [SerializeField] private float          _bgmFadeOut = 1f;

        [SerializeField] private float _mapOpenDelay = 0.5f;
        [SerializeField] private bool _openForSelectionOnStart = false;

        [Header("Transition Timing")]
        [SerializeField] private float _ringDuration = 0.35f;
        [SerializeField] private float _fadeInDuration = 0.25f;
        [SerializeField] private float _fadeOutDuration = 0.25f;

        private RunMapState _runMapState;
        private MapGraphSO  _generatedGraph;
        private bool        _skipGenerator;
        private readonly MapRouteRuleService _routeRuleService = new();
        private bool _pendingVictoryMapOpen;

        public RunMapState CurrentMapState => _runMapState;

        private void Start()
        {
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }
            if (_mapGraph != null)
            {
                bool restored = _saveController != null && _saveController.TryRestoreIfPending();
                if (!restored)
                {
                    InitializeRun();
                    _battleEventChannel?.RaiseEvent(new GoldChangedEvent(0, _playerRunState?.Gold ?? 0));
                    _battleEventChannel?.RaiseEvent(new PlayerHpChangedEvent(0, 0, 0, false));
                    if (_openForSelectionOnStart)
                    {
                        PlayBgm(BgmSounds.MAP);
                        _mapOverlayController.OpenForSelection();
                        RefreshPresenter();
                    }
                }
                _deckController?.RefreshCurrentDeckCount();
            }
        }

        public void RestoreMapState(RunSaveData data)
        {
            if (_generator != null && data.seed != 0)
                SetGeneratedGraph(_generator.Generate(data.seed));

            _runMapState = new RunMapState
            {
                seed            = data.seed,
                graphId         = data.graphId,
                currentNodeId   = data.currentNodeId,
                visitedNodeIds  = new List<string>(data.visitedNodeIds),
                resolvedNodeIds = new List<string>(data.resolvedNodeIds),
            };
            PlayBgm(BgmSounds.MAP);
            _mapOverlayController.OpenForSelection();
            RefreshPresenter();
        }

        private void OnEnable()
        {
            _battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleVictory);
            _battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleDefeat);
            _battleEventChannel.AddListener<BattleResultShownEvent>(OnBattleResultShown);
            _battleEventChannel.AddListener<RewardPanelClosedEvent>(OnRewardPanelClosed);
            _battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            _battleEventChannel.AddListener<RunClearedEvent>(OnRunCleared);
            if (_mapOverlayController != null)
                _mapOverlayController.OnMapOpened += OnMapOverlayOpened;
            if (_mapScreenPresenter != null)
                _mapScreenPresenter.OnNodeClicked += OnNodeViewClicked;
            if (_restPanel != null)
                _restPanel.OnExited += OnContextExited;
            if (_shopPanel != null)
                _shopPanel.OnExited += OnContextExited;
        }

        private void OnDisable()
        {
            _battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleVictory);
            _battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleDefeat);
            _battleEventChannel.RemoveListener<BattleResultShownEvent>(OnBattleResultShown);
            _battleEventChannel.RemoveListener<RewardPanelClosedEvent>(OnRewardPanelClosed);
            _battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            _battleEventChannel.RemoveListener<RunClearedEvent>(OnRunCleared);
            if (_mapOverlayController != null)
                _mapOverlayController.OnMapOpened -= OnMapOverlayOpened;
            if (_mapScreenPresenter != null)
                _mapScreenPresenter.OnNodeClicked -= OnNodeViewClicked;
            if (_restPanel != null)
                _restPanel.OnExited -= OnContextExited;
            if (_shopPanel != null)
                _shopPanel.OnExited -= OnContextExited;
        }

        private void OnMapOverlayOpened() =>
            _battleEventChannel.RaiseEvent(new MapOverlayOpenedEvent());

        public void InitializeRun()
        {
            _playerRunState?.Reset();

            int seed = 0;
            if (_generator != null && !_skipGenerator)
            {
                if (PlayerPrefs.GetInt(NewGamePopupController.KeyUseCustom, 0) == 1)
                {
                    seed = PlayerPrefs.GetInt(NewGamePopupController.KeyPendingValue, 0);
                    PlayerPrefs.SetInt(NewGamePopupController.KeyUseCustom, 0);
                    PlayerPrefs.Save();
                }
                else
                {
                    seed = UnityEngine.Random.Range(0, int.MaxValue);
                }
                SetGeneratedGraph(_generator.Generate(seed));
            }

            _runMapState = new RunMapState { seed = seed, graphId = _mapGraph.name };

            var startNode = _mapGraph.GetStartNode();
            if (startNode == null)
            {
                Debug.LogWarning("[MapFlowController] MapGraphSO에 Start 노드가 없습니다.");
                return;
            }

            _runMapState.currentNodeId = startNode.nodeId;
            _runMapState.MarkVisited(startNode.nodeId);
            _runMapState.MarkResolved(startNode.nodeId);
        }

        private void SetGeneratedGraph(MapGraphSO graph)
        {
            if (_generatedGraph != null) Destroy(_generatedGraph);
            _generatedGraph = graph;
            _mapGraph = graph;
        }

        private void OnDestroy()
        {
            if (_generatedGraph != null) Destroy(_generatedGraph);
        }

        private void OnSessionStart(BattleSessionStartEvent _) => _pendingVictoryMapOpen = false;

        private void OnBattleVictory(BattleVictoryEvent _)
        {
            _pendingVictoryMapOpen = true;

            if (_runMapState != null && !string.IsNullOrEmpty(_runMapState.currentNodeId))
            {
                _runMapState.MarkResolved(_runMapState.currentNodeId);
                if (_routeRuleService.IsRunComplete(_mapGraph, _runMapState))
                {
                    _battleEventChannel.RaiseEvent(new RunClearedEvent());
                    _pendingVictoryMapOpen = false;
                }
            }
        }

        private void OnBattleDefeat(BattleDefeatEvent _) => FadeBgm();

        private void OnBattleResultShown(BattleResultShownEvent evt) { if (!evt.IsVictory) _gameOverPanel?.Show(); }

        private void OnRunCleared(RunClearedEvent _)
        {
            FadeBgm();
            _runClearPanel?.Show();
        }

        private void OnNodeViewClicked(string nodeId)
        {
            if (_mapOverlayController.State != MapOverlayState.SelectionPending) return;
            var selectable = _routeRuleService.GetSelectableNodeIds(_mapGraph, _runMapState);
            if (!selectable.Contains(nodeId)) return;
            _battleEventChannel.RaiseEvent(new MapNodeSelectedEvent(nodeId));
            ExecuteTransitionAsync(nodeId).Forget();
        }

        private async UniTaskVoid ExecuteTransitionAsync(string nodeId)
        {
            var ct = destroyCancellationToken;

            // 1. 전환 상태 진입 + 노드 TransitionSelected 표시
            _mapOverlayController.EnterTransitionInProgress();
            _mapScreenPresenter.SetNodeTransitionSelected(nodeId);

            // 2. 링 연출 대기
            await UniTask.Delay(TimeSpan.FromSeconds(_ringDuration), cancellationToken: ct);

            // 3. 페이드 인
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                await LMotion.Create(0f, 1f, _fadeInDuration)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
            }

            // 4. 맵 닫기 + 상태 갱신 + 스테이지 시작
            _mapOverlayController.CloseForTransition();
            _runMapState.pendingNodeId = nodeId;
            _runMapState.currentNodeId = nodeId;
            _runMapState.MarkVisited(nodeId);

            var node = _mapGraph.GetNode(nodeId);
            if (node != null)
            {
                if ((node.nodeType == MapNodeType.Battle || node.nodeType == MapNodeType.Elite || node.nodeType == MapNodeType.Boss)
                    && node.stageRef != null && _stageBootstrapper != null)
                {
                    PlayBgm(BgmSounds.MAIN);
                    _playerRunState?.SetFloorIndex(node.floorIndex);
                    _stageBootstrapper.BeginStage(node.stageRef).Forget();
                }
                else if (node.nodeType == MapNodeType.Rest)
                {
                    PlayBgm(BgmSounds.REST);
                    _battleEventChannel.RaiseEvent(new NodeContextEnteredEvent());
                    _skyboxController?.SetNight();
                    _restPanel?.EnterNode(node.restContent);
                    await WaitForCameraBlend(ct);
                }
                else if (node.nodeType == MapNodeType.Shop)
                {
                    PlayBgm(BgmSounds.SHOP);
                    _battleEventChannel.RaiseEvent(new NodeContextEnteredEvent());
                    _shopPanel?.EnterNode(node.shopContent);
                    await WaitForCameraBlend(ct);
                }
            }

            // 5. 페이드 아웃
            if (_fadeCanvasGroup != null)
            {
                await LMotion.Create(1f, 0f, _fadeOutDuration)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
                _fadeCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnContextExited() => ExitContextAsync().Forget();

        private async UniTaskVoid ExitContextAsync()
        {
            var ct = destroyCancellationToken;

            // 1. 페이드 인
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.blocksRaycasts = true;
                await LMotion.Create(0f, 1f, _fadeInDuration)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
            }

            // 2. 패널 닫기 + MarkResolved
            var node = _mapGraph.GetNode(_runMapState.currentNodeId);
            if (node?.nodeType == MapNodeType.Rest)
            {
                _skyboxController?.SetDay();
                if (_restPanel != null) await _restPanel.ExitAsync();
                await WaitForCameraBlend(ct);
                _battleEventChannel.RaiseEvent(new RestExitedEvent());
            }
            else if (node?.nodeType == MapNodeType.Shop)
            {
                if (_shopPanel != null) await _shopPanel.ExitAsync();
                await WaitForCameraBlend(ct);
                _battleEventChannel.RaiseEvent(new ShopExitedEvent());
            }

            _runMapState.MarkResolved(_runMapState.currentNodeId);

            bool runComplete = _routeRuleService.IsRunComplete(_mapGraph, _runMapState);
            if (runComplete)
                _battleEventChannel.RaiseEvent(new RunClearedEvent());
            else
            {
                PlayBgm(BgmSounds.MAP);
                _mapOverlayController.OpenForSelection();
                RefreshPresenter();
            }

            // 3. 페이드 아웃 (맵과 동시에 등장)
            if (_fadeCanvasGroup != null)
            {
                await LMotion.Create(1f, 0f, _fadeOutDuration)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
                _fadeCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnRewardPanelClosed(RewardPanelClosedEvent _)
        {
            if (!_pendingVictoryMapOpen) return;
            _pendingVictoryMapOpen = false;
            OpenMapAfterDelayAsync().Forget();
        }

        private async UniTaskVoid OpenMapAfterDelayAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_mapOpenDelay), cancellationToken: destroyCancellationToken);
            PlayBgm(BgmSounds.MAP);
            _mapOverlayController.OpenForSelection();
            RefreshPresenter();
        }

        private void RefreshPresenter()
        {
            if (_mapScreenPresenter != null && _runMapState != null)
                _mapScreenPresenter.Refresh(_mapGraph, _runMapState, _mapOverlayController.State);
        }

        [ContextMenu("Debug: Initialize Run")]
        private void DebugInitializeRun() => InitializeRun();

        public void OpenInspect()
        {
            RefreshPresenter();
            _mapOverlayController.OpenInspect();
        }

        // 튜토리얼 진입 시 Awake 단계에서 맵 그래프를 교체한다 (Start 이전에 호출해야 함).
        // 호출 시 절차적 생성기를 우회하고 지정된 그래프를 그대로 사용한다.
        public void OverrideMapGraph(MapGraphSO graph)
        {
            _mapGraph      = graph;
            _skipGenerator = true;
        }

        // 튜토리얼 Bootstrapper에서 맵 오버레이를 강제로 열 때 사용.
        public void OpenMapOverlay()
        {
            PlayBgm(BgmSounds.MAP);
            _mapOverlayController.OpenForSelection();
            RefreshPresenter();
        }

        [ContextMenu("Debug: Open Inspect")]
        private void DebugOpenInspect() => OpenInspect();

        [ContextMenu("Debug: Open For Selection")]
        private void DebugOpenForSelection()
        {
            _mapOverlayController.OpenForSelection();
            RefreshPresenter();
        }

        private async UniTask WaitForCameraBlend(CancellationToken ct)
        {
            if (_cinemachineBrain == null) return;
            await UniTask.NextFrame(ct);
            await UniTask.WaitUntil(() => !_cinemachineBrain.IsBlending, cancellationToken: ct);
        }

        private void PlayBgm(BgmSounds bgm)
        {
            _soundChannel?.RaiseEvent(new PlayManagedSoundEvent(
                bgm, Vector3.zero, SoundChannelId.Bgm,
                _bgmFadeIn, _bgmFadeOut, crossfadeExisting: true));
        }

        private void FadeBgm()
        {
            _soundChannel?.RaiseEvent(new StopManagedSoundEvent(SoundChannelId.Bgm, _bgmFadeOut));
        }
    }
}
