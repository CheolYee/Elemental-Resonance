using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryGraphEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Story/Story Graph Editor")]
        public static void Open()
        {
            var w = GetWindow<StoryGraphEditorWindow>("Story Graph Editor");
            w.minSize = new Vector2(1000, 500);
        }

        // ── 상태 ────────────────────────────────────
        // [SerializeField]: domain reload / Play mode 진입 후에도 값 유지
        [SerializeField] private StoryEpisodeSO episode;
        [SerializeField] private float          inspectorWidth = 300f;
        [SerializeField] private bool           inspectorCollapsed;

        [SerializeField] private StoryLineSO _selectedLine;
        private string      _saveFolder        = "";
        private List<StoryGraphNodeView> _currentSelectedNodes = new();

        // ── 클립보드 ─────────────────────────────────
        private List<StoryLineSO> _clipboard          = new();
        private List<Vector2>     _clipboardRelPos    = new();

        // ── UI 참조 ─────────────────────────────────
        private StoryGraphCanvasView     _canvas;
        private StoryGraphInspectorPanel _inspectorPanel;
        private ObjectField              _episodeField;   // 복원 시 SetValueWithoutNotify 용
        private Label                    _statusLineId;
        private Label                    _statusNextId;
        private Label                    _statusWarning;
        private Button                   _clearNextBtn;
        private Label                    _folderDisplay;
        private VisualElement            _splitter;
        private Button                   _collapseInspectorBtn;

        // ── Splitter 드래그 상태 ─────────────────────
        private bool  _splitterDragging;
        private float _splitterStartX;
        private float _splitterStartWidth;

        private const float InspectorMinWidth = 220f;
        private const float InspectorMaxWidth = 700f;
        private const string GraphPrefsKeyPrefix = "CheolYee.StoryGraph.";

        // ── 생명주기 ─────────────────────────────────

        private void OnEnable()
        {
            LoadGraphLayoutPrefs();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            StoryBulkLineImportWindow.LinesCreated    += OnBulkLinesCreated;
            StoryBulkLineImportWindow.PreviewChanged  += OnBulkPreviewChanged;
            StoryBulkLineImportWindow.PreviewCleared  += OnBulkPreviewCleared;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            StoryBulkLineImportWindow.LinesCreated   -= OnBulkLinesCreated;
            StoryBulkLineImportWindow.PreviewChanged -= OnBulkPreviewChanged;
            StoryBulkLineImportWindow.PreviewCleared -= OnBulkPreviewCleared;
            SaveGraphLayoutPrefs();
            var key = EpisodePrefsKey;
            if (key != null) _canvas?.SaveViewState(key);
        }

        private string EpisodePrefsKey => episode != null
            ? "StoryGraph_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(episode))
            : null;

        private void OnUndoRedoPerformed()
        {
            // 구조적 변화(노드 추가/삭제)까지 반영하려면 Rebuild 필요
            _selectedLine = null;
            _currentSelectedNodes.Clear();
            RebuildCanvas();
            _inspectorPanel?.SetLine(null, episode);
            RefreshStatusBar();
        }

        private void OnBulkLinesCreated(StoryEpisodeSO created, List<StoryLineSO> newLines)
        {
            if (created != episode) return;
            _selectedLine = null;
            _currentSelectedNodes.Clear();
            RebuildCanvas();
            _canvas.SelectNodes(newLines);
            Repaint();
            RefreshStatusBar();
        }

        private void OnBulkPreviewChanged(Vector2 start, int cols, float xOffset, float yOffset, int count)
        {
            _canvas?.SetBulkImportPreview(start, cols, xOffset, yOffset, count);
            Repaint();
        }

        private void OnBulkPreviewCleared()
        {
            _canvas?.ClearBulkImportPreview();
            Repaint();
        }

        // ── 진입점 ──────────────────────────────────

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            root.Add(BuildTopBar());

            var center = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };

            _canvas = new StoryGraphCanvasView
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _canvas.SelectionChanged    += OnCanvasSelectionChanged;
            _canvas.ConnectionMade      += OnConnectionMade;
            _canvas.NodeDisconnected    += OnNodeDisconnected;
            _canvas.CreateLineRequested += canvasPos => CreateLine(canvasPos);
            center.Add(_canvas);

            center.Add(BuildSplitter());

            _inspectorPanel = new StoryGraphInspectorPanel
            {
                style =
                {
                    width = inspectorWidth
                }
            };
            _inspectorPanel.LineChanged += OnInspectorLineChanged;
            center.Add(_inspectorPanel);
            ApplyGraphInspectorCollapseState();

            root.Add(center);

            root.Add(BuildStatusBar());

            // domain reload / Play mode 진입 후 episode 복원
            if (episode != null)
                RestoreEpisodeToUI();
        }

        // ── Episode 복원 ─────────────────────────────

        /// <summary>
        /// CreateGUI 후 직렬화된 _episode를 ObjectField와 캔버스에 반영한다.
        /// </summary>
        private void RestoreEpisodeToUI()
        {
            if (episode == null || _episodeField == null) return;

            _episodeField.SetValueWithoutNotify(episode);
            _inspectorPanel?.SetLine(null, episode);
            StoryPreviewWindow.NotifyEpisodeChanged(episode);
            LoadFolderFromEpisode();
            RebuildCanvas();

            var key = EpisodePrefsKey;
            if (key != null) _canvas?.LoadViewState(key);

            RestoreSelectedLineAfterCanvasRebuild();
            RefreshStatusBar();
        }

        private void RestoreSelectedLineAfterCanvasRebuild()
        {
            if (_canvas == null)
                return;

            if (IsRestorableSelectedLineValid())
            {
                _canvas.SelectNode(_selectedLine);
                return;
            }

            _selectedLine = null;
            _canvas.SelectNode(null);
        }

        // ── Splitter ─────────────────────────────────

        private VisualElement BuildSplitter()
        {
            var s = new VisualElement
            {
                style =
                {
                    width = 5,
                    flexShrink = 0,
                    alignSelf = Align.Stretch,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f)),
                    cursor = new StyleCursor(StyleKeyword.Auto)
                }
            };
            _splitter = s;

            s.RegisterCallback<MouseEnterEvent>(_ =>
                s.style.backgroundColor = new StyleColor(new Color(0.25f, 0.45f, 0.75f)));
            s.RegisterCallback<MouseLeaveEvent>(_ =>
                s.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f)));

            s.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || inspectorCollapsed) return;
                _splitterDragging   = true;
                _splitterStartX     = evt.position.x;
                _splitterStartWidth = inspectorWidth;
                s.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            s.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_splitterDragging) return;
                // 왼쪽으로 드래그 → inspector 넓어짐
                float delta    = _splitterStartX - evt.position.x;
                float newWidth = Mathf.Clamp(_splitterStartWidth + delta,
                    InspectorMinWidth, InspectorMaxWidth);
                inspectorWidth             = newWidth;
                _inspectorPanel.style.width = newWidth;
                evt.StopPropagation();
            });

            s.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!_splitterDragging) return;
                _splitterDragging = false;
                s.ReleasePointer(evt.pointerId);
                SaveGraphLayoutPrefs();
                evt.StopPropagation();
            });

            return s;
        }

        // ── 키보드 ──────────────────────────────────

        private void OnGUI()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            // Redo: Ctrl+Y / Ctrl+Shift+Z
            if (e.control && !e.shift && e.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                e.Use();
                return;
            }
            if (e.control && e.shift && e.keyCode == KeyCode.Z)
            {
                Undo.PerformRedo();
                e.Use();
                return;
            }

            bool onTextField = StoryGraphInspectorPanel.IsFocusedOnTextField(rootVisualElement);

            // Ctrl+C: Copy
            if (e.control && !e.shift && e.keyCode == KeyCode.C && !onTextField)
            {
                CopySelectedNodes();
                e.Use();
                return;
            }

            // Ctrl+V: Paste
            if (e.control && !e.shift && e.keyCode == KeyCode.V && !onTextField)
            {
                PasteNodes();
                e.Use();
                return;
            }

            // Ctrl+D: Duplicate
            if (e.control && !e.shift && e.keyCode == KeyCode.D && !onTextField)
            {
                DuplicateNodes();
                e.Use();
                return;
            }

            if (e.keyCode != KeyCode.Delete && e.keyCode != KeyCode.Backspace) return;
            if (onTextField) return;

            // 엣지 연결 해제 우선
            var edgeSrc = _canvas?.SelectedEdgeSrc;
            if (edgeSrc != null)
            {
                Undo.RecordObject(edgeSrc.Line, "Disconnect Edge");
                StoryEditorUtility.SetNextLineId(edgeSrc.Line, "");
                _canvas.ClearSelectedEdge();
                _canvas.RefreshAll();
                if (_selectedLine == edgeSrc.Line) _inspectorPanel?.Reload();
                RefreshStatusBar();
                e.Use();
                return;
            }

            if (_canvas?.HasSelectedConnectableEdge == true)
            {
                _canvas.DisconnectSelectedConnectableEdge();
                _canvas.RefreshAll();
                e.Use();
                return;
            }

            // 노드 삭제 (선택된 노드가 있을 때)
            if (_currentSelectedNodes.Count > 0)
            {
                DeleteSelectedNodes();
                e.Use();
                return;
            }

            OnDeleteConnectionClicked();
            e.Use();
        }

        private void OnDeleteConnectionClicked()
        {
            if (_selectedLine == null) return;
            if (string.IsNullOrWhiteSpace(_selectedLine.NextLineId)) return;

            Undo.RecordObject(_selectedLine, "Disconnect nextLineId");
            StoryEditorUtility.SetNextLineId(_selectedLine, "");
            _canvas.RefreshAll();
            _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        // ── 상단 바 ──────────────────────────────────

        private VisualElement BuildTopBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f))
                }
            };

            _episodeField = new ObjectField("Episode")
            {
                objectType = typeof(StoryEpisodeSO),
                style =
                {
                    flexGrow = 1,
                    marginRight = 10
                }
            };
            _episodeField.RegisterValueChangedCallback(evt =>
            {
                var oldKey = EpisodePrefsKey;
                if (oldKey != null) _canvas?.SaveViewState(oldKey);

                episode      = evt.newValue as StoryEpisodeSO;
                _selectedLine = null;
                _inspectorPanel?.SetLine(null, episode);
                StoryPreviewWindow.NotifyEpisodeChanged(episode);
                LoadFolderFromEpisode();
                RebuildCanvas();
                RefreshStatusBar();

                var newKey = EpisodePrefsKey;
                if (newKey != null) _canvas?.LoadViewState(newKey);
            });
            bar.Add(_episodeField);

            var folderLabel = new Label("저장 폴더:")
            {
                style =
                {
                    fontSize = 10,
                    marginRight = 4
                }
            };
            bar.Add(folderLabel);

            _folderDisplay = new Label("(에피소드 폴더)")
            {
                name = "folderDisplay",
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(0.55f, 0.55f, 0.55f)),
                    minWidth = 140,
                    overflow = Overflow.Hidden,
                    marginRight = 4
                }
            };
            bar.Add(_folderDisplay);

            var browseBtn = new Button(OnBrowseFolderClicked)
            {
                text = "…",
                style =
                {
                    width = 28,
                    paddingLeft = 2,
                    paddingRight = 2,
                    marginRight = 10
                }
            };
            bar.Add(browseBtn);

            var addBtn = new Button(OnAddLineClicked)
            {
                text = "+ Line",
                style =
                {
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            bar.Add(addBtn);

            var bulkBtn = new Button(OnBulkImportClicked)
            {
                text = "일괄 입력",
                style =
                {
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8,
                    marginLeft = 4
                }
            };
            bar.Add(bulkBtn);

            var previewBtn = new Button(() => StoryPreviewWindow.Open())
            {
                text = "Preview ▶",
                style =
                {
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8,
                    marginLeft = 6,
                    backgroundColor = new StyleColor(new Color(0.2f, 0.35f, 0.55f))
                }
            };
            bar.Add(previewBtn);

            _collapseInspectorBtn = new Button(ToggleGraphInspectorCollapsed)
            {
                text = inspectorCollapsed ? "Inspector +" : "Inspector -",
                style =
                {
                    height = 22,
                    paddingLeft = 8,
                    paddingRight = 8,
                    marginLeft = 6
                }
            };
            bar.Add(_collapseInspectorBtn);

            return bar;
        }

        // ── 저장 폴더 퍼시스턴스 ────────────────────

        private void LoadFolderFromEpisode()
        {
            if (episode == null)
            {
                _saveFolder            = "";
                _folderDisplay.text    = "(에피소드 폴더)";
                _folderDisplay.tooltip = "";
                return;
            }

            string stored = episode.EditorLineSaveFolder;
            if (!string.IsNullOrEmpty(stored))
            {
                _saveFolder            = stored;
                _folderDisplay.text    = stored;
                _folderDisplay.tooltip = stored;
            }
            else
            {
                _saveFolder            = "";
                _folderDisplay.text    = "(에피소드 폴더)";
                _folderDisplay.tooltip = "";
            }
        }

        private void OnBrowseFolderClicked()
        {
            string folder = EditorUtility.OpenFolderPanel("StoryLineSO 저장 폴더 선택", "Assets", "");
            if (string.IsNullOrEmpty(folder)) return;

            if (folder.StartsWith(Application.dataPath))
                folder = "Assets" + folder[Application.dataPath.Length..];

            _saveFolder            = folder;
            _folderDisplay.text    = folder;
            _folderDisplay.tooltip = folder;

            if (episode != null)
                StoryEditorUtility.SetEditorLineSaveFolder(episode, folder);
        }

        // ── 캔버스 재구성 ────────────────────────────

        private void RebuildCanvas()
        {
            _canvas.Rebuild(episode != null ? episode.Lines : System.Array.Empty<StoryLineSO>());
        }

        // ── 노드 선택 핸들러 ─────────────────────────

        private void OnCanvasSelectionChanged(IReadOnlyList<StoryGraphNodeView> nodes)
        {
            _currentSelectedNodes = new List<StoryGraphNodeView>(nodes);

            if (nodes.Count == 0)
            {
                _selectedLine = null;
                _inspectorPanel?.SetLine(null, episode);
            }
            else if (nodes.Count == 1)
            {
                _selectedLine = nodes[0].Line;
                _inspectorPanel?.SetLine(_selectedLine, episode);
            }
            else
            {
                _selectedLine = null;
                _inspectorPanel?.SetMultipleSelection(nodes.Count);
            }

            // 프리뷰 창에 선택 라인 통보
            StoryLineSO previewLine = nodes.Count == 1 ? nodes[0].Line : null;
            StoryPreviewWindow.NotifyLineSelected(previewLine);

            RefreshStatusBar();
        }

        // ── 연결 핸들러 ──────────────────────────────

        private void OnConnectionMade(StoryGraphNodeView src, StoryGraphNodeView dst)
        {
            if (src?.Line == null || dst?.Line == null) return;

            string newNextId = dst.Line.LineId;
            if (string.IsNullOrWhiteSpace(newNextId))
            {
                EditorUtility.DisplayDialog("연결 실패", "대상 노드의 lineId가 비어 있습니다.", "확인");
                return;
            }

            Undo.RecordObject(src.Line, "Set nextLineId");
            StoryEditorUtility.SetNextLineId(src.Line, newNextId);
            src.Refresh();
            _canvas.RefreshAll();
            if (_selectedLine == src.Line) _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnNodeDisconnected(StoryGraphNodeView node)
        {
            if (_selectedLine == node.Line) _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnInspectorLineChanged()
        {
            _canvas.RefreshAll();
            RefreshStatusBar();
        }

        // ── + Line ───────────────────────────────────

        private void OnAddLineClicked() => CreateLine(null);

        private void OnBulkImportClicked()
        {
            if (episode == null)
            {
                EditorUtility.DisplayDialog("Episode 없음", "상단에서 StoryEpisodeSO를 먼저 선택하세요.", "확인");
                return;
            }

            string folder      = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            Vector2 canvasPos  = _canvas?.CanvasMousePosition ?? Vector2.zero;
            StoryBulkLineImportWindow.Open(episode, folder, canvasPos);
        }

        /// <summary>새 StoryLineSO를 생성한다. canvasPos 가 주어지면 해당 위치에 노드를 배치한다.</summary>
        private void CreateLine(Vector2? canvasPos)
        {
            if (episode == null)
            {
                EditorUtility.DisplayDialog("Episode 없음", "상단에서 StoryEpisodeSO를 먼저 선택하세요.", "확인");
                return;
            }

            string folder = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            var newLine   = StoryEditorUtility.CreateAndAddLine(episode, folder);

            if (canvasPos.HasValue)
                StoryEditorUtility.SetEditorNodePosition(newLine, canvasPos.Value, recordUndo: false);

            RebuildCanvas();
            _canvas.SelectNode(newLine);
            _selectedLine = newLine;
            _inspectorPanel.SetLine(newLine, episode);
            RefreshStatusBar();
        }

        // ── 노드 삭제 (Delete 키) ─────────────────────

        private void DeleteSelectedNodes()
        {
            if (episode == null || _currentSelectedNodes.Count == 0) return;

            string msg = _currentSelectedNodes.Count == 1
                ? $"\"{_currentSelectedNodes[0].Line?.LineId}\" 를 삭제합니다.\nasset 파일도 삭제됩니다. 계속하시겠습니까?"
                : $"선택된 {_currentSelectedNodes.Count}개 라인을 삭제합니다.\nasset 파일도 모두 삭제됩니다. 계속하시겠습니까?";

            if (!EditorUtility.DisplayDialog("Line 삭제", msg, "삭제", "취소")) return;

            var toDelete = new List<StoryLineSO>();
            foreach (var node in _currentSelectedNodes)
                if (node.Line != null) toDelete.Add(node.Line);

            foreach (var line in toDelete)
                StoryEditorUtility.DeleteLine(episode, line);

            _selectedLine = null;
            _currentSelectedNodes.Clear();
            _inspectorPanel.SetLine(null, episode);
            RebuildCanvas();
            RefreshStatusBar();
        }

        // ── Copy / Paste / Duplicate ─────────────────

        private void CopySelectedNodes()
        {
            if (_currentSelectedNodes.Count == 0) return;

            _clipboard.Clear();
            _clipboardRelPos.Clear();

            var positions = new List<Vector2>();
            foreach (var node in _currentSelectedNodes)
            {
                if (node.Line == null) continue;
                _clipboard.Add(node.Line);
                positions.Add(node.Line.EditorNodePosition);
            }

            if (_clipboard.Count == 0) return;

            Vector2 center = Vector2.zero;
            foreach (var p in positions) center += p;
            center /= positions.Count;

            foreach (var p in positions)
                _clipboardRelPos.Add(p - center);
        }

        private void PasteNodes()
        {
            if (episode == null || _clipboard.Count == 0) return;

            string folder      = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            Vector2 pasteCenter = _canvas.CanvasMousePosition;

            var newLines = new List<StoryLineSO>();
            for (int i = 0; i < _clipboard.Count; i++)
            {
                var source = _clipboard[i];
                if (source == null) continue;

                var newLine = StoryEditorUtility.DuplicateLine(episode, source, folder);
                Vector2 relPos = i < _clipboardRelPos.Count ? _clipboardRelPos[i] : Vector2.zero;
                StoryEditorUtility.SetEditorNodePosition(newLine, pasteCenter + relPos);
                newLines.Add(newLine);
            }

            FinishMultiCreate(newLines);
        }

        private void DuplicateNodes()
        {
            if (episode == null || _currentSelectedNodes.Count == 0) return;

            string folder      = string.IsNullOrEmpty(_saveFolder) ? null : _saveFolder;
            Vector2 pasteCenter = _canvas.CanvasMousePosition;

            var positions = new List<Vector2>();
            var sources   = new List<StoryLineSO>();
            foreach (var node in _currentSelectedNodes)
            {
                if (node.Line == null) continue;
                sources.Add(node.Line);
                positions.Add(node.Line.EditorNodePosition);
            }

            if (sources.Count == 0) return;

            Vector2 center = Vector2.zero;
            foreach (var p in positions) center += p;
            center /= positions.Count;

            var newLines = new List<StoryLineSO>();
            for (int i = 0; i < sources.Count; i++)
            {
                var newLine = StoryEditorUtility.DuplicateLine(episode, sources[i], folder);
                StoryEditorUtility.SetEditorNodePosition(newLine, pasteCenter + (positions[i] - center));
                newLines.Add(newLine);
            }

            FinishMultiCreate(newLines);
        }

        private void FinishMultiCreate(List<StoryLineSO> newLines)
        {
            if (newLines.Count == 0) return;

            _selectedLine = null;
            _currentSelectedNodes.Clear();
            RebuildCanvas();
            _canvas.SelectNodes(newLines);

            if (newLines.Count == 1)
            {
                _selectedLine = newLines[0];
                _inspectorPanel.SetLine(_selectedLine, episode);
            }
            else
            {
                _inspectorPanel.SetMultipleSelection(newLines.Count);
            }

            RefreshStatusBar();
        }

        // ── 하단 상태 바 ─────────────────────────────

        private void ToggleGraphInspectorCollapsed()
        {
            if (!inspectorCollapsed)
                CaptureGraphInspectorWidth();

            inspectorCollapsed = !inspectorCollapsed;
            ApplyGraphInspectorCollapseState();
            SaveGraphLayoutPrefs();
        }

        private void ApplyGraphInspectorCollapseState()
        {
            if (_inspectorPanel != null)
            {
                if (inspectorCollapsed)
                {
                    _inspectorPanel.style.display = DisplayStyle.None;
                }
                else
                {
                    _inspectorPanel.style.display = DisplayStyle.Flex;
                    _inspectorPanel.style.width = inspectorWidth;
                }
            }

            if (_splitter != null)
                _splitter.style.display = inspectorCollapsed ? DisplayStyle.None : DisplayStyle.Flex;

            if (_collapseInspectorBtn != null)
                _collapseInspectorBtn.text = inspectorCollapsed ? "Inspector +" : "Inspector -";
        }

        private void CaptureGraphInspectorWidth()
        {
            if (_inspectorPanel == null || inspectorCollapsed)
                return;

            float width = _inspectorPanel.resolvedStyle.width > 0
                ? _inspectorPanel.resolvedStyle.width
                : inspectorWidth;
            inspectorWidth = Mathf.Clamp(width, InspectorMinWidth, InspectorMaxWidth);
        }

        private void LoadGraphLayoutPrefs()
        {
            inspectorCollapsed = EditorPrefs.GetBool(GraphPrefsKeyPrefix + "InspectorCollapsed", inspectorCollapsed);
            inspectorWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(GraphPrefsKeyPrefix + "InspectorWidth", inspectorWidth),
                InspectorMinWidth,
                InspectorMaxWidth);
        }

        private void SaveGraphLayoutPrefs()
        {
            EditorPrefs.SetBool(GraphPrefsKeyPrefix + "InspectorCollapsed", inspectorCollapsed);
            EditorPrefs.SetFloat(GraphPrefsKeyPrefix + "InspectorWidth", inspectorWidth);
        }

        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 5,
                    paddingBottom = 5,
                    borderTopWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f)),
                    backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.16f))
                }
            };

            _statusLineId = new Label("—")
            {
                style =
                {
                    fontSize = 11,
                    marginRight = 14,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            _statusNextId = new Label("")
            {
                style =
                {
                    fontSize = 11,
                    marginRight = 14,
                    color = new StyleColor(new Color(0.65f, 0.65f, 0.65f))
                }
            };

            _statusWarning = new Label("")
            {
                style =
                {
                    fontSize = 11,
                    flexGrow = 1,
                    color = new StyleColor(new Color(1f, 0.6f, 0.2f))
                }
            };

            var hintLbl = new Label("[우클릭] 라인 추가  [Del] 삭제  [Ctrl+C/V] 복사/붙여넣기  [Ctrl+D] 복제")
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(0.45f, 0.45f, 0.45f)),
                    marginRight = 10
                }
            };

            _clearNextBtn = new Button(OnClearNextClicked)
            {
                text = "Next 지우기",
                style =
                {
                    height = 20,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };

            var deleteBtn = new Button(OnDeleteLineClicked)
            {
                text = "Delete Line",
                style =
                {
                    height = 20,
                    paddingLeft = 8,
                    paddingRight = 8,
                    marginLeft = 6,
                    backgroundColor = new StyleColor(new Color(0.55f, 0.15f, 0.15f))
                }
            };

            bar.Add(_statusLineId);
            bar.Add(_statusNextId);
            bar.Add(_statusWarning);
            bar.Add(hintLbl);
            bar.Add(_clearNextBtn);
            bar.Add(deleteBtn);

            return bar;
        }

        private void RefreshStatusBar()
        {
            if (_selectedLine == null)
            {
                _statusLineId.text  = "—";
                _statusNextId.text  = "";
                _statusWarning.text = "";
                _clearNextBtn.SetEnabled(false);
                return;
            }

            bool idEmpty   = string.IsNullOrWhiteSpace(_selectedLine.LineId);
            bool nextEmpty = string.IsNullOrWhiteSpace(_selectedLine.NextLineId);

            _statusLineId.text = idEmpty   ? "⚠ ID 없음" : _selectedLine.LineId;
            _statusNextId.text = nextEmpty ? "→ (종료)"  : $"→ {_selectedLine.NextLineId}";
            _clearNextBtn.SetEnabled(!nextEmpty);

            var warnings = new List<string>();
            if (idEmpty) warnings.Add("lineId 없음");
            if (!nextEmpty && episode != null && !EpisodeContains(episode, _selectedLine.NextLineId))
                warnings.Add($"\"{_selectedLine.NextLineId}\" 에피소드에 없음");
            if (!nextEmpty && _selectedLine.NextLineId == _selectedLine.LineId)
                warnings.Add("자기 자신 참조");

            _statusWarning.text = warnings.Count > 0 ? "⚠ " + string.Join(" / ", warnings) : "";
        }

        private void OnClearNextClicked()
        {
            if (_selectedLine == null) return;
            Undo.RecordObject(_selectedLine, "Clear nextLineId");
            StoryEditorUtility.SetNextLineId(_selectedLine, "");
            _canvas.RefreshAll();
            _inspectorPanel.Reload();
            RefreshStatusBar();
        }

        private void OnDeleteLineClicked()
        {
            if (_selectedLine == null || episode == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Line 삭제",
                $"\"{_selectedLine.LineId}\" 를 삭제합니다.\nasset 파일도 삭제됩니다. 계속하시겠습니까?",
                "삭제", "취소");
            if (!confirm) return;

            StoryEditorUtility.DeleteLine(episode, _selectedLine);
            _selectedLine = null;
            _inspectorPanel.SetLine(null, episode);
            RebuildCanvas();
            RefreshStatusBar();
        }

        // ── 유틸 ────────────────────────────────────

        private static bool EpisodeContains(StoryEpisodeSO ep, string lineId)
        {
            foreach (StoryLineSO epLine in ep.Lines)
                if (epLine != null && epLine.LineId == lineId) return true;

            return false;
        }

        private bool IsRestorableSelectedLineValid()
        {
            if (episode == null || _selectedLine == null)
                return false;

            foreach (StoryLineSO line in episode.Lines)
            {
                if (line == _selectedLine)
                    return true;
            }

            return false;
        }
    }
}
