using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Camera;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ?? ?곗륫 ?몄뒪?숉꽣 ?⑤꼸 鍮뚮뱶 ??????????????????

        private VisualElement BuildActorInspector()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = _previewInspectorExpandedWidth, flexDirection = FlexDirection.Column,
                    borderLeftWidth = 1, borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)),
                    paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8
                }
            };
            _inspectorPanel = panel;

            _inspectorScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            panel.Add(_inspectorScrollView);

            _lineInfoSpeakerLabel = new Label("")
            {
                style =
                {
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.85f, 0.88f, 0.95f)),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 2
                }
            };
            _lineInfoDialogueLabel = new Label("")
            {
                style =
                {
                    fontSize = 10,
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(new Color(0.70f, 0.72f, 0.76f)),
                    marginBottom = 4
                }
            };
            _inspectorScrollView.Add(_lineInfoSpeakerLabel);
            _inspectorScrollView.Add(_lineInfoDialogueLabel);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(MakeBoldLabel("Aspect Settings"));
            _aspectSettingsField = new ObjectField("Aspect SO")
            {
                objectType = typeof(StoryAspectSettingsSO),
                allowSceneObjects = false,
                value = _previewAspectSettings,
                style = { marginBottom = 4 }
            };
            _aspectSettingsField.RegisterValueChangedCallback(e =>
            {
                _previewAspectSettings = e.newValue as StoryAspectSettingsSO;
                SaveAspectSettingsGuid();
                UpdateCameraFrameGeometry();
                float ww = _stageWrapper?.resolvedStyle.width ?? 0f;
                float wh = _stageWrapper?.resolvedStyle.height ?? 0f;
                if (ww > 0 && wh > 0) InitWorldView(ww, wh);
                RebuildActorLayer();
                RefreshAspectMetricsDisplay();
                Repaint();
            });
            _inspectorScrollView.Add(_aspectSettingsField);

            var cameraInitField = new ObjectField("Camera Init SO")
            {
                objectType = typeof(StoryCameraInitSettingsSO),
                allowSceneObjects = false,
                value = _previewCameraInitSettings,
                style = { marginBottom = 4 }
            };
            cameraInitField.RegisterValueChangedCallback(e =>
            {
                _previewCameraInitSettings = e.newValue as StoryCameraInitSettingsSO;
                SaveCameraInitGuid();
                RebuildActorLayer();
                RefreshBackgroundLayer();
                Repaint();
            });
            _inspectorScrollView.Add(cameraInitField);

            _aspectMetricsInfoLabel = new Label("")
            {
                style =
                {
                    fontSize = 9,
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(new Color(0.58f, 0.64f, 0.72f)),
                    marginBottom = 4
                }
            };
            _inspectorScrollView.Add(_aspectMetricsInfoLabel);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(BuildSoundSettingsPanels());
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(BuildAuthoringTools());
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorScrollView.Add(MakeBoldLabel("Stage Actors"));

            _actorListRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 4 } };
            _inspectorScrollView.Add(_actorListRoot);
            _inspectorScrollView.Add(MakeSeparator());

            _inspectorRoot = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            _inspectorScrollView.Add(_inspectorRoot);

            RefreshLineInfoLabel();
            RefreshAspectMetricsDisplay();
            RefreshSoundSettingsPanels();
            return panel;
        }

        private void RefreshAspectMetricsDisplay()
        {
            if (_aspectMetricsInfoLabel == null) return;

            float physical = GetPhysicalAspect();
            float visible  = GetStoryVisibleAspect();
            float ratio    = _previewAspectSettings?.VisibleWidthRatio ?? 1f;

            int renderW  = _renderResolution.x > 0 ? _renderResolution.x : FallbackRenderWidth;
            int renderH  = _renderResolution.y > 0 ? _renderResolution.y : FallbackRenderHeight;
            int visW     = Mathf.RoundToInt(renderW * ratio);
            int letterW  = Mathf.RoundToInt((renderW - visW) * 0.5f);

            _aspectMetricsInfoLabel.text =
                $"Physical: {physical:F3}  Visible: {visible:F3}\n" +
                $"View: {visW}×{renderH}  Letterbox: ±{letterW}px";
        }

        private VisualElement BuildAuthoringTools()
        {
            _authoringToolsRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };

            _authoringToolsRoot.Add(MakeBoldLabel("Stage Authoring"));

            _importPreviousStageBtn = new Button(OnImportPreviousStageClicked)
            {
                text = "Import Previous Stage",
                style = { height = 22, marginBottom = 4 }
            };
            _authoringToolsRoot.Add(_importPreviousStageBtn);

            _previewTransitionBtn = new Button(OnPreviewTransitionClicked)
            {
                text = "Preview Line Motion",
                style = { height = 22, marginBottom = 8 }
            };
            _authoringToolsRoot.Add(_previewTransitionBtn);

            _addActorField = new ObjectField("Actor")
            {
                objectType = typeof(CharacterDefinitionSO),
                allowSceneObjects = false,
                style = { marginBottom = 3 }
            };
            _addActorField.RegisterValueChangedCallback(_ => RefreshAuthoringControls());
            _authoringToolsRoot.Add(_addActorField);

            var actorRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };

            _addActorBtn = new Button(OnAddActorClicked)
            {
                text = "Add Actor",
                style = { flexGrow = 1, marginRight = 4, height = 22 }
            };
            actorRow.Add(_addActorBtn);

            _removeSelectedActorBtn = new Button(OnRemoveSelectedActorClicked)
            {
                text = "Remove Selected",
                style = { flexGrow = 1, height = 22 }
            };
            actorRow.Add(_removeSelectedActorBtn);
            _authoringToolsRoot.Add(actorRow);

            _setBackgroundField = new ObjectField("Background")
            {
                objectType = typeof(BackgroundDefinitionSO),
                allowSceneObjects = false,
                style = { marginBottom = 3 }
            };
            _setBackgroundField.RegisterValueChangedCallback(_ => RefreshAuthoringControls());
            _authoringToolsRoot.Add(_setBackgroundField);

            var bgRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            _setBackgroundBtn = new Button(OnSetBackgroundClicked)
            {
                text = "Set Background",
                style = { flexGrow = 1, marginRight = 4, height = 22 }
            };
            bgRow.Add(_setBackgroundBtn);

            _clearBackgroundBtn = new Button(OnClearBackgroundClicked)
            {
                text = "Clear",
                style = { width = 58, height = 22 }
            };
            bgRow.Add(_clearBackgroundBtn);
            _authoringToolsRoot.Add(bgRow);

            return _authoringToolsRoot;
        }

        private VisualElement BuildInspectorSplitter()
        {
            var splitter = new VisualElement
            {
                name = "StoryPreviewInspectorSplitter",
                style =
                {
                    width = 5,
                    flexShrink = 0,
                    backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f))
                }
            };
            _inspectorSplitter = splitter;

            splitter.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || _inspectorPanel == null || _previewInspectorCollapsed)
                    return;

                _isInspectorResizing = true;
                _inspectorResizeStartX = e.position.x;
                _inspectorResizeStartWidth = _inspectorPanel.resolvedStyle.width > 0
                    ? _inspectorPanel.resolvedStyle.width
                    : InspectorWidth;
                splitter.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            splitter.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isInspectorResizing || _inspectorPanel == null)
                    return;

                float delta = e.position.x - _inspectorResizeStartX;
                float width = Mathf.Clamp(_inspectorResizeStartWidth - delta, MinInspectorWidth, MaxInspectorWidth);
                _inspectorPanel.style.width = width;
                _previewInspectorExpandedWidth = width;
                e.StopPropagation();
            });

            splitter.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isInspectorResizing)
                    return;

                _isInspectorResizing = false;
                splitter.ReleasePointer(e.pointerId);
                SavePreviewLayoutPrefs();
                e.StopPropagation();
            });

            return splitter;
        }

        // ?? ?≫꽣 紐⑸줉 媛깆떊 ????????????????????????????

        private void RefreshActorList()
        {
            if (_actorListRoot == null) return;
            _actorListRoot.Clear();

            _actorListRoot.Add(MakeBoldLabel("Background"));
            _actorListRoot.Add(BuildBackgroundRow());
            _actorListRoot.Add(MakeSeparator());
            _actorListRoot.Add(MakeBoldLabel("Camera"));
            _actorListRoot.Add(BuildCameraRow());
            _actorListRoot.Add(MakeSeparator());
            _actorListRoot.Add(MakeBoldLabel("Sound"));
            _actorListRoot.Add(BuildSoundRow());
            _actorListRoot.Add(MakeSeparator());

            foreach (var kvp in _stageState)
            {
                string actorKey = kvp.Key;
                var data  = kvp.Value;
                var actor = data.actor;
                if (actor == null) continue;

                bool sel = _selectionKind == StageSelectionKind.Actor && actorKey == _selectedActorKey;

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row, alignItems = Align.Center,
                        paddingLeft = 4, paddingRight = 4, paddingTop = 2, paddingBottom = 2, marginBottom = 1,
                        backgroundColor = new StyleColor(sel ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                        borderTopLeftRadius = 2, borderTopRightRadius = 2,
                        borderBottomLeftRadius = 2, borderBottomRightRadius = 2
                    }
                };

                row.Add(new Label(data.visible ? "ON" : "OFF")
                {
                    style =
                    {
                        fontSize = 8, marginRight = 4,
                        color = new StyleColor(data.visible ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.45f, 0.45f, 0.45f))
                    }
                });

                row.Add(new Label(actor.DisplayName)
                {
                    style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
                });

                row.Add(new Label(actorKey)
                {
                    style =
                    {
                        fontSize = 8,
                        color = new StyleColor(new Color(0.52f, 0.52f, 0.56f)),
                        unityTextAlign = TextAnchor.MiddleRight,
                        maxWidth = 76,
                        overflow = Overflow.Hidden
                    }
                });

                if (data.focused)
                {
                    row.Add(new Label("F")
                    {
                        style = { fontSize = 8, color = new StyleColor(new Color(0.9f, 0.75f, 0.2f)) }
                    });
                }

                row.RegisterCallback<PointerDownEvent>(e =>
                {
                    SelectActor(actorKey);
                    e.StopPropagation();
                });

                _actorListRoot.Add(row);
            }
        }

        private VisualElement BuildBackgroundRow()
        {
            bool hasBackground = _bgState != null
                && (_bgState.background != null || !string.IsNullOrWhiteSpace(_bgState.ResolvedBackgroundKey));
            bool visible = _bgState is { visible: true };
            bool selected = _selectionKind == StageSelectionKind.Background;

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginBottom = 1,
                    backgroundColor = new StyleColor(selected ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            row.Add(new Label(visible ? "BG" : "--")
            {
                style =
                {
                    fontSize = 8,
                    marginRight = 5,
                    color = new StyleColor(visible ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.45f, 0.45f, 0.45f))
                }
            });

            string label = hasBackground
                ? _bgState.background != null
                    ? _bgState.background.DisplayName
                    : _bgState.ResolvedBackgroundKey
                : "(No Background)";
            row.Add(new Label(label)
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                SelectBackground();
                e.StopPropagation();
            });

            return row;
        }

        private VisualElement BuildCameraRow()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryCameraStateData state = layout?.CameraTrackEditable?.defaultState ?? new StoryCameraStateData();
            bool selected = _selectionKind == StageSelectionKind.Camera;

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginBottom = 1,
                    backgroundColor = new StyleColor(selected ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            row.Add(new Label("CAM")
            {
                style =
                {
                    fontSize = 8,
                    marginRight = 5,
                    color = new StyleColor(new Color(1f, 0.76f, 0.32f))
                }
            });

            string target = state.targetActorInstanceKey ?? "";
            string label = string.IsNullOrWhiteSpace(target) ? "Camera" : $"Camera ({target})";
            row.Add(new Label(label)
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                SelectCamera();
                e.StopPropagation();
            });

            return row;
        }


        private void RefreshLineInfoLabel()
        {
            if (_lineInfoSpeakerLabel == null || _lineInfoDialogueLabel == null) return;

            if (_currentLine == null)
            {
                _lineInfoSpeakerLabel.text  = "";
                _lineInfoDialogueLabel.text = "";
                return;
            }

            string speaker = _currentLine.GetResolvedSpeakerName();
            _lineInfoSpeakerLabel.text  = string.IsNullOrEmpty(speaker) ? "(Narration)" : speaker;
            _lineInfoDialogueLabel.text = _currentLine.DialogueText ?? "";
        }

        private void RefreshActorInspector()
        {
            if (_inspectorRoot == null) return;
            RefreshLineInfoLabel();
            RefreshSoundSettingsPanels();
            _inspectorRoot.Clear();

            if (!IsStageAuthoringMode)
            {
                _inspectorRoot.Add(new Label("Runtime Preview is read-only.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.55f, 0.55f, 0.58f)), marginTop = 8 }
                });
                return;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                BuildBackgroundInspector();
                return;
            }

            if (_selectionKind == StageSelectionKind.Camera)
            {
                BuildCameraInspector();
                return;
            }

            if (_selectionKind == StageSelectionKind.Sound)
            {
                BuildSoundInspector();
                return;
            }

            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !_stageState.TryGetValue(_selectedActorKey, out var data))
            {
            _inspectorRoot.Add(new Label("No actor selected")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginTop = 8 }
                });
                return;
            }

            CharacterDefinitionSO actor = data.actor;
            ValidateTimelineSelection();
            bool isEditingTimelineKey = _selectedTimelineKeyIndex >= 0;
            _inspectorRoot.Add(MakeBoldLabel("Actor"));
            _inspectorRoot.Add(MakeBoldLabel(actor != null ? actor.DisplayName : data.ResolvedActorKey));
            _inspectorRoot.Add(MakeSeparator());

            var keyField = new TextField("Instance Key")
            {
                value = data.ResolvedActorKey,
                isReadOnly = true,
                style = { marginBottom = 3 }
            };
            _inspectorRoot.Add(keyField);

            var characterField = new ObjectField("Character")
            {
                objectType = typeof(CharacterDefinitionSO),
                allowSceneObjects = false,
                value = actor,
                style = { marginBottom = 3 }
            };
            characterField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var nextActor = e.newValue as CharacterDefinitionSO;
                data.actor = nextActor;
                data.actorKey = StoryActorStateData.ResolveActorKey(nextActor);
                RebuildActorLayer();
                SaveActorStateToCurrent(_selectedActorKey, entry =>
                {
                    entry.actor = nextActor;
                    entry.actorKey = StoryActorStateData.ResolveActorKey(nextActor);
                });
            });
            _inspectorRoot.Add(characterField);

            // ?? Position ????????????????????????????
            var posField = new Vector2Field("Stage Position")
            {
                value = data.stageLocalPosition,
                tooltip = "(0,0)=중앙  world units  clamp 없음",
                style =
                {
                    marginBottom = 3
                }
            };
            posField.SetEnabled(!isEditingTimelineKey);
            posField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.stageLocalPosition = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.stageLocalPosition = e.newValue);
                UpdateActorLayerPositions();
                RecordActorKeyframeFromState(_selectedActorKey, data, includePosition: true, includeScale: false);
            });
            _inspectorRoot.Add(posField);

            // Phase 5A: primary uniform scale multiplier
            var scaleMultiplierField = new FloatField("Scale Mult")
            {
                value = data.scaleMultiplier,
                tooltip = "Per-line uniform scale multiplier. Final scale = CharacterDefinitionSO.BaseScaleMultiplier * scaleMultiplier.",
                style = { marginBottom = 3 }
            };
            scaleMultiplierField.SetEnabled(!isEditingTimelineKey);
            scaleMultiplierField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scaleMultiplier = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.scaleMultiplier = e.newValue);
                UpdateActorLayerPositions();
            });
            _inspectorRoot.Add(scaleMultiplierField);


            // ?? Scale X ?????????????????????????????

            // ?? Visible / Focused ????????????????????
            var visToggle = new Toggle("Visible")
            {
                value = data.visible,
                style =
                {
                    marginBottom = 3
                }
            };
            visToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.visible = e.newValue);
                RebuildActorLayer();
            });
            _inspectorRoot.Add(visToggle);

            var focToggle = new Toggle("Focused")
            {
                value = data.focused,
                style =
                {
                    marginBottom = 3
                }
            };
            focToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.focused = e.newValue);
                RebuildActorLayer();
                RefreshActorInspector();
            });
            _inspectorRoot.Add(focToggle);

            // ?? Sort Order ???????????????????????????
            var sortField = new IntegerField("Sort Order")
            {
                value = data.sortOrder,
                style =
                {
                    marginBottom = 3
                }
            };
            sortField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.sortOrder = e.newValue;
                SaveActorStateToCurrent(_selectedActorKey, entry => entry.sortOrder = e.newValue);
                RebuildActorLayer();
            });
            _inspectorRoot.Add(sortField);

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Transition"));
            _inspectorRoot.Add(new Label("Line transition defaults are still stored on the snapshot, but timeline editing is now the main UI.")
            {
                style =
                {
                    fontSize = 9,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 4
                }
            });

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }

            return;
        }

        private void BuildSelectedTimelineKeyInspector()
        {
            if (_selectedTimelineKeyIndex < 0)
                return;

            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null || _selectedTimelineKeyIndex >= keyframes.Count)
                return;

            StoryActorKeyframeData key = keyframes[_selectedTimelineKeyIndex];
            if (key == null)
                return;
            StoryActorKeyframeData selectedKeyRef = key;

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Key"));
            _inspectorRoot.Add(new Label($"{key.property} @ {StoryTransitionSampler.GetKeyTime(key):0.00}s")
            {
                style = { marginBottom = 4, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            var timeField = new FloatField("Time")
            {
                value = StoryTransitionSampler.GetKeyTime(key),
                style = { marginBottom = 3 }
            };
            timeField.RegisterValueChangedCallback(e =>
            {
                float time = Mathf.Max(0f, e.newValue);
                SaveCurrentTimelineKeyframes(currentKeys =>
                {
                    int index = currentKeys.IndexOf(selectedKeyRef);
                    if (index < 0)
                        return;

                    StoryActorKeyframeData currentKey = currentKeys[index];
                    currentKey.timeSeconds = time;
                    currentKey.normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()));
                }, refresh: false);
                _timelinePlayheadTime = Mathf.Max(0f, e.newValue);
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(timeField);

            if (key.property == StoryActorKeyframeProperty.Position)
            {
                var positionField = new Vector2Field("Position")
                {
                    value = key.stageLocalPosition,
                    style = { marginBottom = 3 }
                };
                positionField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].stageLocalPosition = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(positionField);
            }
            else if (key.property == StoryActorKeyframeProperty.Scale)
            {
                var scaleField = new Vector2Field("Scale")
                {
                    value = key.scale,
                    style = { marginBottom = 3 }
                };
                scaleField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].scale = ResolveNonZeroScale(e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(scaleField);
            }
            else if (key.property == StoryActorKeyframeProperty.Expression)
            {
                var expressionField = new EnumField("Expression", key.expression)
                {
                    style = { marginBottom = 3 }
                };
                expressionField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].expression = (StoryExpressionType)e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(expressionField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundCut)
            {
                var backgroundField = new ObjectField("Background")
                {
                    objectType = typeof(BackgroundDefinitionSO),
                    allowSceneObjects = false,
                    value = key.background,
                    style = { marginBottom = 3 }
                };
                backgroundField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index < 0)
                            return;

                        var background = e.newValue as BackgroundDefinitionSO;
                        currentKeys[index].background = background;
                        currentKeys[index].backgroundKey = ResolveBackgroundKey(background);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(backgroundField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundPosition)
            {
                var bgPosField = new Vector2Field("Position")
                {
                    value = key.stageLocalPosition,
                    style = { marginBottom = 3 }
                };
                bgPosField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].stageLocalPosition = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(bgPosField);
            }
            else if (key.property == StoryActorKeyframeProperty.BackgroundScale)
            {
                var bgScaleField = new Vector2Field("Scale")
                {
                    value = key.scale,
                    style = { marginBottom = 3 }
                };
                bgScaleField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].scale = ResolveNonZeroScale(e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(bgScaleField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraTarget)
            {
                StoryStageLayoutModuleSO currentLayout = FindCurrentStageLayout();

                var targetField = new TextField("Target Actor")
                {
                    value = key.cameraTargetActorKey,
                    style = { marginBottom = 3 }
                };
                targetField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraTargetActorKey = e.newValue ?? "";
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(targetField);

                var followModeField = new EnumField("Follow Mode", key.cameraFollowMode)
                {
                    style = { marginBottom = 3 }
                };
                followModeField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index < 0) return;
                        currentKeys[index].cameraFollowMode = (StoryCameraFollowMode)e.newValue;
                        if ((StoryCameraFollowMode)e.newValue == StoryCameraFollowMode.SnapshotPosition
                            && !string.IsNullOrWhiteSpace(currentKeys[index].cameraTargetActorKey)
                            && TryResolveCurrentFrameActorStagePosition(currentKeys[index].cameraTargetActorKey, out Vector2 snapPosition))
                        {
                            currentKeys[index].cameraSnapshotNormalizedPosition = snapPosition;
                        }
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(followModeField);

                var actorKeysRow = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 }
                };

                foreach (StoryActorStateData actorEntry in EnumerateCurrentLineCameraTargetActors(currentLayout))
                {
                    string captured = actorEntry.ResolvedActorKey;
                    actorKeysRow.Add(MakeBtn(captured, new Color(0.22f, 0.28f, 0.22f), () =>
                    {
                        SaveCurrentTimelineKeyframes(currentKeys =>
                        {
                            int index = currentKeys.IndexOf(selectedKeyRef);
                            if (index >= 0)
                            {
                                currentKeys[index].cameraTargetActorKey = captured;
                                if (currentKeys[index].cameraFollowMode == StoryCameraFollowMode.SnapshotPosition
                                    && TryResolveCurrentFrameActorStagePosition(captured, out Vector2 snapPosition))
                                {
                                    currentKeys[index].cameraSnapshotNormalizedPosition = snapPosition;
                                }
                            }
                        }, refresh: false);
                        ApplyTimelinePlayheadSample();
                        RefreshActorInspector();
                        RefreshTimelinePanel();
                    }));
                }
                actorKeysRow.Add(MakeBtn("Clear", new Color(0.30f, 0.20f, 0.20f), () =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraTargetActorKey = "";
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                }));
                _inspectorRoot.Add(actorKeysRow);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraOffset)
            {
                var camPosField = new Vector2Field("Camera Position")
                {
                    value = key.cameraStageLocalPosition,
                    style = { marginBottom = 3 }
                };
                camPosField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraStageLocalPosition = e.newValue;
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(camPosField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraZoom)
            {
                var zoomField = new FloatField("Camera Zoom")
                {
                    value = key.cameraZoom,
                    style = { marginBottom = 3 }
                };
                zoomField.RegisterValueChangedCallback(e =>
                {
                    SaveCurrentTimelineKeyframes(currentKeys =>
                    {
                        int index = currentKeys.IndexOf(selectedKeyRef);
                        if (index >= 0)
                            currentKeys[index].cameraZoom = Mathf.Max(0.01f, e.newValue);
                    }, refresh: false);
                    ApplyTimelinePlayheadSample();
                    RefreshTimelinePanel();
                });
                _inspectorRoot.Add(zoomField);
            }
            else if (key.property == StoryActorKeyframeProperty.CameraShake)
            {
                _inspectorRoot.Add(new HelpBox("Camera Shake keys are no longer supported.", HelpBoxMessageType.Info));
            }

        }

        private IEnumerable<StoryActorStateData> EnumerateCurrentLineCameraTargetActors(StoryStageLayoutModuleSO layout)
        {
            if (layout?.ActorsEditable == null)
                yield break;

            var emitted = new HashSet<string>();
            foreach (StoryActorStateData entry in layout.ActorsEditable)
            {
                if (entry == null || !entry.visible)
                    continue;

                string key = entry.ResolvedActorKey;
                if (string.IsNullOrWhiteSpace(key) || !emitted.Add(key))
                    continue;

                yield return entry;
            }
        }

        private bool TryResolveCurrentFrameActorStagePosition(string actorInstanceKey, out Vector2 stagePosition)
        {
            if (!string.IsNullOrWhiteSpace(actorInstanceKey)
                && _stageState.TryGetValue(actorInstanceKey, out StoryActorStateData currentState)
                && currentState != null)
            {
                stagePosition = currentState.stageLocalPosition;
                return true;
            }

            StoryActorStateData authoredEntry = FindActorEntry(FindCurrentStageLayout(), actorInstanceKey);
            if (authoredEntry != null)
            {
                stagePosition = authoredEntry.stageLocalPosition;
                return true;
            }

            stagePosition = default;
            return false;
        }

        private void BuildSelectedTimelineGroupInspector()
        {
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(GetCurrentTimelineKeyframes());
            if (selected.Count <= 1)
                return;

            float minTime = float.MaxValue;
            float maxTime = 0f;
            var properties = new SortedSet<string>();
            foreach (StoryActorKeyframeData key in selected)
            {
                if (key == null)
                    continue;

                float time = StoryTransitionSampler.GetKeyTime(key);
                minTime = Mathf.Min(minTime, time);
                maxTime = Mathf.Max(maxTime, time);
                properties.Add(key.property.ToString());
            }

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Keys"));
            _inspectorRoot.Add(new Label($"{selected.Count} keys / {string.Join(", ", properties)}")
            {
                style = { marginBottom = 3, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });
            _inspectorRoot.Add(new Label($"Range: {minTime:0.00}s -> {maxTime:0.00}s")
            {
                style = { marginBottom = 5, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            _inspectorRoot.Add(new Label("Multiple keys are selected. Direct value editing is disabled; use group move, Delete, Copy/Paste, or save as preset.")
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 5,
                    color = new StyleColor(new Color(0.58f, 0.60f, 0.66f))
                }
            });

            if (_selectionKind == StageSelectionKind.Actor)
            {
                _inspectorRoot.Add(MakeBtn("Save Selection as Preset", new Color(0.22f, 0.30f, 0.31f), () =>
                {
                    if (SaveSelectedTimelineKeysAsMotionPreset())
                        StoryMotionPresetLibraryWindow.Open();
                }));
            }
            _inspectorRoot.Add(MakeBtn("Delete Selected Keys", new Color(0.34f, 0.20f, 0.20f), RemoveSelectedTimelineKey));
        }

        private void BuildSelectedTimelineSegmentInspector()
        {
            if (_selectedTimelineSegmentKeyIndex < 0)
                return;

            IReadOnlyList<StoryActorKeyframeData> keyframes = GetCurrentTimelineKeyframes();
            if (keyframes == null
                || _selectedTimelineSegmentKeyIndex >= keyframes.Count
                || keyframes[_selectedTimelineSegmentKeyIndex] == null)
                return;

            int toIndex = _selectedTimelineSegmentKeyIndex;
            StoryActorKeyframeData toKey = keyframes[toIndex];
            StoryActorKeyframeData segmentKeyRef = toKey;

            int fromIndex = FindPreviousKeyIndex(keyframes, toIndex, _selectedTimelineSegmentProperty);
            float fromTime = fromIndex >= 0 ? StoryTransitionSampler.GetKeyTime(keyframes[fromIndex]) : 0f;
            string fromLabel = fromIndex >= 0 ? $"{fromTime:0.00}s" : "Line Start";
            float toTime = StoryTransitionSampler.GetKeyTime(toKey);

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected Segment"));
            _inspectorRoot.Add(new Label($"{toKey.property}: {fromLabel} → {toTime:0.00}s")
            {
                style = { marginBottom = 4, color = new StyleColor(new Color(0.70f, 0.72f, 0.78f)) }
            });

            var easingField = new EnumField("Easing", toKey.easing)
            {
                style = { marginBottom = 3 }
            };
            easingField.RegisterValueChangedCallback(e =>
            {
                SaveCurrentTimelineKeyframes(currentKeys =>
                {
                    int index = currentKeys.IndexOf(segmentKeyRef);
                    if (index >= 0)
                        currentKeys[index].easing = (StoryStageMoveMotionType)e.newValue;
                }, refresh: false);
                ApplyTimelinePlayheadSample();
                RefreshTimelinePanel();
            });
            _inspectorRoot.Add(easingField);
        }

        //
        private void BuildBackgroundInspector()
        {
            StoryBackgroundStateData data = _bgState?.ShallowClone() ?? new StoryBackgroundStateData();
            ValidateTimelineSelection();
            bool isEditingTimelineKey = _selectedTimelineKeyIndex >= 0;

            _inspectorRoot.Add(MakeBoldLabel("Stage Background"));
            _inspectorRoot.Add(MakeSeparator());

            var definitionField = new ObjectField("Definition")
            {
                objectType = typeof(BackgroundDefinitionSO),
                allowSceneObjects = false,
                value = data.background,
                style = { marginBottom = 3 }
            };
            definitionField.SetEnabled(!isEditingTimelineKey);
            definitionField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                var next = e.newValue as BackgroundDefinitionSO;
                data.background = next;
                data.backgroundKey = ResolveBackgroundKey(next);
                data.visible = next != null || !string.IsNullOrWhiteSpace(data.backgroundKey);
                SaveBackgroundStateToCurrent(entry =>
                {
                    entry.background = next;
                    entry.backgroundKey = ResolveBackgroundKey(next);
                    entry.visible = data.visible;
                });
            });
            _inspectorRoot.Add(definitionField);

            var keyField = new TextField("Background Key")
            {
                value = data.ResolvedBackgroundKey,
                style = { marginBottom = 3 }
            };
            keyField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.backgroundKey = e.newValue ?? "";
                SaveBackgroundStateToCurrent(entry => entry.backgroundKey = data.backgroundKey);
            });
            _inspectorRoot.Add(keyField);

            var visibleToggle = new Toggle("Visible")
            {
                value = data.visible,
                style = { marginBottom = 3 }
            };
            visibleToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.visible = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.visible = e.newValue);
            });
            _inspectorRoot.Add(visibleToggle);

            var bgPosField = new Vector2Field("Stage Position")
            {
                value = data.stageLocalPosition,
                tooltip = "Background world-unit offset from parallax base. (0,0) = center.",
                style = { marginBottom = 3 }
            };
            bgPosField.SetEnabled(!isEditingTimelineKey);
            bgPosField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.stageLocalPosition = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.stageLocalPosition = e.newValue);
                RecordBackgroundKeyframeFromState(data, includePosition: true, includeScale: false);
            });
            _inspectorRoot.Add(bgPosField);

            var bgScaleMultField = new FloatField("Scale Mult")
            {
                value = data.scaleMultiplier,
                tooltip = "Uniform scale multiplier. Final = BackgroundDefinitionSO.BaseScaleMultiplier × scaleMultiplier.",
                style = { marginBottom = 3 }
            };
            bgScaleMultField.SetEnabled(!isEditingTimelineKey);
            bgScaleMultField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.scaleMultiplier = Mathf.Max(0.001f, e.newValue);
                SaveBackgroundStateToCurrent(entry => entry.scaleMultiplier = data.scaleMultiplier);
                RecordBackgroundKeyframeFromState(data, includePosition: false, includeScale: true);
            });
            _inspectorRoot.Add(bgScaleMultField);

            var sortField = new IntegerField("Sort Order")
            {
                value = data.sortOrder,
                style = { marginBottom = 3 }
            };
            sortField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.sortOrder = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.sortOrder = e.newValue);
            });
            _inspectorRoot.Add(sortField);

            var overrideParallaxToggle = new Toggle("Override Parallax")
            {
                value = data.overrideParallax,
                style = { marginBottom = 3 }
            };
            var parallaxSlider = new Slider("Parallax Factor", 0f, 1f)
            {
                value = data.parallaxFactorOverride,
                style = { marginBottom = 3 }
            };
            parallaxSlider.SetEnabled(data.overrideParallax);
            overrideParallaxToggle.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.overrideParallax = e.newValue;
                if (e.newValue)
                    data.parallaxFactorOverride = data.background?.ParallaxFactor ?? 0f;
                parallaxSlider.SetValueWithoutNotify(data.parallaxFactorOverride);
                parallaxSlider.SetEnabled(e.newValue);
                SaveBackgroundStateToCurrent(entry =>
                {
                    entry.overrideParallax = e.newValue;
                    entry.parallaxFactorOverride = data.parallaxFactorOverride;
                });
            });
            parallaxSlider.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                data.parallaxFactorOverride = e.newValue;
                SaveBackgroundStateToCurrent(entry => entry.parallaxFactorOverride = e.newValue);
            });
            _inspectorRoot.Add(overrideParallaxToggle);
            _inspectorRoot.Add(parallaxSlider);

            _inspectorRoot.Add(MakeSeparator());

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }
        }

        private void BuildCameraInspector()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout == null)
                return;

            StoryCameraTrackData track = layout.CameraTrackEditable;
            track.defaultState ??= new StoryCameraStateData();
            StoryCameraStateData state = track.defaultState;

            _inspectorRoot.Add(MakeBoldLabel("Camera"));
            _inspectorRoot.Add(MakeSeparator());

            // ── New stagePosition / zoom (Phase Camera) ───────────────
            _inspectorRoot.Add(MakeBoldLabel("Stage Position"));

            var stagePosField = new Vector2Field("Stage Pos")
            {
                value   = state.stageLocalPosition,
                tooltip = "(0,0)=중앙  world units  clamp 없음",
                style   = { marginBottom = 3 }
            };
            stagePosField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.stageLocalPosition = e.newValue);
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshBackgroundLayer();
                Repaint();
            });
            _inspectorRoot.Add(stagePosField);

            var zoomField = new FloatField("Zoom")
            {
                value   = state.zoom,
                tooltip = "1=기본  1.2=20%확대  finalOrtho=baseOrtho/zoom",
                style   = { marginBottom = 6 }
            };
            zoomField.RegisterValueChangedCallback(e =>
            {
                if (!IsStageAuthoringMode) return;
                SaveCameraStateToCurrent(camera => camera.zoom = Mathf.Max(0.01f, e.newValue));
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshBackgroundLayer();
                Repaint();
            });
            _inspectorRoot.Add(zoomField);

            if (HasTimelineMultiSelection)
            {
                BuildSelectedTimelineGroupInspector();
            }
            else
            {
                BuildSelectedTimelineKeyInspector();
                BuildSelectedTimelineSegmentInspector();
            }
        }

        private void OnAddActorClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            var actor = _addActorField?.value as CharacterDefinitionSO;
            if (_currentLine == null || actor == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Add Actor To Stage");
            if (layout == null)
                return;

            RecordStageUndo(layout, "Add Actor To Stage");

            string instanceKey = GenerateActorInstanceKey(actor, layout);
            StoryActorStateData entry = new()
            {
                actor = actor,
                actorKey = StoryActorStateData.ResolveActorKey(actor),
                actorInstanceKey = instanceKey,
                stageLocalPosition = Vector2.zero,
                visible = true,
                focused = true,
                sortOrder = layout.ActorsEditable.Count
            };
            layout.ActorsEditable.Add(entry);

            SelectActor(instanceKey, refreshInspector: false);
            SaveLayoutAndRefresh(layout);
        }

        private List<StoryLineSO> GetAllPreviousLineCandidates(StoryLineSO line)
        {
            var candidates = new List<StoryLineSO>();
            if (line == null || episode == null)
                return candidates;

            // Primary: find all lines whose NextLineId == current line's LineId
            foreach (StoryLineSO candidate in episode.Lines)
            {
                if (candidate != null && candidate.NextLineId == line.LineId)
                    candidates.Add(candidate);
            }

            // Fallback: no explicit link → use linear episode order
            if (candidates.Count == 0)
            {
                int index = FindLineIndex(line);
                if (index > 0 && episode.Lines[index - 1] != null)
                    candidates.Add(episode.Lines[index - 1]);
            }

            return candidates;
        }

        private static readonly StoryActorKeyframeProperty[] _importableKeyProperties =
        {
            StoryActorKeyframeProperty.Position,
            StoryActorKeyframeProperty.Scale,
            StoryActorKeyframeProperty.Expression,
            StoryActorKeyframeProperty.BackgroundCut,
            StoryActorKeyframeProperty.BackgroundPosition,
            StoryActorKeyframeProperty.BackgroundScale,
            StoryActorKeyframeProperty.CameraTarget,
            StoryActorKeyframeProperty.CameraOffset,
            StoryActorKeyframeProperty.CameraZoom,
        };

        private static void ImportLastKeysAtZero(
            List<StoryActorKeyframeData> source,
            List<StoryActorKeyframeData> dest)
        {
            foreach (var prop in _importableKeyProperties)
            {
                StoryActorKeyframeData last = null;
                foreach (var k in source)
                {
                    if (k.property != prop) continue;
                    if (last == null || k.timeSeconds > last.timeSeconds)
                        last = k;
                }
                if (last == null) continue;

                var clone = last.ShallowClone();
                clone.timeSeconds    = 0f;
                clone.normalizedTime = 0f;
                dest.Add(clone);
            }
        }

        private void ApplyImportPreviousStage(StoryLineSO previousLine)
        {
            if (!TryBuildFinalStageStateAtLine(previousLine, out var previousActors, out var previousBackground, out var previousCamera))
                return;

            if (previousActors.Count == 0 && previousBackground == null && IsDefaultCameraState(previousCamera))
                return;

            StoryStageLayoutModuleSO prevLayout = FindStageLayout(previousLine);

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Import Previous Stage");
            if (layout == null)
                return;

            bool hasExistingData = layout.ActorsEditable.Count > 0
                || layout.BackgroundTrackEditable.keyframes.Count > 0
                || layout.ActorTracksEditable.Count > 0
                || layout.CameraTrackEditable.keyframes.Count > 0;

            if (hasExistingData)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Import Previous Stage",
                    "Clear current actor/background/camera tracks and insert the previous line's last keyframe values at t=0?",
                    "Import",
                    "Cancel");
                if (!overwrite)
                    return;
            }

            RecordStageUndo(layout, "Import Previous Stage");
            layout.ActorsEditable.Clear();
            layout.ActorTracksEditable.Clear();
            layout.BackgroundTrackEditable.keyframes.Clear();
            layout.CameraTrackEditable.keyframes.Clear();

            foreach (var pair in previousActors)
            {
                StoryActorStateData clone = pair.Value.ShallowClone();
                clone.EnsureActorInstanceKey(pair.Key);
                clone.SyncActorKey();
                layout.ActorsEditable.Add(clone);
            }

            CopyBackgroundState(previousBackground, layout.BackgroundEditable);
            CopyCameraState(previousCamera, layout.CameraTrackEditable.defaultState);

            if (prevLayout != null)
            {
                ImportLastKeysAtZero(prevLayout.BackgroundTrack.keyframes, layout.BackgroundTrackEditable.keyframes);
                ImportLastKeysAtZero(prevLayout.CameraTrack.keyframes,     layout.CameraTrackEditable.keyframes);

                foreach (StoryActorTrackData prevTrack in prevLayout.ActorTracks)
                {
                    if (string.IsNullOrWhiteSpace(prevTrack.actorInstanceKey)) continue;

                    var newTrack = new StoryActorTrackData { actorInstanceKey = prevTrack.actorInstanceKey };
                    ImportLastKeysAtZero(prevTrack.keyframes, newTrack.keyframes);
                    if (newTrack.keyframes.Count > 0)
                        layout.ActorTracksEditable.Add(newTrack);
                }
            }

            SaveLayoutAndRefresh(layout);
        }

        private void OnImportPreviousStageClicked()
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            List<StoryLineSO> candidates = GetAllPreviousLineCandidates(_currentLine);
            if (candidates.Count == 0)
                return;

            if (candidates.Count == 1)
            {
                ApplyImportPreviousStage(candidates[0]);
                return;
            }

            // Multiple predecessors: show selection menu
            var menu = new GenericMenu();
            foreach (StoryLineSO candidate in candidates)
            {
                StoryLineSO captured = candidate;
                string label = BuildCandidateMenuLabel(candidate);
                menu.AddItem(new GUIContent(label), false, () => ApplyImportPreviousStage(captured));
            }
            menu.ShowAsContext();
        }

        private static string BuildCandidateMenuLabel(StoryLineSO candidate)
        {
            if (candidate == null)
                return "(null)";

            string lineId = !string.IsNullOrWhiteSpace(candidate.LineId) ? candidate.LineId : candidate.name;
            string speaker = candidate.Speaker != null ? candidate.Speaker.DisplayName : "";
            string dialogue = candidate.DialogueText ?? "";
            if (dialogue.Length > 20)
                dialogue = dialogue.Substring(0, 20) + "…";

            if (!string.IsNullOrWhiteSpace(speaker) && !string.IsNullOrWhiteSpace(dialogue))
                return $"{lineId}: {speaker} — {dialogue}";
            if (!string.IsNullOrWhiteSpace(dialogue))
                return $"{lineId}: {dialogue}";
            return lineId;
        }

        private void OnPreviewTransitionClicked()
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StartLineTransitionPreview(_currentLine);
        }

        private void OnRemoveSelectedActorClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null || _selectionKind != StageSelectionKind.Actor || string.IsNullOrWhiteSpace(_selectedActorKey))
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Remove Actor From Stage");
            if (layout == null)
                return;

            RecordStageUndo(layout, "Remove Actor From Stage");

            string key = _selectedActorKey;
            bool wasInCurrentLayout = FindActorEntry(layout, key) != null;

            layout.ActorsEditable.RemoveAll(e => e != null && e.MatchesActorInstance(key));
            layout.ActorTracksEditable.RemoveAll(t => t != null && t.actorInstanceKey == key);

            if (wasInCurrentLayout)
            {
                // 이 라인에서 직접 추가된 액터 → 완전 삭제
                _stageState.Remove(key);
                ClearStageSelection();
            }
            else
            {
                // 이전 라인에서 상속된 액터 → 이 라인에서만 숨기는 tombstone 추가
                StoryActorStateData tombstone = _stageState.TryGetValue(key, out var current)
                    ? current.ShallowClone()
                    : new StoryActorStateData();
                tombstone.EnsureActorInstanceKey(key);
                tombstone.visible = false;
                layout.ActorsEditable.Add(tombstone);
            }

            SaveLayoutAndRefresh(layout);
        }

        private void OnSetBackgroundClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            var background = _setBackgroundField?.value as BackgroundDefinitionSO;
            if (_currentLine == null || background == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Set Stage Background");
            if (layout == null)
                return;

            RecordStageUndo(layout, "Set Stage Background");

            StoryBackgroundStateData state = layout.BackgroundEditable;
            state.background = background;
            state.backgroundKey = ResolveBackgroundKey(background);
            state.visible = true;
            if (Mathf.Approximately(state.scaleMultiplier, 0f))
                state.scaleMultiplier = 1f;

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            SaveLayoutAndRefresh(layout);
        }

        private void OnClearBackgroundClicked()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Clear Stage Background");
            if (layout == null)
                return;

            RecordStageUndo(layout, "Clear Stage Background");

            StoryBackgroundStateData state = layout.BackgroundEditable;
            state.background = null;
            state.backgroundKey = "";
            state.visible = false;

            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            SaveLayoutAndRefresh(layout);
        }

        private void RefreshAuthoringControls()
        {
            bool isAuthoring = IsStageAuthoringMode;
            bool hasLine = _currentLine != null;
            bool hasCurrentStageLayout = FindCurrentStageLayout() != null;
            bool hasPreviousStage = hasLine
                && GetAllPreviousLineCandidates(_currentLine).Count > 0
                && TryBuildStageStateBeforeLine(
                    _currentLine,
                    out var previousActors,
                    out var previousBackground)
                && (previousActors.Count > 0 || previousBackground != null);

            SetElementVisible(_authoringToolsRoot, isAuthoring);
            _importPreviousStageBtn?.SetEnabled(isAuthoring && hasLine && hasPreviousStage);
            _previewTransitionBtn?.SetEnabled(isAuthoring && hasLine && (hasCurrentStageLayout || _stageState.Count > 0 || hasPreviousStage));
            _addActorBtn?.SetEnabled(isAuthoring && hasLine && _addActorField?.value is CharacterDefinitionSO);
            _removeSelectedActorBtn?.SetEnabled(isAuthoring && hasLine && _selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey));
            _setBackgroundBtn?.SetEnabled(isAuthoring && hasLine && _setBackgroundField?.value is BackgroundDefinitionSO);
            _clearBackgroundBtn?.SetEnabled(isAuthoring && hasLine);
        }

        private StoryStageLayoutModuleSO FindCurrentStageLayout()
        {
            if (_currentLine == null)
                return null;

            foreach (var module in _currentLine.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }

        private StoryStageLayoutModuleSO GetOrCreateCurrentStageLayout(string undoName, InteractionContext context = InteractionContext.Stage)
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            if (layout != null)
                return layout;

            if (_currentLine == null)
                return null;

            RecordPreviewUndo(_currentLine, context, undoName);
            return StoryEditorUtility.AddModule(_currentLine, typeof(StoryStageLayoutModuleSO)) as StoryStageLayoutModuleSO;
        }

        private string GenerateActorInstanceKey(CharacterDefinitionSO actor, StoryStageLayoutModuleSO layout)
        {
            string baseKey = StoryActorStateData.ResolveActorKey(actor);
            if (string.IsNullOrWhiteSpace(baseKey))
                baseKey = "actor";

            var used = new HashSet<string>(_stageState.Keys);
            if (layout != null)
            {
                foreach (StoryActorStateData entry in layout.ActorsEditable)
                {
                    if (entry == null) continue;
                    string key = entry.ResolvedActorKey;
                    if (!string.IsNullOrWhiteSpace(key))
                        used.Add(key);
                }
            }

            for (int i = 1; i < 1000; i++)
            {
                string candidate = $"{baseKey}_{i:D2}";
                if (!used.Contains(candidate))
                    return candidate;
            }

            return $"{baseKey}_{Guid.NewGuid():N}";
        }

        private static StoryActorStateData FindActorEntry(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            foreach (StoryActorStateData entry in layout.ActorsEditable)
            {
                if (entry?.MatchesActorInstance(actorInstanceKey) == true)
                    return entry;
            }

            return null;
        }

        private static StoryActorTrackData FindActorTrack(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            foreach (StoryActorTrackData track in layout.ActorTracksEditable)
            {
                if (track != null && track.actorInstanceKey == actorInstanceKey)
                    return track;
            }

            return null;
        }

        private static StoryActorTrackData GetOrCreateActorTrack(StoryStageLayoutModuleSO layout, string actorInstanceKey)
        {
            if (layout == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return null;

            StoryActorTrackData track = FindActorTrack(layout, actorInstanceKey);
            if (track != null)
                return track;

            track = new StoryActorTrackData { actorInstanceKey = actorInstanceKey };
            layout.ActorTracksEditable.Add(track);
            return track;
        }

        private void SaveActorTrackToCurrent(string actorInstanceKey, Action<StoryActorTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null || string.IsNullOrWhiteSpace(actorInstanceKey))
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Actor Timeline", InteractionContext.Timeline);
            if (layout == null)
                return;

            if (!_suppressTimelineUndoRecording)
                RecordTimelineUndo(layout, "Edit Actor Timeline");
            StoryActorTrackData track = GetOrCreateActorTrack(layout, actorInstanceKey);
            if (track == null)
                return;
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = GetPrimarySelectedTimelineKey(track.keyframes);
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveBackgroundTrackToCurrent(Action<StoryBackgroundTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Background Timeline", InteractionContext.Timeline);
            if (layout == null)
                return;

            if (!_suppressTimelineUndoRecording)
                RecordTimelineUndo(layout, "Edit Background Timeline");
            StoryBackgroundTrackData track = layout.BackgroundTrackEditable;
            if (track == null)
                return;
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = GetPrimarySelectedTimelineKey(track.keyframes);
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveCameraTrackToCurrent(Action<StoryCameraTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Camera Timeline", InteractionContext.Timeline);
            if (layout == null)
                return;

            if (!_suppressTimelineUndoRecording)
                RecordTimelineUndo(layout, "Edit Camera Timeline");
            StoryCameraTrackData track = layout.CameraTrackEditable;
            if (track == null)
                return;

            track.defaultState ??= CreateDefaultCameraState();
            track.keyframes ??= new List<StoryActorKeyframeData>();

            StoryActorKeyframeData selectedKey = GetPrimarySelectedTimelineKey(track.keyframes);
            StoryActorKeyframeData selectedSegmentKey = _selectedTimelineSegmentKeyIndex >= 0 && _selectedTimelineSegmentKeyIndex < track.keyframes.Count
                ? track.keyframes[_selectedTimelineSegmentKeyIndex]
                : null;

            setter(track);
            track.keyframes.RemoveAll(k => k == null);
            track.keyframes.Sort((a, b) =>
            {
                int prop = a.property.CompareTo(b.property);
                return prop != 0
                    ? prop
                    : StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b));
            });

            if (selectedKey != null)
                _selectedTimelineKeyIndex = track.keyframes.IndexOf(selectedKey);
            if (selectedSegmentKey != null)
                _selectedTimelineSegmentKeyIndex = track.keyframes.IndexOf(selectedSegmentKey);
            ValidateTimelineSelection();

            MarkLayoutDirty(layout, saveNow: false);

            if (refresh)
            {
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void SaveCameraStateToCurrent(Action<StoryCameraStateData> setter)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Camera State", InteractionContext.Stage);
            if (layout == null)
                return;

            RecordStageUndo(layout, "Edit Camera State");
            StoryCameraTrackData track = layout.CameraTrackEditable;
            track.defaultState ??= CreateDefaultCameraState();
            ClearPreviewCameraSampleState();
            setter(track.defaultState);
            MarkLayoutDirty(layout, saveNow: false);
            RefreshActorList();
            RefreshTimelinePanel();
        }

        private void SaveLayoutAndRefresh(StoryStageLayoutModuleSO layout, bool saveNow = true)
        {
            MarkLayoutDirty(layout, saveNow);

            ClearPreviewCameraSampleState();
            BuildStageStateAt(_currentLine);
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private static string ResolveBackgroundKey(BackgroundDefinitionSO background)
        {
            if (background == null)
                return "";

            return !string.IsNullOrWhiteSpace(background.BackgroundId)
                ? background.BackgroundId
                : background.name;
        }

        private static void CopyBackgroundState(StoryBackgroundStateData source, StoryBackgroundStateData target)
        {
            if (target == null)
                return;

            if (source == null)
            {
                target.background = null;
                target.backgroundKey = "";
                target.visible = false;
                target.tint = Color.white;
                target.sortOrder = -100;
                target.stageLocalPosition = Vector2.zero;
                target.scaleMultiplier = 1f;
                return;
            }

            target.background = source.background;
            target.backgroundKey = source.backgroundKey;
            target.visible = source.visible;
            target.tint = source.tint;
            target.sortOrder = source.sortOrder;
            target.stageLocalPosition = source.stageLocalPosition;
            target.scaleMultiplier = source.scaleMultiplier;
            target.SyncBackgroundKey();
        }

        private bool TryBuildFinalStageStateAtLine(
            StoryLineSO line,
            out Dictionary<string, StoryActorStateData> actors,
            out StoryBackgroundStateData background,
            out StoryCameraStateData camera)
        {
            actors = new Dictionary<string, StoryActorStateData>();
            background = null;
            camera = new StoryCameraStateData();

            if (line == null)
                return false;

            if (!TryBuildStageStateByEpisodeOrder(line, includeTargetLine: true, actors, ref background))
                return false;

            StoryStageLayoutModuleSO layout = FindStageLayout(line);
            if (layout == null)
                return actors.Count > 0 || background != null;

            foreach (StoryActorTrackData track in layout.ActorTracks)
            {
                if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey))
                    continue;

                if (!actors.TryGetValue(track.actorInstanceKey, out StoryActorStateData actorState) || actorState == null)
                    continue;

                float duration = StoryTransitionSampler.GetActorTrackDuration(track);
                actors[track.actorInstanceKey] = StoryTransitionSampler.SampleActorTrackAtTime(actorState, track, duration);
            }

            background = StoryTransitionSampler.SampleBackgroundTrackAtTime(
                background,
                layout.BackgroundTrack,
                StoryTransitionSampler.GetBackgroundTrackDuration(layout.BackgroundTrack));

            camera = StoryTransitionSampler.SampleCameraTrackAtTime(
                layout.CameraTrack,
                "",
                StoryTransitionSampler.GetCameraTrackDuration(layout.CameraTrack));
            return actors.Count > 0 || background != null || !IsDefaultCameraState(camera);
        }

        private static StoryStageLayoutModuleSO FindStageLayout(StoryLineSO line)
        {
            if (line == null)
                return null;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }

        private static void CopyCameraState(StoryCameraStateData source, StoryCameraStateData target)
        {
            if (target == null)
                return;

            if (source == null)
            {
                target.stageLocalPosition = Vector2.zero;
                target.zoom = 1f;
                return;
            }

            target.stageLocalPosition = source.stageLocalPosition;
            target.zoom = source.zoom;
        }

        private static bool IsDefaultCameraState(StoryCameraStateData state)
        {
            if (state == null)
                return true;

            return state.stageLocalPosition == Vector2.zero
                && Mathf.Approximately(state.zoom, 1f);
        }

        private void SaveBackgroundStateToCurrent(Action<StoryBackgroundStateData> setter)
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Stage Background", InteractionContext.Stage);
            if (layout == null)
                return;

            RecordStageUndo(layout, "Edit Stage Background");
            setter(layout.BackgroundEditable);
            layout.BackgroundEditable.SyncBackgroundKey();
            _bgState = layout.Background.ShallowClone();
            MarkLayoutDirty(layout, saveNow: false);
            RefreshBackgroundLayer();
            RefreshActorList();
            RefreshAuthoringControls();
        }

        private void SelectActor(string actorInstanceKey, bool refreshInspector = true)
        {
            if (!IsStageAuthoringMode)
                return;

                if (IsActorRecordSelectionLocked(actorInstanceKey))
                    return;

            bool sameActor = _selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorInstanceKey;
            _selectionKind = StageSelectionKind.Actor;
            _selectedActorKey = actorInstanceKey;
            ClearSoundTimelineSelection();
            if (!sameActor)
                ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            UpdateCameraGizmoVisual();
            if (refreshInspector)
                RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void SelectBackground()
        {
            if (!IsStageAuthoringMode)
                return;

            if (IsBackgroundRecordSelectionLocked())
                return;

            bool wasAlreadyBackground = _selectionKind == StageSelectionKind.Background;
            _selectionKind = StageSelectionKind.Background;
            _selectedActorKey = null;
            ClearSoundTimelineSelection();
            if (!wasAlreadyBackground)
                ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            UpdateCameraGizmoVisual();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void SelectCamera()
        {
            if (!IsStageAuthoringMode)
                return;

            // Block camera selection only when a different track is recording
            if (_timelineRecordEnabled && _timelineRecordSelectionKind != StageSelectionKind.Camera)
                return;

            bool wasAlreadyCamera = _selectionKind == StageSelectionKind.Camera;
            _selectionKind = StageSelectionKind.Camera;
            _selectedActorKey = null;
            ClearSoundTimelineSelection();
            if (!wasAlreadyCamera)
                ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            UpdateCameraGizmoVisual();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void ClearStageSelection()
        {
            _selectionKind = StageSelectionKind.None;
            _selectedActorKey = null;
            ClearSoundTimelineSelection();
            ClearSoundTimelineProxyCache();
            ClearTimelineSelection(refresh: false);
            UpdateCameraGizmoVisual();
            RefreshTimelinePanel();
        }

        private void ValidateStageSelection()
        {
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
        }

        private bool DeleteCurrentStageSelection()
        {
            if (!IsStageAuthoringMode)
                return false;

            if (_selectionKind == StageSelectionKind.Actor && !string.IsNullOrWhiteSpace(_selectedActorKey))
            {
                OnRemoveSelectedActorClicked();
                return true;
            }

            if (_selectionKind == StageSelectionKind.Background)
            {
                OnClearBackgroundClicked();
                return true;
            }

            return false;
        }

        private void SaveActorStateToCurrent(string actorInstanceKey, Action<StoryActorStateData> setter, bool saveNow = false)
        {
            if (!IsStageAuthoringMode)
                return;

            if (_currentLine == null || string.IsNullOrWhiteSpace(actorInstanceKey)) return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Actor Stage State", InteractionContext.Stage);
            if (layout == null) return;

            RecordStageUndo(layout, "Edit Actor Stage State");

            var entry = FindActorEntry(layout, actorInstanceKey);

            if (entry == null)
            {
                entry = _stageState.TryGetValue(actorInstanceKey, out var current)
                    ? current.ShallowClone()
                    : new StoryActorStateData();
                entry.EnsureActorInstanceKey(actorInstanceKey);
                layout.ActorsEditable.Add(entry);
            }

            setter(entry);
            entry.focusVisualAlpha = -1f;
            entry.SyncActorKey();
            entry.EnsureActorInstanceKey(actorInstanceKey);
            _stageState[entry.ResolvedActorKey] = entry.ShallowClone();

            MarkLayoutDirty(layout, saveNow);
        }

        private static void MarkLayoutDirty(StoryStageLayoutModuleSO layout, bool saveNow)
        {
            if (layout == null)
                return;

            EditorUtility.SetDirty(layout);
            if (saveNow)
                AssetDatabase.SaveAssets();
        }
    }
}
