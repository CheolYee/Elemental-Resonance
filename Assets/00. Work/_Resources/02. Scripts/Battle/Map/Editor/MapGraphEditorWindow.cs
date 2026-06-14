using Battle.Map.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow : EditorWindow
    {
        // ── 고정 상수 (UI 레이아웃) ───────────────────────────────────────────────

        private const float NodeWidth     = 55f;
        private const float NodeHeight    = 55f;
        private const float BottomPadding = 60f;
        private const float TopPadding    = 60f;

        // ── 리사이즈 가능 패널 크기 ──────────────────────────────────────────────

        [SerializeField] private float _inspectorWidth   = 230f;
        [SerializeField] private float _validationHeight = 120f;

        // ── 리사이즈 드래그 상태 ─────────────────────────────────────────────────

        private float _inspectorDragStartX;
        private float _inspectorStartWidth;

        private float _validationDragStartY;
        private float _validationStartHeight;

        // ── 런타임 동기화 세팅 (툴바에서 직접 편집) ─────────────────────────────

        [SerializeField] private float _xOffsetScale = 400f;
        [SerializeField] private float _floorSpacing = 150f;

        // ── 스냅 ─────────────────────────────────────────────────────────────────

        [SerializeField] private bool  _snapEnabled = false;
        [SerializeField] private float _snapStep    = 0.1f;

        // ── 공유 상태 ────────────────────────────────────────────────────────────

        [SerializeField] private MapGraphSO   _target;
        private MapNodeDefinition             _selectedNode;

        // ── 드래그 상태 ──────────────────────────────────────────────────────────

        private float             _nodeDragStartMouseX;
        private float             _nodeDragStartXOffset;

        private bool              _isDraggingPort;
        private MapNodeDefinition _portDragFromNode;
        private Vector2           _portDragCurrentPos;

        // ── 연결선 선택 상태 ─────────────────────────────────────────────────────

        private bool   _hasSelectedConnection;
        private string _selectedConnectionFrom;
        private string _selectedConnectionTo;

        // ── 클립보드 ─────────────────────────────────────────────────────────────

        private MapNodeDefinition _clipboard;

        // ── UI 참조 ──────────────────────────────────────────────────────────────

        private IMGUIContainer _canvasContainer;
        private VisualElement  _inspectorPanel;
        private VisualElement  _validationPanel;
        private ObjectField    _graphField;

        // ── 메뉴 ─────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Battle/Map Graph Editor")]
        public static MapGraphEditorWindow Open()
        {
            var w = GetWindow<MapGraphEditorWindow>("Map Graph Editor");
            w.minSize = new Vector2(820, 520);
            return w;
        }

        // ── 생명주기 ─────────────────────────────────────────────────────────────

        private void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildMainArea());
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            if (_target != null) EditorUtility.SetDirty(_target);
            _selectedNode          = null;
            _hasSelectedConnection = false;
            RefreshAll();
        }

        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            bool ctrl = evt.ctrlKey || evt.commandKey;
            if (ctrl)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Z:
                        if (evt.shiftKey) Undo.PerformRedo();
                        else              Undo.PerformUndo();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.Y:
                        Undo.PerformRedo();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.C:
                        CopySelected();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.V:
                        PasteClipboard();
                        evt.StopImmediatePropagation();
                        break;
                }
            }
            else if (evt.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                DeleteSelected();
                evt.StopImmediatePropagation();
            }
        }

        // ── 외부 API ─────────────────────────────────────────────────────────────

        internal void SetTarget(MapGraphSO graph)
        {
            _target                = graph;
            _selectedNode          = null;
            _hasSelectedConnection = false;
            _isDraggingPort        = false;
            UpdateCanvasHeight();
            RefreshAll();
        }

        // ── 유틸 ─────────────────────────────────────────────────────────────────

        internal float ComputeCanvasHeight()
        {
            int maxFloor = _target != null ? _target.GetMaxFloorIndex() : 0;
            return BottomPadding + TopPadding + (maxFloor + 1) * _floorSpacing + NodeHeight;
        }

        private void UpdateCanvasHeight()
        {
            if (_canvasContainer != null)
                _canvasContainer.style.height = ComputeCanvasHeight();
        }

        private void RefreshAll()
        {
            _canvasContainer?.MarkDirtyRepaint();
            RefreshInspector();
            RefreshValidation();
            Repaint();
        }
    }
}
