using Battle.Map.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        // ── Toolbar ──────────────────────────────────────────────────────────────

        private VisualElement BuildToolbar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection     = FlexDirection.Row,
                    alignItems        = Align.Center,
                    flexShrink        = 0,
                    height            = 32,
                    paddingLeft       = 8,
                    paddingRight      = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };

            _graphField = new ObjectField("Map Graph")
            {
                objectType = typeof(MapGraphSO),
                style      = { width = 320, marginRight = 12 }
            };
            if (_target != null) _graphField.SetValueWithoutNotify(_target);
            _graphField.RegisterValueChangedCallback(evt => SetTarget(evt.newValue as MapGraphSO));
            bar.Add(_graphField);

            var createBtn = new Button(CreateNewMapGraph)
            {
                text  = "New MapGraphSO",
                style = { height = 22, paddingLeft = 10, paddingRight = 10 }
            };
            bar.Add(createBtn);

            return bar;
        }

        // ── 메인 영역 ────────────────────────────────────────────────────────────

        private VisualElement BuildMainArea()
        {
            var mainArea = new VisualElement
            {
                style = { flexGrow = 1, flexDirection = FlexDirection.Column }
            };

            // 상단: 캔버스 + 인스펙터
            var topRow = new VisualElement
            {
                style = { flexGrow = 1, flexDirection = FlexDirection.Row }
            };

            // 좌측: 캔버스 (세로 스크롤 전용)
            var canvasScroll = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { flexGrow = 1, backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.17f)) }
            };
            canvasScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            canvasScroll.verticalScrollerVisibility   = ScrollerVisibility.Auto;

            _canvasContainer = new IMGUIContainer(OnCanvasGUI)
            {
                style = { height = ComputeCanvasHeight() }
            };
            canvasScroll.Add(_canvasContainer);
            topRow.Add(canvasScroll);

            // 우측: Inspector 패널
            _inspectorPanel = new VisualElement
            {
                style =
                {
                    width             = InspectorWidth,
                    flexShrink        = 0,
                    flexDirection     = FlexDirection.Column,
                    backgroundColor   = new StyleColor(new Color(0.14f, 0.14f, 0.16f)),
                    borderLeftWidth   = 1,
                    borderLeftColor   = new StyleColor(new Color(0.10f, 0.10f, 0.12f)),
                    paddingLeft       = 10,
                    paddingRight      = 10,
                    paddingTop        = 10,
                    paddingBottom     = 10
                }
            };
            BuildInspectorContent();
            topRow.Add(_inspectorPanel);

            mainArea.Add(topRow);

            // 하단: Validation 리스트
            _validationPanel = new VisualElement
            {
                style =
                {
                    flexShrink        = 0,
                    height            = ValidationHeight,
                    flexDirection     = FlexDirection.Column,
                    backgroundColor   = new StyleColor(new Color(0.11f, 0.11f, 0.13f)),
                    borderTopWidth    = 1,
                    borderTopColor    = new StyleColor(new Color(0.10f, 0.10f, 0.12f)),
                    paddingLeft       = 10,
                    paddingTop        = 8,
                    paddingRight      = 10,
                    paddingBottom     = 8
                }
            };
            BuildValidationContent();
            mainArea.Add(_validationPanel);

            return mainArea;
        }

        // ── 액션 ─────────────────────────────────────────────────────────────────

        private void CreateNewMapGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "새 Map Graph 저장",
                "NewMapGraph",
                "asset",
                "MapGraphSO 저장 위치를 선택하세요.",
                "Assets/05. SO/Maps"
            );
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<MapGraphSO>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _graphField?.SetValueWithoutNotify(asset);
            SetTarget(asset);
        }
    }
}
