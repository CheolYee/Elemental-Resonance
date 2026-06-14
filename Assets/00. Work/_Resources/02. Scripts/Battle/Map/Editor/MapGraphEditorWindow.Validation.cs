using System.Collections.Generic;
using Battle.Map.Data;
using Battle.Map.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        private struct ValidationError
        {
            public string message;
            public string nodeId;   // 클릭 시 포커스할 노드 (null이면 그래프 전체 오류)
        }

        private readonly List<ValidationError> _validationErrors = new();
        internal readonly HashSet<string>      ErrorNodeIds      = new();

        private ScrollView _validationScrollView;
        private Label      _validationCountLabel;

        // ── UI 구성 ──────────────────────────────────────────────────────────────

        private void BuildValidationContent()
        {
            var headerRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 }
            };
            headerRow.Add(MakeSectionHeader("Validation"));
            _validationCountLabel = new Label("")
            {
                style = { fontSize = 10, marginLeft = 8, color = new StyleColor(new Color(0.55f, 0.55f, 0.60f)) }
            };
            headerRow.Add(_validationCountLabel);
            _validationPanel.Add(headerRow);

            _validationScrollView = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
            _validationPanel.Add(_validationScrollView);
        }

        // ── 검증 갱신 ────────────────────────────────────────────────────────────

        private void RefreshValidation()
        {
            if (_validationScrollView == null) return;

            RunValidation();
            _validationScrollView.Clear();

            if (_validationErrors.Count == 0)
            {
                _validationCountLabel.text = "";
                var ok = new Label("✓ 오류 없음")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.30f, 0.80f, 0.35f)) }
                };
                _validationScrollView.Add(ok);
            }
            else
            {
                _validationCountLabel.text = $"({_validationErrors.Count}개)";
                foreach (var err in _validationErrors)
                {
                    var nodeId = err.nodeId;
                    var row = new Button(() =>
                    {
                        if (nodeId == null || _target == null) return;
                        _selectedNode          = _target.GetNode(nodeId);
                        _hasSelectedConnection = false;
                        RefreshInspector();
                        _canvasContainer?.MarkDirtyRepaint();
                    })
                    {
                        text = "⚠ " + err.message,
                        style =
                        {
                            unityTextAlign  = TextAnchor.MiddleLeft,
                            fontSize        = 10,
                            color           = new StyleColor(new Color(1.0f, 0.42f, 0.35f)),
                            marginBottom    = 2,
                            paddingLeft     = 6,
                            paddingTop      = 3,
                            paddingBottom   = 3,
                            backgroundColor = new StyleColor(new Color(0.22f, 0.10f, 0.10f)),
                            borderBottomWidth = 0
                        }
                    };
                    _validationScrollView.Add(row);
                }
            }

            // 캔버스에도 오류 테두리 반영
            _canvasContainer?.MarkDirtyRepaint();
        }

        // ── 검증 실행 ────────────────────────────────────────────────────────────

        private void RunValidation()
        {
            _validationErrors.Clear();
            ErrorNodeIds.Clear();

            if (_target == null || _target.nodes == null || _target.nodes.Count == 0)
                return;

            var nodes    = _target.nodes;
            int maxFloor = _target.GetMaxFloorIndex();

            // 1. Start 노드 수
            int    startCount  = 0;
            string startNodeId = null;
            foreach (var n in nodes)
            {
                if (n.nodeType != MapNodeType.Start) continue;
                startCount++;
                startNodeId = n.nodeId;
            }
            if (startCount == 0)
            {
                AddError("Start 노드가 없습니다.", null);
            }
            else if (startCount > 1)
            {
                foreach (var n in nodes)
                {
                    if (n.nodeType != MapNodeType.Start) continue;
                    AddError($"Start 노드 중복: {n.nodeId}", n.nodeId);
                }
            }

            // 2. 연결 방향 규칙 (same-floor / skip-floor / 역방향)
            foreach (var node in nodes)
            {
                foreach (var nextId in node.nextNodeIds)
                {
                    var next = _target.GetNode(nextId);
                    if (next == null)
                    {
                        AddError($"끊긴 연결: {node.nodeId} → 존재하지 않는 노드 {nextId}", node.nodeId);
                        continue;
                    }
                    int diff = next.floorIndex - node.floorIndex;
                    if (diff == 0)
                    {
                        AddError($"같은 층 연결: {node.nodeId} → {nextId}", node.nodeId);
                        ErrorNodeIds.Add(nextId);
                    }
                    else if (diff < 0)
                    {
                        AddError($"역방향 연결: {node.nodeId}(F{node.floorIndex}) → {nextId}(F{next.floorIndex})", node.nodeId);
                    }
                    else if (diff > 1)
                    {
                        AddError($"층 건너뜀({diff}층 차이): {node.nodeId} → {nextId}", node.nodeId);
                        ErrorNodeIds.Add(nextId);
                    }
                }
            }

            // 3. 중간 층 outgoing 없음
            foreach (var node in nodes)
            {
                if (node.floorIndex < maxFloor && node.nextNodeIds.Count == 0)
                    AddError($"중간 층에 연결 없음: {node.nodeId} (F{node.floorIndex})", node.nodeId);
            }

            // 4. 마지막 층 outgoing 존재
            foreach (var node in nodes)
            {
                if (node.floorIndex == maxFloor && node.nextNodeIds.Count > 0)
                    AddError($"마지막 층에 연결 있음: {node.nodeId} (F{node.floorIndex})", node.nodeId);
            }

            // 5. 도달 불가 노드 (BFS)
            if (startCount == 1 && startNodeId != null)
            {
                var reachable = new HashSet<string>();
                var queue     = new Queue<string>();
                queue.Enqueue(startNodeId);
                reachable.Add(startNodeId);
                while (queue.Count > 0)
                {
                    string id = queue.Dequeue();
                    var    n  = _target.GetNode(id);
                    if (n == null) continue;
                    foreach (string nextId in n.nextNodeIds)
                    {
                        if (reachable.Add(nextId))
                            queue.Enqueue(nextId);
                    }
                }
                foreach (var node in nodes)
                {
                    if (!reachable.Contains(node.nodeId))
                        AddError($"도달 불가 노드: {node.nodeId} (F{node.floorIndex})", node.nodeId);
                }
            }
        }

        private void AddError(string message, string nodeId)
        {
            _validationErrors.Add(new ValidationError { message = message, nodeId = nodeId });
            if (nodeId != null) ErrorNodeIds.Add(nodeId);
        }
    }
}
