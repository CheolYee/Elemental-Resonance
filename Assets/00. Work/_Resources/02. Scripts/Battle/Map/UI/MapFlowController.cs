using System;
using System.Threading;
using Battle.Events;
using Battle.Map.Data;
using Battle.Map.Enums;
using Battle.Map.Events;
using Battle.Map.Runtime;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using Unity.Cinemachine;
using UnityEngine;

namespace Battle.Map.UI
{
    public class MapFlowController : MonoBehaviour
    {
        [SerializeField] private MapGraphSO _mapGraph;
        [SerializeField] private MapOverlayController _mapOverlayController;
        [SerializeField] private MapScreenPresenter _mapScreenPresenter;
        [SerializeField] private EventChannelSO _battleEventChannel;
        [SerializeField] private StageBootstrapper _stageBootstrapper;
        [SerializeField] private RestPanelController _restPanel;
        [SerializeField] private ShopPanelController _shopPanel;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private EnvironmentController _skyboxController;
        [SerializeField] private CinemachineBrain _cinemachineBrain;

        [SerializeField] private float _mapOpenDelay = 0.5f;
        [SerializeField] private bool _openForSelectionOnStart = false;

        [Header("Transition Timing")]
        [SerializeField] private float _ringDuration = 0.35f;
        [SerializeField] private float _fadeInDuration = 0.25f;
        [SerializeField] private float _fadeOutDuration = 0.25f;

        private RunMapState _runMapState;
        private readonly MapRouteRuleService _routeRuleService = new();
        private bool _pendingVictoryMapOpen;

        private void Start()
        {
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }
            if (_mapGraph != null)
            {
                InitializeRun();
                if (_openForSelectionOnStart)
                {
                    _mapOverlayController.OpenForSelection();
                    RefreshPresenter();
                }
            }
        }

        private void OnEnable()
        {
            _battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleVictory);
            _battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleDefeat);
            _battleEventChannel.AddListener<BattleResultShownEvent>(OnBattleResultShown);
            _battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
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
            _battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            if (_mapScreenPresenter != null)
                _mapScreenPresenter.OnNodeClicked -= OnNodeViewClicked;
            if (_restPanel != null)
                _restPanel.OnExited -= OnContextExited;
            if (_shopPanel != null)
                _shopPanel.OnExited -= OnContextExited;
        }

        public void InitializeRun()
        {
            _runMapState = new RunMapState { graphId = _mapGraph.name };

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

        private void OnBattleDefeat(BattleDefeatEvent _)
        {
            // 런 종료 — 맵 열지 않음. Phase 8에서 씬 전환 추가 예정.
        }

        private void OnNodeViewClicked(string nodeId)
        {
            if (_mapOverlayController.State != MapOverlayState.SelectionPending) return;
            var selectable = _routeRuleService.GetSelectableNodeIds(_mapGraph, _runMapState);
            if (!selectable.Contains(nodeId)) return;
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
                if ((node.nodeType == MapNodeType.Battle || node.nodeType == MapNodeType.Elite)
                    && node.stageRef != null && _stageBootstrapper != null)
                    _stageBootstrapper.BeginStage(node.stageRef).Forget();
                else if (node.nodeType == MapNodeType.Rest)
                {
                    _battleEventChannel.RaiseEvent(new NodeContextEnteredEvent());
                    _skyboxController?.SetNight();
                    _restPanel?.Open(node.restContent);
                    await WaitForCameraBlend(ct);
                }
                else if (node.nodeType == MapNodeType.Shop)
                {
                    _battleEventChannel.RaiseEvent(new NodeContextEnteredEvent());
                    _shopPanel?.Open(node.shopContent);
                    await WaitForCameraBlend(ct);
                }
            }

            // 5. 페이드 아웃
            if (_fadeCanvasGroup != null)
            {
                await LMotion.Create(1f, 0f, _fadeOutDuration)
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
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
            }

            // 2. 패널 닫기 + MarkResolved
            var node = _mapGraph.GetNode(_runMapState.currentNodeId);
            if (node?.nodeType == MapNodeType.Rest)
            {
                _skyboxController?.SetDay();
                _restPanel?.Close();
                await WaitForCameraBlend(ct);
            }
            else if (node?.nodeType == MapNodeType.Shop)
            {
                _shopPanel?.Close();
                await WaitForCameraBlend(ct);
            }

            _runMapState.MarkResolved(_runMapState.currentNodeId);

            bool runComplete = _routeRuleService.IsRunComplete(_mapGraph, _runMapState);
            if (runComplete)
                _battleEventChannel.RaiseEvent(new RunClearedEvent());
            else
            {
                _mapOverlayController.OpenForSelection();
                RefreshPresenter();
            }

            // 3. 페이드 아웃 (맵과 동시에 등장)
            if (_fadeCanvasGroup != null)
            {
                await LMotion.Create(1f, 0f, _fadeOutDuration)
                    .Bind(a => _fadeCanvasGroup.alpha = a)
                    .ToUniTask(ct);
                _fadeCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnBattleResultShown(BattleResultShownEvent _)
        {
            if (!_pendingVictoryMapOpen) return;
            _pendingVictoryMapOpen = false;
            OpenMapAfterDelayAsync().Forget();
        }

        private async UniTaskVoid OpenMapAfterDelayAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_mapOpenDelay), cancellationToken: destroyCancellationToken);
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
    }
}
