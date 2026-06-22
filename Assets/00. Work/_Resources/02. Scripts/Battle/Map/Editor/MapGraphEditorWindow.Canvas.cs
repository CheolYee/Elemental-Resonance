using Battle.Map.Data;
using Battle.Map.Enums;
using UnityEditor;
using UnityEngine;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        // ── 색상 ─────────────────────────────────────────────────────────────────

        private static readonly Color BgColor           = new(0.11f, 0.13f, 0.18f);
        private static readonly Color LaneLineColor     = new(0.48f, 0.50f, 0.58f);
        private static readonly Color LaneLabelColor    = new(0.55f, 0.58f, 0.65f);
        private static readonly Color NodeColorStart    = new(0.32f, 0.32f, 0.36f);
        private static readonly Color NodeColorBattle   = new(0.52f, 0.16f, 0.16f);
        private static readonly Color NodeColorElite    = new(0.36f, 0.14f, 0.50f);
        private static readonly Color NodeColorRest     = new(0.16f, 0.42f, 0.18f);
        private static readonly Color NodeColorShop     = new(0.14f, 0.28f, 0.52f);
        private static readonly Color NodeColorBoss     = new(0.65f, 0.08f, 0.08f);
        private static readonly Color BorderSelected    = new(1.00f, 0.85f, 0.00f);
        private static readonly Color BorderError      = new(1.00f, 0.25f, 0.20f);
        private static readonly Color BorderNormal     = new(0.45f, 0.45f, 0.48f);
        private static readonly Color LineColor         = new(1.00f, 1.00f, 1.00f);
        private static readonly Color LineSelectedColor = new(0.10f, 0.85f, 1.00f);
        private static readonly Color PortColor         = new(0.80f, 0.82f, 0.88f);
        private static readonly Color PortDragLineColor = new(1.00f, 1.00f, 1.00f, 1.00f);

        private const float PortRadius  = 5f;
        private const float BorderWidth = 1.5f;

        // ── 캔버스 메인 그리기 ───────────────────────────────────────────────────

        private void OnCanvasGUI()
        {
            float w = _canvasContainer?.layout.width  ?? 400f;
            float h = _canvasContainer?.layout.height ?? 400f;

            EditorGUI.DrawRect(new Rect(0, 0, w, h), BgColor);

            if (_target == null)
            {
                DrawPlaceholderLabel(w, h, "MapGraphSO를 선택하거나 새로 생성하세요.");
                return;
            }

            DrawLanes(w, h);
            DrawConnections(w, h);
            DrawNodes(w, h);
            DrawPortDragLine(w, h);
            HandleInput(w, h);
        }

        // ── 레인 구분선 ──────────────────────────────────────────────────────────

        private void DrawLanes(float w, float h)
        {
            int maxFloor = _target.GetMaxFloorIndex();
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 9,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = LaneLabelColor }
            };

            for (int floor = 0; floor <= maxFloor; floor++)
            {
                float nodeTop = GetNodeY(floor, h);
                GUI.Label(new Rect(6, nodeTop + (NodeHeight - 14f) * 0.5f, 36f, 14f), $"F{floor}", labelStyle);

                if (floor < maxFloor)
                {
                    float lineY = nodeTop + NodeHeight + (_floorSpacing - NodeHeight) * 0.5f;
                    EditorGUI.DrawRect(new Rect(0, lineY - 3.5f, w, 7f), LaneLineColor);
                }
            }
        }

        // ── 연결선 ───────────────────────────────────────────────────────────────

        private void DrawConnections(float w, float h)
        {
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

                    bool  isSelected = _hasSelectedConnection
                                       && _selectedConnectionFrom == node.nodeId
                                       && _selectedConnectionTo   == nextId;
                    Color color = isSelected ? LineSelectedColor : LineColor;
                    float width = isSelected ? 7f : 5f;

                    Handles.DrawBezier(
                        new Vector3(fromPort.x, fromPort.y),
                        new Vector3(toPort.x,   toPort.y),
                        new Vector3(fromPort.x, fromPort.y - tang),
                        new Vector3(toPort.x,   toPort.y   + tang),
                        color, null, width
                    );
                }
            }
        }



        // ── 노드 ─────────────────────────────────────────────────────────────────

        private void DrawNodes(float w, float h)
        {
            var textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 10,
                wordWrap  = true,
                normal    = { textColor = Color.white }
            };

            foreach (var node in _target.nodes)
            {
                Rect nodeRect = GetNodeRect(node, w, h);
                bool selected = node == _selectedNode;

                bool hasError = ErrorNodeIds.Contains(node.nodeId);
                EditorGUI.DrawRect(nodeRect, GetNodeColor(node.nodeType));
                Color border = selected ? BorderSelected : hasError ? BorderError : BorderNormal;
                float bWidth = selected ? 2f : hasError ? 2f : BorderWidth;
                DrawBorder(nodeRect, border, bWidth);
                GUI.Label(
                    new Rect(nodeRect.x + 4, nodeRect.y + 4, nodeRect.width - 8, nodeRect.height - 8),
                    GetNodeLabel(node),
                    textStyle
                );

                DrawPort(GetOutputPort(node, w, h));
                if (node.nodeType != MapNodeType.Start)
                    DrawPort(GetInputPort(node, w, h));
            }
        }

        // ── 포트 드래그 임시 선 ──────────────────────────────────────────────────

        private void DrawPortDragLine(float w, float h)
        {
            if (!_isDraggingPort || _portDragFromNode == null) return;

            Vector2 from = GetOutputPort(_portDragFromNode, w, h);
            Vector2 to   = _portDragCurrentPos;
            float   dist = Vector2.Distance(from, to);
            float   tang = dist * 0.45f;

            Handles.DrawBezier(
                new Vector3(from.x, from.y),
                new Vector3(to.x,   to.y),
                new Vector3(from.x, from.y - tang),
                new Vector3(to.x,   to.y   + tang),
                PortDragLineColor, null, 5f
            );
        }

        // ── 좌표 계산 ────────────────────────────────────────────────────────────

        private float GetNodeY(int floorIndex, float canvasH) =>
            canvasH - BottomPadding - floorIndex * _floorSpacing - NodeHeight;

        private float GetNodeX(MapNodeDefinition node, float canvasW) =>
            canvasW * 0.5f + node.xOffset * _xOffsetScale - NodeWidth * 0.5f;

        private Rect GetNodeRect(MapNodeDefinition node, float canvasW, float canvasH) =>
            new(GetNodeX(node, canvasW), GetNodeY(node.floorIndex, canvasH), NodeWidth, NodeHeight);

        private Vector2 GetOutputPort(MapNodeDefinition node, float canvasW, float canvasH)
        {
            Rect r = GetNodeRect(node, canvasW, canvasH);
            return new Vector2(r.center.x, r.yMin);
        }

        private Vector2 GetInputPort(MapNodeDefinition node, float canvasW, float canvasH)
        {
            Rect r = GetNodeRect(node, canvasW, canvasH);
            return new Vector2(r.center.x, r.yMax);
        }

        private MapNodeDefinition GetNodeAt(Vector2 mousePos, float canvasW, float canvasH)
        {
            if (_target == null) return null;
            foreach (var node in _target.nodes)
            {
                if (GetNodeRect(node, canvasW, canvasH).Contains(mousePos))
                    return node;
            }
            return null;
        }

        // ── 색상/레이블 헬퍼 ─────────────────────────────────────────────────────

        private static Color GetNodeColor(MapNodeType type) => type switch
        {
            MapNodeType.Start  => NodeColorStart,
            MapNodeType.Battle => NodeColorBattle,
            MapNodeType.Elite  => NodeColorElite,
            MapNodeType.Rest   => NodeColorRest,
            MapNodeType.Shop   => NodeColorShop,
            MapNodeType.Boss   => NodeColorBoss,
            _                  => NodeColorStart
        };

        private static string GetNodeLabel(MapNodeDefinition node) => node.nodeType switch
        {
            MapNodeType.Start  => "START",
            MapNodeType.Battle => node.stageRef != null ? node.stageRef.name : "Battle",
            MapNodeType.Elite  => "ELITE",
            MapNodeType.Rest   => "REST",
            MapNodeType.Shop   => "SHOP",
            MapNodeType.Boss   => "BOSS",
            _                  => "???"
        };

        private static void DrawPort(Vector2 center) =>
            EditorGUI.DrawRect(
                new Rect(center.x - PortRadius, center.y - PortRadius, PortRadius * 2f, PortRadius * 2f),
                PortColor
            );

        private static void DrawBorder(Rect r, Color color, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        r.width, t),        color);
            EditorGUI.DrawRect(new Rect(r.x,        r.yMax - t, r.width, t),        color);
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        t,       r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y,        t,       r.height), color);
        }

        private static void DrawPlaceholderLabel(float w, float h, string msg)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 12,
                normal    = { textColor = new Color(0.4f, 0.4f, 0.42f) }
            };
            GUI.Label(new Rect(0, 0, w, h), msg, style);
        }
    }
}
