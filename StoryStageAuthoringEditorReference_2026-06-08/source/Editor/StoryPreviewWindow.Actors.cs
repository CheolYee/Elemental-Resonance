using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── 선택/강조 시각 상수 ──────────────────────────
        private const float SelectionBorderWidth = 3f;
        private static readonly Color SelectionBorderColor = new Color(0.95f, 0.85f, 0.25f);
        private const float CameraGizmoBorderHitThickness = 14f;
        private const float CameraGizmoTitleBarHeight = 22f;
        private const float CameraGizmoClickThresholdPixels = 3f;

        // ── 카메라 Gizmo 참조 (fast-path 업데이트용) ──
        private VisualElement _cameraGizmoElement;
        private VisualElement _cameraGizmoTitleBar;
        private Label _cameraGizmoInfoLabel;
        private VisualElement _cameraGizmoTopHit;
        private VisualElement _cameraGizmoLeftHit;
        private VisualElement _cameraGizmoRightHit;
        private VisualElement _cameraGizmoBottomHit;
        private VisualElement _cameraGizmoFillHit;
        private bool _cameraDragExceededClickThreshold;
        private StoryCameraStateData _cameraPreviewSampleBeforeDrag;

        // ── 액터 레이어 재빌드 ─────────────────────────

        private void RebuildActorLayer(bool refreshInspectorLists = true)
        {
            if (_actorLayer == null) return;

            RefreshBackgroundLayer();
            if (refreshInspectorLists)
                RefreshActorList();

            _actorLayer.Clear();
            _actorElements.Clear();
            _cameraGizmoLayer?.Clear();

            if (IsStageAuthoringMode && _cameraGizmoLayer != null)
            {
                _cameraGizmoElement = BuildCameraGizmoElement();
                _cameraGizmoLayer.Add(_cameraGizmoElement);
            }
            else
            {
                _cameraGizmoElement = null;
                _cameraGizmoTitleBar = null;
                _cameraGizmoInfoLabel = null;
                _cameraGizmoTopHit = null;
                _cameraGizmoLeftHit = null;
                _cameraGizmoRightHit = null;
                _cameraGizmoBottomHit = null;
                _cameraGizmoFillHit = null;
            }

            var ordered = new List<KeyValuePair<string, StoryActorStateData>>(_stageState);
            ordered.Sort((a, b) => a.Value.sortOrder.CompareTo(b.Value.sortOrder));

            bool any = false;
            foreach (var kvp in ordered)
            {
                string actorKey = kvp.Key;
                var data  = kvp.Value;
                var actor = data.actor;
                if (actor == null || data == null || !data.visible) continue;

                var el = CreateActorElement(actorKey, actor, data);
                _actorLayer.Add(el);
                _actorElements[actorKey] = el;
                PositionActorElement(el, data);
                any = true;
            }
            SetEmptyStageVisible(!any);
        }

        private VisualElement CreateActorElement(string actorKey, CharacterDefinitionSO actor, StoryActorStateData data)
        {
            var el = new VisualElement
            {
                name = actorKey,
                tooltip = $"{actor.DisplayName} ({actorKey})",
                pickingMode = IsStageAuthoringMode ? PickingMode.Position : PickingMode.Ignore,
                style = { position = Position.Absolute, overflow = Overflow.Visible, borderTopLeftRadius = 3, borderTopRightRadius = 3 }
            };

            var sprite = StoryStageVisualSizing.ResolveActorSprite(data);
            if (sprite != null)
            {
                el.style.backgroundImage = new StyleBackground(sprite);
                el.style.backgroundSize  = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                el.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                el.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Bottom));
            }
            else
            {
                el.style.backgroundColor = new StyleColor(ActorPlaceholderColor(actor));
                el.Add(new Label(actor.DisplayName) { style = { fontSize = 9, color = new StyleColor(Color.white), unityTextAlign = TextAnchor.UpperCenter } });
            }

            ApplyActorFocusStyle(el, data);
            if (IsStageAuthoringMode)
            {
                AddActorPivotMarker(el, data);
                if (data.focused)
                    AddFocusMarker(el);
                if (_selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorKey)
                    AddCharacterDefinitionHandles(el, data);
            }

            if (IsStageAuthoringMode && _selectionKind == StageSelectionKind.Actor && _selectedActorKey == actorKey)
            {
                el.style.borderTopWidth = el.style.borderRightWidth =
                el.style.borderBottomWidth = el.style.borderLeftWidth = SelectionBorderWidth;
                AddActorScaleHandles(el, actorKey, data);
            }

            RegisterActorInteraction(el, actorKey, data);
            return el;
        }

        private static void AddFocusMarker(VisualElement el)
        {
            var marker = new Label("FOCUS")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 3,
                    right = 3,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 1,
                    paddingBottom = 1,
                    fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.08f, 0.07f, 0.03f)),
                    backgroundColor = new StyleColor(new Color(1f, 0.78f, 0.18f, 0.92f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(marker);
        }

        private static void AddCameraFocusTargetMarker(VisualElement el)
        {
            var marker = new Label("CAM")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    top = 18,
                    right = 3,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 1,
                    paddingBottom = 1,
                    fontSize = 8,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(new Color(0.08f, 0.07f, 0.03f)),
                    backgroundColor = new StyleColor(new Color(1f, 0.74f, 0.32f, 0.92f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(marker);
        }

        private static void AddActorPivotMarker(VisualElement el, StoryActorStateData data)
        {
            float left = 50f;
            float top = 50f;

            var horizontal = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 10,
                    height = 1,
                    marginLeft = -5,
                    backgroundColor = new StyleColor(new Color(0.90f, 0.92f, 1f, 0.92f))
                }
            };
            var vertical = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 1,
                    height = 10,
                    marginTop = -5,
                    backgroundColor = new StyleColor(new Color(0.90f, 0.92f, 1f, 0.92f))
                }
            };
            var center = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = new StyleLength(new Length(left, LengthUnit.Percent)),
                    top = new StyleLength(new Length(top, LengthUnit.Percent)),
                    width = 4,
                    height = 4,
                    marginLeft = -2,
                    marginTop = -2,
                    backgroundColor = new StyleColor(new Color(0.18f, 0.78f, 1f, 0.95f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            el.Add(horizontal);
            el.Add(vertical);
            el.Add(center);
        }

        private void AddCharacterDefinitionHandles(VisualElement el, StoryActorStateData data)
        {
            // Pivot is always center; no handles needed.
        }

        private void RegisterCharacterHandleInteraction(
            VisualElement handle,
            VisualElement actorElement,
            CharacterDefinitionSO actor,
            string undoName,
            Action<Vector2> applyValue,
            Action<VisualElement, Vector2> updateHandleVisual)
        {
            if (handle == null || actorElement == null || actor == null || applyValue == null)
                return;

            bool dragging = false;
            int pointerId = -1;

            handle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0)
                    return;

                SetInteractionContext(InteractionContext.Stage);
                FocusStageWorkspace();
                dragging = true;
                pointerId = e.pointerId;
                RecordStageUndo(actor, undoName);
                handle.CapturePointer(pointerId);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging || e.pointerId != pointerId)
                    return;

                Vector2 local = actorElement.WorldToLocal(e.position);
                applyValue(local);
                updateHandleVisual?.Invoke(handle, local);
                EditorUtility.SetDirty(actor);
                UpdateActorLayerPositions();
                Repaint();
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!dragging || e.pointerId != pointerId)
                    return;

                dragging = false;
                handle.ReleasePointer(pointerId);
                pointerId = -1;
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private string ResolveCurrentPreviewCameraFocusTarget() =>
            ResolveCurrentPreviewCameraState().targetActorInstanceKey;

        private bool ShouldUseTimelinePlayheadForPreviewSampling() =>
            _timelineIsPlaying
            || _isTimelinePlayheadDragging
            || _previewCameraSampleState != null
            || _selectionKind == StageSelectionKind.Camera;

        private void SetPreviewCameraSampleState(StoryCameraStateData state)
        {
            _previewCameraSampleState = state?.ShallowClone();
        }

        private void ClearPreviewCameraSampleState()
        {
            _previewCameraSampleState = null;
        }

        private static void ApplyActorFocusStyle(VisualElement el, StoryActorStateData data)
        {
            float focusBlend = StoryTransitionSampler.ResolveFocusBlend(data.EffectiveFocusAlpha);
            el.style.unityBackgroundImageTintColor = new StyleColor(Color.Lerp(new Color(0.5f, 0.5f, 0.5f), Color.white, focusBlend));
            el.style.opacity = data.EffectiveFocusAlpha;
        }

        // ── 액터 배치 (Stage World 좌표계) ────────────

        /// <summary>
        /// actor element 를 stage world 좌표에 배치한다.
        /// stageLocalPosition(0,0) = 패널 중앙, 단위 = world units.
        /// </summary>
        private void PositionActorElement(VisualElement el, StoryActorStateData data)
        {
            Rect rect = GetActorWorldRect(data);
            el.style.width  = rect.width;
            el.style.height = rect.height;
            el.style.left   = rect.x;
            el.style.top    = rect.y;
        }

        private Rect GetActorWorldRect(StoryActorStateData data)
        {
            StoryActorStateData sample = data.ShallowClone();
            if (ShouldApplyCameraFocusToRenderedPreview())
                sample.stageLocalPosition -= ResolvePreviewCameraFocusOffset();
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();
            return StoryStageVisualSizing.CalculateActorPreviewRect(
                sample,
                StoryStageVisualSizing.ResolveActorSprite(sample),
                new Vector2(DefaultUnitPixels, camH),
                ResolvePreviewCameraWorldWidth());
        }

        private Vector2 CalculateActorVisualSize(StoryActorStateData data)
        {
            StoryActorStateData sample = data.ShallowClone();
            if (ShouldApplyCameraFocusToRenderedPreview())
                sample.stageLocalPosition -= ResolvePreviewCameraFocusOffset();
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();
            return StoryStageVisualSizing.CalculateActorPreviewSize(
                sample,
                StoryStageVisualSizing.ResolveActorSprite(sample),
                new Vector2(DefaultUnitPixels, camH),
                ResolvePreviewCameraWorldWidth());
        }

        private Vector2 ResolvePreviewCameraFocusOffset()
        {
            StoryCameraStateData cameraState = ResolveCurrentPreviewCameraState();
            return cameraState.stageLocalPosition + ResolvePreviewCameraTargetContribution();
        }

        private static bool TryGetPreviewCameraTargetKeys(
            StoryCameraTrackData track,
            float time,
            out StoryActorKeyframeData previous,
            out StoryActorKeyframeData current)
        {
            previous = null;
            current = null;
            if (track == null || track.keyframes == null)
                return false;

            var keys = new List<StoryActorKeyframeData>();
            foreach (StoryActorKeyframeData keyframe in track.keyframes)
            {
                if (keyframe != null && keyframe.property == StoryActorKeyframeProperty.CameraTarget)
                    keys.Add(keyframe);
            }

            keys.Sort((a, b) => StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b)));
            if (keys.Count == 0)
                return false;

            for (int i = 0; i < keys.Count; i++)
            {
                float keyTime = StoryTransitionSampler.GetKeyTime(keys[i]);
                if (keyTime <= time)
                {
                    current = keys[i];
                    if (i > 0)
                        previous = keys[i - 1];
                }
                else
                {
                    break;
                }
            }

            return current != null;
        }

        private float ResolvePreviewCameraTargetX(string actorKey, StoryCameraFollowMode followMode, Vector2 snapshotPosition)
        {
            if (followMode == StoryCameraFollowMode.SnapshotPosition)
                return snapshotPosition.x;

            if (string.IsNullOrWhiteSpace(actorKey)
                || !_stageState.TryGetValue(actorKey, out StoryActorStateData focusState)
                || focusState == null
                || !focusState.visible)
                return float.MinValue;

            return focusState.stageLocalPosition.x;
        }

        private StoryCameraStateData ResolveCurrentPreviewCameraState()
        {
            if (_previewCameraSampleState != null)
                return _previewCameraSampleState.ShallowClone();

            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StoryCameraTrackData track = _isTransitionPreviewing ? _transitionCameraTrack : layout?.CameraTrackEditable;
            float time = _isTransitionPreviewing
                ? _transitionPreviewElapsed
                : ShouldUseTimelinePlayheadForPreviewSampling()
                    ? _timelinePlayheadTime
                    : 0f;
            return StoryTransitionSampler.SampleCameraTrackAtTime(track, "", time);
        }

        private Vector2 ResolvePreviewCameraTargetContribution()
        {
            StoryCameraTrackData track = _isTransitionPreviewing ? _transitionCameraTrack : FindCurrentStageLayout()?.CameraTrackEditable;
            float time = _isTransitionPreviewing
                ? _transitionPreviewElapsed
                : ShouldUseTimelinePlayheadForPreviewSampling()
                    ? _timelinePlayheadTime
                    : 0f;
            return StoryTransitionSampler.SampleCameraTargetContribution(track, time, key =>
            {
                if (string.IsNullOrWhiteSpace(key) || !_stageState.TryGetValue(key, out var s) || s == null)
                    return null;
                return s.stageLocalPosition;
            });
        }

        private float ResolveBasePreviewCameraWorldWidth()
        {
            if (_previewCameraInitSettings != null)
                return _previewCameraInitSettings.GetBaseCameraWorldWidth(GetStoryVisibleAspect());
            return StoryStageVisualSizing.DefaultCameraWorldWidth;
        }

        private float ResolvePreviewCameraWorldWidth()
        {
            // StageAuthoring: camera zoom only affects the gizmo rect, not actor/background sizing.
            float baseWidth = ResolveBasePreviewCameraWorldWidth();
            if (IsStageAuthoringMode)
                return baseWidth;

            StoryCameraStateData cameraState = ResolveCurrentPreviewCameraState();
            return baseWidth / Mathf.Max(0.01f, cameraState.zoom);
        }

        private bool ShouldApplyCameraFocusToRenderedPreview() =>
            _isTransitionPreviewing || IsRuntimePreviewMode;

        private void SetEmptyStageVisible(bool visible)
        {
            if (_emptyStageLabel != null)
                _emptyStageLabel.style.display = visible && IsStageAuthoringMode ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Transition-frame fast path: updates only the style properties of existing actor
        /// VisualElements without destroying or recreating DOM nodes.
        /// Falls back to a full RebuildActorLayer when the visible actor set has changed
        /// (e.g., actor enters or exits stage).
        /// </summary>
        internal void UpdateActorLayerPositions()
        {
            // Background has only one element — cheap to recreate, no special fast path needed.
            RefreshBackgroundLayer();

            if (_actorLayer == null) return;

            // Detect whether the visible actor set matches _actorElements.
            // A mismatch (enter/exit) requires a full rebuild.
            bool needsRebuild = false;
            foreach (var kvp in _stageState)
            {
                bool shouldShow = kvp.Value != null && kvp.Value.visible && kvp.Value.actor != null;
                bool isShowing  = _actorElements.ContainsKey(kvp.Key);
                if (shouldShow != isShowing) { needsRebuild = true; break; }
            }

            if (needsRebuild)
            {
                RebuildActorLayer(refreshInspectorLists: false);
                UpdateLetterboxOverlay();
                return;
            }

            // Fast path: move existing elements in-place.
            foreach (var kvp in _actorElements)
            {
                if (_stageState.TryGetValue(kvp.Key, out var data) && data is { visible: true, actor: not null })
                {
                    kvp.Value.style.display = DisplayStyle.Flex;
                    Sprite sprite = StoryStageVisualSizing.ResolveActorSprite(data);
                    if (sprite != null)
                        kvp.Value.style.backgroundImage = new StyleBackground(sprite);
                    ApplyActorFocusStyle(kvp.Value, data);
                    PositionActorElement(kvp.Value, data);
                }
                else
                {
                    kvp.Value.style.display = DisplayStyle.None;
                }
            }

            SetEmptyStageVisible(_actorElements.Count == 0);
            UpdateCameraGizmoVisual();
            UpdateLetterboxOverlay();
        }

        // ── 카메라 Gizmo fast-path 업데이트 ──────────

        /// <summary>
        /// DOM rebuild 없이 카메라 gizmo 위치/크기/선택 강조를 in-place 갱신한다.
        /// </summary>
        internal void UpdateCameraGizmoVisual()
        {
            UpdateCameraGizmoVisual(ResolveCurrentPreviewCameraState());
        }

        private void UpdateCameraGizmoVisual(StoryCameraStateData cameraState)
        {
            if (_cameraGizmoElement == null || !IsStageAuthoringMode) return;
            cameraState ??= new StoryCameraStateData();
            float panelW = DefaultUnitPixels;
            float panelH = panelW / Mathf.Max(0.0001f, GetStoryVisibleAspect());
            float safeZoom = Mathf.Max(0.01f, cameraState.zoom);
            float viewW = panelW / safeZoom;
            float viewH = panelH / safeZoom;
            Vector2 sp = cameraState.stageLocalPosition + ResolvePreviewCameraTargetContribution();
            float pixelsPerWorld = panelW / ResolveBasePreviewCameraWorldWidth();
            float cx = panelW * 0.5f + sp.x * pixelsPerWorld;
            float cy = panelH * 0.5f - sp.y * pixelsPerWorld;

            _cameraGizmoElement.style.left   = cx - viewW * 0.5f;
            _cameraGizmoElement.style.top    = cy - viewH * 0.5f;
            _cameraGizmoElement.style.width  = viewW;
            _cameraGizmoElement.style.height = viewH;

            bool isSelected = _selectionKind == StageSelectionKind.Camera;
            Color borderColor = isSelected
                ? new Color(1f, 0.76f, 0.22f, 1f)
                : new Color(1f, 0.76f, 0.22f, 0.45f);
            float borderW = isSelected ? 2f : 1f;
            var sc = new StyleColor(borderColor);
            _cameraGizmoElement.style.borderTopWidth    = borderW;
            _cameraGizmoElement.style.borderRightWidth  = borderW;
            _cameraGizmoElement.style.borderBottomWidth = borderW;
            _cameraGizmoElement.style.borderLeftWidth   = borderW;
            _cameraGizmoElement.style.borderTopColor    = sc;
            _cameraGizmoElement.style.borderRightColor  = sc;
            _cameraGizmoElement.style.borderBottomColor = sc;
            _cameraGizmoElement.style.borderLeftColor   = sc;
            _cameraGizmoElement.style.backgroundColor = new StyleColor(Color.clear);

            if (_cameraGizmoTitleBar != null)
            {
                _cameraGizmoTitleBar.style.height = Mathf.Min(CameraGizmoTitleBarHeight, viewH);
                _cameraGizmoTitleBar.style.backgroundColor = new StyleColor(
                    isSelected
                        ? new Color(1f, 0.76f, 0.22f, 0.18f)
                        : new Color(1f, 0.76f, 0.22f, 0.10f));
            }

            if (_cameraGizmoInfoLabel != null)
                _cameraGizmoInfoLabel.text = $"CAM  pos ({sp.x:F2},{sp.y:F2})  z {safeZoom:F2}";
        }

        private bool IsCameraGizmoDragPointer(int pointerId) =>
            _isDraggingCamera && _cameraDragPointerId == pointerId;

        private bool IsPointerTargetWithinActor(object target)
        {
            if (target is not VisualElement targetElement)
                return false;

            foreach (VisualElement actorElement in _actorElements.Values)
            {
                for (VisualElement current = targetElement; current != null; current = current.parent)
                {
                    if (current == actorElement)
                        return true;
                }
            }

            return false;
        }

        private bool IsPanelPositionInsideElement(VisualElement element, Vector2 panelPosition)
        {
            if (element == null)
                return false;

            Rect bounds = element.worldBound;
            return bounds.width > 0f
                && bounds.height > 0f
                && bounds.Contains(panelPosition);
        }

        private bool IsCameraGizmoHandleHit(Vector2 panelPosition)
        {
            if (!IsStageAuthoringMode || _cameraGizmoElement == null)
                return false;

            return IsPanelPositionInsideElement(_cameraGizmoFillHit, panelPosition)
                || IsPanelPositionInsideElement(_cameraGizmoTitleBar, panelPosition)
                || IsPanelPositionInsideElement(_cameraGizmoTopHit, panelPosition)
                || IsPanelPositionInsideElement(_cameraGizmoLeftHit, panelPosition)
                || IsPanelPositionInsideElement(_cameraGizmoRightHit, panelPosition)
                || IsPanelPositionInsideElement(_cameraGizmoBottomHit, panelPosition);
        }

        private Vector2 CalculateDraggedCameraStagePosition(Vector2 panelPosition)
        {
            float pixelsPerWorld = DefaultUnitPixels / ResolveBasePreviewCameraWorldWidth();
            Vector2 panelDelta = (panelPosition - _cameraDragStartPanelPos) / _stageZoom;
            return new Vector2(
                _cameraDragStartStagePos.x + panelDelta.x / pixelsPerWorld,
                _cameraDragStartStagePos.y - panelDelta.y / pixelsPerWorld);
        }

        private bool CanStartCameraGizmoDragForCurrentSelection()
        {
            if (!IsStageAuthoringMode)
                return false;

            if (_timelineRecordEnabled && _timelineRecordSelectionKind != StageSelectionKind.Camera)
                return false;

            if (HasTimelineMultiSelection || _selectedTimelineSegmentKeyIndex >= 0)
                return false;

            return _selectedTimelineKeyIndex < 0
                || _selectedTimelineProperty == StoryActorKeyframeProperty.CameraOffset;
        }

        private bool TryBeginCameraGizmoDrag(VisualElement captureElement, int pointerId, Vector2 panelPosition)
        {
            if (!IsStageAuthoringMode
                || captureElement == null
                || _draggingActorKey != null
                || _scalingActorKey != null)
                return false;

            _cameraPreviewSampleBeforeDrag = _previewCameraSampleState?.ShallowClone();
            _isDraggingCamera = true;
            _cameraDragPointerId = pointerId;
            _cameraDragStartPanelPos = panelPosition;
            _cameraDragExceededClickThreshold = false;

            StoryCameraStateData state = ResolveCurrentPreviewCameraState();
            _cameraDragStartStagePos = state?.stageLocalPosition ?? Vector2.zero;
            captureElement.CapturePointer(pointerId);
            return true;
        }

        private void HandleCameraGizmoPointerDown(VisualElement handle, PointerDownEvent e)
        {
            if (!IsStageAuthoringMode || e.button != 0)
                return;

            // Camera gizmo hit click must never fall through to empty-click clear.
            e.StopPropagation();

            if (_timelineIsPlaying)
                StopTimelinePlayback();

            SetInteractionContext(InteractionContext.Stage);
            FocusStageWorkspace();
            bool wasAlreadyCamera = _selectionKind == StageSelectionKind.Camera;
            SelectCamera();
            if (_selectionKind != StageSelectionKind.Camera)
                return;

            // Selection changed on this gesture: click selects only. Next gesture may drag.
            if (!wasAlreadyCamera)
                return;

            if (!CanStartCameraGizmoDragForCurrentSelection())
                return;

            TryBeginCameraGizmoDrag(handle, e.pointerId, e.position);
        }

        private void RestorePreviewCameraSampleStateAfterCameraDrag()
        {
            if (_cameraPreviewSampleBeforeDrag != null)
                SetPreviewCameraSampleState(_cameraPreviewSampleBeforeDrag);
            else
                ClearPreviewCameraSampleState();

            _cameraPreviewSampleBeforeDrag = null;
        }

        private void UpdateCameraGizmoDragVisual(Vector2 panelPosition)
        {
            if (_cameraGizmoElement == null)
                return;

            if (!_cameraDragExceededClickThreshold)
            {
                Vector2 panelDelta = panelPosition - _cameraDragStartPanelPos;
                if (panelDelta.sqrMagnitude <= CameraGizmoClickThresholdPixels * CameraGizmoClickThresholdPixels)
                    return;

                _cameraDragExceededClickThreshold = true;
            }

            Vector2 newStagePos = CalculateDraggedCameraStagePosition(panelPosition);
            float panelW = DefaultUnitPixels;
            float panelH = panelW / Mathf.Max(0.0001f, GetStoryVisibleAspect());
            float pixelsPerWorld = panelW / ResolveBasePreviewCameraWorldWidth();
            float safeZoom = Mathf.Max(0.01f, ResolveCurrentPreviewCameraState()?.zoom ?? 1f);
            float viewW = panelW / safeZoom;
            float viewH = panelH / safeZoom;
            Vector2 targetContrib = ResolvePreviewCameraTargetContribution();
            float cx = panelW * 0.5f + (newStagePos.x + targetContrib.x) * pixelsPerWorld;
            float cy = panelH * 0.5f - (newStagePos.y + targetContrib.y) * pixelsPerWorld;
            _cameraGizmoElement.style.left = cx - viewW * 0.5f;
            _cameraGizmoElement.style.top = cy - viewH * 0.5f;

            // Update background parallax preview to follow gizmo during drag.
            StoryCameraStateData dragState = (ResolveCurrentPreviewCameraState() ?? new StoryCameraStateData()).ShallowClone();
            dragState.stageLocalPosition = newStagePos;
            SetPreviewCameraSampleState(dragState);
            RefreshBackgroundLayer();
        }

        private void EndCameraGizmoDrag(VisualElement captureElement, int pointerId, Vector2 panelPosition)
        {
            if (captureElement != null && captureElement.HasPointerCapture(pointerId))
                captureElement.ReleasePointer(pointerId);

            bool hadRealDrag = _cameraDragExceededClickThreshold;
            Vector2 finalStagePos = hadRealDrag
                ? CalculateDraggedCameraStagePosition(panelPosition)
                : _cameraDragStartStagePos;
            _isDraggingCamera = false;
            _cameraDragPointerId = -1;
            _cameraDragExceededClickThreshold = false;

            if (!hadRealDrag)
            {
                RestorePreviewCameraSampleStateAfterCameraDrag();
                UpdateCameraGizmoVisual();
                RefreshBackgroundLayer();
                return;
            }

            _cameraPreviewSampleBeforeDrag = null;

            if (_timelineRecordEnabled && _selectionKind == StageSelectionKind.Camera)
            {
                var recordState = new StoryCameraStateData { stageLocalPosition = finalStagePos };
                AddOrUpdateCameraKey(recordState, StoryActorKeyframeProperty.CameraOffset,
                    _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
                ApplyTimelinePlayheadSample();
            }
            else if (!TryApplySelectedTimelineKeyFromCamera(finalStagePos))
            {
                ClearPreviewCameraSampleState();
                SaveCameraStateToCurrent(cam => cam.stageLocalPosition = finalStagePos);
                UpdateCameraGizmoVisual();
                RefreshBackgroundLayer();
                RefreshFocusPreviewGuide();
                Repaint();
            }

            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private void CancelCameraGizmoDrag(VisualElement captureElement, int pointerId)
        {
            if (captureElement != null && captureElement.HasPointerCapture(pointerId))
                captureElement.ReleasePointer(pointerId);

            _isDraggingCamera = false;
            _cameraDragPointerId = -1;
            _cameraDragExceededClickThreshold = false;
            RestorePreviewCameraSampleStateAfterCameraDrag();
            UpdateCameraGizmoVisual();
            RefreshBackgroundLayer();
        }

        // ── 드래그 인터랙션 ───────────────────────────

        private void RegisterActorInteraction(VisualElement el, string actorKey, StoryActorStateData data)
        {
            if (!IsStageAuthoringMode)
                return;

            el.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode) return;
                if (e.button != 0) return;
                if (IsActorRecordSelectionLocked(actorKey))
                    return;
                if (BlocksActorManipulationBySelectedKey(actorKey, StoryActorKeyframeProperty.Position))
                    return;
                SetInteractionContext(InteractionContext.Stage);
                FocusStageWorkspace();
                if (_timelineIsPlaying) StopTimelinePlayback();
                if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentData))
                    data = currentData;
                SelectActor(actorKey);
                _draggingActorKey  = actorKey;
                _dragStartPanelPos = e.position;
                _dragStartNormPos  = data.stageLocalPosition;
                _dragAxisLock = DragAxisLock.None;
                el.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!IsStageAuthoringMode) return;
                if (_draggingActorKey != actorKey) return;

                float camW = DefaultUnitPixels;
                float camH = DefaultUnitPixels / GetStoryVisibleAspect();

                // panel 공간 delta → stagePosition delta (zoom 보정, -1..1 범위)
                Vector2 panelDelta = (Vector2)e.position - _dragStartPanelPos;
                Vector2 worldDelta = panelDelta / _stageZoom;
                worldDelta = ApplyAxisLock(worldDelta, e.shiftKey, ref _dragAxisLock);

                float worldUnitsPerPixel = ResolvePreviewCameraWorldWidth() / camW;
                data.stageLocalPosition = new Vector2(
                    _dragStartNormPos.x + worldDelta.x * worldUnitsPerPixel,
                    _dragStartNormPos.y - worldDelta.y * worldUnitsPerPixel);  // Y 반전

                PositionActorElement(el, data);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_draggingActorKey != actorKey) return;
                _draggingActorKey = null;
                _dragAxisLock = DragAxisLock.None;
                el.ReleasePointer(e.pointerId);
                if (!TryApplySelectedTimelineKeyFromState(actorKey, data, StoryActorKeyframeProperty.Position))
                {
                    SaveActorStateToCurrent(actorKey, entry => entry.stageLocalPosition = data.stageLocalPosition, saveNow: true);
                    RecordActorKeyframeFromState(actorKey, data, includePosition: true, includeScale: false);
                }
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private void AddActorScaleHandles(VisualElement el, string actorKey, StoryActorStateData data)
        {
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.TopLeft, -5f, -5f, null, null);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.TopRight, null, -5f, -5f, null);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.BottomLeft, -5f, null, null, -5f);
            AddActorScaleHandle(el, actorKey, data, ActorScaleHandle.BottomRight, null, null, -5f, -5f);
        }

        private void AddActorScaleHandle(
            VisualElement el,
            string actorKey,
            StoryActorStateData data,
            ActorScaleHandle handleType,
            float? left,
            float? top,
            float? right,
            float? bottom)
        {
            var handle = new VisualElement
            {
                name = $"ScaleHandle_{handleType}",
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    width = 10,
                    height = 10,
                    backgroundColor = new StyleColor(new Color(1f, 0.76f, 0.22f, 0.96f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderRightColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderBottomColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f)),
                    borderLeftColor = new StyleColor(new Color(0.08f, 0.07f, 0.04f))
                }
            };

            if (left.HasValue) handle.style.left = left.Value;
            if (top.HasValue) handle.style.top = top.Value;
            if (right.HasValue) handle.style.right = right.Value;
            if (bottom.HasValue) handle.style.bottom = bottom.Value;

            RegisterActorScaleHandleInteraction(handle, el, actorKey, data, handleType);
            el.Add(handle);
        }

        private void RegisterActorScaleHandleInteraction(
            VisualElement handle,
            VisualElement actorElement,
            string actorKey,
            StoryActorStateData data,
            ActorScaleHandle handleType)
        {
            handle.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                if (IsActorRecordSelectionLocked(actorKey))
                    return;
                if (BlocksActorManipulationBySelectedKey(actorKey, StoryActorKeyframeProperty.Scale))
                    return;
                SetInteractionContext(InteractionContext.Stage);
                FocusStageWorkspace();
                if (_timelineIsPlaying) StopTimelinePlayback();
                if (_stageState.TryGetValue(actorKey, out StoryActorStateData currentData))
                    data = currentData;

                SelectActor(actorKey);
                _scalingActorKey = actorKey;
                _activeScaleHandle = handleType;
                _scaleStartPanelPos = e.position;
                _scaleStartNormPos = data.stageLocalPosition;
                _scaleStartScale = new Vector2(data.scaleMultiplier, data.scaleMultiplier);
                _scaleStartRect = GetActorWorldRect(data);
                _scaleAxisLock = DragAxisLock.None;

                handle.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!IsStageAuthoringMode
                    || _scalingActorKey != actorKey
                    || _activeScaleHandle != handleType)
                    return;

                Vector2 worldDelta = ((Vector2)e.position - _scaleStartPanelPos) / _stageZoom;
                worldDelta = ApplyAxisLock(worldDelta, e.shiftKey, ref _scaleAxisLock);
                ApplyActorScaleDrag(data, handleType, worldDelta, e.altKey && !e.ctrlKey, e.ctrlKey);
                PositionActorElement(actorElement, data);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_scalingActorKey != actorKey || _activeScaleHandle != handleType)
                    return;

                _scalingActorKey = null;
                _activeScaleHandle = ActorScaleHandle.None;
                _scaleAxisLock = DragAxisLock.None;
                handle.ReleasePointer(e.pointerId);
                if (!TryApplySelectedTimelineKeyFromState(actorKey, data, StoryActorKeyframeProperty.Scale))
                {
                    SaveActorStateToCurrent(actorKey, entry =>
                    {
                        entry.stageLocalPosition = data.stageLocalPosition;
                        entry.scaleMultiplier = data.scaleMultiplier;
                    }, saveNow: true);
                    RecordActorKeyframeFromState(actorKey, data, includePosition: false, includeScale: true);
                }
                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private void ApplyActorScaleDrag(
            StoryActorStateData data,
            ActorScaleHandle handleType,
            Vector2 worldDelta,
            bool scaleFromCenter,
            bool keepOppositeCornerFixed)
        {
            data.stageLocalPosition = _scaleStartNormPos;
            data.scaleMultiplier = _scaleStartScale.y;

            float widthDelta = handleType is ActorScaleHandle.TopLeft or ActorScaleHandle.BottomLeft
                ? -worldDelta.x
                : worldDelta.x;
            float heightDelta = handleType is ActorScaleHandle.TopLeft or ActorScaleHandle.TopRight
                ? -worldDelta.y
                : worldDelta.y;

            float multiplier = scaleFromCenter ? 2f : 1f;
            Vector2 targetSize = new(
                Mathf.Max(12f, _scaleStartRect.width + widthDelta * multiplier),
                Mathf.Max(12f, _scaleStartRect.height + heightDelta * multiplier));

            if (_scaleAxisLock == DragAxisLock.X)
                targetSize.y = _scaleStartRect.height;
            else if (_scaleAxisLock == DragAxisLock.Y)
                targetSize.x = _scaleStartRect.width;

            data.scaleMultiplier = ResolveScaleMultiplierForVisualHeight(data, targetSize.y);
            Rect targetRect = ResolveScaledRect(handleType, _scaleStartRect, targetSize, scaleFromCenter, keepOppositeCornerFixed);

            Vector2 actualSize = CalculateActorVisualSize(data);
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();

            float centerX = targetRect.x + actualSize.x * 0.5f;
            float centerY = targetRect.y + actualSize.y * 0.5f;
            float wuPerPixel = ResolvePreviewCameraWorldWidth() / camW;
            data.stageLocalPosition = new Vector2(
                (centerX - camW * 0.5f) * wuPerPixel,
                (camH * 0.5f - centerY) * wuPerPixel);
        }

        private float ResolveScaleMultiplierForVisualHeight(StoryActorStateData data, float targetHeight)
        {
            StoryActorStateData baseSample = data.ShallowClone();
            baseSample.scaleMultiplier = 1f;
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();
            Vector2 baseSize = StoryStageVisualSizing.CalculateActorPreviewSize(
                baseSample,
                StoryStageVisualSizing.ResolveActorSprite(baseSample),
                new Vector2(DefaultUnitPixels, camH));
            return Mathf.Max(0.001f, targetHeight / Mathf.Max(0.0001f, baseSize.y));
        }

        private static Rect ResolveScaledRect(
            ActorScaleHandle handleType,
            Rect startRect,
            Vector2 targetSize,
            bool scaleFromCenter,
            bool keepOppositeCornerFixed)
        {
            if (scaleFromCenter)
            {
                Vector2 center = startRect.center;
                return new Rect(center.x - targetSize.x * 0.5f, center.y - targetSize.y * 0.5f, targetSize.x, targetSize.y);
            }

            // Ctrl explicitly pins the corner opposite to the dragged handle.
            // The non-center fallback uses the same stable behavior for now.
            return handleType switch
            {
                ActorScaleHandle.TopLeft => new Rect(startRect.xMax - targetSize.x, startRect.yMax - targetSize.y, targetSize.x, targetSize.y),
                ActorScaleHandle.TopRight => new Rect(startRect.xMin, startRect.yMax - targetSize.y, targetSize.x, targetSize.y),
                ActorScaleHandle.BottomLeft => new Rect(startRect.xMax - targetSize.x, startRect.yMin, targetSize.x, targetSize.y),
                _ => new Rect(startRect.xMin, startRect.yMin, targetSize.x, targetSize.y)
            };
        }

        private static Vector2 ApplyAxisLock(Vector2 delta, bool shiftKey, ref DragAxisLock axisLock)
        {
            if (!shiftKey)
            {
                axisLock = DragAxisLock.None;
                return delta;
            }

            if (axisLock == DragAxisLock.None)
                axisLock = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? DragAxisLock.X : DragAxisLock.Y;

            return axisLock == DragAxisLock.X
                ? new Vector2(delta.x, 0f)
                : new Vector2(0f, delta.y);
        }

        private void PositionSelectedActorElement()
        {
            if (_selectionKind != StageSelectionKind.Actor
                || string.IsNullOrWhiteSpace(_selectedActorKey)
                || !_stageState.TryGetValue(_selectedActorKey, out var data)
                || !_actorElements.TryGetValue(_selectedActorKey, out var el))
                return;

            PositionActorElement(el, data);
        }

        private void HighlightSelectedActor()
        {
            foreach (var kvp in _actorElements)
            {
                bool  sel = IsStageAuthoringMode && _selectionKind == StageSelectionKind.Actor && kvp.Key == _selectedActorKey;
                float bw  = sel ? SelectionBorderWidth : 0f;
                kvp.Value.style.borderTopWidth    = kvp.Value.style.borderRightWidth    =
                kvp.Value.style.borderBottomWidth = kvp.Value.style.borderLeftWidth = bw;
                kvp.Value.style.borderTopColor    = kvp.Value.style.borderRightColor    =
                kvp.Value.style.borderBottomColor = kvp.Value.style.borderLeftColor =
                    new StyleColor(SelectionBorderColor);
            }
        }

        // ── 카메라 Gizmo ─────────────────────────────

        /// <summary>
        /// Stage Authoring 모드에서 카메라 stagePosition / zoom 을 시각화하는 rect 오버레이.
        /// Camera gizmo layer는 background 위, actor layer 아래에 배치되어 actor picking 우선순위를 유지한다.
        /// stagePosition (0,0) + zoom 1 → 전체 스테이지 프레임.
        /// </summary>
        private VisualElement BuildCameraGizmoElement()
        {
            StoryCameraStateData cameraState = ResolveCurrentPreviewCameraState();
            float storyAspect = GetStoryVisibleAspect();
            float panelW = DefaultUnitPixels;
            float panelH = panelW / Mathf.Max(0.0001f, storyAspect);

            float safeZoom = Mathf.Max(0.01f, cameraState.zoom);
            float viewW = panelW / safeZoom;
            float viewH = panelH / safeZoom;

            Vector2 sp = cameraState.stageLocalPosition + ResolvePreviewCameraTargetContribution();
            float pixelsPerWorld = panelW / ResolveBasePreviewCameraWorldWidth();
            float centerX = panelW * 0.5f + sp.x * pixelsPerWorld;
            float centerY = panelH * 0.5f - sp.y * pixelsPerWorld;

            float left = centerX - viewW * 0.5f;
            float top  = centerY - viewH * 0.5f;

            bool isSelected = _selectionKind == StageSelectionKind.Camera;
            Color borderColor = isSelected
                ? new Color(1f, 0.76f, 0.22f, 1f)
                : new Color(1f, 0.76f, 0.22f, 0.45f);
            float borderW = isSelected ? 2f : 1f;

            VisualElement el = new VisualElement();
            el.name        = "CameraGizmo";
            el.pickingMode = PickingMode.Ignore;
            el.style.position          = Position.Absolute;
            el.style.left              = left;
            el.style.top               = top;
            el.style.width             = viewW;
            el.style.height            = viewH;
            el.style.borderTopWidth    = el.style.borderRightWidth =
            el.style.borderBottomWidth = el.style.borderLeftWidth  = borderW;
            var gizmoBorderSC = new StyleColor(borderColor);
            el.style.borderTopColor    = el.style.borderRightColor =
            el.style.borderBottomColor = el.style.borderLeftColor  = gizmoBorderSC;
            el.style.backgroundColor   = new StyleColor(Color.clear);
            el.style.overflow          = Overflow.Visible;

            _cameraGizmoTitleBar = new VisualElement
            {
                pickingMode = PickingMode.Position,
                style =
                {
                    position  = Position.Absolute,
                    left      = 0,
                    top       = 0,
                    right     = 0,
                    height    = Mathf.Min(CameraGizmoTitleBarHeight, viewH),
                    paddingLeft = 6,
                    paddingRight = 6,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    backgroundColor = new StyleColor(
                        isSelected
                            ? new Color(1f, 0.76f, 0.22f, 0.18f)
                            : new Color(1f, 0.76f, 0.22f, 0.10f))
                }
            };

            _cameraGizmoInfoLabel = new Label($"CAM  pos ({sp.x:F2},{sp.y:F2})  z {safeZoom:F2}")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    fontSize  = 8,
                    color     = new StyleColor(new Color(1f, 0.85f, 0.4f, 0.9f)),
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            _cameraGizmoTitleBar.Add(_cameraGizmoInfoLabel);

            _cameraGizmoFillHit = new VisualElement
            {
                name = "CameraGizmoFillHit",
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    left = 0, top = 0, right = 0, bottom = 0,
                    backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.001f))
                }
            };
            el.Insert(0, _cameraGizmoFillHit);
            el.Add(_cameraGizmoTitleBar);

            _cameraGizmoTopHit = CreateCameraGizmoHitTarget("CameraGizmoTopHit");
            _cameraGizmoTopHit.style.left = 0;
            _cameraGizmoTopHit.style.right = 0;
            _cameraGizmoTopHit.style.top = -CameraGizmoBorderHitThickness * 0.5f;
            _cameraGizmoTopHit.style.height = CameraGizmoBorderHitThickness;
            el.Add(_cameraGizmoTopHit);

            _cameraGizmoLeftHit = CreateCameraGizmoHitTarget("CameraGizmoLeftHit");
            _cameraGizmoLeftHit.style.left = -CameraGizmoBorderHitThickness * 0.5f;
            _cameraGizmoLeftHit.style.top = 0;
            _cameraGizmoLeftHit.style.bottom = 0;
            _cameraGizmoLeftHit.style.width = CameraGizmoBorderHitThickness;
            el.Add(_cameraGizmoLeftHit);

            _cameraGizmoRightHit = CreateCameraGizmoHitTarget("CameraGizmoRightHit");
            _cameraGizmoRightHit.style.right = -CameraGizmoBorderHitThickness * 0.5f;
            _cameraGizmoRightHit.style.top = 0;
            _cameraGizmoRightHit.style.bottom = 0;
            _cameraGizmoRightHit.style.width = CameraGizmoBorderHitThickness;
            el.Add(_cameraGizmoRightHit);

            _cameraGizmoBottomHit = CreateCameraGizmoHitTarget("CameraGizmoBottomHit");
            _cameraGizmoBottomHit.style.left = 0;
            _cameraGizmoBottomHit.style.right = 0;
            _cameraGizmoBottomHit.style.bottom = -CameraGizmoBorderHitThickness * 0.5f;
            _cameraGizmoBottomHit.style.height = CameraGizmoBorderHitThickness;
            el.Add(_cameraGizmoBottomHit);

            // 중심 십자선
            var crossH = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position  = Position.Absolute,
                    left      = new StyleLength(new Length(50f, LengthUnit.Percent)),
                    top       = new StyleLength(new Length(50f, LengthUnit.Percent)),
                    width     = 16,
                    height    = 1,
                    marginLeft = -8,
                    backgroundColor = new StyleColor(new Color(1f, 0.76f, 0.22f, 0.5f))
                }
            };
            var crossV = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position  = Position.Absolute,
                    left      = new StyleLength(new Length(50f, LengthUnit.Percent)),
                    top       = new StyleLength(new Length(50f, LengthUnit.Percent)),
                    width     = 1,
                    height    = 16,
                    marginTop = -8,
                    backgroundColor = new StyleColor(new Color(1f, 0.76f, 0.22f, 0.5f))
                }
            };
            el.Add(crossH);
            el.Add(crossV);

            RegisterCameraGizmoInteraction(_cameraGizmoFillHit);
            RegisterCameraGizmoInteraction(_cameraGizmoTitleBar);
            RegisterCameraGizmoInteraction(_cameraGizmoTopHit);
            RegisterCameraGizmoInteraction(_cameraGizmoLeftHit);
            RegisterCameraGizmoInteraction(_cameraGizmoRightHit);
            RegisterCameraGizmoInteraction(_cameraGizmoBottomHit);
            return el;
        }

        private static VisualElement CreateCameraGizmoHitTarget(string name)
        {
            return new VisualElement
            {
                name = name,
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.001f))
                }
            };
        }

        private void RegisterCameraGizmoInteraction(VisualElement handle)
        {
            if (handle == null)
                return;

            handle.RegisterCallback<PointerDownEvent>(e => HandleCameraGizmoPointerDown(handle, e));

            handle.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!IsCameraGizmoDragPointer(e.pointerId))
                    return;

                UpdateCameraGizmoDragVisual(e.position);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!IsCameraGizmoDragPointer(e.pointerId))
                    return;

                EndCameraGizmoDrag(handle, e.pointerId, e.position);
                e.StopPropagation();
            });

            handle.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (!_isDraggingCamera)
                    return;

                CancelCameraGizmoDrag(handle, _cameraDragPointerId);
            });
        }
    }
}
