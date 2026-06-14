using Battle.Map.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        private Label _inspectorNodeType;
        private Label _inspectorFloor;
        private Label _inspectorNodeId;
        private Label _inspectorConnections;

        private void BuildInspectorContent()
        {
            var header = MakeSectionHeader("Inspector");
            _inspectorPanel.Add(header);

            _inspectorNodeType   = MakeInfoLabel();
            _inspectorFloor      = MakeInfoLabel();
            _inspectorNodeId     = MakeInfoLabel();
            _inspectorConnections = MakeInfoLabel();

            _inspectorPanel.Add(MakeFieldRow("Type",        _inspectorNodeType));
            _inspectorPanel.Add(MakeFieldRow("Floor",       _inspectorFloor));
            _inspectorPanel.Add(MakeFieldRow("Node ID",     _inspectorNodeId));
            _inspectorPanel.Add(MakeFieldRow("Connections", _inspectorConnections));

            RefreshInspector();
        }

        private void RefreshInspector()
        {
            if (_inspectorNodeType == null) return;

            if (_selectedNode == null)
            {
                _inspectorNodeType.text    = "—";
                _inspectorFloor.text       = "—";
                _inspectorNodeId.text      = "—";
                _inspectorConnections.text = "—";
                return;
            }

            _inspectorNodeType.text    = _selectedNode.nodeType.ToString();
            _inspectorFloor.text       = _selectedNode.floorIndex.ToString();
            _inspectorNodeId.text      = _selectedNode.nodeId;
            _inspectorConnections.text = _selectedNode.nextNodeIds.Count.ToString();
        }

        // ── UIToolkit 헬퍼 ───────────────────────────────────────────────────────

        private static Label MakeSectionHeader(string text) => new(text)
        {
            style =
            {
                fontSize               = 11,
                unityFontStyleAndWeight = FontStyle.Bold,
                color                  = new StyleColor(new Color(0.70f, 0.70f, 0.75f)),
                marginBottom           = 10
            }
        };

        private static Label MakeInfoLabel() => new("—")
        {
            style =
            {
                fontSize    = 10,
                color       = new StyleColor(new Color(0.82f, 0.82f, 0.85f)),
                flexGrow    = 1,
                unityTextAlign = TextAnchor.MiddleRight
            }
        };

        private static VisualElement MakeFieldRow(string labelText, VisualElement valueEl)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom  = 6
                }
            };
            row.Add(new Label(labelText)
            {
                style =
                {
                    fontSize = 10,
                    color    = new StyleColor(new Color(0.55f, 0.55f, 0.60f)),
                    width    = 90
                }
            });
            row.Add(valueEl);
            return row;
        }
    }
}
