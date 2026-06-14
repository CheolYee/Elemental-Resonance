using System;
using Battle.Map.Data;
using Battle.Map.Enums;
using UnityEditor;
using UnityEngine;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        private static readonly int NodeDragHash = "MapNodeDrag".GetHashCode();
        private static readonly int PortDragHash = "MapPortDrag".GetHashCode();

        // ── 입력 처리 메인 ───────────────────────────────────────────────────────

        private void HandleInput(float w, float h)
        {
            Event e        = Event.current;
            int nodeDragId = GUIUtility.GetControlID(NodeDragHash, FocusType.Passive);
            int portDragId = GUIUtility.GetControlID(PortDragHash, FocusType.Passive);

            switch (e.type)
            {
                case EventType.KeyDown when e.keyCode is KeyCode.Delete or KeyCode.Backspace:
                    DeleteSelected();
                    e.Use();
                    break;

                case EventType.MouseDown when e.button == 1:
                    ShowContextMenu(e.mousePosition, w, h);
                    e.Use();
                    break;

                case EventType.MouseDown when e.button == 0:
                    OnLeftMouseDown(e, nodeDragId, portDragId, w, h);
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == nodeDragId:
                    OnNodeDragUpdate(e);
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == portDragId:
                    _portDragCurrentPos = e.mousePosition;
                    _canvasContainer.MarkDirtyRepaint();
                    e.Use();
                    break;

                case EventType.MouseUp when e.button == 0:
                    if (GUIUtility.hotControl == portDragId)
                    {
                        EndPortDrag(e.mousePosition, portDragId, w, h);
                        e.Use();
                    }
                    else if (GUIUtility.hotControl == nodeDragId)
                    {
                        GUIUtility.hotControl = 0;
                        RefreshAll();
                        e.Use();
                    }
                    break;
            }
        }

        // ── 좌클릭 처리 ─────────────────────────────────────────────────────────

        private void OnLeftMouseDown(Event e, int nodeDragId, int portDragId, float w, float h)
        {
            // 1. 출력 포트 히트 → 포트 드래그 시작
            var portNode = GetOutputPortAt(e.mousePosition, w, h);
            if (portNode != null)
            {
                _isDraggingPort     = true;
                _portDragFromNode   = portNode;
                _portDragCurrentPos = e.mousePosition;
                GUIUtility.hotControl = portDragId;
                _canvasContainer.MarkDirtyRepaint();
                e.Use();
                return;
            }

            // 2. 노드 히트 → 선택 + 드래그 시작
            var node = GetNodeAt(e.mousePosition, w, h);
            if (node != null)
            {
                bool changed = node != _selectedNode;
                _selectedNode          = node;
                _hasSelectedConnection = false;
                _nodeDragStartMouseX   = e.mousePosition.x;
                _nodeDragStartXOffset  = node.xOffset;
                GUIUtility.hotControl  = nodeDragId;
                if (changed) RefreshInspector();
                _canvasContainer.MarkDirtyRepaint();
                e.Use();
                return;
            }

            // 3. 연결선 히트 → 연결선 선택
            var (fromId, toId) = GetConnectionAt(e.mousePosition, w, h);
            if (fromId != null)
            {
                _selectedNode          = null;
                _hasSelectedConnection = true;
                _selectedConnectionFrom = fromId;
                _selectedConnectionTo   = toId;
                RefreshInspector();
                _canvasContainer.MarkDirtyRepaint();
                e.Use();
                return;
            }

            // 4. 빈 공간 → 선택 해제
            if (_selectedNode != null || _hasSelectedConnection)
            {
                _selectedNode          = null;
                _hasSelectedConnection = false;
                RefreshInspector();
                _canvasContainer.MarkDirtyRepaint();
            }
        }

        // ── 노드 드래그 ─────────────────────────────────────────────────────────

        private void OnNodeDragUpdate(Event e)
        {
            if (_selectedNode == null) return;
            Undo.RecordObject(_target, "Move Map Node");
            float raw = _nodeDragStartXOffset + (e.mousePosition.x - _nodeDragStartMouseX) / _xOffsetScale;
            _selectedNode.xOffset = _snapEnabled
                ? Mathf.Round(raw / _snapStep) * _snapStep
                : raw;
            EditorUtility.SetDirty(_target);
            _canvasContainer.MarkDirtyRepaint();
            e.Use();
        }

        // ── 포트 드래그 종료 → 연결 생성 ────────────────────────────────────────

        private void EndPortDrag(Vector2 mousePos, int portDragId, float w, float h)
        {
            GUIUtility.hotControl = 0;
            _isDraggingPort       = false;

            var targetNode = GetInputPortAt(mousePos, w, h);
            if (targetNode != null
                && targetNode != _portDragFromNode
                && !_portDragFromNode.nextNodeIds.Contains(targetNode.nodeId))
            {
                Undo.RecordObject(_target, "Add Map Connection");
                _portDragFromNode.nextNodeIds.Add(targetNode.nodeId);
                EditorUtility.SetDirty(_target);
            }

            _portDragFromNode = null;
            RefreshAll();
        }

        // ── Delete 키 ────────────────────────────────────────────────────────────

        internal void DeleteSelected()
        {
            if (_target == null) return;

            if (_hasSelectedConnection)
            {
                DeleteConnection(_selectedConnectionFrom, _selectedConnectionTo);
                _hasSelectedConnection = false;
                return;
            }
            if (_selectedNode != null)
            {
                DeleteNode(_selectedNode);
                _selectedNode = null;
            }
        }

        private void DeleteNode(MapNodeDefinition node)
        {
            Undo.RecordObject(_target, "Delete Map Node");
            string id = node.nodeId;
            _target.nodes.Remove(node);
            foreach (var n in _target.nodes)
                n.nextNodeIds.Remove(id);
            EditorUtility.SetDirty(_target);
            if (_canvasContainer != null)
                _canvasContainer.style.height = ComputeCanvasHeight();
            RefreshAll();
        }

        private void DeleteConnection(string fromId, string toId)
        {
            var fromNode = _target.GetNode(fromId);
            if (fromNode == null) return;
            Undo.RecordObject(_target, "Remove Map Connection");
            fromNode.nextNodeIds.Remove(toId);
            EditorUtility.SetDirty(_target);
            RefreshAll();
        }

        // ── 우클릭 컨텍스트 메뉴 ────────────────────────────────────────────────

        private void ShowContextMenu(Vector2 mousePos, float w, float h)
        {
            var menu = new GenericMenu();
            foreach (MapNodeType type in Enum.GetValues(typeof(MapNodeType)))
            {
                var t = type;
                menu.AddItem(new GUIContent($"Add Node/{t}"), false,
                    () => AddNode(t, mousePos, w, h));
            }
            if (_selectedNode != null)
            {
                menu.AddSeparator("");
                var node = _selectedNode;
                menu.AddItem(new GUIContent("Delete Node"), false, () =>
                {
                    DeleteNode(node);
                    _selectedNode = null;
                });
            }
            menu.ShowAsContext();
        }

        private void AddNode(MapNodeType type, Vector2 mousePos, float w, float h)
        {
            Undo.RecordObject(_target, "Add Map Node");
            int   floorIndex = Mathf.Max(0,
                Mathf.RoundToInt((h - mousePos.y - BottomPadding - NodeHeight * 0.5f) / _floorSpacing));
            float xOffset    = (mousePos.x - w * 0.5f) / _xOffsetScale;

            var newNode = new MapNodeDefinition
            {
                nodeId     = GenerateNodeId(),
                nodeType   = type,
                floorIndex = floorIndex,
                xOffset    = xOffset
            };
            _target.nodes.Add(newNode);
            EditorUtility.SetDirty(_target);
            if (_canvasContainer != null)
                _canvasContainer.style.height = ComputeCanvasHeight();
            _selectedNode = newNode;
            RefreshAll();
        }

        // ── Copy / Paste ─────────────────────────────────────────────────────────

        internal void CopySelected()
        {
            if (_selectedNode != null)
                _clipboard = _selectedNode;
        }

        internal void PasteClipboard()
        {
            if (_clipboard == null || _target == null) return;
            Undo.RecordObject(_target, "Paste Map Node");
            var newNode = new MapNodeDefinition
            {
                nodeId      = GenerateNodeId(),
                nodeType    = _clipboard.nodeType,
                floorIndex  = _clipboard.floorIndex,
                xOffset     = _clipboard.xOffset + 20f / _xOffsetScale,
                stageRef    = _clipboard.stageRef,
                restContent = _clipboard.restContent,
                shopContent = _clipboard.shopContent
            };
            _target.nodes.Add(newNode);
            EditorUtility.SetDirty(_target);
            if (_canvasContainer != null)
                _canvasContainer.style.height = ComputeCanvasHeight();
            _selectedNode = newNode;
            RefreshAll();
        }

        // ── 히트 체크 헬퍼 ───────────────────────────────────────────────────────

        private MapNodeDefinition GetOutputPortAt(Vector2 mousePos, float w, float h)
        {
            if (_target == null) return null;
            foreach (var node in _target.nodes)
            {
                if (Vector2.Distance(mousePos, GetOutputPort(node, w, h)) <= PortRadius * 2.5f)
                    return node;
            }
            return null;
        }

        private MapNodeDefinition GetInputPortAt(Vector2 mousePos, float w, float h)
        {
            if (_target == null) return null;
            foreach (var node in _target.nodes)
            {
                if (node.nodeType == MapNodeType.Start) continue;
                if (Vector2.Distance(mousePos, GetInputPort(node, w, h)) <= PortRadius * 2.5f)
                    return node;
            }
            return null;
        }

        private (string fromId, string toId) GetConnectionAt(Vector2 mousePos, float w, float h)
        {
            if (_target == null) return (null, null);
            foreach (var node in _target.nodes)
            {
                Vector2 fromPort = GetOutputPort(node, w, h);
                foreach (string nextId in node.nextNodeIds)
                {
                    var next = _target.GetNode(nextId);
                    if (next == null) continue;
                    Vector2 toPort = GetInputPort(next, w, h);
                    float   dist   = Mathf.Abs(toPort.y - fromPort.y);
                    float   tang   = dist * 0.45f;
                    if (IsNearBezier(
                            fromPort,
                            new Vector2(fromPort.x, fromPort.y - tang),
                            new Vector2(toPort.x,   toPort.y   + tang),
                            toPort, mousePos))
                        return (node.nodeId, nextId);
                }
            }
            return (null, null);
        }

        private static bool IsNearBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
                                         Vector2 mouse, float threshold = 8f)
        {
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                float u = 1 - t;
                var   pt = u*u*u*p0 + 3*u*u*t*p1 + 3*u*t*t*p2 + t*t*t*p3;
                if (Vector2.Distance(pt, mouse) < threshold) return true;
            }
            return false;
        }

        // ── 유틸 ─────────────────────────────────────────────────────────────────

        private static string GenerateNodeId() =>
            Guid.NewGuid().ToString("N")[..8];
    }
}
