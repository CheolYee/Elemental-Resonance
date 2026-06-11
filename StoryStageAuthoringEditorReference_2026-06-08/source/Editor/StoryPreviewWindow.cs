using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Aspect;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Camera;
using Gamelib.SoundSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 스토리 스테이지 미리보기 및 캐릭터 배치 authoring 전용 EditorWindow.
    ///
    /// ■ 구조
    ///   - StoryPreviewWindow.cs          : 핵심 필드 / 생명주기 / 외부 API / 공용 유틸
    ///   - StoryPreviewWindow.Layout.cs   : UI 계층 빌드 (TopBar / LeftPanel / StageRenderingRoot)
    ///   - StoryPreviewWindow.StageWorld.cs : Stage World pan/zoom / GameView 감지
    ///   - StoryPreviewWindow.Actors.cs   : 액터 레이어 렌더링 / 드래그 인터랙션
    ///   - StoryPreviewWindow.Background.cs : 배경 레이어 렌더링
    ///   - StoryPreviewWindow.Inspector.cs  : 액터 목록 / 인스펙터 / SO 저장
    ///   - StoryPreviewWindow.Playback.cs   : 재생 흐름 / 스테이지 상태 누적 / 대화·선택지
    /// </summary>
    public sealed partial class StoryPreviewWindow : EditorWindow
    {
        private enum PreviewMode { RuntimePreview, StageAuthoring }
        private enum DialogueDisplayMode { RenderOnly, EditorOnly, Both, None }
        private enum StageSelectionKind { None, Actor, Background, Camera, Sound }
        private enum InteractionContext { None, Stage, Timeline }
        private enum PreviewUndoDirection { None, Undo, Redo }
        private enum DragAxisLock { None, X, Y }
        private enum ActorScaleHandle { None, TopLeft, TopRight, BottomLeft, BottomRight }

        // ── 메뉴 / 열기 ──────────────────────────────

        [MenuItem("Tools/Story/Story Preview")]
        public static StoryPreviewWindow Open()
        {
            var w = GetWindow<StoryPreviewWindow>("Story Preview");
            w.minSize = new Vector2(780, 520);
            return w;
        }

        internal static bool TryGetOpenPreviewWindow(out StoryPreviewWindow window)
        {
            window = HasOpenInstances<StoryPreviewWindow>()
                ? GetWindow<StoryPreviewWindow>(false, null, false)
                : null;
            return window != null;
        }

        // ── 외부 진입점 ──────────────────────────────

        /// <summary>그래프 에디터가 에피소드를 변경했을 때 호출.</summary>
        public static void NotifyEpisodeChanged(StoryEpisodeSO episode)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).SetEpisode(episode);
        }

        /// <summary>그래프 에디터에서 라인 노드를 선택했을 때 호출.</summary>
        public static void NotifyLineSelected(StoryLineSO line)
        {
            if (!HasOpenInstances<StoryPreviewWindow>()) return;
            GetWindow<StoryPreviewWindow>(false, null, false).OnExternalLineSelected(line);
        }

        // ── 렌더 관련 상수 ────────────────────────────

        private const int   FallbackRenderWidth  = 1080;
        private const int   FallbackRenderHeight = 1920;
        private const float ActorWidthFrac       = 0.24f;
        private const float DefaultActorAspect   = 1.8f;
        private const float ActorMinHeightScale  = 0.45f;
        private const float ActorMaxHeightScale  = 1.10f;
        private const float InspectorWidth       = 220f;
        private const float DialoguePanelH       = 80f;
        private const string PrefsKeyPrefix           = "CheolYee.StoryPreview.";
        private const string PrefsKeyAspectSettingsGuid = PrefsKeyPrefix + "AspectSettingsGuid";
        private const string PrefsKeyCameraInitGuid     = PrefsKeyPrefix + "CameraInitGuid";

        // ── Stage World 상수 ───────────────────────────

        /// <summary>zoom = 1 일 때, 에디터 픽셀 기준 카메라 프레임 너비.</summary>
        private const float DefaultUnitPixels = 540f;
        private const float WorldPaddingPixels = 960f;
        private const float GridMinorPixels = 40f;
        private const float GridMajorPixels = 160f;
        private const float AuthoringFitFill = 0.88f;
        private const float RuntimeFitFill   = 0.92f;
        private const float MinZoom           = 0.12f;
        private const float MaxZoom           = 4f;
        private const float ZoomStep          = 0.03f;
        private const float MinInspectorWidth = 180f;
        private const float MaxInspectorWidth = 460f;
        private const float DefaultTimelineHeight = 260f;
        private const float MinTimelineHeight = 180f;
        private const float MaxTimelineHeight = 420f;
        private const float DefaultTimelinePixelsPerSecond = 120f;
        private const float MinTimelinePixelsPerSecond = 48f;
        private const float MaxTimelinePixelsPerSecond = 360f;
        private const string MotionPresetFolder = "Assets/00. Work/CheolYee/05. SO/Story/MotionPresets";

        // ── 에피소드 / 재생 상태 ──────────────────────

        [SerializeField] private StoryEpisodeSO episode;
        [SerializeField] private PreviewMode    previewMode = PreviewMode.RuntimePreview;
        [SerializeField] private DialogueDisplayMode dialogueDisplayMode = DialogueDisplayMode.RenderOnly;

        private StoryLineSO _currentLine;
        private bool        _isPlaying;
        private bool        _isLineSample;
        private StoryLineSO _pendingFromLine;

        // ── Aspect Settings ───────────────────────────

        private StoryAspectSettingsSO    _previewAspectSettings;
        private StoryCameraInitSettingsSO _previewCameraInitSettings;

        // ── Phase 3: Letterbox overlay ────────────────

        private VisualElement _letterboxLeftOverlay;
        private VisualElement _letterboxRightOverlay;
        private Label _aspectMetricsInfoLabel;

        // ── GameView 해상도 ────────────────────────────

        private Vector2Int _renderResolution       = new(FallbackRenderWidth, FallbackRenderHeight);
        private string     _renderResolutionSource = "Fallback";

        // ── 스테이지 누적 상태 ────────────────────────

        private readonly Dictionary<string, StoryActorStateData> _stageState = new();
        private StoryBackgroundStateData _bgState;
        private StoryCameraStateData _previewCameraSampleState;

        // ── Stage World (pan / zoom) ───────────────────

        private float   _stageZoom      = 1f;
        private Vector2 _stagePanOffset;
        private bool    _isPanDragging;
        private Vector2 _panDragStartMouse;
        private Vector2 _panDragStartOffset;

        // ── 액터 드래그 인터랙션 ──────────────────────

        private StageSelectionKind _selectionKind;
        private string              _selectedActorKey;
        private string              _draggingActorKey;
        private Vector2               _dragStartPanelPos;
        private Vector2               _dragStartNormPos;
        private DragAxisLock          _dragAxisLock;

        // ── 카메라 gizmo 드래그 인터랙션 ─────────────

        private bool    _isDraggingCamera;
        private Vector2 _cameraDragStartPanelPos;
        private Vector2 _cameraDragStartStagePos;
        private int     _cameraDragPointerId = -1;

        // ── Background 드래그 인터랙션 ─────────────

        private bool    _isDraggingBackground;
        private Vector2 _bgDragStartPanelPos;
        private Vector2 _bgDragStartStagePos;

        private string           _scalingActorKey;
        private ActorScaleHandle _activeScaleHandle;
        private Vector2          _scaleStartPanelPos;
        private Vector2          _scaleStartNormPos;
        private Vector2          _scaleStartScale;
        private Rect             _scaleStartRect;
        private DragAxisLock     _scaleAxisLock;

        // ── UI 참조: 상단 바 ──────────────────────────

        private ObjectField _episodeField;
        private EnumField   _previewModeField;
        private EnumField   _dialogueDisplayField;
        private Label       _statusLabel;
        private Button      _playBtn;
        private Button      _fromHereBtn;
        private Button      _sampleLineBtn;
        private Button      _prevLineBtn;
        private Button      _nextLineAuthoringBtn;
        private Button      _stopBtn;
        private Button      _nextBtn;
        private Button      _refreshGameViewBtn;
        private Button      _workspaceModeBtn;
        private Button      _collapseInspectorBtn;
        private Button      _collapseRuntimeUiBtn;
        private Label       _renderAreaLabel;
        private VisualElement _authoringToolsRoot;
        private ObjectField   _addActorField;
        private ObjectField   _setBackgroundField;
        private Button        _addActorBtn;
        private Button        _removeSelectedActorBtn;
        private Button        _importPreviousStageBtn;
        private Button        _previewTransitionBtn;
        private Button        _setBackgroundBtn;
        private Button        _clearBackgroundBtn;

        // ── UI 참조: Stage World ───────────────────────

        private VisualElement _stageWrapper;       // overflow=Hidden, pan/zoom 이벤트 수신
        private VisualElement _stageWorld;         // transform.position/scale 으로 pan/zoom 적용
        private VisualElement _authoringGridLayer;
        private VisualElement _cameraFrameGuide;   // 카메라 뷰 영역 가이드 테두리
        private VisualElement _focusPreviewFrameGuide;
        private VisualElement _cameraGizmoLayer;
        private VisualElement _backgroundLayer;    // 배경 레이어 (world 공간)
        private VisualElement _actorLayer;         // 액터 레이어 (world 공간, 프레임 밖 허용)
        private Label         _emptyStageLabel;

        // ── UI 참조: 카메라 프레임 내부 오버레이 ──────

        private VisualElement _renderChoiceArea;
        private VisualElement _renderDialoguePanel;
        private Label         _renderSpeakerLabel;
        private Label         _renderDialogueLabel;

        // ── UI 참조: RuntimePreview 전용 하단 패널 ────

        private VisualElement _dialoguePanel;
        private Label         _speakerLabel;
        private Label         _dialogueLabel;
        private VisualElement _choiceArea;

        // ── UI 참조: 라인 정보 라벨 ──────────────────

        private Label _lineInfoSpeakerLabel;
        private Label _lineInfoDialogueLabel;

        // ── UI 참조: Aspect Settings ──────────────────

        private ObjectField _aspectSettingsField;
        private VisualElement _soundSettingsPanelRoot;
        private VisualElement _episodeSoundDefaultsRoot;

        // ── UI 참조: 우측 인스펙터 ────────────────────

        private VisualElement _actorListRoot;
        private VisualElement _inspectorRoot;
        private VisualElement _inspectorPanel;
        private ScrollView    _inspectorScrollView;
        private VisualElement _inspectorSplitter;
        private bool _previewInspectorCollapsed;
        private bool _previewRuntimeUiCollapsed;
        private float _previewInspectorExpandedWidth = InspectorWidth;
        private bool _isInspectorResizing;
        private float _inspectorResizeStartX;
        private float _inspectorResizeStartWidth;

        // ── UI 참조: Keyframe timeline ────────────────

        private VisualElement _timelineResizeHandle;
        private VisualElement _timelinePanel;
        private VisualElement _timelineToolbar;
        private VisualElement _timelineRuler;
        private VisualElement _timelineRows;
        private VisualElement _timelinePlayhead;
        private Label _timelineTitleLabel;
        private Button _timelinePlayBtn;
        private Button _timelineRecordBtn;
        private FloatField _timelineSpeedField;
        private Button     _timelineSnapBtn;
        private FloatField _timelineSnapField;
        private float _timelineHeight = DefaultTimelineHeight;
        private float _timelinePixelsPerSecond = DefaultTimelinePixelsPerSecond;
        private float _timelinePlayheadTime;
        private float _timelinePlaybackSpeed = 1f;
        private float _timelineSnapInterval  = 0.1f;
        private bool  _timelineSnapEnabled;
        private bool _timelineRecordEnabled;
        private bool _timelineIsPlaying;
        private string _timelineRecordActorKey;
        private StageSelectionKind _timelineRecordSelectionKind = StageSelectionKind.None;
        private bool _isTimelinePlayheadDragging;
        private int _activeTimelinePointerId = -1;
        private double _timelinePlaybackStartedAt;
        private float _timelinePlaybackStartTime;
        private bool _isTimelineResizing;
        private float _timelineResizeStartY;
        private float _timelineResizeStartHeight;
        private int _selectedTimelineKeyIndex = -1;
        private int _selectedTimelineSegmentKeyIndex = -1;
        private bool _isTimelineKeyDragging;
        private bool _isTimelineGroupKeyDragging;
        private bool _isTimelineBoxSelecting;
        private bool _suppressTimelineUndoRecording;
        private bool _timelineDragUndoActive;
        private string _draggingTimelineActorKey;
        private int _draggingTimelineKeyIndex = -1;
        private StageSelectionKind _draggingTimelineSelectionKind;
        private float _draggingTimelineKeyStartTime;
        private float _timelineKeyDragStartPanelX;
        private Vector2 _timelineBoxSelectStart;
        private VisualElement _timelineSelectionBox;
        private StoryActorKeyframeProperty _selectedTimelineProperty = StoryActorKeyframeProperty.Position;
        private StoryActorKeyframeProperty _selectedTimelineSegmentProperty = StoryActorKeyframeProperty.Position;
        private StoryActorKeyframeData _timelineClipboardKey;
        private StoryActorKeyframeProperty _timelineClipboardProperty = StoryActorKeyframeProperty.Position;
        private StageSelectionKind _timelineClipboardSelectionKind;
        private readonly HashSet<StoryActorKeyframeData> _selectedTimelineKeys = new();
        private readonly Dictionary<StoryActorKeyframeData, VisualElement> _timelineKeyElements = new();
        private readonly List<TimelineKeyDragState> _timelineKeyDragStates = new();
        private readonly List<StoryActorKeyframeData> _timelineClipboardKeys = new();
        private readonly Dictionary<StoryBgmKeyframeData, StoryActorKeyframeData> _bgmTimelineKeyProxies = new();
        private readonly Dictionary<StorySfxKeyframeData, StoryActorKeyframeData> _sfxTimelineKeyProxies = new();
        private readonly Dictionary<StoryActorKeyframeData, StoryBgmKeyframeData> _timelineProxyToBgmKeys = new();
        private readonly Dictionary<StoryActorKeyframeData, StorySfxKeyframeData> _timelineProxyToSfxKeys = new();
        private readonly List<PreviewUndoEntry> _previewUndoEntries = new();
        private readonly List<PreviewUndoEntry> _previewRedoEntries = new();
        private InteractionContext _interactionContext = InteractionContext.Stage;
        private PreviewUndoDirection _pendingPreviewUndoDirection;

        private sealed class TimelineKeyDragState
        {
            public StoryActorKeyframeData key;
            public float startTime;
        }

        private sealed class PreviewUndoEntry
        {
            public int groupId;
            public InteractionContext context;
            public string name;
        }

        // actor → VisualElement 매핑
        private readonly Dictionary<string, VisualElement> _actorElements = new();

        // ── Line transition preview ────────────────────

        private bool _isTransitionPreviewing;
        private bool _transitionActorsInitialized; // true after first RebuildActorLayer for this transition
        private double _transitionPreviewStartedAt;
        private float _transitionPreviewDuration = 0.35f;
        private readonly Dictionary<string, StoryActorStateData> _transitionFromActors = new();
        private readonly Dictionary<string, StoryActorStateData> _transitionToActors = new();
        private readonly Dictionary<string, StoryActorTrackData> _transitionActorTracks = new();
        private StoryBackgroundTrackData _transitionBackgroundTrack;
        private StoryCameraTrackData _transitionCameraTrack;
        private StoryBackgroundStateData _transitionFromBackground;
        private StoryBackgroundStateData _transitionToBackground;
        private string _transitionCameraFocusTarget = "";
        private float _transitionPreviewElapsed;

        // ── 생명주기 ─────────────────────────────────

        private void CreateGUI()
        {
            LoadPreviewLayoutPrefs();

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            root.Add(BuildTopBar());

            var main = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            main.Add(BuildLeftPanel());
            main.Add(BuildInspectorSplitter());
            main.Add(BuildActorInspector());
            root.Add(main);

            RefreshRenderAreaFromGameView();
            ApplyPreviewLayoutVisibility();
            ApplyPreviewModeVisibility();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += UpdateTransitionPreview;
            EditorApplication.update += UpdateTimelinePlayback;
            LoadAspectSettingsFromPrefs();
            LoadCameraInitSettingsFromPrefs();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= UpdateTransitionPreview;
            EditorApplication.update -= UpdateTimelinePlayback;
            SavePreviewLayoutPrefs();
            SaveAspectSettingsGuid();
            SaveCameraInitGuid();
        }

        private void LoadAspectSettingsFromPrefs()
        {
            string guid = EditorPrefs.GetString(PrefsKeyAspectSettingsGuid, "");
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            _previewAspectSettings = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<StoryAspectSettingsSO>(path);
        }

        private void SaveAspectSettingsGuid()
        {
            if (_previewAspectSettings != null)
            {
                string path = AssetDatabase.GetAssetPath(_previewAspectSettings);
                EditorPrefs.SetString(PrefsKeyAspectSettingsGuid, AssetDatabase.AssetPathToGUID(path));
            }
            else
            {
                EditorPrefs.DeleteKey(PrefsKeyAspectSettingsGuid);
            }
        }

        private void LoadCameraInitSettingsFromPrefs()
        {
            string guid = EditorPrefs.GetString(PrefsKeyCameraInitGuid, "");
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            _previewCameraInitSettings = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<StoryCameraInitSettingsSO>(path);
        }

        private void SaveCameraInitGuid()
        {
            if (_previewCameraInitSettings != null)
            {
                string path = AssetDatabase.GetAssetPath(_previewCameraInitSettings);
                EditorPrefs.SetString(PrefsKeyCameraInitGuid, AssetDatabase.AssetPathToGUID(path));
            }
            else
            {
                EditorPrefs.DeleteKey(PrefsKeyCameraInitGuid);
            }
        }

        private StoryCameraStateData CreateDefaultCameraState()
        {
            var state = new StoryCameraStateData();
            if (_previewCameraInitSettings != null)
            {
                state.zoom = _previewCameraInitSettings.DefaultZoom;
                state.stageLocalPosition = _previewCameraInitSettings.DefaultStageLocalPosition;
            }
            return state;
        }
        private void OnFocus()   => RefreshRenderAreaFromGameView();

        private static string FormatUndoName(InteractionContext context, string actionName) =>
            $"{context}: {actionName}";

        private void SetInteractionContext(InteractionContext context)
        {
            if (context == InteractionContext.None)
                return;

            _interactionContext = context;
        }

        private void FocusStageWorkspace()
        {
            _stageWrapper?.Focus();
        }

        private void ShowUndoStatus(InteractionContext context, bool redo)
        {
            if (_statusLabel == null)
                return;

            _statusLabel.text = $"No {context} {(redo ? "redo" : "undo")} available";
        }

        private void RegisterUndoMetadata(InteractionContext context)
        {
            int groupId = Undo.GetCurrentGroup();
            string groupName = Undo.GetCurrentGroupName();
            if (_previewUndoEntries.Count > 0 && _previewUndoEntries[^1].groupId == groupId)
            {
                _previewUndoEntries[^1].context = context;
                _previewUndoEntries[^1].name = groupName;
            }
            else
            {
                _previewUndoEntries.Add(new PreviewUndoEntry
                {
                    groupId = groupId,
                    context = context,
                    name = groupName
                });
            }

            _previewRedoEntries.Clear();
        }

        private void RecordPreviewUndo(UnityEngine.Object target, InteractionContext context, string actionName)
        {
            if (target == null)
                return;

            Undo.RegisterCompleteObjectUndo(target, FormatUndoName(context, actionName));
            RegisterUndoMetadata(context);
        }

        private void RecordStageUndo(UnityEngine.Object target, string actionName) =>
            RecordPreviewUndo(target, InteractionContext.Stage, actionName);

        private void RecordTimelineUndo(UnityEngine.Object target, string actionName) =>
            RecordPreviewUndo(target, InteractionContext.Timeline, actionName);

        private bool TryPerformContextUndo(InteractionContext context, bool redo)
        {
            List<PreviewUndoEntry> source = redo ? _previewRedoEntries : _previewUndoEntries;
            if (source.Count == 0)
            {
                ShowUndoStatus(context, redo);
                return true;
            }

            PreviewUndoEntry next = source[^1];
            if (next.context != context)
            {
                ShowUndoStatus(context, redo);
                return true;
            }

            _pendingPreviewUndoDirection = redo ? PreviewUndoDirection.Redo : PreviewUndoDirection.Undo;
            if (redo)
                Undo.PerformRedo();
            else
                Undo.PerformUndo();
            return true;
        }

        private bool HandleStageShortcut(KeyCode keyCode, bool control, bool shift)
        {
            if (!IsStageAuthoringMode)
                return false;

            if (control && keyCode == KeyCode.Z)
            {
                return TryPerformContextUndo(InteractionContext.Stage, redo: shift);
            }

            if (control && keyCode == KeyCode.Y)
            {
                return TryPerformContextUndo(InteractionContext.Stage, redo: true);
            }

            if (keyCode == KeyCode.Delete || keyCode == KeyCode.Backspace)
                return DeleteCurrentStageSelection();

            return false;
        }

        private bool HandleSharedTimelineShortcut(KeyCode keyCode, bool control)
        {
            if (!IsStageAuthoringMode)
                return false;

            if (!control && keyCode == KeyCode.Space)
            {
                ToggleTimelinePlayback();
                return true;
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

        private void OnUndoRedo()
        {
            CancelTimelinePointerDrag();
            StopTransitionPreview(applyTargetState: false);
            if (_pendingPreviewUndoDirection == PreviewUndoDirection.Undo && _previewUndoEntries.Count > 0)
            {
                PreviewUndoEntry entry = _previewUndoEntries[^1];
                _previewUndoEntries.RemoveAt(_previewUndoEntries.Count - 1);
                _previewRedoEntries.Add(entry);
            }
            else if (_pendingPreviewUndoDirection == PreviewUndoDirection.Redo && _previewRedoEntries.Count > 0)
            {
                PreviewUndoEntry entry = _previewRedoEntries[^1];
                _previewRedoEntries.RemoveAt(_previewRedoEntries.Count - 1);
                _previewUndoEntries.Add(entry);
            }
            _pendingPreviewUndoDirection = PreviewUndoDirection.None;
            BuildStageStateAt(_currentLine);
            if (_selectionKind == StageSelectionKind.Actor && !_stageState.ContainsKey(_selectedActorKey))
                ClearStageSelection();
            ValidateTimelineSelection();
            ClearSoundTimelineProxyCache();
            UpdateSelectedSoundKeysFromTimelineSelection();
            RebuildActorLayer();
            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
            Repaint();
        }

        private void OnGUI()
        {
            var e = Event.current;
            if (previewMode != PreviewMode.StageAuthoring)
                return;

            if (e.type == EventType.MouseUp && _activeTimelinePointerId >= 0)
            {
                CancelTimelinePointerDrag();
                return;
            }
        }

        private void OnRootKeyDown(KeyDownEvent e)
        {
            if (!IsStageAuthoringMode)
                return;

            if (IsFocusedOnTextInput(rootVisualElement))
                return;

            if (HandleSharedTimelineShortcut(e.keyCode, e.ctrlKey || e.commandKey))
            {
                e.StopPropagation();
                return;
            }

            bool handled = _interactionContext == InteractionContext.Timeline
                ? HandleTimelineShortcut(e.keyCode, e.ctrlKey || e.commandKey, e.shiftKey)
                : _interactionContext == InteractionContext.Stage
                    ? HandleStageShortcut(e.keyCode, e.ctrlKey || e.commandKey, e.shiftKey)
                    : false;
            if (handled)
                e.StopPropagation();
        }

        // ── 외부 API ─────────────────────────────────

        public void SetEpisode(StoryEpisodeSO ep)
        {
            episode = ep;
            if (_episodeField != null) _episodeField.SetValueWithoutNotify(ep);
            StopPlayback();
            _stageState.Clear();
            _bgState = null;
            ClearSoundTimelineSelection();
            ClearSoundTimelineProxyCache();
            RebuildActorLayer();
            RefreshActorList();
            RefreshActorInspector();
            RefreshDialogue();
            RefreshButtons();
        }

        public void OnExternalLineSelected(StoryLineSO line)
        {
            _pendingFromLine = line;
            RefreshButtons();
            if (_isPlaying) return;

            if (line != null) { ShowLineSnapshot(line); return; }

            _isLineSample = false;
            _currentLine  = null;
            _stageState.Clear();
            _bgState = null;
            _actorLayer?.Clear();
            _actorElements.Clear();
            ClearStageSelection();
            RefreshBackgroundLayer();
            RefreshActorList();
            SetEmptyStageVisible(true);
            RefreshDialogue();
            RefreshChoices();
            RefreshActorInspector();
        }

        // ── 공용 유틸 (partial 파일 간 공유) ─────────

        internal static Button MakeBtn(string text, Color bg, Action click) =>
            new(click)
            {
                text = text,
                style = { height = 22, paddingLeft = 8, paddingRight = 8, marginRight = 4, backgroundColor = new StyleColor(bg), fontSize = 10 }
            };

        private bool IsStageAuthoringMode => previewMode == PreviewMode.StageAuthoring;
        private bool IsRuntimePreviewMode => previewMode == PreviewMode.RuntimePreview;

        internal static Color ActorPlaceholderColor(CharacterDefinitionSO actor)
        {
            int   hash = (actor.CharacterId ?? actor.name).GetHashCode();
            float h    = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(h, 0.55f, 0.72f);
        }

        internal static void SetElementVisible(VisualElement el, bool visible)
        {
            if (el != null) el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static Label MakeBoldLabel(string text) =>
            new(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10, marginBottom = 3 } };

        internal static VisualElement MakeSeparator() =>
            new() { style = { height = 1, backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f)), marginTop = 5, marginBottom = 5 } };

        private static bool IsFocusedOnTextInput(VisualElement root)
        {
            var element = root?.focusController?.focusedElement as VisualElement;
            while (element != null)
            {
                if (element is TextField or IntegerField or FloatField or LongField or Vector2Field)
                    return true;

                element = element.parent;
            }

            return false;
        }
    }
}
