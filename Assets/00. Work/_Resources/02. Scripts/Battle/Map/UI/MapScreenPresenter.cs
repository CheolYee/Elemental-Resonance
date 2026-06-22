using System;
using System.Collections.Generic;
using Battle.Map.Data;
using Battle.Map.Enums;
using Battle.Map.Runtime;
using UnityEngine;

namespace Battle.Map.UI
{
    public class MapScreenPresenter : MonoBehaviour
    {
        [SerializeField] private MapNodeView   _nodePrefab;
        [SerializeField] private MapLineView   _linePrefab;
        [SerializeField] private RectTransform _contentRoot;

        [Header("Map Layout")]
        [SerializeField] private float _mapWidth    = 400f;
        [SerializeField] private float _floorSpacing = 150f;
        [SerializeField] private float _nodePadding  = 80f;

        [Header("Node Sprites")]
        [SerializeField] private Sprite _startSprite;
        [SerializeField] private Sprite _battleSprite;
        [SerializeField] private Sprite _eliteSprite;
        [SerializeField] private Sprite _restSprite;
        [SerializeField] private Sprite _shopSprite;
        [SerializeField] private Sprite _bossSprite;

        [Header("Node Type Colors")]
        [SerializeField] private Color _startColor  = Color.white;
        [SerializeField] private Color _battleColor = Color.white;
        [SerializeField] private Color _eliteColor  = Color.white;
        [SerializeField] private Color _restColor   = Color.white;
        [SerializeField] private Color _shopColor   = Color.white;
        [SerializeField] private Color _bossColor   = Color.white;

        private MapGraphSO _graph;
        private RunMapState _state;
        private MapOverlayState _overlayState;
        private readonly MapRouteRuleService _routeRuleService = new();
        private readonly List<MapNodeView> _nodeViews = new();
        private readonly List<MapLineView> _lineViews = new();
        private readonly Dictionary<string, MapNodeView> _nodeViewDict = new();

        public event Action<string> OnNodeClicked;

        public void Refresh(MapGraphSO graph, RunMapState state, MapOverlayState overlayState = MapOverlayState.Hidden)
        {
            _graph = graph;
            _state = state;
            _overlayState = overlayState;
            ClearAll();
            BuildAll();
        }

        private void ClearAll()
        {
            foreach (var v in _nodeViews) if (v != null) Destroy(v.gameObject);
            foreach (var v in _lineViews) if (v != null) Destroy(v.gameObject);
            _nodeViews.Clear();
            _lineViews.Clear();
            _nodeViewDict.Clear();
        }

        private void BuildAll()
        {
            int maxFloor = _graph.GetMaxFloorIndex();

            // Content pivot을 하단 중앙으로 고정 → y=0이 맵 최하단
            _contentRoot.pivot        = new Vector2(0.5f, 0f);
            _contentRoot.anchorMin    = new Vector2(0.5f, 0f);
            _contentRoot.anchorMax    = new Vector2(0.5f, 0f);
            _contentRoot.anchoredPosition = Vector2.zero;

            // Content 크기를 노드 전체 범위에 맞게 설정
            float contentHeight = maxFloor * _floorSpacing + _nodePadding * 2f;
            _contentRoot.sizeDelta = new Vector2(_mapWidth, contentHeight);

            // 선을 먼저 생성 (노드 뒤에 렌더링)
            foreach (var node in _graph.nodes)
            {
                var fromPos = GetNodePosition(node);
                foreach (var nextId in node.nextNodeIds)
                {
                    var nextNode = _graph.GetNode(nextId);
                    if (nextNode == null) continue;
                    SpawnLine(node.nodeId, nextNode.nodeId, fromPos, GetNodePosition(nextNode));
                }
            }

            // 노드 생성
            foreach (var node in _graph.nodes)
                SpawnNode(node, GetNodePosition(node));
        }

        private Vector2 GetNodePosition(MapNodeDefinition node)
        {
            float x = node.xOffset * _mapWidth;
            float y = node.floorIndex * _floorSpacing + _nodePadding;
            return new Vector2(x, y);
        }

        private MapNodeVisualState GetVisualState(string nodeId)
        {
            if (nodeId == _state.currentNodeId) return MapNodeVisualState.Current;
            if (_state.IsVisited(nodeId))        return MapNodeVisualState.Visited;
            var selectable = _routeRuleService.GetSelectableNodeIds(_graph, _state);
            if (selectable.Contains(nodeId))     return MapNodeVisualState.Selectable;
            return MapNodeVisualState.Locked;
        }

        private MapLineView.LineState GetLineState(string fromId, string toId)
        {
            if (_state.IsResolved(fromId))
                return MapLineView.LineState.Visited;
            var selectable = _routeRuleService.GetSelectableNodeIds(_graph, _state);
            if (selectable.Contains(toId))
                return MapLineView.LineState.Selectable;
            return MapLineView.LineState.Locked;
        }

        private void SpawnNode(MapNodeDefinition node, Vector2 pos)
        {
            var view = Instantiate(_nodePrefab, _contentRoot);
            SetBottomAnchor((RectTransform)view.transform, pos);
            var visualState = GetVisualState(node.nodeId);
            bool interactable = _overlayState == MapOverlayState.SelectionPending
                                && visualState == MapNodeVisualState.Selectable;
            view.Setup(node.nodeId, GetSprite(node.nodeType), GetNodeColor(node.nodeType), visualState, interactable);
            view.OnClicked += id => OnNodeClicked?.Invoke(id);
            _nodeViews.Add(view);
            _nodeViewDict[node.nodeId] = view;
        }

        public void SetNodeTransitionSelected(string nodeId)
        {
            if (_nodeViewDict.TryGetValue(nodeId, out var view))
                view.MarkTransitionSelected();
        }

        private void SpawnLine(string fromId, string toId, Vector2 fromPos, Vector2 toPos)
        {
            var view = Instantiate(_linePrefab, _contentRoot);
            var rt = (RectTransform)view.transform;
            SetBottomAnchor(rt, Vector2.zero);
            view.Setup(fromPos, toPos, GetLineState(fromId, toId));
            _lineViews.Add(view);
        }

        // Content 하단 기준 좌표계 — y=0이 Content 최하단
        private static void SetBottomAnchor(RectTransform rt, Vector2 pos)
        {
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        private Sprite GetSprite(MapNodeType type) => type switch
        {
            MapNodeType.Battle => _battleSprite,
            MapNodeType.Elite  => _eliteSprite,
            MapNodeType.Rest   => _restSprite,
            MapNodeType.Shop   => _shopSprite,
            MapNodeType.Boss   => _bossSprite != null ? _bossSprite : _eliteSprite,
            _                  => _startSprite
        };

        private Color GetNodeColor(MapNodeType type) => type switch
        {
            MapNodeType.Battle => _battleColor,
            MapNodeType.Elite  => _eliteColor,
            MapNodeType.Rest   => _restColor,
            MapNodeType.Shop   => _shopColor,
            MapNodeType.Boss   => _bossColor,
            _                  => _startColor
        };
    }
}
