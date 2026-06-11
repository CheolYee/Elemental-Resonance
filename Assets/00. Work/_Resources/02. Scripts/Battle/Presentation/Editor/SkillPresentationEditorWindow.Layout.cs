using Battle.Data;
using Battle.Enums;
using Battle.Presentation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
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
                    paddingLeft       = 8,
                    paddingRight      = 8,
                    paddingTop        = 4,
                    paddingBottom     = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };

            _referenceCardField = new ObjectField("Card") { objectType = typeof(CardDataSO), style = { width = 260, marginRight = 12 } };
            _referenceCardField.RegisterValueChangedCallback(evt => SetReferenceCard(evt.newValue as CardDataSO));
            bar.Add(_referenceCardField);

            var layoutField = new ObjectField("Preview Layout")
            {
                objectType = typeof(SkillPreviewLayoutSO),
                value      = _previewLayout,
                style      = { width = 220, marginRight = 12 }
            };
            layoutField.RegisterValueChangedCallback(evt =>
            {
                _previewLayout = evt.newValue as SkillPreviewLayoutSO;
                EditorPrefs.SetString(PrefKeyLayout, _previewLayout != null ? AssetDatabase.GetAssetPath(_previewLayout) : "");
                RebuildPreview();
            });
            bar.Add(layoutField);

            var folderField = new TextField("Prefab Folder")
            {
                value = _previewPrefabFolder,
                style = { width = 200, marginRight = 2 }
            };
            folderField.RegisterValueChangedCallback(evt =>
            {
                _previewPrefabFolder = evt.newValue;
                EditorPrefs.SetString(PrefKeyFolder, evt.newValue);
            });
            bar.Add(folderField);

            var folderBtn = new Button(() =>
            {
                string selected = EditorUtility.OpenFolderPanel("프리팹 폴더 선택", _previewPrefabFolder, "");
                if (string.IsNullOrEmpty(selected)) return;
                if (selected.StartsWith(Application.dataPath))
                    selected = "Assets" + selected.Substring(Application.dataPath.Length);
                _previewPrefabFolder = selected;
                EditorPrefs.SetString(PrefKeyFolder, selected);
                folderField.SetValueWithoutNotify(selected);
            })
            {
                text  = "…",
                style = { width = 22, height = 18, fontSize = 11, marginRight = 12, flexShrink = 0 }
            };
            bar.Add(folderBtn);

            _gradeButtons = new Button[4];
            var gradeNames  = new[] { "Normal", "Rare", "Epic", "Legendary" };
            var gradeValues = new[] { CardGrade.Normal, CardGrade.Rare, CardGrade.Epic, CardGrade.Legendary };
            for (int i = 0; i < 4; i++)
            {
                var grade = gradeValues[i];
                var btn = new Button(() => SelectGrade(grade))
                {
                    text  = gradeNames[i],
                    style = { width = 72, height = 22, marginRight = 2, fontSize = 10 }
                };
                _gradeButtons[i] = btn;
                bar.Add(btn);
            }

            RefreshGradeButtons();
            RefreshDurationField();
            return bar;
        }

        internal VisualElement BuildTimelineControlBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection     = FlexDirection.Row,
                    alignItems        = Align.Center,
                    flexShrink        = 0,
                    height            = 28,
                    paddingLeft       = 6,
                    paddingRight      = 6,
                    backgroundColor   = new StyleColor(new Color(0.10f, 0.10f, 0.12f)),
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f))
                }
            };

            _objectNameLabel = new Label("—")
            {
                style =
                {
                    fontSize       = 10,
                    width          = 80,
                    marginRight    = 4,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    color          = new StyleColor(new Color(0.60f, 0.60f, 0.65f))
                }
            };
            bar.Add(_objectNameLabel);

            _addPropertyBtn = new Button(ShowAddPropertyMenu)
            {
                text  = "+ Add Property",
                style = { height = 18, fontSize = 10, marginRight = 6 }
            };
            bar.Add(_addPropertyBtn);

            bar.Add(new VisualElement { style = { width = 4 } });

            _playBtn  = MakeBtn("▶",  new Color(0.18f, 0.45f, 0.18f), Play);
            _pauseBtn = MakeBtn("⏸", new Color(0.35f, 0.35f, 0.18f), Pause);
            _stopBtn  = MakeBtn("■",  new Color(0.45f, 0.18f, 0.18f), Stop);
            _playBtn.style.width  = 28; _playBtn.style.paddingLeft  = 4; _playBtn.style.paddingRight = 4;
            _pauseBtn.style.width = 28; _pauseBtn.style.paddingLeft = 4; _pauseBtn.style.paddingRight = 4;
            _stopBtn.style.width  = 28; _stopBtn.style.paddingLeft  = 4; _stopBtn.style.paddingRight = 4;
            bar.Add(_playBtn);
            bar.Add(_pauseBtn);
            bar.Add(_stopBtn);

            _timeLabel = new Label("0.00s")
            {
                style = { marginLeft = 6, fontSize = 10, width = 46, color = new StyleColor(new Color(0.72f, 0.78f, 0.88f)) }
            };
            bar.Add(_timeLabel);

            bar.Add(new VisualElement { style = { flexGrow = 1 } });

            var snapRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 6 } };
            snapRow.Add(new Label("Snap") { style = { marginRight = 4, fontSize = 10 } });
            _snapToggle = new Toggle { value = _snapEnabled, style = { width = 16 } };
            _snapToggle.RegisterValueChangedCallback(evt => _snapEnabled = evt.newValue);
            snapRow.Add(_snapToggle);
            bar.Add(snapRow);

            bar.Add(new Label("Step") { style = { marginRight = 4, fontSize = 10, unityTextAlign = TextAnchor.MiddleLeft } });
            _snapStepField = new FloatField { value = ClampSnapStep(_snapStep), style = { width = 52, marginRight = 2 } };
            _snapStepField.RegisterValueChangedCallback(evt =>
            {
                float clamped = ClampSnapStep(evt.newValue);
                _snapStep = clamped;
                if (!Mathf.Approximately(evt.newValue, clamped))
                    _snapStepField.SetValueWithoutNotify(clamped);
            });
            bar.Add(_snapStepField);

            return bar;
        }

        // ── 메인 영역 ────────────────────────────────────────────────────────────

        private VisualElement BuildMainArea()
        {
            MainArea = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };

            // ── 좌측 패널 (렌더링 뷰 + 타임라인) ────────────────────────────────
            LeftPanel = new VisualElement
            {
                style = { flexGrow = 1, flexDirection = FlexDirection.Column, minWidth = 320 }
            };

            RenderingView = new VisualElement
            {
                name  = "RenderingView",
                style =
                {
                    flexShrink      = 0,
                    height          = _renderViewHeight,
                    minHeight       = 120,
                    backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f)),
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f))
                }
            };
            RenderingView.Add(new Label("Preview")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    color          = new StyleColor(new Color(0.35f, 0.35f, 0.38f)),
                    fontSize       = 11,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    position       = Position.Absolute,
                    left = 0, right = 0, top = 0, bottom = 0
                }
            });
            LeftPanel.Add(RenderingView);
            LeftPanel.Add(BuildRenderTimelineSplitHandle());

            // 타임라인 영역 (컨트롤 바 + ScrollView)
            TimelinePanel = new VisualElement
            {
                name  = "TimelinePanel",
                style =
                {
                    flexGrow        = 1,
                    minHeight       = 160,
                    flexDirection   = FlexDirection.Column,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f))
                }
            };
            LeftPanel.Add(TimelinePanel);

            InitTimelinePanelBehavior();
            BuildTimelinePlaceholder();
            MainArea.Add(LeftPanel);

            // ── 좌우 리사이즈 핸들 ──────────────────────────────────────────────
            MainArea.Add(BuildInspectorResizeHandle());

            // ── 우측 패널 (Hierarchy + Inspector) ───────────────────────────────
            RightPanel = new VisualElement
            {
                name  = "RightPanel",
                style =
                {
                    flexShrink    = 0,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.16f))
                }
            };
            RightPanel.RegisterCallback<KeyUpEvent>(OnInspectorRedoKeyUp, TrickleDown.TrickleDown);
            ApplyInspectorWidth(_inspectorWidth);

            HierarchyPanel = new VisualElement
            {
                name  = "HierarchyPanel",
                style =
                {
                    flexShrink    = 0,
                    flexDirection = FlexDirection.Column,
                    height        = _hierarchyPanelHeight,
                    minHeight     = 80,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.10f, 0.10f, 0.12f))
                }
            };
            _objectListRoot = new VisualElement
            {
                style =
                {
                    flexGrow      = 1,
                    flexDirection = FlexDirection.Column,
                    paddingLeft   = 6, paddingRight  = 6,
                    paddingTop    = 6, paddingBottom = 4
                }
            };
            HierarchyPanel.Add(_objectListRoot);
            RightPanel.Add(HierarchyPanel);
            RightPanel.Add(BuildHierarchyInspectorSplitHandle());

            InspectorPanel = new VisualElement
            {
                name  = "InspectorPanel",
                style = { flexGrow = 1, minHeight = 80, flexDirection = FlexDirection.Column }
            };

            var inspectorScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
            InspectorPanel.Add(inspectorScroll);

            _vfxObjectSection = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, display = DisplayStyle.None }
            };
            inspectorScroll.Add(_vfxObjectSection);

            _keyframeInspectorContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column }
            };
            inspectorScroll.Add(_keyframeInspectorContainer);
            RightPanel.Add(InspectorPanel);

            RefreshObjectList();
            BuildInspectorPlaceholder();
            MainArea.Add(RightPanel);

            MainArea.RegisterCallback<GeometryChangedEvent>(_ => ApplyInspectorWidth(_inspectorWidth));
            return MainArea;
        }

        private VisualElement BuildRenderTimelineSplitHandle()
        {
            RenderTimelineSplitHandle = new VisualElement
            {
                style =
                {
                    height         = InspectorHandleWidth,
                    flexShrink     = 0,
                    flexDirection  = FlexDirection.Column,
                    alignItems     = Align.Stretch,
                    justifyContent = Justify.Center
                }
            };
            RenderTimelineSplitHandle.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style       = { height = 1, flexShrink = 0, backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f)) }
            });
            RenderTimelineSplitHandle.RegisterCallback<PointerDownEvent>(OnRenderSplitPointerDown);
            RenderTimelineSplitHandle.RegisterCallback<PointerMoveEvent>(OnRenderSplitPointerMove);
            RenderTimelineSplitHandle.RegisterCallback<PointerUpEvent>(OnRenderSplitPointerUp);
            RenderTimelineSplitHandle.RegisterCallback<PointerCaptureOutEvent>(_ => EndRenderSplit());
            return RenderTimelineSplitHandle;
        }

        private VisualElement BuildHierarchyInspectorSplitHandle()
        {
            HierarchyInspectorSplitHandle = new VisualElement
            {
                style =
                {
                    height         = InspectorHandleWidth,
                    flexShrink     = 0,
                    flexDirection  = FlexDirection.Column,
                    alignItems     = Align.Stretch,
                    justifyContent = Justify.Center
                }
            };
            HierarchyInspectorSplitHandle.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style       = { height = 1, flexShrink = 0, backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f)) }
            });
            HierarchyInspectorSplitHandle.RegisterCallback<PointerDownEvent>(OnHierarchySplitPointerDown);
            HierarchyInspectorSplitHandle.RegisterCallback<PointerMoveEvent>(OnHierarchySplitPointerMove);
            HierarchyInspectorSplitHandle.RegisterCallback<PointerUpEvent>(OnHierarchySplitPointerUp);
            HierarchyInspectorSplitHandle.RegisterCallback<PointerCaptureOutEvent>(_ => EndHierarchySplit());
            return HierarchyInspectorSplitHandle;
        }

        // ── 렌더/타임라인 분할 이벤트 ────────────────────────────────────────────

        private void OnRenderSplitPointerDown(PointerDownEvent e)
        {
            if (e.button != 0 || RenderTimelineSplitHandle == null) return;
            _isRenderSplitDragging = true;
            _renderSplitPointerId  = e.pointerId;
            RenderTimelineSplitHandle.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnRenderSplitPointerMove(PointerMoveEvent e)
        {
            if (!_isRenderSplitDragging || e.pointerId != _renderSplitPointerId || LeftPanel == null) return;
            float newH = RenderingView.resolvedStyle.height + e.deltaPosition.y;
            ApplyRenderViewHeight(newH);
            Repaint();
            e.StopPropagation();
        }

        private void OnRenderSplitPointerUp(PointerUpEvent e)
        {
            if (!_isRenderSplitDragging || e.pointerId != _renderSplitPointerId) return;
            EndRenderSplit();
            e.StopPropagation();
        }

        private void EndRenderSplit()
        {
            if (!_isRenderSplitDragging) return;
            _isRenderSplitDragging = false;
            if (_renderSplitPointerId >= 0 && RenderTimelineSplitHandle != null
                && RenderTimelineSplitHandle.HasPointerCapture(_renderSplitPointerId))
                RenderTimelineSplitHandle.ReleasePointer(_renderSplitPointerId);
            _renderSplitPointerId = -1;
        }

        internal void ApplyRenderViewHeight(float h)
        {
            float total = LeftPanel?.resolvedStyle.height ?? position.height;
            _renderViewHeight = Mathf.Clamp(h, 120f, Mathf.Max(120f, total - 160f - InspectorHandleWidth));
            if (RenderingView != null) RenderingView.style.height = _renderViewHeight;
        }

        // ── Hierarchy/Inspector 분할 이벤트 ──────────────────────────────────────

        private void OnHierarchySplitPointerDown(PointerDownEvent e)
        {
            if (e.button != 0 || HierarchyInspectorSplitHandle == null) return;
            _isHierarchySplitDragging = true;
            _hierarchySplitPointerId  = e.pointerId;
            HierarchyInspectorSplitHandle.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnHierarchySplitPointerMove(PointerMoveEvent e)
        {
            if (!_isHierarchySplitDragging || e.pointerId != _hierarchySplitPointerId || RightPanel == null) return;
            float newH = HierarchyPanel.resolvedStyle.height + e.deltaPosition.y;
            ApplyHierarchyPanelHeight(newH);
            Repaint();
            e.StopPropagation();
        }

        private void OnHierarchySplitPointerUp(PointerUpEvent e)
        {
            if (!_isHierarchySplitDragging || e.pointerId != _hierarchySplitPointerId) return;
            EndHierarchySplit();
            e.StopPropagation();
        }

        private void EndHierarchySplit()
        {
            if (!_isHierarchySplitDragging) return;
            _isHierarchySplitDragging = false;
            if (_hierarchySplitPointerId >= 0 && HierarchyInspectorSplitHandle != null
                && HierarchyInspectorSplitHandle.HasPointerCapture(_hierarchySplitPointerId))
                HierarchyInspectorSplitHandle.ReleasePointer(_hierarchySplitPointerId);
            _hierarchySplitPointerId = -1;
        }

        internal void ApplyHierarchyPanelHeight(float h)
        {
            float total = RightPanel?.resolvedStyle.height ?? position.height;
            _hierarchyPanelHeight = Mathf.Clamp(h, 80f, Mathf.Max(80f, total - 80f - InspectorHandleWidth));
            if (HierarchyPanel != null) HierarchyPanel.style.height = _hierarchyPanelHeight;
        }

        private VisualElement BuildInspectorResizeHandle()
        {
            InspectorResizeHandle = new VisualElement
            {
                name = "InspectorResizeHandle",
                style =
                {
                    width         = InspectorHandleWidth,
                    flexShrink    = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems    = Align.Stretch,
                    justifyContent = Justify.Center
                }
            };

            InspectorResizeHandle.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    width           = 1,
                    flexShrink      = 0,
                    backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f))
                }
            });

            InspectorResizeHandle.RegisterCallback<PointerDownEvent>(OnInspectorResizePointerDown);
            InspectorResizeHandle.RegisterCallback<PointerMoveEvent>(OnInspectorResizePointerMove);
            InspectorResizeHandle.RegisterCallback<PointerUpEvent>(OnInspectorResizePointerUp);
            InspectorResizeHandle.RegisterCallback<PointerCaptureOutEvent>(_ => EndInspectorResize());

            return InspectorResizeHandle;
        }

        private void OnInspectorResizePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || InspectorResizeHandle == null) return;

            _isInspectorResizeDragging = true;
            _inspectorResizePointerId  = evt.pointerId;
            InspectorResizeHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnInspectorResizePointerMove(PointerMoveEvent evt)
        {
            if (!_isInspectorResizeDragging || evt.pointerId != _inspectorResizePointerId || MainArea == null) return;

            float widthFromPointer = MainArea.worldBound.xMax - evt.position.x;
            ApplyInspectorWidth(widthFromPointer);
            Repaint();
            evt.StopPropagation();
        }

        private void OnInspectorResizePointerUp(PointerUpEvent evt)
        {
            if (!_isInspectorResizeDragging || evt.pointerId != _inspectorResizePointerId) return;

            EndInspectorResize();
            evt.StopPropagation();
        }

        private void EndInspectorResize()
        {
            if (!_isInspectorResizeDragging) return;

            _isInspectorResizeDragging = false;
            if (_inspectorResizePointerId >= 0 && InspectorResizeHandle != null
                && InspectorResizeHandle.HasPointerCapture(_inspectorResizePointerId))
            {
                InspectorResizeHandle.ReleasePointer(_inspectorResizePointerId);
            }

            _inspectorResizePointerId = -1;
        }

        private void OnInspectorRedoKeyUp(KeyUpEvent evt)
        {
            bool ctrl = evt.ctrlKey || evt.commandKey;
            if (!ctrl || !evt.shiftKey || evt.keyCode != KeyCode.Z)
                return;

            Undo.PerformRedo();
            evt.StopImmediatePropagation();
        }

        // ── 등급 선택 ────────────────────────────────────────────────────────────

        private void SelectGrade(CardGrade grade)
        {
            _selectedGrade = grade;
            ClearKeyframeSelection();
            RefreshGradeButtons();
            RefreshDurationField();
            RefreshObjectList();
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
        }

        internal void RefreshGradeButtons()
        {
            if (_gradeButtons == null) return;
            var gradeValues = new[] { CardGrade.Normal, CardGrade.Rare, CardGrade.Epic, CardGrade.Legendary };
            for (int i = 0; i < _gradeButtons.Length; i++)
            {
                bool sel = gradeValues[i] == _selectedGrade;
                _gradeButtons[i].style.backgroundColor = new StyleColor(sel
                    ? new Color(0.25f, 0.52f, 0.25f)
                    : new Color(0.26f, 0.26f, 0.30f));
                _gradeButtons[i].SetEnabled(_target != null);
            }
        }

        internal void RefreshDurationField() { /* duration은 키프레임 기반 자동 계산 — 별도 필드 없음 */ }
    }
}
