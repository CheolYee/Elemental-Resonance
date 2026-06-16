using System;
using System.Collections.Generic;
using Battle.Data;
using Battle.Presentation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow : EditorWindow
    {
        private const float DefaultInspectorWidth = 240f;
        private const float MinInspectorWidth     = 220f;
        private const float MinTimelineWidth      = 320f;
        private const float InspectorHandleWidth  = 8f;
        private const float DefaultPixelsPerSecond = 120f;

        // ── 메뉴 ────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Battle/Skill Presentation Editor")]
        public static SkillPresentationEditorWindow Open()
        {
            var w = GetWindow<SkillPresentationEditorWindow>("Skill Presentation Editor");
            w.minSize = new Vector2(800, 480);
            return w;
        }

        // ── 공유 상태 ────────────────────────────────────────────────────────────

        [SerializeField] private SkillPresentationDataSO _target;
        [SerializeField] private CardDataSO              _referenceCard;
        [SerializeField] private float                   _inspectorWidth       = DefaultInspectorWidth;
        [SerializeField] private bool                    _snapEnabled;
        [SerializeField] private float                   _snapStep             = 0.1f;
        [SerializeField] private float                   _renderViewHeight     = 240f;
        [SerializeField] private float                   _hierarchyPanelHeight = 200f;
        private SkillKeyframeData                        _selectedKeyframe;
        private string                                   _selectedRowTag;   // "animation" / "effect" / etc.
        private int                                      _selectedKeyIndex  = -1;

        // ── 오브젝트 선택 상태 (Phase 2) ─────────────────────────────────────────

        private SkillObjectKind _selectedObjectKind = SkillObjectKind.None;
        private int             _selectedVfxIndex   = -1;

        // ── 다중 선택 ────────────────────────────────────────────────────────────

        internal readonly HashSet<SkillKeyframeData>                     SelectedKeyframes = new();
        internal readonly Dictionary<SkillKeyframeData, VisualElement>   KeyframeMarkers   = new();

        // ── 박스 선택 상태 ───────────────────────────────────────────────────────

        internal bool          IsBoxSelecting;
        internal int           BoxSelectPointerId  = -1;
        internal Vector2       BoxSelectStartWorld;
        internal VisualElement BoxSelectionEl;

        // ── Keyframe 드래그 상태 ─────────────────────────────────────────────────

        private bool          _isKeyframeDragPending;
        private bool          _isKeyframeDragging;
        private int           _keyframeDragPointerId    = -1;
        private float         _keyframeDragStartScreenX;
        private float         _keyframeDragAnchorStartTime;
        private readonly List<(SkillKeyframeData key, float startTime)> _keyframeDragSnapshots = new();

        // ── 프리뷰 ───────────────────────────────────────────────────────────────────

        [SerializeField] private SkillPreviewLayoutSO _previewLayout;

        // ── 재생 상태 ────────────────────────────────────────────────────────────

        private float  _currentTime;
        private bool   _isPlaying;
        private double _playbackStartedAt;
        private float  _playbackStartTime;

        // ── Playhead 드래그 상태 ─────────────────────────────────────────────────

        internal bool  IsPlayheadDragging;
        internal int   PlayheadDragPointerId = -1;
        private  float _dragStartScreenX;
        private  float _dragStartTime;

        // ── Inspector 리사이즈 상태 ─────────────────────────────────────────────

        private bool _isInspectorResizeDragging;
        private int  _inspectorResizePointerId = -1;

        // ── 렌더/타임라인 분할 리사이즈 상태 ────────────────────────────────────

        private bool _isRenderSplitDragging;
        private int  _renderSplitPointerId = -1;

        // ── Hierarchy/Inspector 분할 리사이즈 상태 ───────────────────────────────

        private bool _isHierarchySplitDragging;
        private int  _hierarchySplitPointerId = -1;

        // ── Timeline 뷰 ──────────────────────────────────────────────────────────

        internal float               PixelsPerSecond  = DefaultPixelsPerSecond;
        internal List<VisualElement> PlayheadElements = new();

        // ── 클립보드 ─────────────────────────────────────────────────────────────

        // (rowTag, 가장 빠른 키프레임 기준 시간 오프셋, 복제된 키프레임)
        private readonly List<(string rowTag, float timeOffset, SkillKeyframeData clone)> _clipboard = new();

        // ── UI 참조: Toolbar ─────────────────────────────────────────────────────

        private ObjectField   _referenceCardField;
        private Button        _playBtn;
        private Button        _pauseBtn;
        private Button        _stopBtn;
        private Label         _timeLabel;
        private Toggle        _snapToggle;
        private FloatField    _snapStepField;
        private VisualElement _timelineControlBar;
        private Button        _addPropertyBtn;
        private VisualElement _vfxObjectSection;
        private bool          _isEasingLineSelected;

        // key = toKey, value = (container, fromKey, dashesEl or null for solid lines)
        private readonly Dictionary<SkillKeyframeData, (VisualElement container, SkillKeyframeData fromKey, VisualElement dashesEl)>
            _easingLineContainers = new();

        // ── UI 참조: 패널 ────────────────────────────────────────────────────────

        internal VisualElement TimelinePanel;
        internal VisualElement InspectorPanel;
        internal VisualElement MainArea;
        internal VisualElement InspectorResizeHandle;
        internal VisualElement LeftPanel;
        internal VisualElement RenderingView;
        internal VisualElement RenderTimelineSplitHandle;
        internal VisualElement RightPanel;
        internal VisualElement HierarchyPanel;
        internal VisualElement HierarchyInspectorSplitHandle;
        private  Label         _objectNameLabel;

        // ── 생명주기 ─────────────────────────────────────────────────────────────

        private const string PrefKeyCard   = "SPE_ReferenceCardPath";
        private const string PrefKeyLayout = "SPE_PreviewLayoutPath";
        private const string PrefKeyFolder = "SPE_PrefabFolder";

        private void LoadPersistedState()
        {
            var cardPath = EditorPrefs.GetString(PrefKeyCard, "");
            if (!string.IsNullOrEmpty(cardPath))
                _referenceCard = AssetDatabase.LoadAssetAtPath<CardDataSO>(cardPath);

            var layoutPath = EditorPrefs.GetString(PrefKeyLayout, "");
            if (!string.IsNullOrEmpty(layoutPath))
                _previewLayout = AssetDatabase.LoadAssetAtPath<SkillPreviewLayoutSO>(layoutPath);

            var folder = EditorPrefs.GetString(PrefKeyFolder, "");
            if (!string.IsNullOrEmpty(folder))
                _previewPrefabFolder = folder;
        }

        private void CreateGUI()
        {
            LoadPersistedState();
            _target = _referenceCard != null ? _referenceCard.presentationData : null;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<ValidateCommandEvent>(OnValidateCommand, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand, TrickleDown.TrickleDown);
            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildMainArea());

            // 타임라인 컨트롤 바는 TimelinePanel 맨 위에 삽입 (BuildMainArea 이후)
            _timelineControlBar = BuildTimelineControlBar();
            TimelinePanel?.Insert(0, _timelineControlBar);

            if (_referenceCard != null)
                SetReferenceCard(_referenceCard, refreshUi: true);

            OnCreateGUIPreview();
        }

        partial void OnCreateGUIPreview();

        private void OnEnable()
        {
            EditorApplication.update += UpdatePlayback;
            Undo.undoRedoPerformed   += OnUndoRedo;
            OnEnablePreview();
        }

        partial void OnEnablePreview();

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePlayback;
            Undo.undoRedoPerformed   -= OnUndoRedo;
            OnDisablePreview();
        }

        partial void OnDisablePreview();

        private void OnDestroy()
        {
            OnDestroyPreview();
        }

        partial void OnDestroyPreview();

        private void OnUndoRedo()
        {
            if (_target == null) return;

            SkillKeyframeData previousPrimary = _selectedKeyframe;
            string previousRowTag = _selectedRowTag;
            int previousKeyIndex = _selectedKeyIndex;
            var previousSelection = new List<SkillKeyframeData>(SelectedKeyframes);

            RestoreKeyframeSelectionAfterUndoRedo(previousPrimary, previousRowTag, previousKeyIndex, previousSelection);
            RefreshDurationField();
            RefreshObjectList();
            RefreshTimeline();
            RefreshInspector();
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is CardDataSO card && card != _referenceCard)
                SetReferenceCard(card);
        }

        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            if (TryHandleUndoRedoShortcut(evt))
                evt.StopImmediatePropagation();
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (IsUndoRedoCommand(evt.commandName))
                evt.StopPropagation();
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (!TryHandleUndoRedoCommand(evt.commandName))
                return;

            evt.StopImmediatePropagation();
        }

        // ── 외부 API ─────────────────────────────────────────────────────────────

        internal void SetReferenceCard(CardDataSO card, bool refreshUi = true)
        {
            _referenceCard = card;
            _target = card != null ? card.presentationData : null;
            EditorPrefs.SetString(PrefKeyCard, card != null ? AssetDatabase.GetAssetPath(card) : "");
            Stop();
            ClearSelection();
            _clipboard.Clear();
            _referenceCardField?.SetValueWithoutNotify(card);

            if (!refreshUi)
                return;

            RefreshDurationField();
            RefreshObjectList();
            RefreshTimeline();
            RefreshInspector();
            RebuildPreview();
        }

        private void SyncTargetFromReferenceCardIfNeeded()
        {
            SkillPresentationDataSO presentationData = _referenceCard != null
                ? _referenceCard.presentationData
                : null;

            if (_referenceCard == null && _target == null)
                return;

            if (_referenceCard != null && _target == presentationData)
                return;

            SetReferenceCard(_referenceCard);
        }

        internal SkillPresentationTimeline GetEditableTimeline()
        {
            return _target?.timeline;
        }

        internal void ClearKeyframeSelection()
        {
            _selectedKeyframe     = null;
            _selectedRowTag       = null;
            _selectedKeyIndex     = -1;
            _isEasingLineSelected = false;
            SelectedKeyframes.Clear();
        }

        internal void ClearSelection()
        {
            _selectedObjectKind = SkillObjectKind.None;
            _selectedVfxIndex   = -1;
            ClearKeyframeSelection();
        }

        internal void SelectObject(SkillObjectKind kind, int vfxIndex = -1)
        {
            _selectedObjectKind = kind;
            _selectedVfxIndex   = vfxIndex;
            ClearKeyframeSelection();
            RefreshObjectList();
            RefreshTimeline();
            RefreshInspector();
        }


        // ── 공용 유틸 (partial 파일 간 공유) ────────────────────────────────────

        internal static Button MakeBtn(string text, Color bg, Action click) =>
            new(click)
            {
                text = text,
                style =
                {
                    height          = 22,
                    paddingLeft     = 8,
                    paddingRight    = 8,
                    marginRight     = 4,
                    backgroundColor = new StyleColor(bg),
                    fontSize        = 10
                }
            };

        internal static void SetVisible(VisualElement el, bool visible) =>
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        internal static bool TryHandleUndoRedoShortcut(KeyDownEvent evt)
        {
            bool ctrl = evt.ctrlKey || evt.commandKey;
            if (!ctrl)
                return false;

            if (evt.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                return true;
            }

            if (evt.keyCode != KeyCode.Z)
                return false;

            if (evt.shiftKey)
                Undo.PerformRedo();
            else
                Undo.PerformUndo();

            return true;
        }

        internal static bool TryHandleUndoRedoCommand(string commandName)
        {
            if (string.Equals(commandName, "Redo", StringComparison.Ordinal))
            {
                Undo.PerformRedo();
                return true;
            }

            if (string.Equals(commandName, "Undo", StringComparison.Ordinal))
            {
                Undo.PerformUndo();
                return true;
            }

            return false;
        }

        internal static bool IsUndoRedoCommand(string commandName) =>
            string.Equals(commandName, "Undo", StringComparison.Ordinal)
            || string.Equals(commandName, "Redo", StringComparison.Ordinal);

        internal float ClampInspectorWidth(float width)
        {
            float desired = width <= 0f ? DefaultInspectorWidth : width;

            float availableWidth = MainArea?.resolvedStyle.width > 0f
                ? MainArea.resolvedStyle.width
                : position.width;
            float maxWidth = Mathf.Max(MinInspectorWidth, availableWidth - MinTimelineWidth - InspectorHandleWidth - 1f);

            return Mathf.Clamp(desired, MinInspectorWidth, maxWidth);
        }

        internal void ApplyInspectorWidth(float width)
        {
            _inspectorWidth = ClampInspectorWidth(width);
            if (RightPanel != null)
                RightPanel.style.width = _inspectorWidth;
        }

        private void RestoreKeyframeSelectionAfterUndoRedo(
            SkillKeyframeData previousPrimary,
            string previousRowTag,
            int previousKeyIndex,
            List<SkillKeyframeData> previousSelection)
        {
            ClearKeyframeSelection();

            SkillPresentationTimeline timeline = GetEditableTimeline();
            if (timeline == null) return;

            if (previousSelection != null)
            {
                for (int i = 0; i < previousSelection.Count; i++)
                {
                    SkillKeyframeData key = previousSelection[i];
                    if (key != null && GetRowTagForKeyframe(key, timeline) != null)
                        SelectedKeyframes.Add(key);
                }
            }

            SkillKeyframeData restoredPrimary = null;
            if (previousPrimary != null && GetRowTagForKeyframe(previousPrimary, timeline) != null)
            {
                restoredPrimary = previousPrimary;
            }
            else if (!string.IsNullOrEmpty(previousRowTag))
            {
                restoredPrimary = GetKeyframeByRowAndIndex(timeline, previousRowTag, previousKeyIndex);
            }
            else if (SelectedKeyframes.Count > 0)
            {
                foreach (SkillKeyframeData key in SelectedKeyframes)
                {
                    restoredPrimary = key;
                    break;
                }
            }

            if (restoredPrimary == null) return;

            _selectedKeyframe = restoredPrimary;
            _selectedRowTag = GetRowTagForKeyframe(restoredPrimary, timeline);
            _selectedKeyIndex = GetKeyframeIndexInRow(timeline, _selectedRowTag, restoredPrimary);
            SelectedKeyframes.Add(restoredPrimary);
        }

        private static SkillKeyframeData GetKeyframeByRowAndIndex(
            SkillPresentationTimeline timeline,
            string rowTag,
            int keyIndex)
        {
            if (timeline == null || keyIndex < 0) return null;

            List<SkillKeyframeData> keys = GetTrackKeyframes(timeline, rowTag);
            return keys != null && keyIndex < keys.Count ? keys[keyIndex] : null;
        }

        private static int GetKeyframeIndexInRow(
            SkillPresentationTimeline timeline,
            string rowTag,
            SkillKeyframeData keyframe)
        {
            if (timeline == null || keyframe == null || string.IsNullOrEmpty(rowTag)) return -1;
            List<SkillKeyframeData> keys = GetTrackKeyframes(timeline, rowTag);
            return keys?.IndexOf(keyframe) ?? -1;
        }

        private static List<SkillKeyframeData> GetTrackKeyframes(SkillPresentationTimeline tl, string rowTag)
        {
            if (tl == null || rowTag == null) return null;
            if (rowTag.StartsWith("vfx_"))
            {
                ParseVfxRowTag(rowTag, out int vfxIdx, out _);
                if (vfxIdx >= 0 && tl.vfxObjects != null && vfxIdx < tl.vfxObjects.Count)
                    return tl.vfxObjects[vfxIdx]?.keyframes;
                return null;
            }
            if (rowTag.StartsWith("camera_")) return tl.cameraTrack?.keyframes;
            if (rowTag.StartsWith("caster_")) return tl.casterTrack?.keyframes;
            return rowTag switch
            {
                "animation" => tl.animationTrack?.keyframes,
                "effect"    => tl.effectTrack?.keyframes,
                "ui"        => tl.uiTrack?.keyframes,
                "sfx"       => tl.sfxTrack?.keyframes,
                _           => null
            };
        }
    }
}
