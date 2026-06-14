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
                style = { height = 22, paddingLeft = 10, paddingRight = 10, marginRight = 16 }
            };
            bar.Add(createBtn);

            // ── 런타임 동기화 세팅 ────────────────────────────────────────────────
            bar.Add(new Label("Map Width")
            {
                style = { fontSize = 10, marginRight = 4, color = new StyleColor(new Color(0.6f, 0.6f, 0.65f)) }
            });
            var mapWidthField = new FloatField { value = _xOffsetScale, style = { width = 55, marginRight = 12 } };
            mapWidthField.RegisterValueChangedCallback(evt =>
            {
                _xOffsetScale = Mathf.Max(1f, evt.newValue);
                mapWidthField.SetValueWithoutNotify(_xOffsetScale);
                RefreshAll();
            });
            bar.Add(mapWidthField);

            bar.Add(new Label("Floor Spacing")
            {
                style = { fontSize = 10, marginRight = 4, color = new StyleColor(new Color(0.6f, 0.6f, 0.65f)) }
            });
            var floorSpacingField = new FloatField { value = _floorSpacing, style = { width = 55, marginRight = 16 } };
            floorSpacingField.RegisterValueChangedCallback(evt =>
            {
                _floorSpacing = Mathf.Max(10f, evt.newValue);
                floorSpacingField.SetValueWithoutNotify(_floorSpacing);
                UpdateCanvasHeight();
                RefreshAll();
            });
            bar.Add(floorSpacingField);

            // ── 스냅 ─────────────────────────────────────────────────────────────
            var snapToggle = new UnityEngine.UIElements.Toggle("Snap")
            {
                value = _snapEnabled,
                style = { marginRight = 6 }
            };
            snapToggle.labelElement.style.fontSize      = 10;
            snapToggle.labelElement.style.color         = new StyleColor(new Color(0.6f, 0.6f, 0.65f));
            snapToggle.labelElement.style.minWidth      = 34;
            snapToggle.RegisterValueChangedCallback(evt => _snapEnabled = evt.newValue);
            bar.Add(snapToggle);

            bar.Add(new Label("Step")
            {
                style = { fontSize = 10, marginRight = 4, color = new StyleColor(new Color(0.6f, 0.6f, 0.65f)) }
            });
            var snapStepField = new FloatField { value = _snapStep, style = { width = 48 } };
            snapStepField.RegisterValueChangedCallback(evt =>
            {
                _snapStep = Mathf.Max(0.01f, evt.newValue);
                snapStepField.SetValueWithoutNotify(_snapStep);
            });
            bar.Add(snapStepField);

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

            // Inspector 리사이즈 핸들
            topRow.Add(BuildInspectorResizeHandle());

            // 우측: Inspector 패널
            _inspectorPanel = new VisualElement
            {
                style =
                {
                    width             = _inspectorWidth,
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

            // Validation 리사이즈 핸들
            mainArea.Add(BuildValidationResizeHandle());

            // 하단: Validation 리스트
            _validationPanel = new VisualElement
            {
                style =
                {
                    flexShrink        = 0,
                    height            = _validationHeight,
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

        // ── 리사이즈 핸들 ────────────────────────────────────────────────────────

        private static readonly Color HandleNormal = new(0.08f, 0.08f, 0.10f);
        private static readonly Color HandleHover  = new(0.25f, 0.45f, 0.75f, 0.7f);

        private VisualElement BuildInspectorResizeHandle()
        {
            var handle = new VisualElement
            {
                style =
                {
                    width           = 5,
                    flexShrink      = 0,
                    backgroundColor = new StyleColor(HandleNormal)
                }
            };

            handle.RegisterCallback<MouseEnterEvent>(_ =>
                handle.style.backgroundColor = new StyleColor(HandleHover));
            handle.RegisterCallback<MouseLeaveEvent>(_ =>
                handle.style.backgroundColor = new StyleColor(HandleNormal));

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                _inspectorDragStartX  = evt.position.x;
                _inspectorStartWidth  = _inspectorPanel.layout.width;
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!handle.HasPointerCapture(evt.pointerId)) return;
                float delta   = _inspectorDragStartX - evt.position.x;
                _inspectorWidth = Mathf.Clamp(_inspectorStartWidth + delta, 150f, 500f);
                _inspectorPanel.style.width = _inspectorWidth;
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                handle.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });

            return handle;
        }

        private VisualElement BuildValidationResizeHandle()
        {
            var handle = new VisualElement
            {
                style =
                {
                    height          = 5,
                    flexShrink      = 0,
                    backgroundColor = new StyleColor(HandleNormal)
                }
            };

            handle.RegisterCallback<MouseEnterEvent>(_ =>
                handle.style.backgroundColor = new StyleColor(HandleHover));
            handle.RegisterCallback<MouseLeaveEvent>(_ =>
                handle.style.backgroundColor = new StyleColor(HandleNormal));

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                _validationDragStartY   = evt.position.y;
                _validationStartHeight  = _validationPanel.layout.height;
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!handle.HasPointerCapture(evt.pointerId)) return;
                float delta      = _validationDragStartY - evt.position.y;
                _validationHeight = Mathf.Clamp(_validationStartHeight + delta, 60f, 350f);
                _validationPanel.style.height = _validationHeight;
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                handle.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });

            return handle;
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
