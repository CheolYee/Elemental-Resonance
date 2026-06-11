using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
    {
        private const float RowLabelWidth = 110f;
        private const float RowHeight     = 28f;
        private const float MarkerSize    = 10f;
        private const float RulerHeight   = 26f;

        private static readonly Color ColorAnimation    = new(0.38f, 0.62f, 1.00f);
        private static readonly Color ColorEffect       = new(0.95f, 0.62f, 0.30f);
        private static readonly Color ColorVfx          = new(0.38f, 0.82f, 0.42f);
        private static readonly Color ColorCameraPos    = new(0.95f, 0.72f, 0.28f);
        private static readonly Color ColorCameraRot    = new(0.90f, 0.62f, 0.22f);
        private static readonly Color ColorCameraZoom   = new(0.85f, 0.52f, 0.18f);
        private static readonly Color ColorCameraShake  = new(0.90f, 0.38f, 0.38f);
        private static readonly Color ColorUi           = new(0.72f, 0.42f, 0.95f);
        private static readonly Color ColorSfx          = new(0.40f, 0.82f, 0.95f);
        private static readonly Color ColorSelected     = new(1.00f, 0.85f, 0.20f);
        private static readonly Color ColorPlayhead     = new(1.00f, 0.38f, 0.22f, 0.95f);
        private static readonly Color ColorEndMarker    = new(0.95f, 0.90f, 0.40f);

        internal void BuildTimelinePlaceholder() => RefreshTimeline();

        internal void InitTimelinePanelBehavior()
        {
            if (TimelinePanel == null) return;
            TimelinePanel.focusable = true;
            TimelinePanel.RegisterCallback<KeyDownEvent>(OnTimelinePanelKeyDown);
            TimelinePanel.RegisterCallback<PointerMoveEvent>(OnTimelinePanelPointerMove);
            TimelinePanel.RegisterCallback<PointerUpEvent>(OnTimelinePanelPointerUp);
            TimelinePanel.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                CancelPlayheadDrag();
                CancelBoxSelection();
                bool wasDragging = _isKeyframeDragging;
                _isKeyframeDragPending = false;
                _isKeyframeDragging    = false;
                _keyframeDragSnapshots.Clear();
                _keyframeDragPointerId = -1;
                if (wasDragging) FinalizeKeyframeDrag();
            });
        }

        // ── 키보드 ───────────────────────────────────────────────────────────────

        private void OnTimelinePanelKeyDown(KeyDownEvent e)
        {
            bool ctrl = e.ctrlKey || e.commandKey;

            if (TryHandleUndoRedoShortcut(e)) { e.StopPropagation(); return; }

            if (ctrl && e.keyCode == KeyCode.C) { CopySelectedKeyframes(); e.StopPropagation(); return; }
            if (ctrl && e.keyCode == KeyCode.V) { PasteKeyframes(); e.StopPropagation(); return; }

            if (e.keyCode == KeyCode.Space)
            {
                if (_isPlaying) Pause(); else Play();
                e.StopPropagation();
                return;
            }

            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            {
                RemoveSelectedKeyframes();
                e.StopPropagation();
            }
        }

        // ── Playhead / 박스 선택 드래그 ──────────────────────────────────────────

        private void OnTimelinePanelPointerMove(PointerMoveEvent e)
        {
            if (IsPlayheadDragging && e.pointerId == PlayheadDragPointerId)
            {
                float deltaSeconds = (e.position.x - _dragStartScreenX) / Mathf.Max(1f, PixelsPerSecond);
                _currentTime = Mathf.Max(0f, _dragStartTime + deltaSeconds);
                UpdateTimeLabel();
                UpdatePlayheadPosition();
                Repaint();
                e.StopPropagation();
                return;
            }

            if ((_isKeyframeDragPending || _isKeyframeDragging) && e.pointerId == _keyframeDragPointerId)
            {
                HandleKeyframeDragMove(e);
                return;
            }

            if (IsBoxSelecting && e.pointerId == BoxSelectPointerId)
            {
                UpdateBoxSelection(e.position);
                e.StopPropagation();
            }
        }

        private void OnTimelinePanelPointerUp(PointerUpEvent e)
        {
            if (IsPlayheadDragging && e.pointerId == PlayheadDragPointerId)
            {
                CancelPlayheadDrag();
                RefreshTimeline();
                e.StopPropagation();
                return;
            }

            if ((_isKeyframeDragPending || _isKeyframeDragging) && e.pointerId == _keyframeDragPointerId)
            {
                bool wasDragging = _isKeyframeDragging;
                CancelKeyframeDrag();
                if (wasDragging) FinalizeKeyframeDrag();
                Repaint();
                e.StopPropagation();
                return;
            }

            if (IsBoxSelecting && e.pointerId == BoxSelectPointerId)
            {
                FinishBoxSelection(e.position);
                e.StopPropagation();
            }
        }

        private void CancelPlayheadDrag()
        {
            if (!IsPlayheadDragging) return;
            IsPlayheadDragging = false;
            if (PlayheadDragPointerId >= 0 && TimelinePanel != null
                && TimelinePanel.HasPointerCapture(PlayheadDragPointerId))
                TimelinePanel.ReleasePointer(PlayheadDragPointerId);
            PlayheadDragPointerId = -1;
        }

        private void UpdatePlayheadPosition()
        {
            float x = Mathf.Max(0f, TimeToPixel(_currentTime));
            foreach (var el in PlayheadElements)
                el.style.left = x;
        }

        // ── 박스 선택 ────────────────────────────────────────────────────────────

        private void StartBoxSelection(PointerDownEvent e)
        {
            IsBoxSelecting      = true;
            BoxSelectPointerId  = e.pointerId;
            BoxSelectStartWorld = e.position;
            TimelinePanel.CapturePointer(e.pointerId);

            BoxSelectionEl?.RemoveFromHierarchy();
            BoxSelectionEl = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style       =
                {
                    position          = Position.Absolute,
                    left = 0, top = 0, width = 0, height = 0,
                    backgroundColor   = new StyleColor(new Color(0.30f, 0.52f, 1f, 0.15f)),
                    borderTopWidth    = 1, borderRightWidth  = 1,
                    borderBottomWidth = 1, borderLeftWidth   = 1,
                    borderTopColor    = new StyleColor(new Color(0.50f, 0.72f, 1f, 0.80f)),
                    borderRightColor  = new StyleColor(new Color(0.50f, 0.72f, 1f, 0.80f)),
                    borderBottomColor = new StyleColor(new Color(0.50f, 0.72f, 1f, 0.80f)),
                    borderLeftColor   = new StyleColor(new Color(0.50f, 0.72f, 1f, 0.80f))
                }
            };
            TimelinePanel.Add(BoxSelectionEl);
        }

        private void UpdateBoxSelection(Vector2 worldPos)
        {
            if (BoxSelectionEl == null || TimelinePanel == null) return;
            Vector2 localA = TimelinePanel.WorldToLocal(BoxSelectStartWorld);
            Vector2 localB = TimelinePanel.WorldToLocal(worldPos);
            float minX = Mathf.Min(localA.x, localB.x), minY = Mathf.Min(localA.y, localB.y);
            float maxX = Mathf.Max(localA.x, localB.x), maxY = Mathf.Max(localA.y, localB.y);
            BoxSelectionEl.style.left   = minX; BoxSelectionEl.style.top    = minY;
            BoxSelectionEl.style.width  = maxX - minX; BoxSelectionEl.style.height = maxY - minY;
            Repaint();
        }

        private void FinishBoxSelection(Vector2 worldPos)
        {
            float minX = Mathf.Min(BoxSelectStartWorld.x, worldPos.x);
            float minY = Mathf.Min(BoxSelectStartWorld.y, worldPos.y);
            float maxX = Mathf.Max(BoxSelectStartWorld.x, worldPos.x);
            float maxY = Mathf.Max(BoxSelectStartWorld.y, worldPos.y);
            var worldRect = new Rect(minX, minY, maxX - minX, maxY - minY);

            bool wasDrag = worldRect.width > 4f || worldRect.height > 4f;
            if (wasDrag)
            {
                foreach (var pair in KeyframeMarkers)
                    if (pair.Key != null && pair.Value != null && worldRect.Overlaps(pair.Value.worldBound))
                        SelectedKeyframes.Add(pair.Key);
            }

            CancelBoxSelection();
            RefreshTimeline();
            RefreshInspector();
            Repaint();
        }

        private void CancelBoxSelection()
        {
            if (!IsBoxSelecting) return;
            IsBoxSelecting = false;
            BoxSelectionEl?.RemoveFromHierarchy();
            BoxSelectionEl = null;
            if (BoxSelectPointerId >= 0 && TimelinePanel != null
                && TimelinePanel.HasPointerCapture(BoxSelectPointerId))
                TimelinePanel.ReleasePointer(BoxSelectPointerId);
            BoxSelectPointerId = -1;
        }

        // ── Keyframe 드래그 ──────────────────────────────────────────────────────

        private void StartKeyframeDragPending(PointerDownEvent e, SkillKeyframeData anchorKey)
        {
            _isKeyframeDragPending       = true;
            _isKeyframeDragging          = false;
            _keyframeDragPointerId       = e.pointerId;
            _keyframeDragStartScreenX    = e.position.x;
            _keyframeDragAnchorStartTime = anchorKey?.timeSeconds ?? 0f;
            _keyframeDragSnapshots.Clear();
            TimelinePanel.CapturePointer(e.pointerId);
        }

        private void HandleKeyframeDragMove(PointerMoveEvent e)
        {
            float deltaPixels  = e.position.x - _keyframeDragStartScreenX;
            float deltaSeconds = deltaPixels / Mathf.Max(1f, PixelsPerSecond);

            if (_isKeyframeDragPending)
            {
                if (Mathf.Abs(deltaPixels) < 2f) { e.StopPropagation(); return; }
                _isKeyframeDragPending = false;
                _isKeyframeDragging    = true;
                Undo.RegisterCompleteObjectUndo(_target, SelectedKeyframes.Count > 1 ? "Move Keyframes" : "Move Keyframe");
                _keyframeDragSnapshots.Clear();
                foreach (var k in SelectedKeyframes)
                    _keyframeDragSnapshots.Add((k, k.timeSeconds));
                if (_keyframeDragSnapshots.Count == 0 && _selectedKeyframe != null)
                    _keyframeDragSnapshots.Add((_selectedKeyframe, _selectedKeyframe.timeSeconds));
            }

            float minStart = float.MaxValue;
            foreach (var (_, st) in _keyframeDragSnapshots)
                if (st < minStart) minStart = st;
            float clamped = ResolveDraggedDeltaSeconds(deltaSeconds, _keyframeDragAnchorStartTime, minStart);

            foreach (var (k, startTime) in _keyframeDragSnapshots)
            {
                k.timeSeconds = startTime + clamped;
                if (KeyframeMarkers.TryGetValue(k, out var markerEl))
                    markerEl.style.left = TimeToPixel(k.timeSeconds) - MarkerSize * 0.5f;
            }

            UpdateDraggedEasingLines();
            Repaint();
            e.StopPropagation();
        }

        private void CancelKeyframeDrag()
        {
            _isKeyframeDragPending = false;
            _isKeyframeDragging    = false;
            _keyframeDragAnchorStartTime = 0f;
            _keyframeDragSnapshots.Clear();
            if (_keyframeDragPointerId >= 0 && TimelinePanel != null
                && TimelinePanel.HasPointerCapture(_keyframeDragPointerId))
                TimelinePanel.ReleasePointer(_keyframeDragPointerId);
            _keyframeDragPointerId = -1;
        }

        private void FinalizeKeyframeDrag()
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            SortByTime(tl.animationTrack.keyframes);
            SortByTime(tl.effectTrack.keyframes);
            SortByTime(tl.cameraTrack.keyframes);
            SortByTime(tl.uiTrack.keyframes);
            SortByTime(tl.sfxTrack.keyframes);
            if (tl.vfxObjects != null)
                foreach (var vfxObj in tl.vfxObjects)
                    if (vfxObj?.keyframes != null) SortByTime(vfxObj.keyframes);
            EditorUtility.SetDirty(_target);
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
        }

        // ── Timeline 빌드 ────────────────────────────────────────────────────────

        internal void RefreshTimeline()
        {
            if (TimelinePanel == null) return;
            PlayheadElements.Clear();
            KeyframeMarkers.Clear();
            _easingLineContainers.Clear();
            TimelinePanel.Clear();

            // 컨트롤 바 유지 (Clear 후 재추가)
            if (_timelineControlBar != null)
                TimelinePanel.Add(_timelineControlBar);
            if (_addPropertyBtn != null)
                _addPropertyBtn.SetEnabled(_selectedObjectKind != SkillObjectKind.None
                    && _selectedObjectKind != SkillObjectKind.EndMarker && _target != null);

            if (_referenceCard == null) { TimelinePanel.Add(MakeTimelinePlaceholderLabel("CardDataSO를 선택하세요.")); return; }
            if (_target == null)        { TimelinePanel.Add(MakeTimelinePlaceholderLabel("선택한 CardDataSO에 presentationData가 없습니다.")); return; }

            if (_selectedObjectKind == SkillObjectKind.None)
            {
                UpdateObjectNameLabel();
                TimelinePanel.Add(MakeTimelinePlaceholderLabel("오브젝트를 선택하세요."));
                return;
            }

            var tl = GetEditableTimeline();
            if (tl == null) { TimelinePanel.Add(MakeTimelinePlaceholderLabel("Timeline을 가져올 수 없습니다.")); return; }

            float duration  = Mathf.Max(1f, GetDisplayDuration());
            float laneWidth = Mathf.Max(400f, TimeToPixel(duration) + 48f);

            UpdateObjectNameLabel();

            var scroll  = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { style = { flexGrow = 1 } };
            var content = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            scroll.Add(content);

            content.Add(BuildRulerRow(duration, laneWidth));

            bool anyRow = BuildObjectRows(content, tl, laneWidth, duration);
            if (!anyRow)
                content.Add(MakeTimelinePlaceholderLabel("타임라인에 프로퍼티를 추가해주세요."));

            TimelinePanel.Add(scroll);
        }

        // ── Add Property 바 (ScrollView 내부 상단 고정) ──────────────────────────

        private VisualElement BuildAddPropertyBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection     = FlexDirection.Row,
                    alignItems        = Align.Center,
                    flexShrink        = 0,
                    height            = 24,
                    paddingLeft       = 6,
                    paddingRight      = 6,
                    backgroundColor   = new StyleColor(new Color(0.10f, 0.10f, 0.12f)),
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f))
                }
            };
            var btn = new Button(ShowAddPropertyMenu)
            {
                text  = "+ Add Property",
                style = { height = 18, fontSize = 10 }
            };
            btn.SetEnabled(_selectedObjectKind != SkillObjectKind.None && _target != null);
            bar.Add(btn);
            return bar;
        }

        internal void UpdateObjectNameLabel()
        {
            if (_objectNameLabel == null) return;
            if (_selectedObjectKind == SkillObjectKind.None)
            {
                _objectNameLabel.text  = "—";
                _objectNameLabel.style.color = new StyleColor(new Color(0.40f, 0.40f, 0.45f));
                return;
            }
            string name = _selectedObjectKind == SkillObjectKind.Vfx
                ? $"VFX [{_selectedVfxIndex}]"
                : _selectedObjectKind.ToString();
            _objectNameLabel.text  = name;
            _objectNameLabel.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.90f));
        }

        // ── Add Property ──────────────────────────────────────────────────────────

        private void ShowAddPropertyMenu()
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            var menu = new GenericMenu();

            switch (_selectedObjectKind)
            {
                case SkillObjectKind.Animation:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.AnimParam);
                    break;
                case SkillObjectKind.Effect:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.EffectSlot);
                    break;
                case SkillObjectKind.Camera:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.CamPosition);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.CamRotation);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.CamZoom);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.CamShake);
                    break;
                case SkillObjectKind.Ui:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.UiAction);
                    break;
                case SkillObjectKind.Sfx:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.SfxId);
                    break;
                case SkillObjectKind.Vfx:
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.VfxActive);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.VfxPosition);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.VfxRotation);
                    AddPropertyMenuItem(menu, tl, SkillKeyframeProperty.VfxScale);
                    break;
                case SkillObjectKind.EndMarker:
                    // 마커는 AddObject 시 자동 생성 — 추가 프로퍼티 없음
                    break;
            }
            menu.ShowAsContext();
        }

        private void AddPropertyMenuItem(GenericMenu menu, SkillPresentationTimeline tl, SkillKeyframeProperty property)
        {
            bool exists = HasProperty(tl, _selectedObjectKind, _selectedVfxIndex, property);
            SkillKeyframeProperty captured = property;
            if (exists)
                menu.AddDisabledItem(new GUIContent(property.ToString()));
            else
                menu.AddItem(new GUIContent(property.ToString()), false, () => AddPropertyKey(captured));
        }

        private bool HasProperty(SkillPresentationTimeline tl, SkillObjectKind kind, int vfxIndex, SkillKeyframeProperty property)
        {
            if (tl == null) return false;
            switch (kind)
            {
                case SkillObjectKind.Animation: return (tl.animationTrack?.keyframes?.Count ?? 0) > 0;
                case SkillObjectKind.Effect:    return (tl.effectTrack?.keyframes?.Count    ?? 0) > 0;
                case SkillObjectKind.Ui:        return (tl.uiTrack?.keyframes?.Count        ?? 0) > 0;
                case SkillObjectKind.Sfx:       return (tl.sfxTrack?.keyframes?.Count       ?? 0) > 0;
                case SkillObjectKind.Camera:
                    if (tl.cameraTrack?.keyframes == null) return false;
                    foreach (var k in tl.cameraTrack.keyframes)
                        if (k != null && k.property == property) return true;
                    return false;
                case SkillObjectKind.Vfx:
                    if (vfxIndex < 0 || tl.vfxObjects == null || vfxIndex >= tl.vfxObjects.Count) return false;
                    var vfxKfs = tl.vfxObjects[vfxIndex]?.keyframes;
                    if (vfxKfs == null) return false;
                    foreach (var k in vfxKfs)
                        if (k != null && k.property == property) return true;
                    return false;
                case SkillObjectKind.EndMarker: return tl.endMarkerKeyframe != null;
                default: return false;
            }
        }

        private void AddPropertyKey(SkillKeyframeProperty property)
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            Undo.RecordObject(_target, "Add Property");

            SkillKeyframeData newKey;
            if (property == SkillKeyframeProperty.EffectSlot)
                newKey = CreateEffectKeyframeData(0f);
            else
                newKey = new SkillKeyframeData { property = property, timeSeconds = 0f };

            switch (property)
            {
                case SkillKeyframeProperty.CamPosition:
                case SkillKeyframeProperty.CamRotation:
                case SkillKeyframeProperty.CamZoom:
                    newKey.fieldOfView = 60f;
                    break;
                case SkillKeyframeProperty.VfxScale:
                    newKey.scale = Vector3.one;
                    break;
                case SkillKeyframeProperty.VfxActive:
                    newKey.vfxActiveAction = SkillVfxActiveAction.Play;
                    break;
            }

            string rowTag = GetRowTagForProperty(_selectedObjectKind, _selectedVfxIndex, property);
            AddKeyframeToTimeline(tl, rowTag, newKey);

            ClearKeyframeSelection();
            _selectedKeyframe = newKey;
            _selectedRowTag   = rowTag;
            SelectedKeyframes.Add(newKey);

            EditorUtility.SetDirty(_target);
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
            Repaint();
        }

        private static string GetRowTagForProperty(SkillObjectKind kind, int vfxIndex, SkillKeyframeProperty property)
        {
            if (kind == SkillObjectKind.Vfx)    return $"vfx_{vfxIndex}_{property}";
            if (kind == SkillObjectKind.Camera)  return "camera_" + property;
            return kind switch
            {
                SkillObjectKind.Animation  => "animation",
                SkillObjectKind.Effect     => "effect",
                SkillObjectKind.Ui         => "ui",
                SkillObjectKind.Sfx        => "sfx",
                SkillObjectKind.EndMarker  => "endmarker",
                _                          => null
            };
        }

        // ── 오브젝트별 Row 빌드 ───────────────────────────────────────────────────

        private bool BuildObjectRows(VisualElement content, SkillPresentationTimeline tl, float laneWidth, float duration)
        {
            bool any = false;
            switch (_selectedObjectKind)
            {
                case SkillObjectKind.Animation:
                    if (HasProperty(tl, SkillObjectKind.Animation, -1, SkillKeyframeProperty.AnimParam))
                    {
                        content.Add(BuildRow("Animation", tl.animationTrack.keyframes, ColorAnimation, "animation", laneWidth, duration));
                        any = true;
                    }
                    break;

                case SkillObjectKind.Effect:
                    if (HasProperty(tl, SkillObjectKind.Effect, -1, SkillKeyframeProperty.EffectSlot))
                    {
                        content.Add(BuildRow("Effect", tl.effectTrack.keyframes, ColorEffect, "effect", laneWidth, duration));
                        any = true;
                    }
                    break;

                case SkillObjectKind.Camera:
                    any |= TryBuildCameraRow(content, tl, SkillKeyframeProperty.CamPosition,  "Cam Position", ColorCameraPos,    laneWidth, duration);
                    any |= TryBuildCameraRow(content, tl, SkillKeyframeProperty.CamRotation,  "Cam Rotation", ColorCameraRot,    laneWidth, duration);
                    any |= TryBuildCameraRow(content, tl, SkillKeyframeProperty.CamZoom,      "Cam Zoom",     ColorCameraZoom,   laneWidth, duration);
                    any |= TryBuildCameraRow(content, tl, SkillKeyframeProperty.CamShake,     "Cam Shake",    ColorCameraShake,  laneWidth, duration);
                    break;

                case SkillObjectKind.Ui:
                    if (HasProperty(tl, SkillObjectKind.Ui, -1, SkillKeyframeProperty.UiAction))
                    {
                        content.Add(BuildRow("UI", tl.uiTrack.keyframes, ColorUi, "ui", laneWidth, duration));
                        any = true;
                    }
                    break;

                case SkillObjectKind.Sfx:
                    if (HasProperty(tl, SkillObjectKind.Sfx, -1, SkillKeyframeProperty.SfxId))
                    {
                        content.Add(BuildRow("SFX", tl.sfxTrack.keyframes, ColorSfx, "sfx", laneWidth, duration));
                        any = true;
                    }
                    break;

                case SkillObjectKind.Vfx:
                    if (_selectedVfxIndex >= 0 && tl.vfxObjects != null && _selectedVfxIndex < tl.vfxObjects.Count)
                    {
                        any |= TryBuildVfxPropertyRow(content, tl, SkillKeyframeProperty.VfxActive,   "VFX Active",   ColorVfx, laneWidth, duration);
                        any |= TryBuildVfxPropertyRow(content, tl, SkillKeyframeProperty.VfxPosition, "VFX Position", ColorVfx, laneWidth, duration);
                        any |= TryBuildVfxPropertyRow(content, tl, SkillKeyframeProperty.VfxRotation, "VFX Rotation", ColorVfx, laneWidth, duration);
                        any |= TryBuildVfxPropertyRow(content, tl, SkillKeyframeProperty.VfxScale,    "VFX Scale",    ColorVfx, laneWidth, duration);
                    }
                    break;

                case SkillObjectKind.EndMarker:
                    if (tl.endMarkerKeyframe != null)
                    {
                        var endKeys = new System.Collections.Generic.List<SkillKeyframeData> { tl.endMarkerKeyframe };
                        content.Add(BuildRow("End Time", endKeys, ColorEndMarker, "endmarker", laneWidth, duration));
                        any = true;
                    }
                    break;
            }
            return any;
        }

        private bool TryBuildCameraRow(VisualElement content, SkillPresentationTimeline tl, SkillKeyframeProperty property, string label, Color color, float laneWidth, float duration)
        {
            if (!HasProperty(tl, SkillObjectKind.Camera, -1, property)) return false;
            content.Add(BuildCameraRow(label, tl, property, color, laneWidth, duration));
            return true;
        }

        private bool TryBuildVfxPropertyRow(VisualElement content, SkillPresentationTimeline tl, SkillKeyframeProperty property, string label, Color color, float laneWidth, float duration)
        {
            if (!HasProperty(tl, SkillObjectKind.Vfx, _selectedVfxIndex, property)) return false;
            string rowTag  = $"vfx_{_selectedVfxIndex}_{property}";
            var    vfxObj  = tl.vfxObjects[_selectedVfxIndex];

            var row  = MakeRowContainer();
            row.Add(MakeRowLabel(label));
            var lane = CreateLane(laneWidth, RowHeight, new Color(0.095f, 0.095f, 0.105f));

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1) { float t = PixelToTime(e.localPosition.x); _currentTime = t; UpdateTimeLabel(); ShowAddKeyframeMenu(rowTag, t); e.StopPropagation(); return; }
                if (e.button != 0) return;
                TimelinePanel?.Focus();
                if (!(e.ctrlKey || e.commandKey)) ClearKeyframeSelection();
                _currentTime = PixelToTime(e.localPosition.x);
                UpdateTimeLabel();
                StartBoxSelection(e);
                e.StopPropagation();
            });

            if (NeedsEasingLine(property))
                AddEasingLines(lane, vfxObj.keyframes, property, rowTag);

            int globalIndex = 0;
            foreach (var key in vfxObj.keyframes)
            {
                if (key != null && key.property == property)
                    AddKeyMarker(lane, key, globalIndex, color, rowTag);
                globalIndex++;
            }

            AddPlayhead(lane, duration, RowHeight);
            row.Add(lane);
            content.Add(row);
            return true;
        }

        // ── VFX rowTag 파싱 ───────────────────────────────────────────────────────

        private static void ParseVfxRowTag(string rowTag, out int vfxIndex, out SkillKeyframeProperty property)
        {
            vfxIndex = -1;
            property = SkillKeyframeProperty.VfxActive;
            if (string.IsNullOrEmpty(rowTag) || !rowTag.StartsWith("vfx_")) return;
            int sep = rowTag.IndexOf('_', 4);
            if (sep < 0) return;
            if (!int.TryParse(rowTag.Substring(4, sep - 4), out vfxIndex)) { vfxIndex = -1; return; }
            if (!System.Enum.TryParse(rowTag.Substring(sep + 1), out property)) vfxIndex = -1;
        }

        // ── Ruler ────────────────────────────────────────────────────────────────

        private VisualElement BuildRulerRow(float duration, float laneWidth)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, height = RulerHeight, flexShrink = 0 }
            };
            row.Add(new Label("Time")
            {
                style = { width = RowLabelWidth, paddingLeft = 6, unityTextAlign = TextAnchor.MiddleLeft, fontSize = 10, color = new StyleColor(new Color(0.68f, 0.68f, 0.72f)) }
            });

            var lane = CreateLane(laneWidth, RulerHeight, new Color(0.10f, 0.10f, 0.115f));
            float tickInterval = ResolveTickInterval();
            for (float t = 0f; t <= duration + 0.001f; t += tickInterval)
                AddTimeTick(lane, t);
            AddPlayhead(lane, duration, RulerHeight);

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                TimelinePanel?.Focus();
                _currentTime          = Mathf.Max(0f, PixelToTime(e.localPosition.x));
                _dragStartScreenX     = e.position.x;
                _dragStartTime        = _currentTime;
                IsPlayheadDragging    = true;
                PlayheadDragPointerId = e.pointerId;
                TimelinePanel.CapturePointer(e.pointerId);
                UpdateTimeLabel();
                UpdatePlayheadPosition();
                e.StopPropagation();
            });
            row.Add(lane);
            return row;
        }

        // ── 일반 Row (point marker) ───────────────────────────────────────────────

        private VisualElement BuildRow(
            string label,
            List<SkillKeyframeData> keys,
            Color markerColor,
            string rowTag,
            float laneWidth,
            float duration)
        {
            var row = MakeRowContainer();
            row.Add(MakeRowLabel(label));
            var lane = CreateLane(laneWidth, RowHeight, new Color(0.095f, 0.095f, 0.105f));

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1) { float t = PixelToTime(e.localPosition.x); _currentTime = t; UpdateTimeLabel(); ShowAddKeyframeMenu(rowTag, t); e.StopPropagation(); return; }
                if (e.button != 0) return;
                TimelinePanel?.Focus();
                bool additive = e.ctrlKey || e.commandKey;
                if (!additive) ClearKeyframeSelection();
                _currentTime = PixelToTime(e.localPosition.x);
                UpdateTimeLabel();
                StartBoxSelection(e);
                e.StopPropagation();
            });

            if (keys != null)
            {
                int index = 0;
                foreach (var key in keys) { if (key != null) AddKeyMarker(lane, key, index, markerColor, rowTag); index++; }
            }
            AddPlayhead(lane, duration, RowHeight);
            row.Add(lane);
            return row;
        }

        // ── Camera Row ────────────────────────────────────────────────────────────

        private VisualElement BuildCameraRow(
            string label,
            SkillPresentationTimeline tl,
            SkillKeyframeProperty property,
            Color markerColor,
            float laneWidth,
            float duration)
        {
            string rowTag = "camera_" + property;
            var row = MakeRowContainer();
            row.Add(MakeRowLabel(label));
            var lane = CreateLane(laneWidth, RowHeight, new Color(0.095f, 0.095f, 0.105f));

            lane.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1) { float t = PixelToTime(e.localPosition.x); _currentTime = t; UpdateTimeLabel(); ShowAddKeyframeMenu(rowTag, t); e.StopPropagation(); return; }
                if (e.button != 0) return;
                TimelinePanel?.Focus();
                bool additive = e.ctrlKey || e.commandKey;
                if (!additive) ClearKeyframeSelection();
                _currentTime = PixelToTime(e.localPosition.x);
                UpdateTimeLabel();
                StartBoxSelection(e);
                e.StopPropagation();
            });

            if (tl.cameraTrack?.keyframes != null)
            {
                if (NeedsEasingLine(property))
                    AddEasingLines(lane, tl.cameraTrack.keyframes, property, rowTag);

                int globalIndex = 0;
                foreach (var key in tl.cameraTrack.keyframes)
                {
                    if (key != null && key.property == property)
                        AddKeyMarker(lane, key, globalIndex, markerColor, rowTag);
                    globalIndex++;
                }
            }

            AddPlayhead(lane, duration, RowHeight);
            row.Add(lane);
            return row;
        }

        // ── Marker ───────────────────────────────────────────────────────────────

        private void AddKeyMarker(
            VisualElement lane,
            SkillKeyframeData key,
            int index,
            Color markerColor,
            string rowTag)
        {
            bool selected = ReferenceEquals(_selectedKeyframe, key) || SelectedKeyframes.Contains(key);
            float x       = TimeToPixel(key.timeSeconds);

            var marker = new VisualElement
            {
                tooltip = $"{rowTag} @ {key.timeSeconds:0.00}s",
                style   =
                {
                    position          = Position.Absolute,
                    left              = x - MarkerSize * 0.5f,
                    top               = (RowHeight - MarkerSize) * 0.5f,
                    width             = MarkerSize,
                    height            = MarkerSize,
                    backgroundColor   = new StyleColor(selected ? ColorSelected : markerColor),
                    borderTopWidth    = 1, borderRightWidth  = 1,
                    borderBottomWidth = 1, borderLeftWidth   = 1,
                    borderTopColor    = new StyleColor(Color.black),
                    borderRightColor  = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor   = new StyleColor(Color.black)
                }
            };

            marker.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 1)
                {
                    if (!SelectedKeyframes.Contains(key)) { ClearKeyframeSelection(); _selectedKeyframe = key; _selectedRowTag = rowTag; _selectedKeyIndex = index; SelectedKeyframes.Add(key); }
                    MovePlayheadToTime(key.timeSeconds, refreshTimeline: true);
                    ShowRemoveKeyframeMenu();
                    e.StopPropagation();
                    return;
                }
                if (e.button != 0) return;
                TimelinePanel?.Focus();

                if (e.ctrlKey || e.commandKey)
                {
                    if (SelectedKeyframes.Contains(key))
                    {
                        SelectedKeyframes.Remove(key);
                        if (ReferenceEquals(_selectedKeyframe, key)) { _selectedKeyframe = null; _selectedRowTag = null; _selectedKeyIndex = -1; }
                    }
                    else
                    {
                        SelectedKeyframes.Add(key);
                        _selectedKeyframe = key; _selectedRowTag = rowTag; _selectedKeyIndex = index;
                    }
                    MovePlayheadToTime(key.timeSeconds, refreshTimeline: true);
                    RefreshInspector();
                    e.StopPropagation();
                    return;
                }

                if (!SelectedKeyframes.Contains(key))
                {
                    ClearKeyframeSelection();
                    _selectedKeyframe = key; _selectedRowTag = rowTag; _selectedKeyIndex = index;
                    SelectedKeyframes.Add(key);
                    MovePlayheadToTime(key.timeSeconds, refreshTimeline: true);
                    RefreshInspector();
                }
                else
                {
                    _selectedKeyframe = key; _selectedRowTag = rowTag; _selectedKeyIndex = index;
                    RefreshInspector();
                    MovePlayheadToTime(key.timeSeconds);
                }
                StartKeyframeDragPending(e, key);
                e.StopPropagation();
            });

            lane.Add(marker);
            KeyframeMarkers[key] = marker;
        }

        // ── 컨텍스트 메뉴 ────────────────────────────────────────────────────────

        private void ShowAddKeyframeMenu(string rowTag, float time)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"Add Keyframe @ {time:0.00}s"), false, () => AddKeyframeAt(rowTag, time));
            menu.ShowAsContext();
        }

        private void ShowRemoveKeyframeMenu()
        {
            var menu   = new GenericMenu();
            int count  = SelectedKeyframes.Count > 1 ? SelectedKeyframes.Count : 1;
            string lbl = count > 1 ? $"Remove {count} Keyframes" : "Remove Keyframe";
            menu.AddItem(new GUIContent(lbl), false, RemoveSelectedKeyframes);
            menu.ShowAsContext();
        }

        // ── Keyframe CRUD ─────────────────────────────────────────────────────────

        private void AddKeyframeAt(string rowTag, float time)
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            Undo.RecordObject(_target, "Add Keyframe");

            SkillKeyframeData newKey = null;

            if (rowTag != null && rowTag.StartsWith("vfx_"))
            {
                ParseVfxRowTag(rowTag, out int vfxIdx, out var vfxProp);
                if (vfxIdx >= 0 && tl.vfxObjects != null && vfxIdx < tl.vfxObjects.Count)
                {
                    newKey = vfxProp switch
                    {
                        SkillKeyframeProperty.VfxActive   => new SkillKeyframeData { property = vfxProp, timeSeconds = time, vfxActiveAction = SkillVfxActiveAction.Play },
                        SkillKeyframeProperty.VfxScale    => new SkillKeyframeData { property = vfxProp, timeSeconds = time, scale = Vector3.one },
                        _                                  => new SkillKeyframeData { property = vfxProp, timeSeconds = time }
                    };
                    tl.vfxObjects[vfxIdx].keyframes.Add(newKey);
                    SortByTime(tl.vfxObjects[vfxIdx].keyframes);
                }
            }
            else if (rowTag != null && rowTag.StartsWith("camera_") &&
                System.Enum.TryParse<SkillKeyframeProperty>(rowTag.Substring("camera_".Length), out var camProp))
            {
                newKey = new SkillKeyframeData { property = camProp, timeSeconds = time, fieldOfView = 60f };
                tl.cameraTrack.keyframes.Add(newKey);
                SortByTime(tl.cameraTrack.keyframes);
            }
            else
            {
                switch (rowTag)
                {
                    case "animation":
                        newKey = new SkillKeyframeData { property = SkillKeyframeProperty.AnimParam, timeSeconds = time };
                        tl.animationTrack.keyframes.Add(newKey);
                        SortByTime(tl.animationTrack.keyframes);
                        break;
                    case "effect":
                        newKey = CreateEffectKeyframeData(time);
                        tl.effectTrack.keyframes.Add(newKey);
                        SortByTime(tl.effectTrack.keyframes);
                        break;
                    case "ui":
                        newKey = new SkillKeyframeData { property = SkillKeyframeProperty.UiAction, timeSeconds = time };
                        tl.uiTrack.keyframes.Add(newKey);
                        SortByTime(tl.uiTrack.keyframes);
                        break;
                    case "sfx":
                        newKey = new SkillKeyframeData { property = SkillKeyframeProperty.SfxId, timeSeconds = time };
                        tl.sfxTrack.keyframes.Add(newKey);
                        SortByTime(tl.sfxTrack.keyframes);
                        break;
                    case "endmarker":
                        if (tl.endMarkerKeyframe != null)
                        {
                            tl.endMarkerKeyframe.timeSeconds = time;
                            newKey = tl.endMarkerKeyframe;
                        }
                        break;
                }
            }

            if (newKey != null)
            {
                ClearKeyframeSelection();
                _selectedKeyframe = newKey;
                _selectedRowTag   = rowTag;
                SelectedKeyframes.Add(newKey);
            }

            EditorUtility.SetDirty(_target);
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
            Repaint();
        }

        private SkillKeyframeData CreateEffectKeyframeData(float time)
        {
            var key = new SkillKeyframeData { property = SkillKeyframeProperty.EffectSlot, timeSeconds = time, valueMultiplier = 1f };
            if (_referenceCard == null) return key;
            if (_referenceCard.EnsureEffectSlotIds()) EditorUtility.SetDirty(_referenceCard);
            var slots = _referenceCard.effectSlots;
            if (slots == null || slots.Count == 0 || slots[0] == null) return key;
            key.effectSlotId = slots[0].effectSlotId;
            return key;
        }

        internal void RemoveSelectedKeyframes()
        {
            if (_target == null) return;
            var tl = GetEditableTimeline();
            if (tl == null) return;

            var toRemove = new List<SkillKeyframeData>();
            if (SelectedKeyframes.Count > 0) toRemove.AddRange(SelectedKeyframes);
            else if (_selectedKeyframe != null) toRemove.Add(_selectedKeyframe);
            if (toRemove.Count == 0) return;

            Undo.RecordObject(_target, toRemove.Count > 1 ? "Remove Keyframes" : "Remove Keyframe");

            bool anyRemoved = false;
            foreach (var key in toRemove)
            {
                string tag = GetRowTagForKeyframe(key, tl) ?? _selectedRowTag;
                bool removed = false;

                if (tag != null && tag.StartsWith("vfx_"))
                {
                    ParseVfxRowTag(tag, out int vfxIdx, out _);
                    if (vfxIdx >= 0 && tl.vfxObjects != null && vfxIdx < tl.vfxObjects.Count)
                        removed = tl.vfxObjects[vfxIdx].keyframes.Remove(key);
                }
                else if (tag != null && tag.StartsWith("camera_"))
                    removed = tl.cameraTrack?.keyframes.Remove(key) ?? false;
                else if (tag == "endmarker")
                {
                    tl.endMarkerKeyframe = null;
                    tl.addedObjects.Remove(SkillObjectKind.EndMarker);
                    if (_selectedObjectKind == SkillObjectKind.EndMarker) ClearSelection();
                    removed = true;
                }
                else
                    removed = tag switch
                    {
                        "animation" => tl.animationTrack.keyframes.Remove(key),
                        "effect"    => tl.effectTrack.keyframes.Remove(key),
                        "ui"        => tl.uiTrack.keyframes.Remove(key),
                        "sfx"       => tl.sfxTrack.keyframes.Remove(key),
                        _           => false
                    };

                anyRemoved |= removed;
            }

            if (!anyRemoved) return;
            ClearKeyframeSelection();
            EditorUtility.SetDirty(_target);
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
            Repaint();
        }

        // ── Copy / Paste ──────────────────────────────────────────────────────────

        private void CopySelectedKeyframes()
        {
            var tl = GetEditableTimeline();
            if (tl == null) return;
            var toClone = new List<(string rowTag, SkillKeyframeData key)>();

            if (SelectedKeyframes.Count > 0)
            {
                foreach (var key in SelectedKeyframes)
                {
                    string tag = GetRowTagForKeyframe(key, tl);
                    if (tag != null) toClone.Add((tag, key));
                }
            }
            else if (_selectedKeyframe != null && _selectedRowTag != null)
                toClone.Add((_selectedRowTag, _selectedKeyframe));

            if (toClone.Count == 0) return;
            float minTime = float.MaxValue;
            foreach (var (_, k) in toClone) if (k.timeSeconds < minTime) minTime = k.timeSeconds;
            _clipboard.Clear();
            foreach (var (tag, k) in toClone)
            {
                var clone = CloneKeyframe(k);
                if (clone != null) _clipboard.Add((tag, k.timeSeconds - minTime, clone));
            }
        }

        private void PasteKeyframes()
        {
            if (_clipboard.Count == 0 || _target == null) return;
            var tl = GetEditableTimeline();
            if (tl == null) return;
            Undo.RecordObject(_target, "Paste Keyframes");

            var newKeys = new List<SkillKeyframeData>();
            foreach (var (rowTag, timeOffset, template) in _clipboard)
            {
                var newKey = CloneKeyframe(template);
                if (newKey == null) continue;
                newKey.timeSeconds = Mathf.Max(0f, _currentTime + timeOffset);
                AddKeyframeToTimeline(tl, rowTag, newKey);
                newKeys.Add(newKey);
            }

            ClearKeyframeSelection();
            foreach (var k in newKeys) SelectedKeyframes.Add(k);
            if (newKeys.Count == 1)
            {
                _selectedKeyframe = newKeys[0];
                _selectedRowTag   = GetRowTagForKeyframe(newKeys[0], tl);
                _selectedKeyIndex = -1;
            }

            EditorUtility.SetDirty(_target);
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
            Repaint();
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────────────

        private static void AddKeyframeToTimeline(SkillPresentationTimeline tl, string rowTag, SkillKeyframeData key)
        {
            if (rowTag != null && rowTag.StartsWith("vfx_"))
            {
                ParseVfxRowTag(rowTag, out int vfxIdx, out _);
                if (vfxIdx >= 0 && tl.vfxObjects != null && vfxIdx < tl.vfxObjects.Count)
                {
                    tl.vfxObjects[vfxIdx].keyframes.Add(key);
                    SortByTime(tl.vfxObjects[vfxIdx].keyframes);
                }
                return;
            }
            if (rowTag != null && rowTag.StartsWith("camera_"))
            {
                tl.cameraTrack.keyframes.Add(key);
                SortByTime(tl.cameraTrack.keyframes);
                return;
            }
            switch (rowTag)
            {
                case "animation":  tl.animationTrack.keyframes.Add(key); SortByTime(tl.animationTrack.keyframes); break;
                case "effect":     tl.effectTrack.keyframes.Add(key);    SortByTime(tl.effectTrack.keyframes);    break;
                case "ui":         tl.uiTrack.keyframes.Add(key);        SortByTime(tl.uiTrack.keyframes);        break;
                case "sfx":        tl.sfxTrack.keyframes.Add(key);       SortByTime(tl.sfxTrack.keyframes);       break;
                case "endmarker":  if (tl.endMarkerKeyframe != null) tl.endMarkerKeyframe.timeSeconds = key.timeSeconds; break;
            }
        }

        private static string GetRowTagForKeyframe(SkillKeyframeData key, SkillPresentationTimeline tl)
        {
            if (key == null || tl == null) return null;
            if (tl.endMarkerKeyframe == key)                          return "endmarker";
            if (tl.animationTrack?.keyframes?.Contains(key) == true) return "animation";
            if (tl.effectTrack?.keyframes?.Contains(key)    == true) return "effect";
            if (tl.cameraTrack?.keyframes?.Contains(key)    == true) return "camera_" + key.property;
            if (tl.uiTrack?.keyframes?.Contains(key)        == true) return "ui";
            if (tl.sfxTrack?.keyframes?.Contains(key)       == true) return "sfx";
            if (tl.vfxObjects != null)
                for (int i = 0; i < tl.vfxObjects.Count; i++)
                    if (tl.vfxObjects[i]?.keyframes?.Contains(key) == true)
                        return $"vfx_{i}_{key.property}";
            return null;
        }

        private static SkillKeyframeData CloneKeyframe(SkillKeyframeData src)
        {
            if (src == null) return null;
            return new SkillKeyframeData
            {
                property          = src.property,
                timeSeconds       = src.timeSeconds,
                animParam         = src.animParam,
                effectSlotId      = src.effectSlotId,
                valueMultiplier   = src.valueMultiplier,
                position          = src.position,
                rotationEuler     = src.rotationEuler,
                scale             = src.scale,
                isHold            = src.isHold,
                easing            = src.easing,
                cameraPosition    = src.cameraPosition,
                cameraRotationEuler = src.cameraRotationEuler,
                fieldOfView       = src.fieldOfView,
                amplitude         = src.amplitude,
                shakeDuration     = src.shakeDuration,
                uiAction          = src.uiAction,
                sfxId             = src.sfxId,
                vfxActiveAction   = src.vfxActiveAction
            };
        }

        // ── Playhead / Ticks ─────────────────────────────────────────────────────

        private void AddPlayhead(VisualElement lane, float duration, float height)
        {
            float x  = Mathf.Clamp(TimeToPixel(_currentTime), 0f, TimeToPixel(duration));
            var el   = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style       = { position = Position.Absolute, left = x, top = 0, width = 2, height = height, backgroundColor = new StyleColor(ColorPlayhead) }
            };
            lane.Add(el);
            PlayheadElements.Add(el);
        }

        private void AddTimeTick(VisualElement lane, float time)
        {
            float x = TimeToPixel(time);
            lane.Add(new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style       = { position = Position.Absolute, left = x, top = 0, width = 1, bottom = 0, backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.24f)) }
            });
            lane.Add(new Label($"{time:0.0}s")
            {
                pickingMode = PickingMode.Ignore,
                style       = { position = Position.Absolute, left = x + 3, top = 4, fontSize = 9, color = new StyleColor(new Color(0.62f, 0.62f, 0.66f)) }
            });
        }

        // ── 이징 연결선 ──────────────────────────────────────────────────────────

        private static bool NeedsEasingLine(SkillKeyframeProperty property) => property switch
        {
            SkillKeyframeProperty.CamPosition => true,
            SkillKeyframeProperty.CamRotation => true,
            SkillKeyframeProperty.CamZoom     => true,
            SkillKeyframeProperty.VfxPosition => true,
            SkillKeyframeProperty.VfxRotation => true,
            SkillKeyframeProperty.VfxScale    => true,
            _                                 => false
        };

        private void AddEasingLines(VisualElement lane, List<SkillKeyframeData> allKeys, SkillKeyframeProperty property, string rowTag)
        {
            var propKeys = new List<SkillKeyframeData>();
            foreach (var k in allKeys)
                if (k != null && k.property == property) propKeys.Add(k);
            propKeys.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            for (int i = 0; i + 1 < propKeys.Count; i++)
                AddEasingLine(lane, propKeys[i], propKeys[i + 1], rowTag);
        }

        private void AddEasingLine(VisualElement lane, SkillKeyframeData fromKey, SkillKeyframeData toKey, string rowTag)
        {
            float x1 = TimeToPixel(fromKey.timeSeconds) + MarkerSize * 0.5f;
            float x2 = TimeToPixel(toKey.timeSeconds)   - MarkerSize * 0.5f;
            float lineWidth = x2 - x1;
            if (lineWidth < 4f) return;

            bool  isSelected   = ReferenceEquals(_selectedKeyframe, toKey) && _isEasingLineSelected;
            Color baseColor    = GetEasingLineColor(toKey);
            Color displayColor = isSelected ? ColorSelected : baseColor;
            float centerY      = RowHeight * 0.5f;
            const float lineH  = 4f;

            var container = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left     = x1,
                    width    = lineWidth,
                    top      = 0,
                    height   = RowHeight
                }
            };

            VisualElement dashesEl = null;
            if (toKey.isHold)
            {
                dashesEl = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style       = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0 }
                };
                BuildDashChildren(dashesEl, lineWidth, centerY, lineH, displayColor);
                container.Add(dashesEl);
            }
            else
            {
                container.Add(new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style       =
                    {
                        position        = Position.Absolute,
                        left            = 0, right = 0,
                        top             = centerY - lineH * 0.5f,
                        height          = lineH,
                        backgroundColor = new StyleColor(displayColor)
                    }
                });
            }

            string labelText = toKey.isHold ? "Hold" : toKey.easing.ToString();
            container.Add(new Label(labelText)
            {
                pickingMode = PickingMode.Ignore,
                style       =
                {
                    position       = Position.Absolute,
                    left           = 0, right = 0,
                    top            = centerY - 14,
                    fontSize       = 9,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color          = new StyleColor(new Color(1f, 1f, 1f, 0.90f)),
                    overflow       = Overflow.Hidden
                }
            });

            SkillKeyframeData capturedKey = toKey;
            string            capturedTag = rowTag;
            container.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) { e.StopPropagation(); return; }
                TimelinePanel?.Focus();
                ClearKeyframeSelection();
                _selectedKeyframe     = capturedKey;
                _selectedRowTag       = capturedTag;
                _isEasingLineSelected = true;
                SelectedKeyframes.Add(capturedKey);
                RefreshTimeline();
                RefreshInspector();
                e.StopPropagation();
            });

            lane.Add(container);
            _easingLineContainers[toKey] = (container, fromKey, dashesEl);
        }

        private static void BuildDashChildren(VisualElement dashesEl, float lineWidth, float centerY, float lineH, Color color)
        {
            dashesEl.Clear();
            float x = 0f;
            while (x < lineWidth)
            {
                float w = Mathf.Min(5f, lineWidth - x);
                if (w <= 0f) break;
                dashesEl.Add(new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    style       =
                    {
                        position        = Position.Absolute,
                        left            = x,
                        top             = centerY - lineH * 0.5f,
                        width           = w,
                        height          = lineH,
                        backgroundColor = new StyleColor(color)
                    }
                });
                x += 9f;
            }
        }

        private void UpdateDraggedEasingLines()
        {
            if (_easingLineContainers.Count == 0) return;
            var dragged = new HashSet<SkillKeyframeData>(_keyframeDragSnapshots.Count);
            foreach (var (k, _) in _keyframeDragSnapshots) dragged.Add(k);

            foreach (var kvp in _easingLineContainers)
            {
                var (container, fromKey, dashesEl) = kvp.Value;
                if (!dragged.Contains(kvp.Key) && !dragged.Contains(fromKey)) continue;
                UpdateEasingLineBounds(container, fromKey, kvp.Key, dashesEl);
            }
        }

        private void UpdateEasingLineBounds(VisualElement container, SkillKeyframeData fromKey, SkillKeyframeData toKey, VisualElement dashesEl)
        {
            float x1 = TimeToPixel(fromKey.timeSeconds) + MarkerSize * 0.5f;
            float x2 = TimeToPixel(toKey.timeSeconds)   - MarkerSize * 0.5f;
            float lineWidth = x2 - x1;

            if (lineWidth < 4f) { container.style.display = DisplayStyle.None; return; }
            container.style.display = DisplayStyle.Flex;
            container.style.left    = x1;
            container.style.width   = lineWidth;

            if (dashesEl != null)
                BuildDashChildren(dashesEl, lineWidth, RowHeight * 0.5f, 4f, GetEasingLineColor(toKey));
        }

        private static Color GetEasingLineColor(SkillKeyframeData key)
        {
            if (key.isHold) return new Color(0.45f, 0.45f, 0.50f);
            string name = key.easing.ToString();
            if (name == "Linear")              return new Color(0.72f, 0.72f, 0.75f);
            if (name.StartsWith("EaseInOut"))  return new Color(0.62f, 0.42f, 0.88f);
            if (name.StartsWith("EaseIn"))     return new Color(0.28f, 0.55f, 0.92f);
            if (name.StartsWith("EaseOut"))    return new Color(0.95f, 0.62f, 0.30f);
            return new Color(0.72f, 0.72f, 0.75f);
        }

        // ── 공용 헬퍼 ────────────────────────────────────────────────────────────

        private static VisualElement MakeRowContainer() => new()
        {
            style = { flexDirection = FlexDirection.Row, height = RowHeight, flexShrink = 0, borderTopWidth = 1, borderTopColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f)) }
        };

        private static Label MakeRowLabel(string text) => new(text)
        {
            style = { width = RowLabelWidth, paddingLeft = 6, unityTextAlign = TextAnchor.MiddleLeft, fontSize = 10, color = new StyleColor(new Color(0.78f, 0.78f, 0.82f)) }
        };

        private static VisualElement CreateLane(float width, float height, Color bg) => new()
        {
            style = { position = Position.Relative, width = width, height = height, backgroundColor = new StyleColor(bg) }
        };

        private float ResolveTickInterval()
        {
            if (PixelsPerSecond >= 200f) return 0.25f;
            if (PixelsPerSecond <= 80f)  return 1f;
            return 0.5f;
        }

        private static Label MakeTimelinePlaceholderLabel(string text) => new(text)
        {
            style = { color = new StyleColor(new Color(0.50f, 0.50f, 0.55f)), paddingLeft = 14, paddingTop = 14, fontSize = 11, whiteSpace = WhiteSpace.Normal }
        };

        internal void MovePlayheadToTime(float timeSeconds, bool refreshTimeline = false)
        {
            _currentTime = Mathf.Max(0f, timeSeconds);
            UpdateTimeLabel();
            if (refreshTimeline) RefreshTimeline();
            else                 UpdatePlayheadPosition();
            Repaint();
        }

        internal float ResolveDraggedDeltaSeconds(float rawDeltaSeconds, float anchorStartTime, float minStartTime)
        {
            float minDelta     = -Mathf.Max(0f, minStartTime);
            float clampedDelta = Mathf.Max(rawDeltaSeconds, minDelta);
            if (!_snapEnabled) return clampedDelta;
            float step = ClampSnapStep(_snapStep);
            if (step <= 0f) return clampedDelta;
            float snappedTargetTime = Mathf.Round((anchorStartTime + clampedDelta) / step) * step;
            float snappedDelta      = snappedTargetTime - anchorStartTime;
            if (Mathf.Sign(clampedDelta) != 0f && Mathf.Sign(snappedDelta) != Mathf.Sign(clampedDelta))
                return clampedDelta;
            return Mathf.Max(snappedDelta, minDelta);
        }

        internal static float ClampSnapStep(float step) => Mathf.Max(0f, step);
    }
}
