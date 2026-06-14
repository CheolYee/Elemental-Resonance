using Battle.Map.Data;
using Battle.Map.Enums;
using UnityEditor;
using UnityEngine;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        // ── 색상 ─────────────────────────────────────────────────────────────────

        private static readonly Color BgColor         = new(0.15f, 0.15f, 0.17f);
        private static readonly Color LaneLineColor   = new(0.22f, 0.22f, 0.25f);
        private static readonly Color LaneLabelColor  = new(0.30f, 0.30f, 0.34f);
        private static readonly Color NodeColorStart  = new(0.32f, 0.32f, 0.36f);
        private static readonly Color NodeColorBattle = new(0.52f, 0.16f, 0.16f);
        private static readonly Color NodeColorElite  = new(0.36f, 0.14f, 0.50f);
        private static readonly Color NodeColorRest   = new(0.16f, 0.42f, 0.18f);
        private static readonly Color NodeColorShop   = new(0.14f, 0.28f, 0.52f);
        private static readonly Color BorderSelected  = new(1.00f, 0.85f, 0.00f);
        private static readonly Color BorderNormal    = new(0.45f, 0.45f, 0.48f);
        private static readonly Color LineColor       = new(0.65f, 0.65f, 0.68f);
        private static readonly Color PortColor       = new(0.72f, 0.72f, 0.76f);

        private const float PortRadius    = 5f;
        private const float BorderWidth  = 1.5f;
        private const float XOffsetScale = 400f; // 런타임 MapScreenPresenter._mapWidth 기준

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
            HandleMouseEvents(w, h);
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

                // 층 레이블
                GUI.Label(new Rect(6, nodeTop + (NodeHeight - 14f) * 0.5f, 36f, 14f), $"F{floor}", labelStyle);

                // 층 간 구분선 (레인 하단)
                if (floor < maxFloor)
                {
                    float lineY = nodeTop + NodeHeight + (FloorSpacing - NodeHeight) * 0.5f;
                    Handles.color = LaneLineColor;
                    Handles.DrawLine(new Vector3(0, lineY), new Vector3(w, lineY));
                }
            }
        }

        // ── 연결선 ───────────────────────────────────────────────────────────────

        private void DrawConnections(float w, float h)
        {
            Handles.color = LineColor;
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

                    Handles.DrawBezier(
                        new Vector3(fromPort.x, fromPort.y),
                        new Vector3(toPort.x,   toPort.y),
                        new Vector3(fromPort.x, fromPort.y - tang),
                        new Vector3(toPort.x,   toPort.y   + tang),
                        LineColor, null, 2f
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

                // 배경
                EditorGUI.DrawRect(nodeRect, GetNodeColor(node.nodeType));

                // 테두리
                DrawBorder(nodeRect, selected ? BorderSelected : BorderNormal, selected ? 2f : BorderWidth);

                // 레이블
                GUI.Label(
                    new Rect(nodeRect.x + 4, nodeRect.y + 4, nodeRect.width - 8, nodeRect.height - 8),
                    GetNodeLabel(node),
                    textStyle
                );

                // 출력 포트 (노드 상단)
                DrawPort(GetOutputPort(node, w, h));

                // 입력 포트 (노드 하단, Start 제외)
                if (node.nodeType != MapNodeType.Start)
                    DrawPort(GetInputPort(node, w, h));
            }
        }

        // ── 마우스 이벤트 ────────────────────────────────────────────────────────

        private void HandleMouseEvents(float w, float h)
        {
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return;

            MapNodeDefinition hit = GetNodeAt(e.mousePosition, w, h);
            if (hit == _selectedNode) return;

            _selectedNode = hit;
            RefreshInspector();
            _canvasContainer?.MarkDirtyRepaint();
            if (hit != null) e.Use();
        }

        // ── 좌표 계산 ────────────────────────────────────────────────────────────

        private float GetNodeY(int floorIndex, float canvasH)
        {
            // floorIndex=0 이 하단, 위로 올라갈수록 Y 감소
            return canvasH - BottomPadding - floorIndex * FloorSpacing - NodeHeight;
        }

        private float GetNodeX(MapNodeDefinition node, float canvasW)
        {
            return canvasW * 0.5f + node.xOffset * XOffsetScale - NodeWidth * 0.5f;
        }

        private Rect GetNodeRect(MapNodeDefinition node, float canvasW, float canvasH)
        {
            return new Rect(GetNodeX(node, canvasW), GetNodeY(node.floorIndex, canvasH), NodeWidth, NodeHeight);
        }

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

        // ── 헬퍼 ─────────────────────────────────────────────────────────────────

        private static Color GetNodeColor(MapNodeType type) => type switch
        {
            MapNodeType.Start  => NodeColorStart,
            MapNodeType.Battle => NodeColorBattle,
            MapNodeType.Elite  => NodeColorElite,
            MapNodeType.Rest   => NodeColorRest,
            MapNodeType.Shop   => NodeColorShop,
            _                  => NodeColorStart
        };

        private static string GetNodeLabel(MapNodeDefinition node) => node.nodeType switch
        {
            MapNodeType.Start  => "START",
            MapNodeType.Battle => node.stageRef != null ? node.stageRef.name : "Battle",
            MapNodeType.Elite  => "ELITE",
            MapNodeType.Rest   => "REST",
            MapNodeType.Shop   => "SHOP",
            _                  => "???"
        };

        private static void DrawPort(Vector2 center)
        {
            EditorGUI.DrawRect(
                new Rect(center.x - PortRadius, center.y - PortRadius, PortRadius * 2f, PortRadius * 2f),
                PortColor
            );
        }

        private static void DrawBorder(Rect r, Color color, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x,          r.y,          r.width, t),        color);
            EditorGUI.DrawRect(new Rect(r.x,          r.yMax - t,   r.width, t),        color);
            EditorGUI.DrawRect(new Rect(r.x,          r.y,          t,       r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - t,   r.y,          t,       r.height), color);
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
