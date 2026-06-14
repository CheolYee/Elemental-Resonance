using Battle.Map.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow : EditorWindow
    {
        // ── 상수 ──────────────────────────────────────────────────────────────────

        private const float NodeWidth       = 55f;
        private const float NodeHeight      = 55f;
        private const float FloorSpacing    = 110f;
        private const float BottomPadding   = 60f;
        private const float TopPadding      = 60f;
        private const float InspectorWidth  = 230f;
        private const float ValidationHeight = 120f;

        // ── 공유 상태 ────────────────────────────────────────────────────────────

        [SerializeField] private MapGraphSO    _target;
        private MapNodeDefinition              _selectedNode;

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
            _selectedNode = null;
            RefreshAll();
        }

        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            bool ctrl = evt.ctrlKey || evt.commandKey;
            if (!ctrl) return;
            if (evt.keyCode == KeyCode.Z)
            {
                if (evt.shiftKey) Undo.PerformRedo();
                else Undo.PerformUndo();
                evt.StopImmediatePropagation();
            }
            else if (evt.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                evt.StopImmediatePropagation();
            }
        }

        // ── 외부 API ─────────────────────────────────────────────────────────────

        internal void SetTarget(MapGraphSO graph)
        {
            _target       = graph;
            _selectedNode = null;
            if (_canvasContainer != null)
                _canvasContainer.style.height = ComputeCanvasHeight();
            RefreshAll();
        }

        // ── 공용 유틸 ────────────────────────────────────────────────────────────

        internal float ComputeCanvasHeight()
        {
            int maxFloor = _target != null ? _target.GetMaxFloorIndex() : 0;
            return BottomPadding + TopPadding + (maxFloor + 1) * FloorSpacing + NodeHeight;
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
