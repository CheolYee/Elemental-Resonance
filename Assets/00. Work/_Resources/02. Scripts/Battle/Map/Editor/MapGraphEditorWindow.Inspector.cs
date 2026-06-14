using Battle.Data;
using Battle.Map.Data;
using Battle.Map.Enums;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        private Label         _noSelectionLabel;
        private VisualElement _inspectorContent;
        private EnumField     _nodeTypeField;
        private IntegerField  _floorIndexField;
        private Label         _nodeIdLabel;
        private ObjectField   _stageRefField;
        private VisualElement _stageRefRow;

        // ── UI 구성 ──────────────────────────────────────────────────────────────

        private void BuildInspectorContent()
        {
            _inspectorPanel.Add(MakeSectionHeader("Inspector"));

            _noSelectionLabel = new Label("노드를 선택하세요.")
            {
                style =
                {
                    fontSize   = 10,
                    color      = new StyleColor(new Color(0.50f, 0.50f, 0.55f)),
                    marginTop  = 8,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            _inspectorPanel.Add(_noSelectionLabel);

            _inspectorContent = new VisualElement
            {
                style = { display = DisplayStyle.None, flexDirection = FlexDirection.Column }
            };
            _inspectorPanel.Add(_inspectorContent);

            // nodeType 드롭다운
            _nodeTypeField = new EnumField(MapNodeType.Battle);
            _nodeTypeField.RegisterValueChangedCallback(evt =>
            {
                if (_selectedNode == null) return;
                Undo.RecordObject(_target, "Change Node Type");
                _selectedNode.nodeType = (MapNodeType)evt.newValue;
                EditorUtility.SetDirty(_target);
                UpdateStageRefVisibility();
                RefreshAll();
            });
            _inspectorContent.Add(MakeRow("Type", _nodeTypeField));

            // floorIndex
            _floorIndexField = new IntegerField { value = 0 };
            _floorIndexField.RegisterValueChangedCallback(evt =>
            {
                if (_selectedNode == null) return;
                int clamped = Mathf.Max(0, evt.newValue);
                if (clamped != evt.newValue)
                    _floorIndexField.SetValueWithoutNotify(clamped);
                Undo.RecordObject(_target, "Change Node Floor");
                _selectedNode.floorIndex = clamped;
                EditorUtility.SetDirty(_target);
                UpdateCanvasHeight();
                RefreshAll();
            });
            _inspectorContent.Add(MakeRow("Floor", _floorIndexField));

            // nodeId (읽기 전용)
            _nodeIdLabel = new Label
            {
                style =
                {
                    fontSize   = 9,
                    color      = new StyleColor(new Color(0.45f, 0.45f, 0.50f)),
                    marginBottom = 10,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            _inspectorContent.Add(MakeRow("ID", _nodeIdLabel));

            // stageRef (Battle 전용)
            _stageRefField = new ObjectField { objectType = typeof(BattleStageSO) };
            _stageRefField.RegisterValueChangedCallback(evt =>
            {
                if (_selectedNode == null) return;
                Undo.RecordObject(_target, "Set Stage Ref");
                _selectedNode.stageRef = evt.newValue as BattleStageSO;
                EditorUtility.SetDirty(_target);
                _canvasContainer?.MarkDirtyRepaint();
            });
            _stageRefRow = MakeRow("Stage", _stageRefField);
            _inspectorContent.Add(_stageRefRow);

            RefreshInspector();
        }

        // ── 갱신 ─────────────────────────────────────────────────────────────────

        private void RefreshInspector()
        {
            if (_noSelectionLabel == null) return;

            if (_selectedNode == null)
            {
                _noSelectionLabel.style.display = DisplayStyle.Flex;
                _inspectorContent.style.display  = DisplayStyle.None;
                return;
            }

            _noSelectionLabel.style.display = DisplayStyle.None;
            _inspectorContent.style.display  = DisplayStyle.Flex;

            _nodeTypeField.SetValueWithoutNotify(_selectedNode.nodeType);
            _floorIndexField.SetValueWithoutNotify(_selectedNode.floorIndex);
            _nodeIdLabel.text = _selectedNode.nodeId;
            _stageRefField.SetValueWithoutNotify(_selectedNode.stageRef);
            UpdateStageRefVisibility();
        }

        private void UpdateStageRefVisibility()
        {
            if (_stageRefRow == null || _selectedNode == null) return;
            bool needsStage = _selectedNode.nodeType is MapNodeType.Battle or MapNodeType.Elite;
            _stageRefRow.style.display = needsStage ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── 공용 UIToolkit 헬퍼 ─────────────────────────────────────────────────

        internal static Label MakeSectionHeader(string text) => new(text)
        {
            style =
            {
                fontSize                = 11,
                unityFontStyleAndWeight = FontStyle.Bold,
                color                   = new StyleColor(new Color(0.70f, 0.70f, 0.75f)),
                marginBottom            = 10
            }
        };

        private static VisualElement MakeRow(string labelText, VisualElement field)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems    = Align.Center,
                    marginBottom  = 6
                }
            };
            row.Add(new Label(labelText)
            {
                style =
                {
                    fontSize   = 10,
                    color      = new StyleColor(new Color(0.55f, 0.55f, 0.60f)),
                    width      = 48,
                    flexShrink = 0
                }
            });
            field.style.flexGrow = 1;
            row.Add(field);
            return row;
        }
    }
}
