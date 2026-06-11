using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Gamelib.SoundSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        private const float TimelinePropertyWidth = 130f;
        private const float TimelineRowHeight = 24f;
        private const float TimelineMarkerSize = 10f;
        private const float TimelineKeyHitSeconds = 0.04f;
        private const float TimelineSegmentHitHeight = 16f;
        private const float TimelineSegmentHeight = 5f;
        private const float TimelineSelectedSegmentHeight = 7f;

        private VisualElement BuildTimelineResizeHandle()
        {
            _timelineResizeHandle = new VisualElement
            {
                name = "StoryPreviewTimelineResizeHandle",
                style =
                {
                    height = 5,
                    flexShrink = 0,
                    backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f))
                }
            };

            _timelineResizeHandle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                _isTimelineResizing = true;
                _timelineResizeStartY = e.position.y;
                _timelineResizeStartHeight = _timelineHeight;
                _timelineResizeHandle.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            _timelineResizeHandle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isTimelineResizing) return;
                float delta = _timelineResizeStartY - e.position.y;
                _timelineHeight = Mathf.Clamp(_timelineResizeStartHeight + delta, MinTimelineHeight, MaxTimelineHeight);
                if (_timelinePanel != null) _timelinePanel.style.height = _timelineHeight;
                e.StopPropagation();
            });

            _timelineResizeHandle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isTimelineResizing) return;
                _isTimelineResizing = false;
                _timelineResizeHandle.ReleasePointer(e.pointerId);
                SavePreviewLayoutPrefs();
                e.StopPropagation();
            });

            return _timelineResizeHandle;
        }

        private VisualElement BuildTimelinePanel()
        {
            _timelinePanel = new VisualElement
            {
                name = "StoryPreviewKeyframeEditor",
                style =
                {
                    height = _timelineHeight,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.135f)),
                    borderTopWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.22f, 0.22f, 0.24f))
                }
            };
            _timelinePanel.focusable = true;

            _timelineToolbar = new VisualElement
            {
                style =
                {
                    height = 30,
                    flexShrink = 0,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    overflow = Overflow.Hidden,
                    paddingLeft = 6,
                    paddingRight = 6
                }
            };
            _timelineToolbar.RegisterCallback<PointerDownEvent>(_ => SetInteractionContext(InteractionContext.Timeline));

            _timelineTitleLabel = new Label("Keyframe Editor")
            {
                style =
                {
                    width = 150,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.86f, 0.86f, 0.9f))
                }
            };
            var coreToolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexShrink = 0
                }
            };
            var extendedScroll = new ScrollView(ScrollViewMode.Horizontal)
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    height = 30,
                    marginLeft = 4,
                    overflow = Overflow.Hidden
                }
            };
            var extendedToolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexShrink = 0
                }
            };
            extendedScroll.Add(extendedToolbar);
            _timelineToolbar.Add(coreToolbar);
            _timelineToolbar.Add(extendedScroll);

            coreToolbar.Add(_timelineTitleLabel);

            _timelinePlayBtn = MakeBtn("Play", new Color(0.20f, 0.35f, 0.25f), ToggleTimelinePlayback);
            _timelineRecordBtn = MakeBtn("Record", new Color(0.36f, 0.18f, 0.18f), ToggleTimelineRecord);
            coreToolbar.Add(_timelinePlayBtn);
            coreToolbar.Add(MakeBtn("|<", new Color(0.23f, 0.23f, 0.27f), SelectFirstTimelineKey));
            coreToolbar.Add(MakeBtn("<", new Color(0.23f, 0.23f, 0.27f), SelectPreviousTimelineKey));
            coreToolbar.Add(MakeBtn(">", new Color(0.23f, 0.23f, 0.27f), SelectNextTimelineKey));
            coreToolbar.Add(MakeBtn(">|", new Color(0.23f, 0.23f, 0.27f), SelectLastTimelineKey));
            coreToolbar.Add(_timelineRecordBtn);
            extendedToolbar.Add(MakeBtn("Add Property", new Color(0.24f, 0.28f, 0.34f), ShowAddPropertyMenu));
            extendedToolbar.Add(MakeBtn("Add Key", new Color(0.24f, 0.30f, 0.24f), AddTimelineKeyAtPlayhead));
            extendedToolbar.Add(MakeBtn("Remove Key", new Color(0.34f, 0.20f, 0.20f), RemoveSelectedTimelineKey));
            extendedToolbar.Add(MakeBtn("Preset Library", new Color(0.25f, 0.25f, 0.34f), StoryMotionPresetLibraryWindow.Open));

            var speedSlider = new Slider(0.25f, 4f)
            {
                value = _timelinePlaybackSpeed,
                style = { width = 90, marginLeft = 6, marginRight = 4 }
            };
            speedSlider.RegisterValueChangedCallback(e =>
            {
                _timelinePlaybackSpeed = Mathf.Max(0.1f, e.newValue);
                if (_timelineSpeedField != null) _timelineSpeedField.SetValueWithoutNotify(_timelinePlaybackSpeed);
                SavePreviewLayoutPrefs();
            });
            extendedToolbar.Add(speedSlider);

            _timelineSpeedField = new FloatField("Speed")
            {
                value = _timelinePlaybackSpeed,
                style = { width = 88 }
            };
            _timelineSpeedField.RegisterValueChangedCallback(e =>
            {
                _timelinePlaybackSpeed = Mathf.Max(0.1f, e.newValue);
                speedSlider.SetValueWithoutNotify(Mathf.Clamp(_timelinePlaybackSpeed, 0.25f, 4f));
                SavePreviewLayoutPrefs();
            });
            extendedToolbar.Add(_timelineSpeedField);

            _timelineSnapBtn = new Button(OnTimelineSnapToggled)
            {
                text  = _timelineSnapEnabled ? "Snap ON" : "Snap OFF",
                style =
                {
                    width = 68, marginLeft = 8,
                    backgroundColor = new StyleColor(_timelineSnapEnabled
                        ? new Color(0.22f, 0.48f, 0.22f)
                        : new Color(0.28f, 0.28f, 0.28f))
                }
            };
            extendedToolbar.Add(_timelineSnapBtn);

            var snapFieldRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginLeft = 4 }
            };
            snapFieldRow.Add(new Label("s:")
            {
                style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)), marginRight = 2 }
            });
            _timelineSnapField = new FloatField
            {
                value = _timelineSnapInterval,
                style = { width = 46 }
            };
            _timelineSnapField.RegisterValueChangedCallback(e =>
            {
                _timelineSnapInterval = Mathf.Max(0.01f, e.newValue);
                _timelineSnapField.SetValueWithoutNotify(_timelineSnapInterval);
                SavePreviewLayoutPrefs();
            });
            snapFieldRow.Add(_timelineSnapField);
            extendedToolbar.Add(snapFieldRow);

            _timelinePanel.Add(_timelineToolbar);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                style = { flexGrow = 1 }
            };
            var content = new VisualElement { style = { flexDirection = FlexDirection.Column, minHeight = 170 } };
            scroll.Add(content);

            _timelineRuler = new VisualElement { style = { flexDirection = FlexDirection.Row, height = 28, flexShrink = 0 } };
            _timelineRows = new VisualElement { style = { flexDirection = FlexDirection.Column, minHeight = 96 } };
            content.Add(_timelineRuler);
            content.Add(_timelineRows);
            _timelinePanel.Add(scroll);

            _timelinePanel.RegisterCallback<KeyDownEvent>(OnTimelineKeyDown);
            _timelinePanel.RegisterCallback<PointerDownEvent>(OnTimelinePanelPointerDown);
            _timelinePanel.RegisterCallback<WheelEvent>(OnTimelineWheel);
            _timelinePanel.RegisterCallback<PointerMoveEvent>(OnTimelinePointerMove);
            _timelinePanel.RegisterCallback<PointerUpEvent>(OnTimelinePointerUp);
            _timelinePanel.RegisterCallback<PointerCaptureOutEvent>(_ => CancelTimelinePointerDrag());
            RefreshTimelinePanel();
            return _timelinePanel;
        }

        private void OnTimelineSnapToggled()
        {
            _timelineSnapEnabled = !_timelineSnapEnabled;
            if (_timelineSnapBtn != null)
            {
                _timelineSnapBtn.text = _timelineSnapEnabled ? "Snap ON" : "Snap OFF";
                _timelineSnapBtn.style.backgroundColor = new StyleColor(_timelineSnapEnabled
                    ? new Color(0.22f, 0.48f, 0.22f)
                    : new Color(0.28f, 0.28f, 0.28f));
            }
            SavePreviewLayoutPrefs();
        }

        private float ApplySnap(float time) =>
            _timelineSnapEnabled && _timelineSnapInterval > 0f
                ? Mathf.Round(time / _timelineSnapInterval) * _timelineSnapInterval
                : time;

        private void RefreshTimelinePanel()
        {
            if (_timelinePanel == null || _timelineRows == null || _timelineRuler == null) return;

            bool visible = IsStageAuthoringMode;
            SetElementVisible(_timelinePanel, visible);
            SetElementVisible(_timelineResizeHandle, visible);
            if (!visible) return;

            _timelinePanel.style.height = _timelineHeight;
            _timelineRows.Clear();
            _timelineRuler.Clear();
            _timelineKeyElements.Clear();

            if (_timelineTitleLabel != null) _timelineTitleLabel.text = ResolveTimelineTitle();
            if (_timelineRecordBtn != null) _timelineRecordBtn.text = _timelineRecordEnabled ? "Record On" : "Record";
            if (_timelineRecordBtn != null) _timelineRecordBtn.SetEnabled(_selectionKind != StageSelectionKind.Sound);
            if (_timelineSnapBtn  != null)
            {
                _timelineSnapBtn.text = _timelineSnapEnabled ? "Snap ON" : "Snap OFF";
                _timelineSnapBtn.style.backgroundColor = new StyleColor(_timelineSnapEnabled
                    ? new Color(0.22f, 0.48f, 0.22f)
                    : new Color(0.28f, 0.28f, 0.28f));
            }
            if (_timelineSnapField != null) _timelineSnapField.SetValueWithoutNotify(_timelineSnapInterval);
            if (_timelinePlayBtn != null) _timelinePlayBtn.text = _timelineIsPlaying ? "Stop" : "Play";
            if (_timelineSpeedField != null) _timelineSpeedField.SetValueWithoutNotify(_timelinePlaybackSpeed);

            BuildTimelineRuler();

            if (_currentLine == null)
            {
                AddTimelineEmpty("Select a story line.");
                return;
            }

            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                BuildActorTimelineRows();
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                BuildBackgroundTimelineRows();
                return;
            }

            if (_selectionKind == StageSelectionKind.Camera)
            {
                BuildCameraTimelineRows();
                return;
            }

            if (_selectionKind == StageSelectionKind.Sound)
            {
                BuildSoundTimelineRows();
                return;
            }

            AddTimelineEmpty("Select an actor, background, camera, or sound in Stage Authoring.");
        }

        private string ResolveTimelineTitle()
        {
            if (_timelineRecordEnabled && _timelineRecordSelectionKind == StageSelectionKind.Camera)
                return "REC: Camera";
            if (_timelineRecordEnabled && _timelineRecordSelectionKind == StageSelectionKind.Background)
                return "REC: Background";
            if (_timelineRecordEnabled && !string.IsNullOrWhiteSpace(_timelineRecordActorKey))
                return $"REC: {_timelineRecordActorKey}";
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
                return $"Actor Track: {_selectedActorKey}";
            if (_selectionKind == StageSelectionKind.Camera)
                return "Camera Track";
            if (_selectionKind == StageSelectionKind.Sound)
                return "Sound Track";
            return _selectionKind == StageSelectionKind.Background ? "Background Transition" : "Keyframe Editor";
        }

        private void BuildTimelineRuler()
        {
            _timelineRuler.Add(new Label("Property")
            {
                style =
                {
                    width = TimelinePropertyWidth,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    paddingLeft = 6,
                    color = new StyleColor(new Color(0.68f, 0.68f, 0.72f))
                }
            });

            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            var lane = CreateTimelineLane(duration, 28f, StoryActorKeyframeProperty.Position, allowKeyContext: false);
            float tick = ResolveTimelineTickSeconds();
            for (float t = 0f; t <= duration + 0.001f; t += tick) AddTimelineTick(lane, t, true);
            AddTimelinePlayhead(lane, duration, 28f);
            _timelineRuler.Add(lane);
        }

        private void BuildActorTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryActorTrackData track = FindActorTrack(layout, _selectedActorKey);

            if (track?.keyframes == null || track.keyframes.Count == 0)
            {
                AddTimelineEmpty("No actor track. Use Add Property.");
                return;
            }

            bool hasVisibleRow = false;
            if (HasProperty(track, StoryActorKeyframeProperty.Position))
            {
                BuildActorTimelineRow("Position", track, StoryActorKeyframeProperty.Position);
                hasVisibleRow = true;
            }
            if (HasProperty(track, StoryActorKeyframeProperty.Scale))
            {
                BuildActorTimelineRow("Scale", track, StoryActorKeyframeProperty.Scale);
                hasVisibleRow = true;
            }
            if (HasProperty(track, StoryActorKeyframeProperty.Expression))
            {
                BuildActorTimelineRow("Expression", track, StoryActorKeyframeProperty.Expression);
                hasVisibleRow = true;
            }

            if (!hasVisibleRow)
                AddTimelineEmpty("No editable property row. Use Add Property.");
        }

        private void BuildActorTimelineRow(string label, StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            BuildTimelineKeyframeRow(label, track?.keyframes, property);
        }

        private void BuildTimelineKeyframeRow(string label, IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property)
        {
            var row = CreateTimelineRow(label);
            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            var lane = CreateTimelineLane(duration, TimelineRowHeight, property);

            var indexes = GetOrderedKeyIndexes(keyframes, property);
            if (SupportsTimelineSegment(property))
            {
                if (indexes.Count > 0)
                    AddPreFirstSegmentBar(lane, keyframes, indexes[0]);

                for (int i = 0; i < indexes.Count - 1; i++)
                    AddSegmentBar(lane, keyframes, indexes[i], indexes[i + 1]);
            }

            foreach (int keyIndex in indexes)
                AddActorKeyMarker(lane, keyIndex, keyframes[keyIndex], property);

            AddTimelinePlayhead(lane, duration, TimelineRowHeight);
            row.Add(lane);
            _timelineRows.Add(row);
        }

        private void BuildBackgroundTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryBackgroundStateData state = layout != null ? layout.Background : _bgState;
            if (state == null || !state.HasBackground)
            {
                AddTimelineEmpty("No background state on this line.");
                return;
            }

            StoryBackgroundTrackData track = layout?.BackgroundTrack;
            if (track?.keyframes == null || track.keyframes.Count == 0)
            {
                AddTimelineEmpty("No background track. Use Add Property.");
                return;
            }

            bool hasVisibleRow = false;
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.BackgroundCut))
            {
                BuildTimelineKeyframeRow("Cut", track.keyframes, StoryActorKeyframeProperty.BackgroundCut);
                hasVisibleRow = true;
            }
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.BackgroundPosition))
            {
                BuildTimelineKeyframeRow("Position", track.keyframes, StoryActorKeyframeProperty.BackgroundPosition);
                hasVisibleRow = true;
            }
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.BackgroundScale))
            {
                BuildTimelineKeyframeRow("Scale", track.keyframes, StoryActorKeyframeProperty.BackgroundScale);
                hasVisibleRow = true;
            }

            if (!hasVisibleRow)
                AddTimelineEmpty("No editable background property row. Use Add Property.");
        }

        private void BuildCameraTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryCameraTrackData track = layout?.CameraTrackEditable;
            if (track?.keyframes == null || track.keyframes.Count == 0)
            {
                AddTimelineEmpty("No camera track. Use Add Property.");
                return;
            }

            bool hasVisibleRow = false;
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.CameraTarget))
            {
                BuildTimelineKeyframeRow("Target", track.keyframes, StoryActorKeyframeProperty.CameraTarget);
                hasVisibleRow = true;
            }
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.CameraOffset))
            {
                BuildTimelineKeyframeRow("Offset", track.keyframes, StoryActorKeyframeProperty.CameraOffset);
                hasVisibleRow = true;
            }
            if (HasProperty(track.keyframes, StoryActorKeyframeProperty.CameraZoom))
            {
                BuildTimelineKeyframeRow("Zoom", track.keyframes, StoryActorKeyframeProperty.CameraZoom);
                hasVisibleRow = true;
            }

            if (!hasVisibleRow)
                AddTimelineEmpty("No editable camera property row. Use Add Property.");
        }

        private VisualElement CreateTimelineRow(string label)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, height = TimelineRowHeight, flexShrink = 0 } };
            row.Add(new Label(label)
            {
                style =
                {
                    width = TimelinePropertyWidth,
                    paddingLeft = 6,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    color = new StyleColor(new Color(0.78f, 0.78f, 0.82f))
                }
            });
            return row;
        }

        private VisualElement CreateTimelineLane(float duration, float height, StoryActorKeyframeProperty property, bool allowKeyContext = true)
        {
            var lane = new VisualElement
            {
                style =
                {
                    position = Position.Relative,
                    width = Mathf.Max(360f, duration * _timelinePixelsPerSecond + 32f),
                    height = height,
                    backgroundColor = new StyleColor(new Color(0.095f, 0.095f, 0.105f))
                }
            };

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode) return;
                if (e.button == 1 && allowKeyContext && IsTimelineKeyProperty(property))
                {
                    SetInteractionContext(InteractionContext.Timeline);
                    _timelinePanel?.Focus();
                    _selectedTimelineProperty = property;
                    ClearTimelineSelection(refresh: false);
                    float time = ResolveTimelineTimeFromLaneX(e.localPosition.x);
                    _timelinePlayheadTime = time;
                    ApplyTimelinePlayheadSample();
                    ShowRowKeyContextMenu(property, time);
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                    e.StopPropagation();
                    return;
                }

                if (e.button != 0) return;
                SetInteractionContext(InteractionContext.Timeline);
                _timelinePanel?.Focus();
                _selectedTimelineProperty = property;
                if (allowKeyContext && IsTimelineKeyProperty(property))
                {
                    BeginTimelineBoxSelection(e);
                }
                else
                {
                    ClearTimelineSelection(refresh: false);
                    if (_selectionKind == StageSelectionKind.Sound)
                        ClearSoundTimelineSelection();
                    _isTimelinePlayheadDragging = true;
                    _activeTimelinePointerId = e.pointerId;
                    _timelinePanel?.CapturePointer(e.pointerId);
                    SetTimelinePlayheadFromLocalX(e.localPosition.x);
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                }
                e.StopPropagation();
            });

            if (allowKeyContext && IsMotionPresetProperty(property))
            {
                lane.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    StoryMotionPresetSO preset = GetDraggedMotionPreset();
                    if (!CanApplyMotionPresetToProperty(preset, property))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    e.StopPropagation();
                });

                lane.RegisterCallback<DragPerformEvent>(e =>
                {
                    StoryMotionPresetSO preset = GetDraggedMotionPreset();
                    if (!CanApplyMotionPresetToProperty(preset, property))
                        return;

                    DragAndDrop.AcceptDrag();
                    _selectedTimelineProperty = property;
                    ApplyMotionPresetToCurrentTrack(preset, property, ResolveTimelineTimeFromLaneX(e.localMousePosition.x));
                    e.StopPropagation();
                });
            }

            return lane;
        }

        private void AddActorKeyMarker(VisualElement lane, int index, StoryActorKeyframeData keyframe, StoryActorKeyframeProperty property)
        {
            float x = StoryTransitionSampler.GetKeyTime(keyframe) * _timelinePixelsPerSecond;
            bool selected = IsTimelineKeySelected(keyframe, index, property);
            var marker = new VisualElement
            {
                name = $"ActorKey_{property}_{index}",
                tooltip = $"{property} {StoryTransitionSampler.GetKeyTime(keyframe):0.00}s",
                style =
                {
                    position = Position.Absolute,
                    left = x - TimelineMarkerSize * 0.5f,
                    top = (TimelineRowHeight - TimelineMarkerSize) * 0.5f,
                    width = TimelineMarkerSize,
                    height = TimelineMarkerSize,
                    backgroundColor = new StyleColor(selected ? new Color(1f, 0.82f, 0.22f) : ResolveKeyColor(property)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black)
                }
            };

            marker.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                SetInteractionContext(InteractionContext.Timeline);
                _timelinePanel?.Focus();

                if (e.ctrlKey || e.commandKey)
                {
                    ToggleTimelineKeySelection(keyframe, index, property);
                    _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(keyframe);
                    ApplyTimelinePlayheadSample();
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                    e.StopPropagation();
                    return;
                }

                if (_selectedTimelineKeys.Count > 1 && _selectedTimelineKeys.Contains(keyframe))
                {
                    BeginTimelineGroupKeyDrag(e);
                }
                else
                {
                    SelectSingleTimelineKey(keyframe, index, property);
                    _isTimelineKeyDragging = true;
                    _draggingTimelineActorKey = _selectedActorKey;
                    _draggingTimelineSelectionKind = _selectionKind;
                    _draggingTimelineKeyIndex = index;
                    _draggingTimelineKeyStartTime = StoryTransitionSampler.GetKeyTime(keyframe);
                    BeginTimelineDragUndo("Move Timeline Key");
                    _activeTimelinePointerId = e.pointerId;
                    _timelinePanel?.CapturePointer(e.pointerId);
                }

                _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(keyframe);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                e.StopPropagation();
            });
            lane.Add(marker);
            _timelineKeyElements[keyframe] = marker;
        }

        private void AddSegmentBar(VisualElement lane, IReadOnlyList<StoryActorKeyframeData> keyframes, int fromIndex, int toIndex)
        {
            StoryActorKeyframeData from = keyframes[fromIndex];
            StoryActorKeyframeData to = keyframes[toIndex];
            float fromX = StoryTransitionSampler.GetKeyTime(from) * _timelinePixelsPerSecond;
            float toX = StoryTransitionSampler.GetKeyTime(to) * _timelinePixelsPerSecond;
            float width = Mathf.Max(2f, toX - fromX);
            bool selected = _selectedTimelineSegmentKeyIndex == toIndex
                && _selectedTimelineSegmentProperty == to.property;
            Color segmentColor = ResolveEasingColor(to.easing);
            var hitArea = new VisualElement
            {
                tooltip = $"{to.property} segment [{StoryTransitionSampler.GetKeyTime(from):0.00}s → {to.easing}]",
                style =
                {
                    position = Position.Absolute,
                    left = fromX,
                    top = (TimelineRowHeight - TimelineSegmentHitHeight) * 0.5f,
                    width = width,
                    height = TimelineSegmentHitHeight,
                    backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.001f))
                }
            };
            hitArea.RegisterCallback<PointerDownEvent>(e => HandleSegmentPointerDown(e, from, to, fromIndex, toIndex));
            lane.Add(hitArea);

            float visualHeight = selected ? TimelineSelectedSegmentHeight : TimelineSegmentHeight;
            var bar = new VisualElement
            {
                tooltip = $"{to.property} segment [{StoryTransitionSampler.GetKeyTime(from):0.00}s → {to.easing}]",
                style =
                {
                    position = Position.Absolute,
                    left = fromX,
                    top = TimelineRowHeight * 0.5f - visualHeight * 0.5f,
                    width = width,
                    height = visualHeight,
                    backgroundColor = new StyleColor(selected ? Color.Lerp(segmentColor, new Color(1f, 0.60f, 0.18f, 0.95f), 0.45f) : segmentColor),
                    borderTopWidth = selected ? 1 : 0,
                    borderBottomWidth = selected ? 1 : 0,
                    borderTopColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f)),
                    borderBottomColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f))
                }
            };
            bar.RegisterCallback<PointerDownEvent>(e => HandleSegmentPointerDown(e, from, to, fromIndex, toIndex));
            lane.Add(bar);

            if (width >= 56f)
            {
                lane.Add(new Label(ShortEasingLabel(to.easing))
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        left = fromX + width * 0.5f - 38f,
                        top = 1,
                        width = 76,
                        height = 12,
                        fontSize = 8,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = new StyleColor(selected ? new Color(1f, 0.84f, 0.38f) : ResolveEasingLabelColor(to.easing))
                    }
                });
            }
        }

        private void AddPreFirstSegmentBar(VisualElement lane, IReadOnlyList<StoryActorKeyframeData> keyframes, int firstKeyIndex)
        {
            StoryActorKeyframeData firstKey = keyframes[firstKeyIndex];
            float firstKeyTime = StoryTransitionSampler.GetKeyTime(firstKey);
            if (firstKeyTime <= 0.001f) return;

            float fromX = 0f;
            float toX = firstKeyTime * _timelinePixelsPerSecond;
            float width = Mathf.Max(2f, toX - fromX);
            bool selected = _selectedTimelineSegmentKeyIndex == firstKeyIndex
                && _selectedTimelineSegmentProperty == firstKey.property;

            Color baseColor = ResolveEasingColor(firstKey.easing);
            Color segmentColor = Color.Lerp(baseColor, new Color(0.25f, 0.25f, 0.27f, 0.85f), 0.45f);

            var hitArea = new VisualElement
            {
                tooltip = $"{firstKey.property} segment [Line Start → {firstKey.easing}]",
                style =
                {
                    position = Position.Absolute,
                    left = fromX,
                    top = (TimelineRowHeight - TimelineSegmentHitHeight) * 0.5f,
                    width = width,
                    height = TimelineSegmentHitHeight,
                    backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.001f))
                }
            };
            hitArea.RegisterCallback<PointerDownEvent>(e =>
                HandleSegmentPointerDown(e, null, firstKey, -1, firstKeyIndex));
            lane.Add(hitArea);

            float visualHeight = selected ? TimelineSelectedSegmentHeight : TimelineSegmentHeight;
            var bar = new VisualElement
            {
                tooltip = $"{firstKey.property} segment [Line Start → {firstKey.easing}]",
                style =
                {
                    position = Position.Absolute,
                    left = fromX,
                    top = TimelineRowHeight * 0.5f - visualHeight * 0.5f,
                    width = width,
                    height = visualHeight,
                    backgroundColor = new StyleColor(selected
                        ? Color.Lerp(segmentColor, new Color(1f, 0.60f, 0.18f, 0.95f), 0.45f)
                        : segmentColor),
                    borderTopWidth = selected ? 1 : 0,
                    borderBottomWidth = selected ? 1 : 0,
                    borderTopColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f)),
                    borderBottomColor = new StyleColor(new Color(1f, 0.88f, 0.30f, 0.95f))
                }
            };
            bar.RegisterCallback<PointerDownEvent>(e =>
                HandleSegmentPointerDown(e, null, firstKey, -1, firstKeyIndex));
            lane.Add(bar);

            if (width >= 56f)
            {
                lane.Add(new Label(ShortEasingLabel(firstKey.easing))
                {
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position = Position.Absolute,
                        left = fromX + width * 0.5f - 38f,
                        top = 1,
                        width = 76,
                        height = 12,
                        fontSize = 8,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = new StyleColor(selected
                            ? new Color(1f, 0.84f, 0.38f)
                            : ResolveEasingLabelColor(firstKey.easing))
                    }
                });
            }
        }

        private void HandleSegmentPointerDown(
            PointerDownEvent e,
            StoryActorKeyframeData from,
            StoryActorKeyframeData to,
            int fromIndex,
            int toIndex)
        {
            if (e.button != 0 && e.button != 1)
                return;

            _timelinePanel?.Focus();
            SetInteractionContext(InteractionContext.Timeline);
            if (from != null && ShouldStartTimelineGroupDragFromSegment(e, from, to))
            {
                BeginTimelineGroupKeyDrag(e);
                e.StopPropagation();
                return;
            }

            SelectTimelineSegment(toIndex, to.property);
            _timelinePlayheadTime = from != null
                ? StoryTransitionSampler.GetKeyTime(from)
                : 0f;
            ApplyTimelinePlayheadSample();
            if (e.button == 1)
                ShowEasingMenu(toIndex, to.property);
            else
                RefreshTimelinePanel();
            RefreshActorInspector();
            e.StopPropagation();
        }

        private bool ShouldStartTimelineGroupDragFromSegment(
            PointerDownEvent e,
            StoryActorKeyframeData from,
            StoryActorKeyframeData to)
        {
            return e.button == 0
                && _selectedTimelineKeys.Count > 1
                && (from != null && _selectedTimelineKeys.Contains(from)
                    || to != null && _selectedTimelineKeys.Contains(to));
        }

        private Color ResolveKeyColor(StoryActorKeyframeProperty property) =>
            property switch
            {
                StoryActorKeyframeProperty.Position => new Color(0.38f, 0.62f, 1f),
                StoryActorKeyframeProperty.Scale => new Color(0.60f, 0.82f, 0.35f),
                StoryActorKeyframeProperty.Easing => new Color(0.82f, 0.56f, 1f),
                StoryActorKeyframeProperty.Expression => new Color(0.95f, 0.64f, 0.40f),
                StoryActorKeyframeProperty.BackgroundCut => new Color(0.92f, 0.48f, 0.58f),
                StoryActorKeyframeProperty.BackgroundPosition => new Color(0.35f, 0.74f, 0.86f),
                StoryActorKeyframeProperty.BackgroundScale => new Color(0.72f, 0.82f, 0.38f),
                StoryActorKeyframeProperty.CameraTarget => new Color(1f, 0.72f, 0.30f),
                StoryActorKeyframeProperty.CameraOffset => new Color(0.40f, 0.82f, 1f),
                StoryActorKeyframeProperty.CameraZoom => new Color(0.94f, 0.84f, 0.42f),
                StoryActorKeyframeProperty.SoundBgm => new Color(0.36f, 0.78f, 0.95f),
                StoryActorKeyframeProperty.SoundSfx => new Color(0.92f, 0.66f, 0.32f),
                _ => Color.white
            };

        private static Color ResolveEasingColor(StoryStageMoveMotionType easing) =>
            easing switch
            {
                StoryStageMoveMotionType.Instant => new Color(0.70f, 0.70f, 0.72f, 0.62f),
                StoryStageMoveMotionType.Linear => new Color(0.42f, 0.70f, 1f, 0.76f),
                StoryStageMoveMotionType.EaseIn => new Color(0.95f, 0.56f, 0.38f, 0.78f),
                StoryStageMoveMotionType.EaseOut => new Color(0.48f, 0.86f, 0.52f, 0.78f),
                StoryStageMoveMotionType.EaseInOut => new Color(0.82f, 0.64f, 1f, 0.80f),
                StoryStageMoveMotionType.SmoothStep => new Color(0.98f, 0.78f, 0.35f, 0.80f),
                StoryStageMoveMotionType.SmootherStep => new Color(0.42f, 0.88f, 0.82f, 0.80f),
                _ => new Color(0.74f, 0.74f, 0.78f, 0.55f)
            };

        private static Color ResolveEasingLabelColor(StoryStageMoveMotionType easing)
        {
            Color color = ResolveEasingColor(easing);
            return new Color(Mathf.Min(1f, color.r + 0.22f), Mathf.Min(1f, color.g + 0.22f), Mathf.Min(1f, color.b + 0.22f), 0.95f);
        }

        private static string ShortEasingLabel(StoryStageMoveMotionType easing) =>
            easing switch
            {
                StoryStageMoveMotionType.EaseInOut => "InOut",
                StoryStageMoveMotionType.SmoothStep => "Smooth",
                StoryStageMoveMotionType.SmootherStep => "Smoother",
                _ => easing.ToString()
            };

        private void AddTimelineTick(VisualElement lane, float time, bool withLabel)
        {
            float x = time * _timelinePixelsPerSecond;
            lane.Add(new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = x,
                    top = 0,
                    width = 1,
                    bottom = 0,
                    backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.24f))
                }
            });
            if (!withLabel) return;
            lane.Add(new Label($"{time:0.0}s")
            {
                style =
                {
                    position = Position.Absolute,
                    left = x + 3,
                    top = 3,
                    fontSize = 9,
                    color = new StyleColor(new Color(0.62f, 0.62f, 0.66f))
                }
            });
        }

        private void AddTimelinePlayhead(VisualElement lane, float duration, float height)
        {
            float x = Mathf.Clamp(_timelinePlayheadTime, 0f, duration) * _timelinePixelsPerSecond;
            lane.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = x,
                    top = 0,
                    width = 2,
                    height = height,
                    backgroundColor = new StyleColor(new Color(1f, 0.38f, 0.22f, 0.95f))
                }
            });
        }

        private IReadOnlyList<StoryActorKeyframeData> GetCurrentTimelineKeyframes()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
                return FindActorTrack(layout, _selectedActorKey)?.keyframes;
            if (_selectionKind == StageSelectionKind.Background)
                return layout?.BackgroundTrack?.keyframes;
            if (_selectionKind == StageSelectionKind.Camera)
                return layout?.CameraTrackEditable?.keyframes;
            if (_selectionKind == StageSelectionKind.Sound)
                return GetCurrentSoundTimelineKeyframes();
            return null;
        }

        private void SaveCurrentTimelineKeyframes(Action<List<StoryActorKeyframeData>> setter, bool refresh = false)
        {
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                SaveActorTrackToCurrent(_selectedActorKey, track => setter(track.keyframes), refresh);
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                SaveBackgroundTrackToCurrent(track => setter(track.keyframes), refresh);
                return;
            }

            if (_selectionKind == StageSelectionKind.Camera)
                SaveCameraTrackToCurrent(track => setter(track.keyframes), refresh);
        }

        private void SaveTimelineKeyframesFor(
            StageSelectionKind selectionKind,
            string actorKey,
            Action<List<StoryActorKeyframeData>> setter,
            bool refresh = false)
        {
            if (selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(actorKey))
            {
                SaveActorTrackToCurrent(actorKey, track => setter(track.keyframes), refresh);
                return;
            }

            if (selectionKind == StageSelectionKind.Background)
            {
                SaveBackgroundTrackToCurrent(track => setter(track.keyframes), refresh);
                return;
            }

            if (selectionKind == StageSelectionKind.Camera)
                SaveCameraTrackToCurrent(track => setter(track.keyframes), refresh);
        }

        private static System.Collections.Generic.List<int> GetOrderedKeyIndexes(IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property)
        {
            var indexes = new System.Collections.Generic.List<int>();
            if (keyframes == null)
                return indexes;

            for (int i = 0; i < keyframes.Count; i++)
            {
                if (keyframes[i] != null && keyframes[i].property == property)
                    indexes.Add(i);
            }

            indexes.Sort((a, b) => StoryTransitionSampler.GetKeyTime(keyframes[a]).CompareTo(StoryTransitionSampler.GetKeyTime(keyframes[b])));
            return indexes;
        }

        private bool IsTimelineKeySelected(StoryActorKeyframeData key, int index, StoryActorKeyframeProperty property)
        {
            return key != null && _selectedTimelineKeys.Contains(key)
                || (index == _selectedTimelineKeyIndex && property == _selectedTimelineProperty);
        }

        private void SelectSingleTimelineKey(StoryActorKeyframeData key, int index, StoryActorKeyframeProperty property)
        {
            _selectedTimelineKeys.Clear();
            if (key != null)
                _selectedTimelineKeys.Add(key);
            _selectedTimelineKeyIndex = index;
            _selectedTimelineProperty = property;
            _selectedTimelineSegmentKeyIndex = -1;
        }

        private void ToggleTimelineKeySelection(StoryActorKeyframeData key, int index, StoryActorKeyframeProperty property)
        {
            if (key == null)
                return;

            _selectedTimelineSegmentKeyIndex = -1;
            if (_selectedTimelineKeys.Contains(key))
                _selectedTimelineKeys.Remove(key);
            else
                _selectedTimelineKeys.Add(key);

            NormalizeTimelineSelectionFromSet(property);
        }

        private void NormalizeTimelineSelectionFromSet(StoryActorKeyframeProperty fallbackProperty)
        {
            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            ValidateTimelineSelectionSet(keyframes);

            if (_selectedTimelineKeys.Count == 1)
            {
                foreach (StoryActorKeyframeData key in _selectedTimelineKeys)
                {
                    _selectedTimelineKeyIndex = keyframes != null ? IndexOfTimelineKey(keyframes, key) : -1;
                    _selectedTimelineProperty = key != null ? key.property : fallbackProperty;
                    break;
                }
            }
            else
            {
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineProperty = fallbackProperty;
            }

            _selectedTimelineSegmentKeyIndex = -1;
        }

        private void ValidateTimelineSelectionSet(IReadOnlyList<StoryActorKeyframeData> keyframes)
        {
            if (_selectedTimelineKeys.Count == 0)
                return;

            if (keyframes == null)
            {
                _selectedTimelineKeys.Clear();
                return;
            }

            _selectedTimelineKeys.RemoveWhere(key =>
                key == null
                || !ContainsTimelineKey(keyframes, key)
                || !IsTimelineKeyProperty(key.property)
                || !IsPropertyAllowedForCurrentSelection(key.property));
        }

        private bool HasTimelineMultiSelection => _selectedTimelineKeys.Count > 1;

        private List<StoryActorKeyframeData> GetSelectedTimelineKeys(IReadOnlyList<StoryActorKeyframeData> keyframes)
        {
            var result = new List<StoryActorKeyframeData>();
            if (keyframes == null)
                return result;

            ValidateTimelineSelectionSet(keyframes);
            foreach (StoryActorKeyframeData key in keyframes)
            {
                if (key != null && _selectedTimelineKeys.Contains(key))
                    result.Add(key);
            }

            if (result.Count == 0
                && _selectedTimelineKeyIndex >= 0
                && _selectedTimelineKeyIndex < keyframes.Count
                && keyframes[_selectedTimelineKeyIndex] != null)
            {
                result.Add(keyframes[_selectedTimelineKeyIndex]);
            }

            return result;
        }

        private StoryActorKeyframeData GetPrimarySelectedTimelineKey(IReadOnlyList<StoryActorKeyframeData> keyframes)
        {
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(keyframes);
            if (selected.Count > 0)
                return selected[0];

            if (keyframes != null
                && _selectedTimelineKeyIndex >= 0
                && _selectedTimelineKeyIndex < keyframes.Count)
                return keyframes[_selectedTimelineKeyIndex];

            return null;
        }

        private static int IndexOfTimelineKey(IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeData key)
        {
            if (keyframes == null || key == null)
                return -1;

            for (int i = 0; i < keyframes.Count; i++)
                if (ReferenceEquals(keyframes[i], key))
                    return i;

            return -1;
        }

        private static bool ContainsTimelineKey(IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeData key) =>
            IndexOfTimelineKey(keyframes, key) >= 0;

        private void AddTimelineEmpty(string message)
        {
            var empty = new Label(message)
            {
                style =
                {
                    paddingLeft = 8,
                    paddingTop = 8,
                    minHeight = 76,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            empty.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                ClearTimelineSelection(refresh: true);
                e.StopPropagation();
            });
            _timelineRows.Add(empty);
        }

        private float ResolveTimelineTickSeconds()
        {
            if (_timelinePixelsPerSecond >= 220f) return 0.25f;
            if (_timelinePixelsPerSecond <= 70f) return 1f;
            return 0.5f;
        }

        private float GetTimelineDuration()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout == null) return 1f;
            float d = 0f;
            foreach (StoryActorTrackData track in layout.ActorTracks)
                if (track != null) d = Mathf.Max(d, StoryTransitionSampler.GetActorTrackDuration(track));
            d = Mathf.Max(d, StoryTransitionSampler.GetBackgroundTrackDuration(layout.BackgroundTrack));
            d = Mathf.Max(d, StoryTransitionSampler.GetCameraTrackDuration(layout.CameraTrackEditable));
            d = Mathf.Max(d, GetSoundTrackDuration(layout));
            return Mathf.Max(1f, d);
        }

        private float ResolveTimelineTimeFromLaneX(float localX) =>
            Mathf.Max(0f, localX / Mathf.Max(1f, _timelinePixelsPerSecond));

        private void SetTimelinePlayheadFromLocalX(float localX)
        {
            _timelinePlayheadTime = ApplySnap(ResolveTimelineTimeFromLaneX(localX));
            ApplyTimelinePlayheadSample();
            RefreshTimelinePanel();
        }

        private void BeginTimelineBoxSelection(PointerDownEvent e)
        {
            if (!(e.ctrlKey || e.commandKey))
                ClearTimelineSelection(refresh: false);

            _isTimelineBoxSelecting = true;
            _activeTimelinePointerId = e.pointerId;
            _timelineBoxSelectStart = e.position;
            _timelinePanel?.CapturePointer(e.pointerId);

            _timelineSelectionBox?.RemoveFromHierarchy();
            _timelineSelectionBox = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new StyleColor(new Color(0.30f, 0.52f, 1f, 0.18f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.45f, 0.68f, 1f, 0.72f)),
                    borderRightColor = new StyleColor(new Color(0.45f, 0.68f, 1f, 0.72f)),
                    borderBottomColor = new StyleColor(new Color(0.45f, 0.68f, 1f, 0.72f)),
                    borderLeftColor = new StyleColor(new Color(0.45f, 0.68f, 1f, 0.72f))
                }
            };
            _timelinePanel?.Add(_timelineSelectionBox);
            UpdateTimelineBoxSelection(e.position);
        }

        private void UpdateTimelineBoxSelection(Vector2 worldPosition)
        {
            if (_timelineSelectionBox == null || _timelinePanel == null)
                return;

            Rect worldRect = MakeRect(_timelineBoxSelectStart, worldPosition);
            Vector2 min = _timelinePanel.WorldToLocal(worldRect.min);
            Vector2 max = _timelinePanel.WorldToLocal(worldRect.max);
            Rect localRect = MakeRect(min, max);
            _timelineSelectionBox.style.left = localRect.xMin;
            _timelineSelectionBox.style.top = localRect.yMin;
            _timelineSelectionBox.style.width = localRect.width;
            _timelineSelectionBox.style.height = localRect.height;
        }

        private void FinishTimelineBoxSelection(Vector2 worldPosition)
        {
            Rect worldRect = MakeRect(_timelineBoxSelectStart, worldPosition);
            bool wasDrag = worldRect.width > 4f || worldRect.height > 4f;
            if (wasDrag)
            {
                SelectTimelineKeysInWorldRect(worldRect);
            }
            else
            {
                NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
            }

            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SelectTimelineKeysInWorldRect(Rect worldRect)
        {
            foreach (var pair in _timelineKeyElements)
            {
                StoryActorKeyframeData key = pair.Key;
                VisualElement marker = pair.Value;
                if (key == null || marker == null)
                    continue;

                if (worldRect.Overlaps(marker.worldBound))
                    _selectedTimelineKeys.Add(key);
            }

            NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
        }

        private static Rect MakeRect(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float xMax = Mathf.Max(a.x, b.x);
            float yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void BeginTimelineDragUndo(string undoName)
        {
            if (_timelineDragUndoActive)
                return;

            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout == null)
                return;

            RecordPreviewUndo(layout, InteractionContext.Timeline, undoName);
            _suppressTimelineUndoRecording = true;
            _timelineDragUndoActive = true;
        }

        private void EndTimelineDragUndo()
        {
            _suppressTimelineUndoRecording = false;
            _timelineDragUndoActive = false;
        }

        private void BeginTimelineGroupKeyDrag(PointerDownEvent e)
        {
            _timelineKeyDragStates.Clear();
            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null)
                return;

            ValidateTimelineSelectionSet(keyframes);
            foreach (StoryActorKeyframeData key in _selectedTimelineKeys)
            {
                _timelineKeyDragStates.Add(new TimelineKeyDragState
                {
                    key = key,
                    startTime = StoryTransitionSampler.GetKeyTime(key)
                });
            }

            if (_timelineKeyDragStates.Count == 0)
                return;

            _isTimelineGroupKeyDragging = true;
            _draggingTimelineActorKey = _selectedActorKey;
            _draggingTimelineSelectionKind = _selectionKind;
            _activeTimelinePointerId = e.pointerId;
            _timelineKeyDragStartPanelX = e.position.x;
            BeginTimelineDragUndo("Move Timeline Keys");
            _timelinePanel?.CapturePointer(e.pointerId);
        }

        private void MoveTimelineGroupKeys(float currentPanelX)
        {
            if (_draggingTimelineSelectionKind == StageSelectionKind.Sound)
            {
                MoveSoundTimelineGroupKeys(currentPanelX);
                return;
            }

            float deltaTime = (currentPanelX - _timelineKeyDragStartPanelX) / Mathf.Max(1f, _timelinePixelsPerSecond);
            SaveTimelineKeyframesFor(_draggingTimelineSelectionKind, _draggingTimelineActorKey, keyframes =>
            {
                var proposedTimes = new Dictionary<StoryActorKeyframeData, float>();
                foreach (TimelineKeyDragState state in _timelineKeyDragStates)
                    proposedTimes[state.key] = ApplySnap(Mathf.Max(0f, state.startTime + deltaTime));

                bool hasCollision = HasTimelineKeyCollision(keyframes, proposedTimes);
                foreach (TimelineKeyDragState state in _timelineKeyDragStates)
                {
                    int index = keyframes.IndexOf(state.key);
                    if (index < 0)
                        continue;

                    StoryActorKeyframeData key = keyframes[index];
                    key.timeSeconds = hasCollision
                        ? state.startTime
                        : proposedTimes[state.key];
                    key.normalizedTime = Mathf.Clamp01(key.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                }
            }, refresh: false);
            NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
            RefreshTimelinePanel();
        }

        private void OnTimelinePointerMove(PointerMoveEvent e)
        {
            if (e.pointerId != _activeTimelinePointerId)
                return;

            if (_isTimelineBoxSelecting)
            {
                UpdateTimelineBoxSelection(e.position);
                e.StopPropagation();
                return;
            }

            if (_isTimelinePlayheadDragging)
            {
                SetTimelinePlayheadFromLocalX(e.localPosition.x - TimelinePropertyWidth);
                e.StopPropagation();
                return;
            }

            if (_isTimelineGroupKeyDragging)
            {
                MoveTimelineGroupKeys(e.position.x);
                e.StopPropagation();
                return;
            }

            if (_isTimelineKeyDragging)
            {
                float time = ApplySnap(Mathf.Max(0f, (e.localPosition.x - TimelinePropertyWidth) / Mathf.Max(1f, _timelinePixelsPerSecond)));
                SetTimelineKeyTime(_draggingTimelineSelectionKind, _draggingTimelineActorKey, _draggingTimelineKeyIndex, time, refresh: false);
                _draggingTimelineKeyIndex = _selectedTimelineKeyIndex;
                _timelinePlayheadTime = time;
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
                e.StopPropagation();
            }
        }

        private void OnTimelinePointerUp(PointerUpEvent e)
        {
            if (e.pointerId != _activeTimelinePointerId)
                return;

            if (_isTimelineBoxSelecting)
                FinishTimelineBoxSelection(e.position);

            bool wasKeyDragging = _isTimelineKeyDragging || _isTimelineGroupKeyDragging;
            CancelTimelinePointerDrag();
            if (wasKeyDragging)
                RefreshActorInspector();

            e.StopPropagation();
        }

        private void OnTimelineKeyDown(KeyDownEvent e)
        {
            // Keyboard shortcut ownership is centralized in OnGUI.
        }

        private void OnTimelinePanelPointerDown(PointerDownEvent e)
        {
            if (!IsStageAuthoringMode || e.button != 0 || _activeTimelinePointerId >= 0)
                return;

            if (!IsTimelineNeutralClickTarget(e.target as VisualElement))
                return;

            SetInteractionContext(InteractionContext.Timeline);
            _timelinePanel?.Focus();
            ClearTimelineSelection(refresh: true);
            e.StopPropagation();
        }

        private bool IsTimelineNeutralClickTarget(VisualElement target)
        {
            if (target == null)
                return false;

            if (IsDescendantOf(target, _timelineToolbar))
                return false;

            for (VisualElement current = target; current != null; current = current.parent)
            {
                if (current is Button || current is BaseField<float> || current is BaseField<int> || current is BaseField<string>)
                    return false;
            }

            return target == _timelinePanel
                || target == _timelineRows
                || target == _timelineRuler
                || target is Label;
        }

        private static bool IsDescendantOf(VisualElement element, VisualElement ancestor)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current == ancestor)
                    return true;
            }

            return false;
        }

        private bool IsTimelinePanelFocused()
        {
            if (_timelinePanel?.panel?.focusController?.focusedElement is VisualElement focusedElement)
                return IsDescendantOf(focusedElement, _timelinePanel);

            return false;
        }

        private void CancelTimelinePointerDrag()
        {
            _isTimelinePlayheadDragging = false;
            _isTimelineKeyDragging = false;
            _isTimelineGroupKeyDragging = false;
            _isTimelineBoxSelecting = false;
            _draggingTimelineActorKey = null;
            _draggingTimelineSelectionKind = StageSelectionKind.None;
            _draggingTimelineKeyIndex = -1;
            _timelineKeyDragStates.Clear();

            if (_timelineSelectionBox != null)
            {
                _timelineSelectionBox.RemoveFromHierarchy();
                _timelineSelectionBox = null;
            }

            if (_timelinePanel != null && _activeTimelinePointerId >= 0 && _timelinePanel.HasPointerCapture(_activeTimelinePointerId))
                _timelinePanel.ReleasePointer(_activeTimelinePointerId);

            _activeTimelinePointerId = -1;
            EndTimelineDragUndo();
        }

        private void ClearTimelineSelection(bool refresh)
        {
            _selectedTimelineKeyIndex = -1;
            _selectedTimelineSegmentKeyIndex = -1;
            _selectedTimelineKeys.Clear();
            if (!refresh)
                return;

            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SelectTimelineSegment(int keyIndex, StoryActorKeyframeProperty property)
        {
            _selectedTimelineKeys.Clear();
            _selectedTimelineKeyIndex = -1;
            _selectedTimelineSegmentKeyIndex = keyIndex;
            _selectedTimelineSegmentProperty = property;
            _selectedTimelineProperty = property;
        }

        private void OnTimelineWheel(WheelEvent e)
        {
            if (!e.ctrlKey) return;
            SetInteractionContext(InteractionContext.Timeline);
            float delta = -e.delta.y * 8f;
            _timelinePixelsPerSecond = Mathf.Clamp(_timelinePixelsPerSecond + delta, MinTimelinePixelsPerSecond, MaxTimelinePixelsPerSecond);
            SavePreviewLayoutPrefs();
            RefreshTimelinePanel();
            e.StopPropagation();
        }

        private void ToggleTimelineRecord()
        {
            if (!_timelineRecordEnabled)
            {
                if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
                {
                    _timelineRecordActorKey = _selectedActorKey;
                    _timelineRecordSelectionKind = StageSelectionKind.Actor;
                    _timelineRecordEnabled = true;
                }
                else if (_selectionKind == StageSelectionKind.Background)
                {
                    _timelineRecordActorKey = null;
                    _timelineRecordSelectionKind = StageSelectionKind.Background;
                    _timelineRecordEnabled = true;
                }
                else if (_selectionKind == StageSelectionKind.Camera)
                {
                    _timelineRecordActorKey = null;
                    _timelineRecordSelectionKind = StageSelectionKind.Camera;
                    _timelineRecordEnabled = true;
                }
                else if (_selectionKind == StageSelectionKind.Sound)
                {
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                _timelineRecordEnabled = false;
                _timelineRecordActorKey = null;
                _timelineRecordSelectionKind = StageSelectionKind.None;
            }
            RefreshTimelinePanel();
        }

        private void StopTimelinePlayback()
        {
            if (!_timelineIsPlaying) return;
            _timelineIsPlaying = false;
            RefreshTimelinePanel();
        }

        private void ToggleTimelinePlayback()
        {
            if (_timelineIsPlaying)
            {
                _timelineIsPlaying = false;
                RefreshTimelinePanel();
                return;
            }

            float duration = GetTimelineDuration();
            if (_timelinePlayheadTime >= duration - 0.001f)
            {
                _timelinePlayheadTime = GetTimelineRestartTime();
                ApplyTimelinePlayheadSample();
            }

            _timelinePlaybackStartTime = _timelinePlayheadTime;
            _timelinePlaybackStartedAt = EditorApplication.timeSinceStartup;
            _timelineIsPlaying = true;
            RefreshTimelinePanel();
        }

        private float GetTimelineRestartTime() => 0f;

        private void UpdateTimelinePlayback()
        {
            if (!_timelineIsPlaying || !IsStageAuthoringMode) return;
            float previousTime = _timelinePlayheadTime;
            float elapsed = (float)(EditorApplication.timeSinceStartup - _timelinePlaybackStartedAt) * Mathf.Max(0.1f, _timelinePlaybackSpeed);
            _timelinePlayheadTime = _timelinePlaybackStartTime + elapsed;
            float duration = GetTimelineDuration();
            if (_timelinePlayheadTime >= duration)
            {
                _timelinePlayheadTime = duration;
                _timelineIsPlaying = false;
            }
            ApplyTimelinePlayheadSample();
            RefreshTimelinePanel();
        }

        private void ApplyTimelinePlayheadSample()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();

            // Sample all actor tracks in the layout (not just the selected one)
            if (layout != null)
            {
                foreach (StoryActorTrackData track in layout.ActorTracks)
                {
                    if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey)) continue;
                    if (!TryResolveTimelineBaseActorState(track.actorInstanceKey, out StoryActorStateData baseState)) continue;
                    StoryActorStateData sample = StoryTransitionSampler.SampleActorTrackAtTime(baseState, track, _timelinePlayheadTime);
                    if (sample != null)
                        _stageState[track.actorInstanceKey] = sample;
                }
            }

            // Sample background track (use layout base to avoid accumulation from prior samples)
            StoryBackgroundStateData baseBackground = layout?.Background ?? _bgState;
            if (baseBackground != null)
            {
                StoryBackgroundStateData bgSample = StoryTransitionSampler.SampleBackgroundTrackAtTime(
                    baseBackground.ShallowClone(), layout?.BackgroundTrack, _timelinePlayheadTime);
                if (bgSample != null)
                    _bgState = bgSample;
            }

            StoryCameraStateData cameraSample = StoryTransitionSampler.SampleCameraTrackAtTime(
                layout?.CameraTrackEditable,
                "",
                _timelinePlayheadTime);
            SetPreviewCameraSampleState(cameraSample);

            // Update all stage visuals: actors, background, camera gizmo
            UpdateActorLayerPositions();
            UpdateCameraGizmoVisual(cameraSample);
            RefreshFocusPreviewGuide();
            Repaint();
        }

        private bool TryResolveTimelineBaseActorState(string actorKey, out StoryActorStateData baseState)
        {
            baseState = null;
            if (string.IsNullOrWhiteSpace(actorKey))
                return false;

            StoryActorStateData currentLineEntry = FindActorEntry(FindCurrentStageLayout(), actorKey);
            if (currentLineEntry != null)
            {
                baseState = currentLineEntry.ShallowClone();
                return true;
            }

            if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentState))
            {
                baseState = currentState.ShallowClone();
                return true;
            }

            return false;
        }

        private void ShowAddPropertyMenu()
        {
            var menu = new GenericMenu();
            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.Position, "Position");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.Scale, "Scale");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.Expression, "Expression");
                menu.ShowAsContext();
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.BackgroundCut, "Cut");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.BackgroundPosition, "Position");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.BackgroundScale, "Scale");
                menu.ShowAsContext();
                return;
            }

            if (_selectionKind == StageSelectionKind.Camera)
            {
                IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.CameraTarget, "Camera Target");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.CameraOffset, "Camera Position");
                AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.CameraZoom, "Camera Zoom");
                menu.ShowAsContext();
                return;
            }

            if (_selectionKind == StageSelectionKind.Sound)
            {
                IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
                if (HasEnumOptions<BgmSounds>())
                    AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.SoundBgm, "BGM");
                else
                    menu.AddDisabledItem(new GUIContent("BGM (No BgmSounds enum value)"));

                if (HasEnumOptions<SfxSounds>())
                    AddPropertyMenuItem(menu, keyframes, StoryActorKeyframeProperty.SoundSfx, "SFX");
                else
                    menu.AddDisabledItem(new GUIContent("SFX (No SfxSounds enum value)"));

                menu.ShowAsContext();
                return;
            }

            menu.AddDisabledItem(new GUIContent("Select an actor, background, camera, or sound"));
            menu.ShowAsContext();
        }

        private void ShowRowKeyContextMenu(StoryActorKeyframeProperty property, float time)
        {
            if (IsSoundTimelineProperty(property))
            {
                ShowSoundRowKeyContextMenu(property, time);
                return;
            }

            var menu = new GenericMenu();
            if (!TryGetCurrentTimelineSourceState(property, out var actorState, out var backgroundState, out var cameraState))
            {
                menu.AddDisabledItem(new GUIContent($"Add {property} Key"));
                menu.ShowAsContext();
                return;
            }

            string label = $"Add/Update {property} Key @ {time:0.00}s";
            menu.AddItem(new GUIContent(label), false, () =>
            {
                _selectedTimelineProperty = property;
                _timelinePlayheadTime = time;
                if (IsActorTimelineProperty(property))
                    AddOrUpdateKey(_selectedActorKey, actorState, property, time, createIfMissing: true);
                else if (IsCameraTimelineProperty(property))
                    AddOrUpdateCameraKey(cameraState, property, time, createIfMissing: true);
                else
                    AddOrUpdateBackgroundKey(backgroundState, property, time, createIfMissing: true);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
            });
            menu.ShowAsContext();
        }

        private bool TryGetCurrentTimelineSourceState(
            StoryActorKeyframeProperty property,
            out StoryActorStateData actorState,
            out StoryBackgroundStateData backgroundState,
            out StoryCameraStateData cameraState)
        {
            actorState = null;
            backgroundState = null;
            cameraState = null;

            if (IsActorTimelineProperty(property)
                && _selectionKind == StageSelectionKind.Actor
                && !string.IsNullOrWhiteSpace(_selectedActorKey)
                && _stageState.TryGetValue(_selectedActorKey, out actorState))
                return true;

            if (IsBackgroundTimelineProperty(property) && _selectionKind == StageSelectionKind.Background)
            {
                backgroundState = _bgState?.ShallowClone() ?? FindCurrentStageLayout()?.Background?.ShallowClone();
                return backgroundState != null;
            }

            if (IsCameraTimelineProperty(property) && _selectionKind == StageSelectionKind.Camera)
            {
                StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
                if (layout == null)
                    return false;

                StoryCameraTrackData track = layout.CameraTrackEditable;
                track.defaultState ??= new StoryCameraStateData();
                cameraState = track.defaultState.ShallowClone();
                return true;
            }

            return false;
        }

        private void AddPropertyMenuItem(GenericMenu menu, IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property, string label)
        {
            if (HasProperty(keyframes, property))
            {
                menu.AddDisabledItem(new GUIContent(label));
                return;
            }
            menu.AddItem(new GUIContent(label), false, () => AddPropertyKey(property));
        }

        private void AddPropertyKey(StoryActorKeyframeProperty property)
        {
            _selectedTimelineProperty = property;
            AddTimelineKeyAtPlayhead();
        }

        private static bool HasProperty(StoryActorTrackData track, StoryActorKeyframeProperty property)
        {
            return HasProperty(track?.keyframes, property);
        }

        private static bool HasProperty(IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property)
        {
            if (keyframes == null) return false;
            foreach (StoryActorKeyframeData key in keyframes)
                if (key != null && key.property == property) return true;
            return false;
        }

        private void AddTimelineKeyAtPlayhead()
        {
            StoryActorKeyframeProperty property = _selectedTimelineProperty == StoryActorKeyframeProperty.Easing
                ? ResolveDefaultPropertyForSelection()
                : _selectedTimelineProperty;
            if (!IsPropertyAllowedForCurrentSelection(property))
                property = ResolveDefaultPropertyForSelection();

            if (IsSoundTimelineProperty(property))
            {
                if (property == StoryActorKeyframeProperty.SoundBgm && !HasEnumOptions<BgmSounds>())
                    return;
                if (property == StoryActorKeyframeProperty.SoundSfx && !HasEnumOptions<SfxSounds>())
                    return;

                AddSoundKeyAtPlayhead(
                    property == StoryActorKeyframeProperty.SoundBgm ? SoundTimelineRowKind.Bgm : SoundTimelineRowKind.Sfx,
                    StoryBgmKeyOperation.Play);
                RefreshTimelinePanel();
                return;
            }

            if (!TryGetCurrentTimelineSourceState(property, out var actorState, out var backgroundState, out var cameraState))
                return;

            if (IsActorTimelineProperty(property))
                AddOrUpdateKey(_selectedActorKey, actorState, property, _timelinePlayheadTime, createIfMissing: true);
            else if (IsCameraTimelineProperty(property))
                AddOrUpdateCameraKey(cameraState, property, _timelinePlayheadTime, createIfMissing: true);
            else
                AddOrUpdateBackgroundKey(backgroundState, property, _timelinePlayheadTime, createIfMissing: true);

            RefreshTimelinePanel();
        }

        internal bool ApplyMotionPresetToCurrentTrack(
            StoryMotionPresetSO preset,
            StoryActorKeyframeProperty? propertyOverride = null,
            float? timeOverride = null)
        {
            if (preset == null) return false;

            float offset = Mathf.Max(0f, timeOverride ?? _timelinePlayheadTime);
            var firstKeys = GetFirstPresetKeyPerProperty(preset.keyframes);

            if (_selectionKind == StageSelectionKind.Actor)
            {
                if (string.IsNullOrWhiteSpace(_selectedActorKey)) return false;

                StoryActorKeyframeProperty property = propertyOverride ?? preset.property;
                if (propertyOverride.HasValue && !CanApplyMotionPresetToProperty(preset, property))
                    return false;

                _stageState.TryGetValue(_selectedActorKey, out StoryActorStateData curActor);
                Vector2 posDelta   = Vector2.zero;
                Vector2 scaleDelta = Vector2.zero;
                if (curActor != null)
                {
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.Position, out var fPos))
                        posDelta = curActor.stageLocalPosition - fPos.stageLocalPosition;
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.Scale, out var fScale))
                    {
                        float sm = curActor.scaleMultiplier > 0f ? curActor.scaleMultiplier : 1f;
                        scaleDelta = new Vector2(sm, sm) - fScale.scale;
                    }
                }

                bool appliedAny = false;
                SaveActorTrackToCurrent(_selectedActorKey, track =>
                {
                    _selectedTimelineKeys.Clear();
                    foreach (StoryActorKeyframeData sourceKey in preset.keyframes)
                    {
                        if (sourceKey == null || !IsMotionPresetProperty(sourceKey.property) || !IsActorTimelineProperty(sourceKey.property))
                            continue;
                        if (propertyOverride.HasValue && sourceKey.property != property)
                            continue;

                        StoryActorKeyframeData clone = sourceKey.ShallowClone();
                        clone.property = sourceKey.property;
                        clone.timeSeconds = offset + StoryTransitionSampler.GetKeyTime(sourceKey);
                        clone.normalizedTime = Mathf.Clamp01(clone.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                        if (clone.property == StoryActorKeyframeProperty.Position)
                            clone.stageLocalPosition += posDelta;
                        else if (clone.property == StoryActorKeyframeProperty.Scale)
                            clone.scale += scaleDelta;

                        int existing = FindKeyIndexNearTime(track, clone.property, clone.timeSeconds);
                        if (existing >= 0)
                            track.keyframes[existing] = clone;
                        else
                            track.keyframes.Add(clone);

                        _selectedTimelineKeys.Add(clone);
                        appliedAny = true;
                    }

                    _selectedTimelineProperty = property;
                    _selectedTimelineKeyIndex = -1;
                    _selectedTimelineSegmentKeyIndex = -1;
                }, refresh: false);

                if (!appliedAny) return false;
                NormalizeTimelineSelectionFromSet(property);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                return true;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                Vector2 bgPosDelta   = Vector2.zero;
                Vector2 bgScaleDelta = Vector2.zero;
                if (_bgState != null)
                {
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.BackgroundPosition, out var fBgPos))
                        bgPosDelta = _bgState.stageLocalPosition - fBgPos.stageLocalPosition;
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.BackgroundScale, out var fBgScale))
                    {
                        float sm = _bgState.scaleMultiplier > 0f ? _bgState.scaleMultiplier : 1f;
                        bgScaleDelta = new Vector2(sm, sm) - fBgScale.scale;
                    }
                }

                bool appliedAny = false;
                StoryActorKeyframeProperty primaryProperty = StoryActorKeyframeProperty.BackgroundPosition;
                SaveBackgroundTrackToCurrent(track =>
                {
                    _selectedTimelineKeys.Clear();
                    foreach (StoryActorKeyframeData sourceKey in preset.keyframes)
                    {
                        if (sourceKey == null || !IsMotionPresetProperty(sourceKey.property) || !IsBackgroundTimelineProperty(sourceKey.property))
                            continue;
                        if (propertyOverride.HasValue && sourceKey.property != propertyOverride.Value)
                            continue;

                        StoryActorKeyframeData clone = sourceKey.ShallowClone();
                        clone.property = sourceKey.property;
                        clone.timeSeconds = offset + StoryTransitionSampler.GetKeyTime(sourceKey);
                        clone.normalizedTime = Mathf.Clamp01(clone.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                        if (clone.property == StoryActorKeyframeProperty.BackgroundPosition)
                            clone.stageLocalPosition += bgPosDelta;
                        else if (clone.property == StoryActorKeyframeProperty.BackgroundScale)
                            clone.scale += bgScaleDelta;

                        int existing = FindKeyIndexNearTime(track.keyframes, clone.property, clone.timeSeconds);
                        if (existing >= 0)
                            track.keyframes[existing] = clone;
                        else
                            track.keyframes.Add(clone);

                        _selectedTimelineKeys.Add(clone);
                        if (!appliedAny) primaryProperty = clone.property;
                        appliedAny = true;
                    }

                    _selectedTimelineProperty = primaryProperty;
                    _selectedTimelineKeyIndex = -1;
                    _selectedTimelineSegmentKeyIndex = -1;
                }, refresh: false);

                if (!appliedAny) return false;
                NormalizeTimelineSelectionFromSet(primaryProperty);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                return true;
            }

            if (_selectionKind == StageSelectionKind.Camera)
            {
                Vector2 camOffDelta  = Vector2.zero;
                float   camZoomDelta = 0f;
                StoryCameraStateData curCam = ResolveCurrentPreviewCameraState();
                if (curCam != null)
                {
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.CameraOffset, out var fCamOff))
                        camOffDelta = curCam.stageLocalPosition - fCamOff.cameraStageLocalPosition;
                    if (firstKeys.TryGetValue(StoryActorKeyframeProperty.CameraZoom, out var fCamZoom))
                        camZoomDelta = curCam.zoom - fCamZoom.cameraZoom;
                }

                bool appliedAny = false;
                StoryActorKeyframeProperty primaryProperty = StoryActorKeyframeProperty.CameraOffset;
                SaveCameraTrackToCurrent(track =>
                {
                    _selectedTimelineKeys.Clear();
                    foreach (StoryActorKeyframeData sourceKey in preset.keyframes)
                    {
                        if (sourceKey == null || !IsMotionPresetProperty(sourceKey.property) || !IsCameraTimelineProperty(sourceKey.property))
                            continue;
                        if (propertyOverride.HasValue && sourceKey.property != propertyOverride.Value)
                            continue;

                        StoryActorKeyframeData clone = sourceKey.ShallowClone();
                        clone.property = sourceKey.property;
                        clone.timeSeconds = offset + StoryTransitionSampler.GetKeyTime(sourceKey);
                        clone.normalizedTime = Mathf.Clamp01(clone.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                        if (clone.property == StoryActorKeyframeProperty.CameraOffset)
                            clone.cameraStageLocalPosition += camOffDelta;
                        else if (clone.property == StoryActorKeyframeProperty.CameraZoom)
                            clone.cameraZoom = Mathf.Max(0.01f, clone.cameraZoom + camZoomDelta);

                        int existing = FindKeyIndexNearTime(track.keyframes, clone.property, clone.timeSeconds);
                        if (existing >= 0)
                            track.keyframes[existing] = clone;
                        else
                            track.keyframes.Add(clone);

                        _selectedTimelineKeys.Add(clone);
                        if (!appliedAny) primaryProperty = clone.property;
                        appliedAny = true;
                    }

                    _selectedTimelineProperty = primaryProperty;
                    _selectedTimelineKeyIndex = -1;
                    _selectedTimelineSegmentKeyIndex = -1;
                }, refresh: false);

                if (!appliedAny) return false;
                NormalizeTimelineSelectionFromSet(primaryProperty);
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                return true;
            }

            return false;
        }

        private static Dictionary<StoryActorKeyframeProperty, StoryActorKeyframeData> GetFirstPresetKeyPerProperty(
            IEnumerable<StoryActorKeyframeData> keyframes)
        {
            var result = new Dictionary<StoryActorKeyframeProperty, StoryActorKeyframeData>();
            if (keyframes == null) return result;
            foreach (StoryActorKeyframeData k in keyframes)
            {
                if (k == null) continue;
                if (!result.TryGetValue(k.property, out StoryActorKeyframeData existing)
                    || StoryTransitionSampler.GetKeyTime(k) < StoryTransitionSampler.GetKeyTime(existing))
                    result[k.property] = k;
            }
            return result;
        }

        internal bool SaveSelectedTimelineKeysAsMotionPreset()
        {
            IReadOnlyList<StoryActorKeyframeData> sourceKeyframes;
            string defaultNameSuffix;

            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                sourceKeyframes = FindActorTrack(FindCurrentStageLayout(), _selectedActorKey)?.keyframes;
                defaultNameSuffix = SanitizePresetAssetName(_selectedActorKey);
            }
            else if (_selectionKind == StageSelectionKind.Background)
            {
                sourceKeyframes = FindCurrentStageLayout()?.BackgroundTrack?.keyframes;
                defaultNameSuffix = "Background";
            }
            else if (_selectionKind == StageSelectionKind.Camera)
            {
                sourceKeyframes = FindCurrentStageLayout()?.CameraTrackEditable?.keyframes;
                defaultNameSuffix = "Camera";
            }
            else
            {
                return false;
            }

            List<StoryActorKeyframeData> selectedKeys = GetSelectedTimelineKeys(sourceKeyframes);
            selectedKeys.RemoveAll(key => key == null || !IsMotionPresetProperty(key.property));
            if (selectedKeys.Count == 0)
            {
                EditorUtility.DisplayDialog("Save Motion Preset", "Select one or more preset-eligible timeline keys first.", "OK");
                return false;
            }

            EnsureMotionPresetFolder();
            selectedKeys.Sort((a, b) =>
            {
                int timeCompare = StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
                return timeCompare != 0 ? timeCompare : a.property.CompareTo(b.property);
            });

            StoryActorKeyframeProperty primaryProperty = selectedKeys[0].property;
            string defaultName = $"MotionPreset_{defaultNameSuffix}_Selection";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Motion Preset",
                defaultName,
                "asset",
                "Choose where to save the motion preset asset.",
                MotionPresetFolder);
            if (string.IsNullOrWhiteSpace(path))
                return false;

            StoryMotionPresetSO preset = AssetDatabase.LoadAssetAtPath<StoryMotionPresetSO>(path);
            if (preset == null)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                {
                    EditorUtility.DisplayDialog("Cannot Save Preset", "The selected asset is not a StoryMotionPresetSO.", "OK");
                    return false;
                }

                preset = CreateInstance<StoryMotionPresetSO>();
                AssetDatabase.CreateAsset(preset, path);
            }
            else
            {
                Undo.RecordObject(preset, "Edit Motion Preset");
            }

            preset.presetId = $"{primaryProperty}_{DateTime.Now:yyyyMMddHHmmss}";
            preset.property = primaryProperty;
            preset.keyframes.Clear();

            float firstTime = StoryTransitionSampler.GetKeyTime(selectedKeys[0]);
            float lastTime = firstTime;
            foreach (StoryActorKeyframeData key in selectedKeys)
                lastTime = Mathf.Max(lastTime, StoryTransitionSampler.GetKeyTime(key));

            float duration = Mathf.Max(1f, lastTime - firstTime);
            foreach (StoryActorKeyframeData key in selectedKeys)
            {
                StoryActorKeyframeData clone = key.ShallowClone();
                clone.timeSeconds = Mathf.Max(0f, StoryTransitionSampler.GetKeyTime(clone) - firstTime);
                clone.normalizedTime = Mathf.Clamp01(clone.timeSeconds / duration);
                preset.keyframes.Add(clone);
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            StoryMotionPresetLibraryWindow.RefreshOpenWindows();
            return true;
        }

        private bool CanApplyMotionPresetToProperty(StoryMotionPresetSO preset, StoryActorKeyframeProperty property)
        {
            return preset != null
                && IsMotionPresetProperty(property)
                && preset.keyframes != null
                && preset.keyframes.Exists(key => key != null && key.property == property);
        }

        private static StoryMotionPresetSO GetDraggedMotionPreset()
        {
            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (obj is StoryMotionPresetSO preset)
                    return preset;
            }

            return null;
        }

        private static void EnsureMotionPresetFolder()
        {
            string[] parts = MotionPresetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizePresetAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Actor";

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
                    chars[i] = '_';
            }

            return new string(chars);
        }

        private void RemoveSelectedTimelineKey()
        {
            if (_selectionKind == StageSelectionKind.Sound)
            {
                DeleteSelectedSoundKey();
                return;
            }

            IReadOnlyList<StoryActorKeyframeData> currentKeys = GetCurrentTimelineKeyframes();
            var keysToRemove = GetSelectedTimelineKeys(currentKeys);
            if (keysToRemove.Count == 0) return;

            SaveCurrentTimelineKeyframes(keyframes =>
            {
                keyframes.RemoveAll(keysToRemove.Contains);
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
                _selectedTimelineKeys.Clear();
            }, refresh: false);
            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SelectFirstTimelineKey() => SelectTimelineKeyByOffset(int.MinValue);
        private void SelectPreviousTimelineKey() => SelectTimelineKeyByOffset(-1);
        private void SelectNextTimelineKey() => SelectTimelineKeyByOffset(1);
        private void SelectLastTimelineKey() => SelectTimelineKeyByOffset(int.MaxValue);

        private void SelectTimelineKeyByOffset(int offset)
        {
            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            var visibleIndexes = new System.Collections.Generic.List<int>();
            if (keyframes != null)
            {
                for (int i = 0; i < keyframes.Count; i++)
                {
                    StoryActorKeyframeData key = keyframes[i];
                    if (key != null && IsTimelineKeyProperty(key.property) && IsPropertyAllowedForCurrentSelection(key.property))
                        visibleIndexes.Add(i);
                }
            }

            visibleIndexes.Sort((a, b) => StoryTransitionSampler.GetKeyTime(keyframes[a]).CompareTo(StoryTransitionSampler.GetKeyTime(keyframes[b])));
            int count = visibleIndexes.Count;
            if (count == 0) return;
            int currentVisibleIndex = Mathf.Max(0, visibleIndexes.IndexOf(_selectedTimelineKeyIndex));
            if (offset == int.MinValue) currentVisibleIndex = 0;
            else if (offset == int.MaxValue) currentVisibleIndex = count - 1;
            else currentVisibleIndex = Mathf.Clamp(currentVisibleIndex + offset, 0, count - 1);
            _selectedTimelineKeyIndex = visibleIndexes[currentVisibleIndex];
            SelectSingleTimelineKey(keyframes[_selectedTimelineKeyIndex], _selectedTimelineKeyIndex, keyframes[_selectedTimelineKeyIndex].property);
            _timelinePlayheadTime = StoryTransitionSampler.GetKeyTime(keyframes[_selectedTimelineKeyIndex]);
            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void SetTimelineKeyTime(StageSelectionKind selectionKind, string actorKey, int keyIndex, float timeSeconds, bool refresh)
        {
            if (keyIndex < 0) return;

            if (selectionKind == StageSelectionKind.Sound)
            {
                SetSoundTimelineKeyTime(keyIndex, timeSeconds, refresh);
                return;
            }

            SaveTimelineKeyframesFor(selectionKind, actorKey, keyframes =>
            {
                if (keyIndex >= keyframes.Count) return;
                StoryActorKeyframeData key = keyframes[keyIndex];
                if (key == null)
                    return;

                float targetTime = Mathf.Max(0f, timeSeconds);
                if (HasTimelineKeyCollision(keyframes, key, targetTime, null))
                    targetTime = _draggingTimelineKeyStartTime;

                key.timeSeconds = targetTime;
                key.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                if (key.property == StoryActorKeyframeProperty.CameraTarget
                    && key.cameraFollowMode == StoryCameraFollowMode.SnapshotPosition
                    && !string.IsNullOrWhiteSpace(key.cameraTargetActorKey)
                    && TryResolveCurrentFrameActorStagePosition(key.cameraTargetActorKey, out Vector2 snapPosition))
                {
                    key.cameraSnapshotNormalizedPosition = snapPosition;
                }
            }, refresh);
        }

        private bool HasTimelineKeyCollision(
            IReadOnlyList<StoryActorKeyframeData> keyframes,
            StoryActorKeyframeData movingKey,
            float proposedTime,
            ICollection<StoryActorKeyframeData> ignoredKeys)
        {
            if (keyframes == null || movingKey == null)
                return false;

            foreach (StoryActorKeyframeData key in keyframes)
            {
                if (key == null || ReferenceEquals(key, movingKey))
                    continue;
                if (ignoredKeys != null && ignoredKeys.Contains(key))
                    continue;
                if (key.property != movingKey.property)
                    continue;
                if (Mathf.Abs(StoryTransitionSampler.GetKeyTime(key) - proposedTime) <= TimelineKeyHitSeconds)
                    return true;
            }

            return false;
        }

        private bool HasTimelineKeyCollision(
            IReadOnlyList<StoryActorKeyframeData> keyframes,
            Dictionary<StoryActorKeyframeData, float> proposedTimes)
        {
            if (keyframes == null || proposedTimes == null || proposedTimes.Count == 0)
                return false;

            List<KeyValuePair<StoryActorKeyframeData, float>> entries =
                new List<KeyValuePair<StoryActorKeyframeData, float>>(proposedTimes);
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    if (entries[i].Key == null || entries[j].Key == null)
                        continue;
                    if (entries[i].Key.property != entries[j].Key.property)
                        continue;
                    if (Mathf.Abs(entries[i].Value - entries[j].Value) <= TimelineKeyHitSeconds)
                        return true;
                }

                if (HasTimelineKeyCollision(keyframes, entries[i].Key, entries[i].Value, proposedTimes.Keys))
                    return true;
            }

            return false;
        }

        private void RecordActorKeyframeFromState(string actorKey, StoryActorStateData state, bool includePosition, bool includeScale)
        {
            if (!_timelineRecordEnabled || !IsStageAuthoringMode || state == null || string.IsNullOrWhiteSpace(actorKey)) return;
            if (_timelineRecordSelectionKind != StageSelectionKind.Actor) return;
            if (!string.Equals(actorKey, _timelineRecordActorKey, StringComparison.Ordinal)) return;
            if (includePosition) AddOrUpdateKey(actorKey, state, StoryActorKeyframeProperty.Position, _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
            if (includeScale) AddOrUpdateKey(actorKey, state, StoryActorKeyframeProperty.Scale, _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
            RefreshTimelinePanel();
        }

        private void RecordBackgroundKeyframeFromState(StoryBackgroundStateData state, bool includePosition, bool includeScale)
        {
            if (!_timelineRecordEnabled || !IsStageAuthoringMode || state == null) return;
            if (_timelineRecordSelectionKind != StageSelectionKind.Background) return;
            if (includePosition) AddOrUpdateBackgroundKey(state, StoryActorKeyframeProperty.BackgroundPosition, _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
            if (includeScale) AddOrUpdateBackgroundKey(state, StoryActorKeyframeProperty.BackgroundScale, _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
            RefreshTimelinePanel();
        }

        private bool IsActorRecordSelectionLocked(string actorKey)
        {
            if (!_timelineRecordEnabled)
                return false;

            if (_timelineRecordSelectionKind == StageSelectionKind.Background
                || _timelineRecordSelectionKind == StageSelectionKind.Camera)
                return true;

            return !string.IsNullOrWhiteSpace(_timelineRecordActorKey)
                && !string.Equals(actorKey, _timelineRecordActorKey, StringComparison.Ordinal);
        }

        private bool IsBackgroundRecordSelectionLocked() =>
            _timelineRecordEnabled
            && (_timelineRecordSelectionKind == StageSelectionKind.Camera
                || (_timelineRecordSelectionKind == StageSelectionKind.Actor
                    && !string.IsNullOrWhiteSpace(_timelineRecordActorKey)));

        private bool BlocksActorManipulationBySelectedKey(string actorKey, StoryActorKeyframeProperty property)
        {
            if (!string.Equals(actorKey, _selectedActorKey, StringComparison.Ordinal))
                return false;

            if (HasTimelineMultiSelection)
                return true;

            return _selectedTimelineKeyIndex >= 0 && _selectedTimelineProperty != property;
        }

        private bool TryApplySelectedTimelineKeyFromState(string actorKey, StoryActorStateData state, StoryActorKeyframeProperty property)
        {
            if (state == null
                || string.IsNullOrWhiteSpace(actorKey)
                || HasTimelineMultiSelection
                || _selectedTimelineKeyIndex < 0
                || !string.Equals(actorKey, _selectedActorKey, StringComparison.Ordinal)
                || _selectedTimelineProperty != property)
                return false;

            int keyIndex = _selectedTimelineKeyIndex;
            SaveActorTrackToCurrent(actorKey, track =>
            {
                if (keyIndex < 0 || keyIndex >= track.keyframes.Count)
                    return;

                StoryActorKeyframeData key = track.keyframes[keyIndex];
                if (key == null || key.property != property)
                    return;

                if (property == StoryActorKeyframeProperty.Position)
                    key.stageLocalPosition = state.stageLocalPosition;
                else if (property == StoryActorKeyframeProperty.Scale)
                {
                    float sm = state.scaleMultiplier > 0f ? state.scaleMultiplier : 1f;
                    key.scale = new Vector2(sm, sm);
                }
            }, refresh: false);

            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
            return true;
        }

        // Background Position key가 선택된 상태에서 drag 완료 시 해당 key를 직접 수정한다.
        // 선택된 key가 없거나 Background selection이 아니면 false를 반환한다.
        private bool TryApplySelectedTimelineKeyFromBackground(Vector2 finalPos)
        {
            if (HasTimelineMultiSelection
                || _selectedTimelineKeyIndex < 0
                || _selectionKind != StageSelectionKind.Background
                || _selectedTimelineProperty != StoryActorKeyframeProperty.BackgroundPosition)
                return false;

            int keyIndex = _selectedTimelineKeyIndex;
            SaveBackgroundTrackToCurrent(track =>
            {
                if (keyIndex < 0 || keyIndex >= track.keyframes.Count) return;
                StoryActorKeyframeData key = track.keyframes[keyIndex];
                if (key == null || key.property != StoryActorKeyframeProperty.BackgroundPosition) return;
                key.stageLocalPosition = finalPos;
            }, refresh: false);

            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
            return true;
        }

        private bool TryApplySelectedTimelineKeyFromCamera(Vector2 finalPos)
        {
            if (HasTimelineMultiSelection
                || _selectedTimelineKeyIndex < 0
                || _selectionKind != StageSelectionKind.Camera
                || _selectedTimelineProperty != StoryActorKeyframeProperty.CameraOffset)
                return false;

            int keyIndex = _selectedTimelineKeyIndex;
            SaveCameraTrackToCurrent(track =>
            {
                if (keyIndex < 0 || keyIndex >= track.keyframes.Count) return;
                StoryActorKeyframeData key = track.keyframes[keyIndex];
                if (key == null || key.property != StoryActorKeyframeProperty.CameraOffset) return;
                key.cameraStageLocalPosition = finalPos;
            }, refresh: false);

            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
            return true;
        }

        private void AddOrUpdateKey(
            string actorKey,
            StoryActorStateData state,
            StoryActorKeyframeProperty property,
            float time,
            bool createIfMissing,
            bool selectKey = true,
            bool refreshInspector = true)
        {
            int previousKeyIndex = _selectedTimelineKeyIndex;
            int previousSegmentIndex = _selectedTimelineSegmentKeyIndex;
            StoryActorKeyframeProperty previousProperty = _selectedTimelineProperty;
            StoryActorKeyframeProperty previousSegmentProperty = _selectedTimelineSegmentProperty;

            SaveActorTrackToCurrent(actorKey, track =>
            {
                int index = FindKeyIndexNearTime(track, property, time);
                StoryActorKeyframeData key;
                if (index >= 0) key = track.keyframes[index];
                else
                {
                    if (!createIfMissing) return;
                    key = new StoryActorKeyframeData { property = property };
                    track.keyframes.Add(key);
                }
                key.property = property;
                key.timeSeconds = Mathf.Max(0f, time);
                key.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                if (property == StoryActorKeyframeProperty.Position) key.stageLocalPosition = state.stageLocalPosition;
                if (property == StoryActorKeyframeProperty.Scale)
                {
                    float sm = state.scaleMultiplier > 0f ? state.scaleMultiplier : 1f;
                    key.scale = new Vector2(sm, sm);
                }
                if (property == StoryActorKeyframeProperty.Expression)
                    key.expression = state.ResolvedExpression;
                if (selectKey)
                {
                    _selectedTimelineProperty = property;
                    _selectedTimelineSegmentKeyIndex = -1;
                }
            }, refresh: false);

            StoryActorTrackData refreshed = FindActorTrack(FindCurrentStageLayout(), actorKey);
            if (selectKey)
            {
                _selectedTimelineKeyIndex = FindKeyIndexNearTime(refreshed, property, time);
                _selectedTimelineKeys.Clear();
                if (refreshed != null
                    && _selectedTimelineKeyIndex >= 0
                    && _selectedTimelineKeyIndex < refreshed.keyframes.Count)
                    _selectedTimelineKeys.Add(refreshed.keyframes[_selectedTimelineKeyIndex]);
            }
            else
            {
                _selectedTimelineKeyIndex = previousKeyIndex;
                _selectedTimelineSegmentKeyIndex = previousSegmentIndex;
                _selectedTimelineProperty = previousProperty;
                _selectedTimelineSegmentProperty = previousSegmentProperty;
                ValidateTimelineSelection();
            }

            if (refreshInspector)
                RefreshActorInspector();
        }

        private void AddOrUpdateBackgroundKey(
            StoryBackgroundStateData state,
            StoryActorKeyframeProperty property,
            float time,
            bool createIfMissing,
            bool selectKey = true,
            bool refreshInspector = true)
        {
            if (state == null || !IsBackgroundTimelineProperty(property))
                return;

            int previousKeyIndex = _selectedTimelineKeyIndex;
            int previousSegmentIndex = _selectedTimelineSegmentKeyIndex;
            StoryActorKeyframeProperty previousProperty = _selectedTimelineProperty;
            StoryActorKeyframeProperty previousSegmentProperty = _selectedTimelineSegmentProperty;

            SaveBackgroundTrackToCurrent(track =>
            {
                int index = FindKeyIndexNearTime(track.keyframes, property, time);
                StoryActorKeyframeData key;
                if (index >= 0) key = track.keyframes[index];
                else
                {
                    if (!createIfMissing) return;
                    key = new StoryActorKeyframeData { property = property };
                    track.keyframes.Add(key);
                }

                key.property = property;
                key.timeSeconds = Mathf.Max(0f, time);
                key.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                if (property == StoryActorKeyframeProperty.BackgroundCut)
                {
                    var bg = state.background ?? FindCurrentStageLayout()?.Background?.background;
                    key.background = bg;
                    key.backgroundKey = bg != null ? ResolveBackgroundKey(bg) : state.ResolvedBackgroundKey;
                }
                else if (property == StoryActorKeyframeProperty.BackgroundPosition)
                {
                    key.stageLocalPosition = state.stageLocalPosition;
                }
                else if (property == StoryActorKeyframeProperty.BackgroundScale)
                {
                    float sm = state.scaleMultiplier > 0f ? state.scaleMultiplier : 1f;
                    key.scale = new Vector2(sm, sm);
                }

                if (selectKey)
                {
                    _selectedTimelineProperty = property;
                    _selectedTimelineSegmentKeyIndex = -1;
                }
            }, refresh: false);

            StoryBackgroundTrackData refreshed = FindCurrentStageLayout()?.BackgroundTrack;
            if (selectKey)
            {
                _selectedTimelineKeyIndex = FindKeyIndexNearTime(refreshed?.keyframes, property, time);
                _selectedTimelineKeys.Clear();
                if (refreshed?.keyframes != null
                    && _selectedTimelineKeyIndex >= 0
                    && _selectedTimelineKeyIndex < refreshed.keyframes.Count)
                    _selectedTimelineKeys.Add(refreshed.keyframes[_selectedTimelineKeyIndex]);
            }
            else
            {
                _selectedTimelineKeyIndex = previousKeyIndex;
                _selectedTimelineSegmentKeyIndex = previousSegmentIndex;
                _selectedTimelineProperty = previousProperty;
                _selectedTimelineSegmentProperty = previousSegmentProperty;
                ValidateTimelineSelection();
            }

            if (refreshInspector)
                RefreshActorInspector();
        }

        private void AddOrUpdateCameraKey(
            StoryCameraStateData state,
            StoryActorKeyframeProperty property,
            float time,
            bool createIfMissing,
            bool selectKey = true,
            bool refreshInspector = true)
        {
            if (state == null || !IsCameraTimelineProperty(property))
                return;

            int previousKeyIndex = _selectedTimelineKeyIndex;
            int previousSegmentIndex = _selectedTimelineSegmentKeyIndex;
            StoryActorKeyframeProperty previousProperty = _selectedTimelineProperty;
            StoryActorKeyframeProperty previousSegmentProperty = _selectedTimelineSegmentProperty;

            SaveCameraTrackToCurrent(track =>
            {
                int index = FindKeyIndexNearTime(track.keyframes, property, time);
                StoryActorKeyframeData key;
                if (index >= 0) key = track.keyframes[index];
                else
                {
                    if (!createIfMissing) return;
                    key = new StoryActorKeyframeData { property = property };
                    track.keyframes.Add(key);
                }

                key.property = property;
                key.timeSeconds = Mathf.Max(0f, time);
                key.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                if (property == StoryActorKeyframeProperty.CameraTarget)
                {
                    key.cameraTargetActorKey = state.targetActorInstanceKey ?? string.Empty;
                    key.cameraFollowMode = state.followMode;
                    if (!string.IsNullOrWhiteSpace(key.cameraTargetActorKey)
                        && TryResolveCurrentFrameActorStagePosition(key.cameraTargetActorKey, out Vector2 actorStagePosition))
                    {
                        key.cameraSnapshotNormalizedPosition = actorStagePosition;
                    }
                    else
                    {
                        key.cameraSnapshotNormalizedPosition = state.snapshotNormalizedPosition;
                    }
                }
                else if (property == StoryActorKeyframeProperty.CameraOffset)
                {
                    key.cameraStageLocalPosition = state.stageLocalPosition;
                }
                else if (property == StoryActorKeyframeProperty.CameraZoom)
                {
                    key.cameraZoom = Mathf.Max(0.01f, state.zoom);
                }

                if (selectKey)
                {
                    _selectedTimelineProperty = property;
                    _selectedTimelineSegmentKeyIndex = -1;
                }
            }, refresh: false);

            StoryCameraTrackData refreshed = FindCurrentStageLayout()?.CameraTrackEditable;
            if (selectKey)
            {
                _selectedTimelineKeyIndex = FindKeyIndexNearTime(refreshed?.keyframes, property, time);
                _selectedTimelineKeys.Clear();
                if (refreshed?.keyframes != null
                    && _selectedTimelineKeyIndex >= 0
                    && _selectedTimelineKeyIndex < refreshed.keyframes.Count)
                    _selectedTimelineKeys.Add(refreshed.keyframes[_selectedTimelineKeyIndex]);
            }
            else
            {
                _selectedTimelineKeyIndex = previousKeyIndex;
                _selectedTimelineSegmentKeyIndex = previousSegmentIndex;
                _selectedTimelineProperty = previousProperty;
                _selectedTimelineSegmentProperty = previousSegmentProperty;
                ValidateTimelineSelection();
            }

            if (refreshInspector)
                RefreshActorInspector();
        }

        private static int FindKeyIndexNearTime(StoryActorTrackData track, StoryActorKeyframeProperty property, float timeSeconds)
        {
            return FindKeyIndexNearTime(track?.keyframes, property, timeSeconds);
        }

        private static int FindKeyIndexNearTime(IReadOnlyList<StoryActorKeyframeData> keyframes, StoryActorKeyframeProperty property, float timeSeconds)
        {
            if (keyframes == null) return -1;
            for (int i = 0; i < keyframes.Count; i++)
            {
                StoryActorKeyframeData key = keyframes[i];
                if (key != null
                    && key.property == property
                    && Mathf.Abs(StoryTransitionSampler.GetKeyTime(key) - timeSeconds) <= TimelineKeyHitSeconds)
                    return i;
            }
            return -1;
        }

        private void ValidateTimelineSelection()
        {
            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null)
            {
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
                _selectedTimelineKeys.Clear();
                return;
            }

            ValidateTimelineSelectionSet(keyframes);

            if (_selectedTimelineKeys.Count == 1)
            {
                StoryActorKeyframeData selectedKey = GetPrimarySelectedTimelineKey(keyframes);
                _selectedTimelineKeyIndex = IndexOfTimelineKey(keyframes, selectedKey);
                if (selectedKey != null)
                    _selectedTimelineProperty = selectedKey.property;
            }
            else if (_selectedTimelineKeys.Count > 1)
            {
                _selectedTimelineKeyIndex = -1;
            }

            if (_selectedTimelineKeyIndex >= 0
                && (_selectedTimelineKeyIndex >= keyframes.Count
                    || keyframes[_selectedTimelineKeyIndex] == null
                    || keyframes[_selectedTimelineKeyIndex].property != _selectedTimelineProperty
                    || !IsTimelineKeyProperty(keyframes[_selectedTimelineKeyIndex].property)
                    || !IsPropertyAllowedForCurrentSelection(keyframes[_selectedTimelineKeyIndex].property)))
            {
                _selectedTimelineKeyIndex = -1;
            }

            if (_selectedTimelineSegmentKeyIndex >= 0
                && (_selectedTimelineSegmentKeyIndex >= keyframes.Count
                    || keyframes[_selectedTimelineSegmentKeyIndex] == null
                    || keyframes[_selectedTimelineSegmentKeyIndex].property != _selectedTimelineSegmentProperty
                    || !SupportsTimelineSegment(_selectedTimelineSegmentProperty)))
            {
                _selectedTimelineSegmentKeyIndex = -1;
            }
        }

        private static bool IsTimelineKeyProperty(StoryActorKeyframeProperty property) =>
            IsActorTimelineProperty(property)
            || IsBackgroundTimelineProperty(property)
            || IsCameraTimelineProperty(property)
            || IsSoundTimelineProperty(property);

        private static bool IsActorTimelineProperty(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.Position
            || property == StoryActorKeyframeProperty.Scale
            || property == StoryActorKeyframeProperty.Expression;

        private static bool IsCameraTimelineProperty(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.CameraTarget
            || property == StoryActorKeyframeProperty.CameraOffset
            || property == StoryActorKeyframeProperty.CameraZoom;

        private static bool IsBackgroundTimelineProperty(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.BackgroundCut
            || property == StoryActorKeyframeProperty.BackgroundPosition
            || property == StoryActorKeyframeProperty.BackgroundScale;

        private static bool IsSoundTimelineProperty(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.SoundBgm
            || property == StoryActorKeyframeProperty.SoundSfx;

        private static bool SupportsTimelineSegment(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.Position
            || property == StoryActorKeyframeProperty.Scale
            || property == StoryActorKeyframeProperty.CameraTarget
            || property == StoryActorKeyframeProperty.CameraOffset
            || property == StoryActorKeyframeProperty.CameraZoom
            || property == StoryActorKeyframeProperty.BackgroundPosition
            || property == StoryActorKeyframeProperty.BackgroundScale;

        private static bool IsMotionPresetProperty(StoryActorKeyframeProperty property) =>
            property == StoryActorKeyframeProperty.Position
            || property == StoryActorKeyframeProperty.Scale
            || property == StoryActorKeyframeProperty.BackgroundPosition
            || property == StoryActorKeyframeProperty.BackgroundScale
            || property == StoryActorKeyframeProperty.CameraOffset
            || property == StoryActorKeyframeProperty.CameraZoom;

        private bool IsPropertyAllowedForCurrentSelection(StoryActorKeyframeProperty property) =>
            _selectionKind == StageSelectionKind.Actor
                ? IsActorTimelineProperty(property)
                : _selectionKind == StageSelectionKind.Background
                    ? IsBackgroundTimelineProperty(property)
                    : _selectionKind == StageSelectionKind.Camera
                        ? IsCameraTimelineProperty(property)
                        : _selectionKind == StageSelectionKind.Sound && IsSoundTimelineProperty(property);

        private StoryActorKeyframeProperty ResolveDefaultPropertyForSelection() =>
            _selectionKind == StageSelectionKind.Background
                ? StoryActorKeyframeProperty.BackgroundPosition
                : _selectionKind == StageSelectionKind.Camera
                    ? StoryActorKeyframeProperty.CameraTarget
                    : _selectionKind == StageSelectionKind.Sound
                        ? ResolveDefaultSoundTimelineProperty()
                        : StoryActorKeyframeProperty.Position;

        private static int FindNextKeyIndex(IReadOnlyList<StoryActorKeyframeData> keyframes, int fromIndex, StoryActorKeyframeProperty property)
        {
            if (keyframes == null || fromIndex < 0 || fromIndex >= keyframes.Count)
                return -1;

            float fromTime = StoryTransitionSampler.GetKeyTime(keyframes[fromIndex]);
            int nextIndex = -1;
            float nextTime = float.MaxValue;
            for (int i = 0; i < keyframes.Count; i++)
            {
                StoryActorKeyframeData key = keyframes[i];
                if (key == null || key.property != property)
                    continue;

                float time = StoryTransitionSampler.GetKeyTime(key);
                if (time > fromTime && time < nextTime)
                {
                    nextTime = time;
                    nextIndex = i;
                }
            }

            return nextIndex;
        }

        private static int FindPreviousKeyIndex(
            IReadOnlyList<StoryActorKeyframeData> keyframes,
            int keyIndex,
            StoryActorKeyframeProperty property)
        {
            if (keyframes == null || keyIndex < 0 || keyIndex >= keyframes.Count)
                return -1;

            float keyTime = StoryTransitionSampler.GetKeyTime(keyframes[keyIndex]);
            int prevIndex = -1;
            float prevTime = -1f;
            for (int i = 0; i < keyframes.Count; i++)
            {
                if (i == keyIndex) continue;
                StoryActorKeyframeData k = keyframes[i];
                if (k == null || k.property != property) continue;
                float t = StoryTransitionSampler.GetKeyTime(k);
                if (t < keyTime && t > prevTime) { prevTime = t; prevIndex = i; }
            }
            return prevIndex;
        }

        private void ShowEasingMenu(int keyIndex, StoryActorKeyframeProperty property)
        {
            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null || keyIndex < 0 || keyIndex >= keyframes.Count || !SupportsTimelineSegment(property)) return;
            var menu = new GenericMenu();
            foreach (StoryStageMoveMotionType easing in Enum.GetValues(typeof(StoryStageMoveMotionType)))
            {
                StoryStageMoveMotionType captured = easing;
                menu.AddItem(new GUIContent(captured.ToString()), keyframes[keyIndex].easing == captured, () =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        if (keyIndex >= 0 && keyIndex < currentKeys.Count)
                            currentKeys[keyIndex].easing = captured;
                    }, refresh: true);
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                });
            }
            menu.ShowAsContext();
        }

        private void CopySelectedTimelineKey()
        {
            if (_selectionKind == StageSelectionKind.Sound)
            {
                CopySelectedSoundTimelineKeys();
                return;
            }

            var selected = GetSelectedTimelineKeys(GetCurrentTimelineKeyframes());
            if (selected.Count == 0) return;

            selected.Sort((a, b) => StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b)));
            _timelineClipboardKeys.Clear();
            foreach (StoryActorKeyframeData key in selected)
                _timelineClipboardKeys.Add(key.ShallowClone());

            _timelineClipboardKey = _timelineClipboardKeys.Count == 1 ? _timelineClipboardKeys[0] : null;
            _timelineClipboardProperty = _timelineClipboardKeys[0].property;
            _timelineClipboardSelectionKind = _selectionKind;
        }

        private void PasteTimelineKeyAtPlayhead()
        {
            if (_selectionKind == StageSelectionKind.Sound)
            {
                PasteSoundTimelineKeysAtPlayhead();
                return;
            }

            if (_timelineClipboardKeys.Count == 0 || _timelineClipboardSelectionKind != _selectionKind) return;
            float firstTime = float.MaxValue;
            foreach (StoryActorKeyframeData key in _timelineClipboardKeys)
                firstTime = Mathf.Min(firstTime, StoryTransitionSampler.GetKeyTime(key));

            SaveCurrentTimelineKeyframes(keyframes =>
            {
                _selectedTimelineKeys.Clear();
                foreach (StoryActorKeyframeData source in _timelineClipboardKeys)
                {
                    if (!IsPropertyAllowedForCurrentSelection(source.property))
                        continue;

                    StoryActorKeyframeData clone = source.ShallowClone();
                    clone.timeSeconds = _timelinePlayheadTime + Mathf.Max(0f, StoryTransitionSampler.GetKeyTime(source) - firstTime);
                    clone.normalizedTime = Mathf.Clamp01(clone.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                    keyframes.Add(clone);
                    _selectedTimelineKeys.Add(clone);
                }

                _selectedTimelineProperty = _timelineClipboardProperty;
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
            }, refresh: true);
            NormalizeTimelineSelectionFromSet(_timelineClipboardProperty);
            RefreshTimelinePanel();
        }

        private bool HandleTimelineShortcut(KeyCode keyCode, bool control, bool shift)
        {
            if (!IsStageAuthoringMode)
                return false;

            if (!control && keyCode == KeyCode.Space)
            {
                ToggleTimelinePlayback();
                return true;
            }

            if (control && keyCode == KeyCode.Z)
            {
                return TryPerformContextUndo(InteractionContext.Timeline, redo: shift);
            }

            if (control && keyCode == KeyCode.Y)
            {
                return TryPerformContextUndo(InteractionContext.Timeline, redo: true);
            }

            if (keyCode == KeyCode.Delete || keyCode == KeyCode.Backspace)
            {
                if (_selectionKind == StageSelectionKind.Sound)
                    return DeleteSelectedSoundKey();

                if (_selectedTimelineKeyIndex >= 0 || _selectedTimelineKeys.Count > 0)
                {
                    RemoveSelectedTimelineKey();
                    return true;
                }
                return false;
            }
            if (control && keyCode == KeyCode.C)
            {
                CopySelectedTimelineKey();
                return true;
            }
            if (control && keyCode == KeyCode.V)
            {
                PasteTimelineKeyAtPlayhead();
                return true;
            }
            return false;
        }
    }
}
