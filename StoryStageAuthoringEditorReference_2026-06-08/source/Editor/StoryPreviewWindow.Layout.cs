using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── 상단 바 ──────────────────────────────────

        private VisualElement BuildTopBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, alignItems = Align.Center,
                    paddingLeft = 8, paddingRight = 8, paddingTop = 5, paddingBottom = 5,
                    borderBottomWidth = 1, borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };

            _episodeField = new ObjectField("Episode") { objectType = typeof(StoryEpisodeSO), style = { flexGrow = 1, marginRight = 12 } };
            _episodeField.RegisterValueChangedCallback(e => SetEpisode(e.newValue as StoryEpisodeSO));
            bar.Add(_episodeField);

            _previewModeField = new EnumField("Mode", previewMode) { style = { width = 180, marginRight = 8 } };
            _previewModeField.RegisterValueChangedCallback(e =>
            {
                previewMode = (PreviewMode)e.newValue;
                ApplyPreviewModeVisibility();
                RebuildActorLayer();
                RefreshActorInspector();
            });
            bar.Add(_previewModeField);

            _dialogueDisplayField = new EnumField("Dialogue", dialogueDisplayMode) { style = { width = 165, marginRight = 8 } };
            _dialogueDisplayField.RegisterValueChangedCallback(e =>
            {
                dialogueDisplayMode = (DialogueDisplayMode)e.newValue;
                RefreshDialogue();
                RefreshChoices();
                ApplyPreviewModeVisibility();
            });
            bar.Add(_dialogueDisplayField);

            _workspaceModeBtn = MakeBtn("Workspace", new Color(0.24f, 0.24f, 0.28f), TogglePreviewWorkspaceMode);
            _collapseInspectorBtn = MakeBtn("Inspector -", new Color(0.22f, 0.22f, 0.26f), TogglePreviewInspectorCollapsed);
            _collapseRuntimeUiBtn = MakeBtn("Runtime UI -", new Color(0.22f, 0.22f, 0.26f), TogglePreviewRuntimeUiCollapsed);

            _playBtn           = MakeBtn("▶ Play",      new Color(0.18f, 0.50f, 0.18f), OnPlay);
            _fromHereBtn       = MakeBtn("▶ From Here", new Color(0.18f, 0.35f, 0.55f), OnFromHere);
            _sampleLineBtn     = MakeBtn("Sample Line", new Color(0.35f, 0.35f, 0.18f), OnSampleLine);
            _prevLineBtn       = MakeBtn("Prev Line",   new Color(0.24f, 0.28f, 0.34f), OnPreviousLine);
            _nextLineAuthoringBtn = MakeBtn("Next Line", new Color(0.24f, 0.28f, 0.34f), OnNextLine);
            _stopBtn           = MakeBtn("■ Stop",      new Color(0.50f, 0.18f, 0.18f), OnStop);
            _nextBtn           = MakeBtn("▷ Next",      new Color(0.30f, 0.30f, 0.30f), OnNext);
            _refreshGameViewBtn = MakeBtn("↻ Game View", new Color(0.25f, 0.25f, 0.32f), RefreshRenderAreaFromGameView);

            bar.Add(_workspaceModeBtn);
            bar.Add(_collapseInspectorBtn);
            bar.Add(_collapseRuntimeUiBtn);
            bar.Add(_playBtn); bar.Add(_fromHereBtn); bar.Add(_sampleLineBtn);
            bar.Add(_prevLineBtn); bar.Add(_nextLineAuthoringBtn);
            bar.Add(_stopBtn); bar.Add(_nextBtn);     bar.Add(_refreshGameViewBtn);

            _statusLabel = new Label("정지") { style = { marginLeft = 10, fontSize = 10, color = new StyleColor(new Color(0.55f, 0.55f, 0.55f)) } };
            bar.Add(_statusLabel);

            _renderAreaLabel = new Label("") { style = { marginLeft = 10, fontSize = 10, color = new StyleColor(new Color(0.62f, 0.68f, 0.78f)) } };
            bar.Add(_renderAreaLabel);

            RefreshPreviewLayoutButtons();
            return bar;
        }

        // ── 좌측 패널 ─────────────────────────────────

        private void TogglePreviewWorkspaceMode()
        {
            bool collapse = !_previewInspectorCollapsed || !_previewRuntimeUiCollapsed;
            if (collapse)
                CapturePreviewInspectorWidth();

            _previewInspectorCollapsed = collapse;
            _previewRuntimeUiCollapsed = collapse;
            ApplyPreviewLayoutVisibility();
            SavePreviewLayoutPrefs();
        }

        private void TogglePreviewInspectorCollapsed()
        {
            if (!_previewInspectorCollapsed)
                CapturePreviewInspectorWidth();

            _previewInspectorCollapsed = !_previewInspectorCollapsed;
            ApplyPreviewLayoutVisibility();
            SavePreviewLayoutPrefs();
        }

        private void TogglePreviewRuntimeUiCollapsed()
        {
            _previewRuntimeUiCollapsed = !_previewRuntimeUiCollapsed;
            ApplyPreviewLayoutVisibility();
            SavePreviewLayoutPrefs();
        }

        private void LoadPreviewLayoutPrefs()
        {
            _previewInspectorCollapsed = UnityEditor.EditorPrefs.GetBool(PrefsKeyPrefix + "InspectorCollapsed", false);
            _previewRuntimeUiCollapsed = UnityEditor.EditorPrefs.GetBool(PrefsKeyPrefix + "RuntimeUiCollapsed", false);
            _previewInspectorExpandedWidth = Mathf.Clamp(
                UnityEditor.EditorPrefs.GetFloat(PrefsKeyPrefix + "InspectorWidth", InspectorWidth),
                MinInspectorWidth,
                MaxInspectorWidth);
            _timelineHeight = Mathf.Clamp(
                UnityEditor.EditorPrefs.GetFloat(PrefsKeyPrefix + "TimelineHeight", DefaultTimelineHeight),
                MinTimelineHeight,
                MaxTimelineHeight);
            _timelinePixelsPerSecond = Mathf.Clamp(
                UnityEditor.EditorPrefs.GetFloat(PrefsKeyPrefix + "TimelinePixelsPerSecond", DefaultTimelinePixelsPerSecond),
                MinTimelinePixelsPerSecond,
                MaxTimelinePixelsPerSecond);
            _timelinePlaybackSpeed = Mathf.Max(0.1f, UnityEditor.EditorPrefs.GetFloat(PrefsKeyPrefix + "TimelinePlaybackSpeed", 1f));
            _timelineSnapEnabled   = UnityEditor.EditorPrefs.GetBool(PrefsKeyPrefix  + "TimelineSnapEnabled",  false);
            _timelineSnapInterval  = Mathf.Max(0.01f, UnityEditor.EditorPrefs.GetFloat(PrefsKeyPrefix + "TimelineSnapInterval", 0.1f));
        }

        private void SavePreviewLayoutPrefs()
        {
            UnityEditor.EditorPrefs.SetBool(PrefsKeyPrefix + "InspectorCollapsed", _previewInspectorCollapsed);
            UnityEditor.EditorPrefs.SetBool(PrefsKeyPrefix + "RuntimeUiCollapsed", _previewRuntimeUiCollapsed);
            UnityEditor.EditorPrefs.SetFloat(PrefsKeyPrefix + "InspectorWidth", _previewInspectorExpandedWidth);
            UnityEditor.EditorPrefs.SetFloat(PrefsKeyPrefix + "TimelineHeight", _timelineHeight);
            UnityEditor.EditorPrefs.SetFloat(PrefsKeyPrefix + "TimelinePixelsPerSecond", _timelinePixelsPerSecond);
            UnityEditor.EditorPrefs.SetFloat(PrefsKeyPrefix + "TimelinePlaybackSpeed", _timelinePlaybackSpeed);
            UnityEditor.EditorPrefs.SetBool(PrefsKeyPrefix  + "TimelineSnapEnabled",   _timelineSnapEnabled);
            UnityEditor.EditorPrefs.SetFloat(PrefsKeyPrefix + "TimelineSnapInterval",  _timelineSnapInterval);
        }

        private void ApplyPreviewLayoutVisibility()
        {
            ApplyInspectorPanelVisibility();
            RefreshPreviewLayoutButtons();
            ApplyPreviewModeVisibility();
        }

        private void ApplyInspectorPanelVisibility()
        {
            bool showInspector = IsStageAuthoringMode && !_previewInspectorCollapsed;

            if (_inspectorPanel != null)
            {
                if (showInspector)
                {
                    _inspectorPanel.style.display = DisplayStyle.Flex;
                    _inspectorPanel.style.width = Mathf.Clamp(_previewInspectorExpandedWidth, MinInspectorWidth, MaxInspectorWidth);
                }
                else
                {
                    _inspectorPanel.style.display = DisplayStyle.None;
                }
            }

            SetElementVisible(_inspectorSplitter, showInspector);
        }

        private void RefreshPreviewLayoutButtons()
        {
            if (_workspaceModeBtn != null)
                _workspaceModeBtn.text = _previewInspectorCollapsed && _previewRuntimeUiCollapsed ? "Workspace On" : "Workspace";

            if (_collapseInspectorBtn != null)
                _collapseInspectorBtn.text = _previewInspectorCollapsed ? "Inspector +" : "Inspector -";

            if (_collapseRuntimeUiBtn != null)
                _collapseRuntimeUiBtn.text = _previewRuntimeUiCollapsed ? "Runtime UI +" : "Runtime UI -";
        }

        private void CapturePreviewInspectorWidth()
        {
            if (_inspectorPanel == null || _previewInspectorCollapsed)
                return;

            float width = _inspectorPanel.resolvedStyle.width > 0
                ? _inspectorPanel.resolvedStyle.width
                : _previewInspectorExpandedWidth;
            _previewInspectorExpandedWidth = Mathf.Clamp(width, MinInspectorWidth, MaxInspectorWidth);
        }

        private VisualElement BuildLeftPanel()
        {
            var left = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };

            _stageWrapper = new VisualElement
            {
                style = { flexGrow = 1, overflow = Overflow.Hidden, backgroundColor = new StyleColor(new Color(0.04f, 0.04f, 0.05f)) }
            };
            _stageWrapper.focusable = true;
            BuildStoryRenderingRoot();
            _stageWrapper.Add(_stageWorld);
            BuildLetterboxOverlays();
            _stageWrapper.RegisterCallback<GeometryChangedEvent>(OnStageWrapperResized);
            RegisterPanZoomCallbacks();
            left.Add(_stageWrapper);

            left.Add(BuildTimelineResizeHandle());
            left.Add(BuildTimelinePanel());

            // RuntimePreview 전용 하단 대화 패널
            _dialoguePanel = new VisualElement
            {
                style =
                {
                    flexShrink = 0, height = DialoguePanelH,
                    paddingLeft = 14, paddingRight = 14, paddingTop = 8, paddingBottom = 8,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f)),
                    borderTopWidth = 1, borderTopColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f))
                }
            };
            _speakerLabel  = new Label("") { style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.4f)), marginBottom = 3 } };
            _dialogueLabel = new Label("") { style = { fontSize = 12, whiteSpace = WhiteSpace.Normal, color = new StyleColor(new Color(0.92f, 0.92f, 0.92f)) } };
            _dialoguePanel.Add(_speakerLabel);
            _dialoguePanel.Add(_dialogueLabel);
            left.Add(_dialoguePanel);

            _choiceArea = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 4 } };
            left.Add(_choiceArea);

            return left;
        }

        // ── Stage Rendering Root 구축 ─────────────────

        private void BuildStoryRenderingRoot()
        {
            // Stage World: pan/zoom transform 대상. overflow=Visible 로 카메라 프레임 밖도 표시.
            _stageWorld = new VisualElement
            {
                name = "StageWorld",
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    width = DefaultUnitPixels,
                    height = DefaultUnitPixels,
                    overflow = Overflow.Visible
                }
            };
            _stageWorld.style.transformOrigin = new TransformOrigin(
                new Length(0, LengthUnit.Pixel),
                new Length(0, LengthUnit.Pixel),
                0);

            _authoringGridLayer = BuildAuthoringGridLayer();

            // 배경 레이어: UpdateCameraFrameGeometry() 에서 크기 갱신.
            _backgroundLayer = new VisualElement
            {
                name = "StoryPreviewBackground",
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, top = 0, overflow = Overflow.Visible }
            };

            // 액터 레이어: world 좌표계, 카메라 프레임 밖 배치 허용.
            _actorLayer = new VisualElement
            {
                name = "StoryPreviewActorStage",
                style = { position = Position.Absolute, left = 0, top = 0, overflow = Overflow.Visible }
            };

            // 카메라 프레임 가이드: 게임 카메라가 보는 영역을 나타내는 테두리.
            // 크기는 DefaultUnitPixels × (DefaultUnitPixels / aspect). UpdateCameraFrameGeometry() 에서 갱신.
            _cameraFrameGuide = new VisualElement
            {
                name = "CameraFrameGuide",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute, left = 0, top = 0,
                    width = DefaultUnitPixels, height = DefaultUnitPixels,
                    // 파란 Reference Frame 제거: border 없음
                    overflow = Overflow.Visible
                }
            };
            _cameraGizmoLayer = BuildCameraGizmoLayer();

            // 대화 오버레이 (카메라 프레임 내부 하단)
            _renderDialoguePanel = new VisualElement
            {
                name = "StoryPreviewDialogueOverlay",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute, left = 18, right = 18, bottom = 18, minHeight = 82,
                    paddingLeft = 12, paddingRight = 12, paddingTop = 8, paddingBottom = 8,
                    backgroundColor = new StyleColor(new Color(0.04f, 0.04f, 0.05f, 0.82f)),
                    borderTopWidth = 1, borderRightWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1,
                    borderTopColor    = new StyleColor(new Color(0.7f, 0.7f, 0.76f, 0.55f)),
                    borderRightColor  = new StyleColor(new Color(0.7f, 0.7f, 0.76f, 0.55f)),
                    borderBottomColor = new StyleColor(new Color(0.7f, 0.7f, 0.76f, 0.55f)),
                    borderLeftColor   = new StyleColor(new Color(0.7f, 0.7f, 0.76f, 0.55f))
                }
            };
            _renderSpeakerLabel  = new Label("") { pickingMode = PickingMode.Ignore, style = { fontSize = 10, marginBottom = 4, color = new StyleColor(new Color(0.88f, 0.82f, 0.5f)) } };
            _renderDialogueLabel = new Label("") { pickingMode = PickingMode.Ignore, style = { fontSize = 12, whiteSpace = WhiteSpace.Normal, color = new StyleColor(new Color(0.94f, 0.94f, 0.96f)) } };
            _renderDialoguePanel.Add(_renderSpeakerLabel);
            _renderDialoguePanel.Add(_renderDialogueLabel);

            // 선택지 오버레이 (카메라 프레임 내부)
            _renderChoiceArea = new VisualElement
            {
                name = "StoryPreviewChoiceOverlay",
                style = { position = Position.Absolute, left = 18, right = 18, bottom = 118, flexDirection = FlexDirection.Column, alignItems = Align.Stretch }
            };

            // 빈 스테이지 안내 라벨
            _emptyStageLabel = new Label("Stage Layout 모듈 또는 표시 중인 액터가 없습니다.")
            {
                name = "StoryPreviewEmptyStageLabel",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute, left = 12, right = 12, top = 12,
                    unityTextAlign = TextAnchor.UpperCenter,
                    whiteSpace = WhiteSpace.Normal, fontSize = 11,
                    color = new StyleColor(new Color(0.65f, 0.65f, 0.7f))
                }
            };

            _cameraFrameGuide.Add(_emptyStageLabel);
            _cameraFrameGuide.Add(_renderChoiceArea);
            _cameraFrameGuide.Add(_renderDialoguePanel);

            _stageWorld.Add(_authoringGridLayer);
            _stageWorld.Add(_backgroundLayer);
            _stageWorld.Add(_cameraGizmoLayer);
            _stageWorld.Add(_actorLayer);
            _stageWorld.Add(_cameraFrameGuide);
        }

        // ── Letterbox Overlays ────────────────────────

        private void BuildLetterboxOverlays()
        {
            _letterboxLeftOverlay = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 0, bottom = 0, left = 0, width = 0,
                    display = DisplayStyle.None
                }
            };
            _letterboxRightOverlay = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 0, bottom = 0, left = 0, width = 0,
                    display = DisplayStyle.None
                }
            };
            _stageWrapper.Add(_letterboxLeftOverlay);
            _stageWrapper.Add(_letterboxRightOverlay);
        }

        internal void UpdateLetterboxOverlay()
        {
            if (_letterboxLeftOverlay == null || _letterboxRightOverlay == null) return;

            float ratio = _previewAspectSettings?.VisibleWidthRatio ?? 1f;
            if (ratio >= 0.999f)
            {
                _letterboxLeftOverlay.style.display  = DisplayStyle.None;
                _letterboxRightOverlay.style.display = DisplayStyle.None;
                return;
            }

            // DefaultUnitPixels already equals the visible width. In authoring mode
            // actors are not shifted by camera offset, so we shift the window manually.
            float camOffsetCanvas = 0f;
            if (!ShouldApplyCameraFocusToRenderedPreview())
            {
                float pixelsPerWorld = DefaultUnitPixels / ResolvePreviewCameraWorldWidth();
                camOffsetCanvas = ResolvePreviewCameraFocusOffset().x * pixelsPerWorld;
            }

            float visibleLeftPanel  = _stagePanOffset.x + camOffsetCanvas * _stageZoom;
            float visibleRightPanel = _stagePanOffset.x + (DefaultUnitPixels + camOffsetCanvas) * _stageZoom;
            float wrapW             = _stageWrapper?.resolvedStyle.width ?? 0f;

            Color color = IsRuntimePreviewMode && _previewAspectSettings != null
                ? _previewAspectSettings.LetterboxColor
                : new Color(0f, 0f, 0f, 0.38f);

            _letterboxLeftOverlay.style.display = DisplayStyle.Flex;
            _letterboxLeftOverlay.style.left    = 0;
            _letterboxLeftOverlay.style.width   = Mathf.Max(0f, visibleLeftPanel);
            _letterboxLeftOverlay.style.backgroundColor = new StyleColor(color);

            _letterboxRightOverlay.style.display = DisplayStyle.Flex;
            _letterboxRightOverlay.style.left    = Mathf.Max(0f, visibleRightPanel);
            _letterboxRightOverlay.style.width   = Mathf.Max(0f, wrapW - visibleRightPanel);
            _letterboxRightOverlay.style.backgroundColor = new StyleColor(color);

            if (IsRuntimePreviewMode && _previewAspectSettings != null)
            {
                SetLetterboxSprite(_letterboxLeftOverlay,  _previewAspectSettings.LeftLetterboxSprite);
                SetLetterboxSprite(_letterboxRightOverlay, _previewAspectSettings.RightLetterboxSprite);
            }
            else
            {
                _letterboxLeftOverlay.style.backgroundImage  = StyleKeyword.Null;
                _letterboxRightOverlay.style.backgroundImage = StyleKeyword.Null;
            }
        }

        private static void SetLetterboxSprite(VisualElement el, Sprite sprite)
        {
            el.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : StyleKeyword.Null;
        }

        private static VisualElement BuildAuthoringGridLayer()
        {
            float size = DefaultUnitPixels + WorldPaddingPixels * 2f;
            var grid = new VisualElement
            {
                name = "StageAuthoringGrid",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = -WorldPaddingPixels,
                    top = -WorldPaddingPixels,
                    width = size,
                    height = size,
                    backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.19f))
                }
            };

            AddGridLines(grid, size, GridMinorPixels, new Color(0.26f, 0.26f, 0.27f, 0.55f), 1f);
            AddGridLines(grid, size, GridMajorPixels, new Color(0.34f, 0.34f, 0.36f, 0.72f), 1.5f);
            return grid;
        }

        private static void AddGridLines(VisualElement grid, float size, float interval, Color color, float thickness)
        {
            for (float p = 0f; p <= size; p += interval)
            {
                grid.Add(new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        left = p,
                        top = 0,
                        width = thickness,
                        height = size,
                        backgroundColor = new StyleColor(color)
                    }
                });

                grid.Add(new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        left = 0,
                        top = p,
                        width = size,
                        height = thickness,
                        backgroundColor = new StyleColor(color)
                    }
                });
            }
        }

        private static VisualElement BuildCameraGizmoLayer()
        {
            return new VisualElement
            {
                name = "StoryPreviewCameraGizmoLayer",
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, overflow = Overflow.Visible }
            };
        }

    }
}
