using System.Collections.Generic;
using System.Linq;
using Battle.Data;
using Battle.Effects;
using Battle.Enums;
using Battle.Presentation;
using DeckBuilding;
using UnityEditor;
using UnityEngine;

namespace Battle.Editor
{
    public class CardCreatorWindow : EditorWindow
    {
        // ── DB 레퍼런스 ──────────────────────────────────────────────
        private CardDatabaseSO _cardDb;
        private CardDatabaseSO _startDeckDb;

        private const string PrefCardDbGuid    = "CardCreator_CardDbGuid";
        private const string PrefStartDeckGuid = "CardCreator_StartDeckGuid";

        // ── 레이아웃 ──────────────────────────────────────────────────
        private float       _leftPanelWidth = 280f;
        private const float DividerWidth    = 5f;
        private const float TopBarHeight    = 44f;
        private const float MinLeftWidth    = 180f;
        private const float MinRightWidth   = 400f;

        // ── 카드 목록 ─────────────────────────────────────────────────
        private List<CardDataSO> _allCards      = new();
        private List<CardDataSO> _filteredCards = new();
        private Vector2          _leftScrollPos;
        private int              _filterElementIndex; // 0 = 전체, 1+ = ElementType 순서
        private string           _searchText = "";

        // 필터 드롭다운: "전체" + ElementType 이름 목록
        private static readonly string[] ElementFilterOptions =
            new[] { "전체" }.Concat(System.Enum.GetNames(typeof(ElementType))).ToArray();

        private int _sortIndex;
        private static readonly string[] SortOptions = { "이름순", "등급순", "타입순", "코스트순", "속성순" };

        // ── 선택·편집 상태 ───────────────────────────────────────────
        private CardDataSO       _selectedCard;
        private SerializedObject _selectedSerializedObject;
        private bool             _isCreatingNew;
        private Vector2          _rightScrollPos;
        private float            _editFormContentHeight = 900f;

        // ── 신규 생성 임시 필드 ──────────────────────────────────────
        private ElementType       _newElementType;
        private string            _newCardName     = "";
        private int               _newCost;
        private CardTargetType    _newTargetType;
        private CardDisposePolicy _newDisposePolicy;
        private CardGrade         _newGrade;
        private CardType          _newCardType;
        private string            _newDescription  = "";
        private Sprite            _newArtwork;
        private bool              _addToCardDb;
        private bool              _addToStartDeck;

        // ── 복제 모드 ────────────────────────────────────────────────
        private bool        _isDuplicateMode;
        private CardDataSO  _duplicateSource;

        // ── 리네임 ───────────────────────────────────────────────────
        private string      _renameBuffer = "";

        // ── 스타일 캐시 ───────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _cardLabelStyle;
        private GUIStyle _cardCostStyle;
        private GUIStyle _cardTypeTagStyle;
        private GUIStyle _cardTargetStyle;
        private GUIStyle _fileNameStyle;
        private bool     _stylesInitialized;

        // ── 등급 색상 ────────────────────────────────────────────────
        private static Color GradeColor(CardGrade grade) => grade switch
        {
            CardGrade.Normal    => new Color(0.45f, 0.45f, 0.45f, 0.35f),
            CardGrade.Rare      => new Color(0.15f, 0.35f, 0.70f, 0.35f),
            CardGrade.Epic      => new Color(0.45f, 0.15f, 0.65f, 0.35f),
            CardGrade.Legendary => new Color(0.60f, 0.45f, 0.05f, 0.35f),
            _                   => new Color(0.30f, 0.30f, 0.30f, 0.35f)
        };

        private static string TargetTypeLabel(CardTargetType t) => t switch
        {
            CardTargetType.None        => "없음",
            CardTargetType.SingleEnemy => "단일 적",
            CardTargetType.SingleAlly  => "단일 아군",
            CardTargetType.AllEnemies  => "범위 적",
            CardTargetType.RandomEnemy => "랜덤 적",
            _                          => ""
        };

        // ── 썸네일 캐시 ──────────────────────────────────────────────
        private readonly Dictionary<CardDataSO, Texture2D> _thumbnailCache = new();

        // ─────────────────────────────────────────────────────────────

        [MenuItem("Window/Battle/Card Creator")]
        private static void Open()
        {
            var window = GetWindow<CardCreatorWindow>("Card Creator");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            RestoreDbFromPrefs();
            LoadAllCards();
        }

        private void OnDisable()
        {
            if (_selectedCard != null)
            {
                EditorUtility.SetDirty(_selectedCard);
                AssetDatabase.SaveAssets();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  EditorPrefs 저장·복원
        // ─────────────────────────────────────────────────────────────
        private void RestoreDbFromPrefs()
        {
            _cardDb      = LoadAssetByGuidPref<CardDatabaseSO>(PrefCardDbGuid);
            _startDeckDb = LoadAssetByGuidPref<CardDatabaseSO>(PrefStartDeckGuid);
        }

        private static T LoadAssetByGuidPref<T>(string prefKey) where T : Object
        {
            var guid = EditorPrefs.GetString(prefKey, "");
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SaveAssetGuidPref(string prefKey, Object asset)
        {
            if (asset == null) { EditorPrefs.DeleteKey(prefKey); return; }
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
                EditorPrefs.SetString(prefKey, guid);
        }

        // ─────────────────────────────────────────────────────────────
        //  카드 로드 & 필터
        // ─────────────────────────────────────────────────────────────
        private void LoadAllCards()
        {
            _allCards.Clear();
            _thumbnailCache.Clear();

            var guids = AssetDatabase.FindAssets("t:CardDataSO",
                new[] { "Assets/05. SO/Cards/PlayerCards" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
                if (card != null) _allCards.Add(card);
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            // index 0 = 전체, index 1+ = (ElementType)(index - 1)
            bool        hasElementFilter = _filterElementIndex > 0;
            ElementType targetElement    = hasElementFilter
                ? (ElementType)(_filterElementIndex - 1)
                : ElementType.None;

            var filtered = _allCards.Where(card =>
            {
                if (hasElementFilter && card.elementType != targetElement)
                    return false;
                if (!string.IsNullOrEmpty(_searchText))
                {
                    var q = _searchText.ToLower();
                    if (!card.cardName.ToLower().Contains(q) &&
                        !card.cardId.ToLower().Contains(q))
                        return false;
                }
                return true;
            });

            _filteredCards = _sortIndex switch
            {
                1 => filtered.OrderBy(c => (int)c.grade).ToList(),
                2 => filtered.OrderBy(c => (int)c.cardType).ToList(),
                3 => filtered.OrderBy(c => c.cost).ToList(),
                4 => filtered.OrderBy(c => (int)c.elementType).ToList(),
                _ => filtered.OrderBy(c => string.IsNullOrEmpty(c.cardName) ? c.cardId : c.cardName).ToList()
            };
        }

        private Texture2D GetThumbnail(CardDataSO card)
        {
            if (card.artwork == null) return null;
            if (_thumbnailCache.TryGetValue(card, out var cached) && cached != null)
                return cached;
            var tex = AssetPreview.GetAssetPreview(card.artwork);
            if (tex != null)
                _thumbnailCache[card] = tex;
            else
                Repaint(); // 미리보기 생성 중 — 준비되면 캐시에 저장
            return tex;
        }

        // ─────────────────────────────────────────────────────────────
        //  경로 유틸
        // ─────────────────────────────────────────────────────────────
        private static string GetCardFolder(ElementType elementType) =>
            $"Assets/05. SO/Cards/PlayerCards/{elementType}";

        private static string GetPresentationFolder(ElementType elementType) =>
            $"Assets/05. SO/Battle/SkillPresentations/{elementType}";

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            var parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var name   = System.IO.Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private int CalculateNextIndex(ElementType elementType)
        {
            var folder = GetCardFolder(elementType);
            if (!AssetDatabase.IsValidFolder(folder)) return 1;
            var guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { folder });
            return guids.Length + 1;
        }

        private string BuildFileName(ElementType elementType, string cardName)
        {
            var trimmed = cardName.Trim();
            if (elementType == ElementType.None) return trimmed;
            int index = CalculateNextIndex(elementType);
            return $"{elementType}_{index:D2}_{trimmed}";
        }

        private CardDataSO FindDuplicateArtwork(Sprite artwork, CardDataSO exclude = null)
        {
            if (artwork == null) return null;
            return _allCards.FirstOrDefault(c => c != exclude && c.artwork == artwork);
        }

        // ─────────────────────────────────────────────────────────────
        //  OnGUI
        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();
            HandleDividerDrag();
            DrawTopBar();
            DrawMainLayout();
        }

        private void InitStyles()
        {
            if (_stylesInitialized && _headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft
            };
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleLeft
            };
            _cardLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize  = 11,
                padding   = new RectOffset(0, 0, 0, 0)
            };
            _cardCostStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft
            };
            _cardTypeTagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 9,
                alignment = TextAnchor.MiddleRight
            };
            _cardTargetStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 9,
                alignment = TextAnchor.MiddleLeft
            };
            _fileNameStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(6, 6, 4, 4)
            };
            _stylesInitialized = true;
        }

        // ── 구분선 드래그 ────────────────────────────────────────────
        private void HandleDividerDrag()
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            float contentY      = TopBarHeight + 1f;
            float contentHeight = position.height - contentY;
            var   dividerRect   = new Rect(_leftPanelWidth, contentY, DividerWidth, contentHeight);

            EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.ResizeHorizontal);

            var e = Event.current;
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (dividerRect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        _leftPanelWidth = Mathf.Clamp(
                            e.mousePosition.x,
                            MinLeftWidth,
                            position.width - MinRightWidth - DividerWidth);
                        Repaint();
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        // ── 상단 DB 슬롯 ─────────────────────────────────────────────
        private void DrawTopBar()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, TopBarHeight),
                new Color(0.18f, 0.18f, 0.18f));

            GUILayout.BeginArea(new Rect(8, 4, position.width - 16, TopBarHeight - 8));
            GUILayout.BeginHorizontal();

            GUILayout.Label("전체 DB", GUILayout.Width(48));
            var newCardDb = (CardDatabaseSO)EditorGUILayout.ObjectField(
                _cardDb, typeof(CardDatabaseSO), false, GUILayout.Width(200));
            if (newCardDb != _cardDb)
            {
                _cardDb = newCardDb;
                SaveAssetGuidPref(PrefCardDbGuid, _cardDb);
            }

            GUILayout.Space(16);

            GUILayout.Label("시작 덱 DB", GUILayout.Width(56));
            var newStartDeck = (CardDatabaseSO)EditorGUILayout.ObjectField(
                _startDeckDb, typeof(CardDatabaseSO), false, GUILayout.Width(200));
            if (newStartDeck != _startDeckDb)
            {
                _startDeckDb = newStartDeck;
                SaveAssetGuidPref(PrefStartDeckGuid, _startDeckDb);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            EditorGUI.DrawRect(new Rect(0, TopBarHeight, position.width, 1f),
                new Color(0.1f, 0.1f, 0.1f));
        }

        // ── 2분할 메인 레이아웃 ───────────────────────────────────────
        private void DrawMainLayout()
        {
            float contentY      = TopBarHeight + 1f;
            float contentHeight = position.height - contentY;

            GUILayout.BeginArea(new Rect(0, contentY, _leftPanelWidth, contentHeight));
            DrawLeftPanel(contentHeight);
            GUILayout.EndArea();

            EditorGUI.DrawRect(
                new Rect(_leftPanelWidth, contentY, DividerWidth, contentHeight),
                new Color(0.12f, 0.12f, 0.12f));

            float rightX = _leftPanelWidth + DividerWidth;
            float rightW = position.width - rightX;
            GUILayout.BeginArea(new Rect(rightX, contentY, rightW, contentHeight));
            DrawRightPanel(rightW, contentHeight);
            GUILayout.EndArea();
        }

        // ─────────────────────────────────────────────────────────────
        //  왼쪽 패널
        // ─────────────────────────────────────────────────────────────
        private const float CardItemHeight   = 60f;
        private const float BottomButtonArea = 36f;

        private void DrawLeftPanel(float totalHeight)
        {
            var headerRect  = new Rect(6, 6, _leftPanelWidth - 28, 18);
            var refreshRect = new Rect(_leftPanelWidth - 26, 6, 22, 18);
            GUI.Label(headerRect, "카드 목록", _headerStyle);
            if (GUI.Button(refreshRect, "↺")) { LoadAllCards(); _thumbnailCache.Clear(); }

            float fw     = _leftPanelWidth - 12f;
            float halfW  = (fw - 4f) * 0.5f;
            var filterRect = new Rect(6, 28, halfW, 18);
            var sortRect   = new Rect(6 + halfW + 4, 28, halfW, 18);
            EditorGUI.BeginChangeCheck();
            _filterElementIndex = EditorGUI.Popup(filterRect, _filterElementIndex, ElementFilterOptions);
            _sortIndex          = EditorGUI.Popup(sortRect,   _sortIndex,          SortOptions);
            if (EditorGUI.EndChangeCheck()) ApplyFilter();

            var searchRect = new Rect(6, 50, _leftPanelWidth - 12, 18);
            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUI.TextField(searchRect, _searchText);
            if (EditorGUI.EndChangeCheck()) ApplyFilter();

            EditorGUI.DrawRect(new Rect(0, 72, _leftPanelWidth, 1), new Color(0.2f, 0.2f, 0.2f));

            float scrollTop    = 73f;
            float scrollBottom = totalHeight - BottomButtonArea - 1f;
            float scrollHeight = scrollBottom - scrollTop;

            var   scrollViewRect = new Rect(0, scrollTop, _leftPanelWidth, scrollHeight);
            float contentH       = _filteredCards.Count * CardItemHeight;
            var   contentRect    = new Rect(0, 0, _leftPanelWidth - 14, contentH);

            _leftScrollPos = GUI.BeginScrollView(scrollViewRect, _leftScrollPos, contentRect,
                false, true);
            for (int i = 0; i < _filteredCards.Count; i++)
            {
                var itemRect = new Rect(0, i * CardItemHeight, _leftPanelWidth - 14, CardItemHeight);
                DrawCardListItem(_filteredCards[i], itemRect);
            }
            GUI.EndScrollView();

            EditorGUI.DrawRect(new Rect(0, scrollBottom, _leftPanelWidth, 1),
                new Color(0.2f, 0.2f, 0.2f));

            float btnY = scrollBottom + 6f;
            float btnW = (_leftPanelWidth - 18f) * 0.65f;
            float dupW = (_leftPanelWidth - 18f) * 0.32f;

            if (GUI.Button(new Rect(6, btnY, btnW, 24), "새 카드"))
            {
                ResetNewCardForm();
                _isCreatingNew = true;
                _selectedCard  = null;
                _selectedSerializedObject = null;
            }

            GUI.enabled = _selectedCard != null;
            if (GUI.Button(new Rect(btnW + 12, btnY, dupW, 24), "복제"))
                StartDuplicate(_selectedCard);
            GUI.enabled = true;
        }

        private void DrawCardListItem(CardDataSO card, Rect itemRect)
        {
            bool isSelected = card == _selectedCard;

            // 등급 배경 (항상 먼저)
            EditorGUI.DrawRect(itemRect, GradeColor(card.grade));

            // 선택 / 호버 오버레이
            if (isSelected)
                EditorGUI.DrawRect(itemRect, new Color(0.24f, 0.48f, 0.90f, 0.30f));
            else if (itemRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(itemRect, new Color(1f, 1f, 1f, 0.05f));

            // 썸네일 48×48
            var thumbRect = new Rect(itemRect.x + 2, itemRect.y + 6, 48, 48);
            var thumb     = GetThumbnail(card);
            if (thumb != null) GUI.DrawTexture(thumbRect, thumb, ScaleMode.ScaleToFit);
            else               EditorGUI.DrawRect(thumbRect, new Color(0.25f, 0.25f, 0.25f));

            float textX = thumbRect.xMax + 4f;
            float textW = itemRect.xMax - textX - 4f;

            // 코스트 "[2]" — 굵은 13px
            string costText = $"[{card.cost}]";
            float  costW    = 32f;
            GUI.Label(new Rect(textX, itemRect.y + 8f, costW, 18f), costText,
                _cardCostStyle ?? EditorStyles.boldLabel);

            // 카드 타입 태그 "[공격]" — 오른쪽 끝 고정
            string typeText = card.cardType == CardType.Attack ? "[공격]" : "[지원]";
            float  tagW     = 38f;
            var    tagRect  = new Rect(itemRect.xMax - tagW - 4f, itemRect.y + 8f, tagW, 18f);
            GUI.Label(tagRect, typeText, _cardTypeTagStyle ?? EditorStyles.miniLabel);

            // 카드 이름 — 코스트와 타입 태그 사이
            float nameX = textX + costW + 2f;
            float nameW = tagRect.x - nameX - 2f;
            string nameText = string.IsNullOrEmpty(card.cardName) ? card.cardId : card.cardName;
            GUI.Label(new Rect(nameX, itemRect.y + 8f, nameW, 18f), nameText,
                _cardLabelStyle ?? EditorStyles.label);

            // 타겟 타입 — 두 번째 줄, 작은 회색 텍스트
            GUI.Label(new Rect(textX, itemRect.y + 34f, textW, 14f),
                TargetTypeLabel(card.targetType), _cardTargetStyle ?? EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                _selectedCard             = card;
                _selectedSerializedObject = new SerializedObject(card);
                _isCreatingNew            = false;
                _renameBuffer             = card.cardId;
                GUI.FocusControl(null);
                Event.current.Use();
                Repaint();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  오른쪽 패널
        // ─────────────────────────────────────────────────────────────
        private void DrawRightPanel(float panelWidth, float panelHeight)
        {
            if (_isCreatingNew)
                DrawCreateForm(panelWidth, panelHeight);
            else if (_selectedCard != null)
                DrawEditForm(panelWidth, panelHeight);
            else
                GUI.Label(
                    new Rect(0, panelHeight * 0.44f, panelWidth, 40),
                    "왼쪽에서 카드를 선택하거나\n새 카드를 만드세요.",
                    EditorStyles.centeredGreyMiniLabel);
        }

        // GUILayout을 사용하지 않는 순수 rect 기반 경고/안내 박스
        private static void DrawInfoBox(Rect rect, string text, bool isWarning = false)
        {
            EditorGUI.DrawRect(rect, isWarning
                ? new Color(0.6f, 0.5f, 0.1f, 0.3f)
                : new Color(0.2f, 0.3f, 0.5f, 0.3f));
            GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, rect.height - 4),
                text, EditorStyles.miniLabel);
        }

        // DB 등록 한 행 (상태 표시 + 등록/해제 버튼)
        private float DrawDbRow(float pad, float y, float fw, string label,
            CardDatabaseSO db,
            System.Func<CardDatabaseSO, bool> isRegistered,
            System.Action<CardDatabaseSO> register,
            System.Action<CardDatabaseSO> unregister)
        {
            const float btnW = 52f;
            const float rowH = 22f;

            if (db == null)
            {
                DrawInfoBox(new Rect(pad, y, fw, rowH), $"{label}: 상단에서 DB를 연결해주세요.");
                return y + rowH + 2f;
            }

            bool registered = isRegistered(db);
            string statusText = registered ? $"✓  {label}" : $"✗  {label}";
            var statusColor   = registered ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.8f, 0.8f, 0.8f);

            // 상태 레이블
            var prevColor = GUI.color;
            GUI.color = statusColor;
            GUI.Label(new Rect(pad, y + 2f, fw - btnW - 8f, rowH), statusText, EditorStyles.boldLabel);
            GUI.color = prevColor;

            // 등록 / 해제 버튼
            string btnLabel = registered ? "해제" : "등록";
            if (GUI.Button(new Rect(pad + fw - btnW, y, btnW, rowH - 2f), btnLabel))
            {
                if (registered) unregister(db);
                else            register(db);
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
            }

            return y + rowH;
        }

        // ── 신규 생성 폼 ─────────────────────────────────────────────
        private void ResetNewCardForm()
        {
            _newElementType   = ElementType.None;
            _newCardName      = "";
            _newCost          = 1;
            _newTargetType    = CardTargetType.SingleEnemy;
            _newDisposePolicy = CardDisposePolicy.Discard;
            _newGrade         = CardGrade.Normal;
            _newCardType      = CardType.Attack;
            _newDescription   = "";
            _newArtwork       = null;
            _addToCardDb      = true;
            _addToStartDeck   = false;
            _isDuplicateMode  = false;
            _duplicateSource  = null;
        }

        private void StartDuplicate(CardDataSO source)
        {
            _newElementType   = source.elementType;
            _newCardName      = "";          // 이름만 비워서 새로 입력받음
            _newCost          = source.cost;
            _newTargetType    = source.targetType;
            _newDisposePolicy = source.disposePolicy;
            _newGrade         = source.grade;
            _newCardType      = source.cardType;
            _newDescription   = source.description;
            _newArtwork       = source.artwork;
            _addToCardDb      = false;
            _addToStartDeck   = false;
            _isDuplicateMode  = true;
            _duplicateSource  = source;
            _isCreatingNew    = true;
            _rightScrollPos   = Vector2.zero;
        }

        private void DrawCreateForm(float panelWidth, float panelHeight)
        {
            const float pad = 12f;
            float       fw  = panelWidth - pad * 2f;   // 필드 너비
            float       lw  = 110f;                    // 레이블 너비

            _rightScrollPos = GUI.BeginScrollView(
                new Rect(0, 0, panelWidth, panelHeight),
                _rightScrollPos,
                new Rect(0, 0, panelWidth - 14, 800),
                false, true);

            float y = 12f;

            // 헤더
            string formTitle = _isDuplicateMode
                ? $"복제 — 원본: {_duplicateSource?.cardName ?? _duplicateSource?.cardId}"
                : "새 카드 만들기";
            GUI.Label(new Rect(pad, y, fw, 22), formTitle, _headerStyle);
            y += 28f;

            if (_isDuplicateMode)
            {
                DrawInfoBox(new Rect(pad, y, fw, 26),
                    "새 이름을 입력하고 '카드 생성'을 누르면 복제됩니다. effectSlots는 자동 복사됩니다.");
                y += 30f;
            }

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.3f, 0.3f, 0.3f));
            y += 8f;

            // ElementType
            GUI.Label(new Rect(pad, y, lw, 18), "속성");
            EditorGUI.BeginChangeCheck();
            _newElementType = (ElementType)EditorGUI.EnumPopup(new Rect(pad + lw, y, fw - lw, 18), _newElementType);
            if (EditorGUI.EndChangeCheck()) _newCardName = "";
            y += 22f;

            // 카드 이름
            GUI.Label(new Rect(pad, y, lw, 18), "이름");
            _newCardName = EditorGUI.TextField(new Rect(pad + lw, y, fw - lw, 18), _newCardName);
            y += 22f;

            // 파일명 미리보기
            var previewName = string.IsNullOrWhiteSpace(_newCardName)
                ? $"{_newElementType}_01_..."
                : BuildFileName(_newElementType, _newCardName);
            GUI.Label(new Rect(pad, y, lw, 18), "파일명 (ID)");
            GUI.Label(new Rect(pad + lw, y, fw - lw, 18), previewName, _fileNameStyle);
            y += 26f;

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // 코스트
            GUI.Label(new Rect(pad, y, lw, 18), "코스트");
            _newCost = EditorGUI.IntField(new Rect(pad + lw, y, 60, 18), _newCost);
            y += 22f;

            // 타겟 타입
            GUI.Label(new Rect(pad, y, lw, 18), "타겟 타입");
            _newTargetType = (CardTargetType)EditorGUI.EnumPopup(
                new Rect(pad + lw, y, fw - lw, 18), _newTargetType);
            y += 22f;

            // 폐기 정책
            GUI.Label(new Rect(pad, y, lw, 18), "폐기 방식");
            _newDisposePolicy = (CardDisposePolicy)EditorGUI.EnumPopup(
                new Rect(pad + lw, y, fw - lw, 18), _newDisposePolicy);
            y += 22f;

            // 등급
            GUI.Label(new Rect(pad, y, lw, 18), "등급");
            _newGrade = (CardGrade)EditorGUI.EnumPopup(
                new Rect(pad + lw, y, fw - lw, 18), _newGrade);
            y += 22f;

            // 카드 타입
            GUI.Label(new Rect(pad, y, lw, 18), "카드 타입");
            _newCardType = (CardType)EditorGUI.EnumPopup(
                new Rect(pad + lw, y, fw - lw, 18), _newCardType);
            y += 26f;

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // 설명
            GUI.Label(new Rect(pad, y, lw, 18), "설명");
            _newDescription = EditorGUI.TextArea(new Rect(pad + lw, y, fw - lw, 56), _newDescription);
            y += 64f;

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // Artwork
            y = DrawArtworkField(pad, y, fw, lw, ref _newArtwork, null);

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // DB 등록 체크박스
            GUI.Label(new Rect(pad, y, fw, 18), "DB 등록", _subHeaderStyle ?? EditorStyles.boldLabel);
            y += 22f;
            _addToCardDb    = EditorGUI.Toggle(new Rect(pad, y, fw, 18), "전체 DB에 추가", _addToCardDb);
            y += 20f;
            _addToStartDeck = EditorGUI.Toggle(new Rect(pad, y, fw, 18), "시작 덱 DB에 추가", _addToStartDeck);
            y += 28f;

            // 생성 버튼
            bool canCreate = !string.IsNullOrWhiteSpace(_newCardName);
            GUI.enabled = canCreate;
            if (GUI.Button(new Rect(pad, y, fw, 28), "카드 생성"))
                CreateCard();
            GUI.enabled = true;

            if (!canCreate)
            {
                y += 32f;
                DrawInfoBox(new Rect(pad, y, fw, 28), "⚠ 카드 이름을 입력하세요.", true);
            }

            GUI.EndScrollView();
        }

        // ── 기존 카드 편집 폼 ────────────────────────────────────────
        private void DrawEditForm(float panelWidth, float panelHeight)
        {
            if (_selectedSerializedObject == null) return;
            _selectedSerializedObject.Update();

            const float pad = 12f;
            float       fw  = panelWidth - pad * 2f;
            float       lw  = 110f;

            _rightScrollPos = GUI.BeginScrollView(
                new Rect(0, 0, panelWidth, panelHeight),
                _rightScrollPos,
                new Rect(0, 0, panelWidth - 14, _editFormContentHeight),
                false, true);

            float y = 12f;

            // 헤더
            string headerText = string.IsNullOrEmpty(_selectedCard.cardName)
                ? _selectedCard.cardId
                : _selectedCard.cardName;
            GUI.Label(new Rect(pad, y, fw, 22), headerText, _headerStyle);
            y += 28f;

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.3f, 0.3f, 0.3f));
            y += 8f;

            // cardId (읽기 전용)
            GUI.Label(new Rect(pad, y, lw, 18), "카드 ID");
            GUI.enabled = false;
            EditorGUI.TextField(new Rect(pad + lw, y, fw - lw, 18), _selectedCard.cardId);
            GUI.enabled = true;
            y += 22f;

            // 파일명 변경
            GUI.Label(new Rect(pad, y, lw, 18), "파일명 변경");
            _renameBuffer = EditorGUI.TextField(new Rect(pad + lw, y, fw - lw - 62f, 18), _renameBuffer);
            bool renameChanged = _renameBuffer != _selectedCard.cardId;
            GUI.enabled = renameChanged && !string.IsNullOrWhiteSpace(_renameBuffer);
            if (GUI.Button(new Rect(pad + fw - 58f, y, 58f, 18), "적용"))
                RenameCard(_selectedCard, _renameBuffer);
            GUI.enabled = true;
            y += 26f;

            // cardName
            y = DrawSerializedField("cardName", "이름", pad, y, fw, lw);

            // elementType
            y = DrawSerializedField("elementType", "속성", pad, y, fw, lw);

            // cost
            y = DrawSerializedField("cost", "코스트", pad, y, fw, lw);

            // targetType
            y = DrawSerializedField("targetType", "타겟 타입", pad, y, fw, lw);

            // disposePolicy
            y = DrawSerializedField("disposePolicy", "폐기 방식", pad, y, fw, lw);

            // grade
            y = DrawSerializedField("grade", "등급", pad, y, fw, lw);

            // cardType
            y = DrawSerializedField("cardType", "카드 타입", pad, y, fw, lw);

            y += 4f;
            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // description
            GUI.Label(new Rect(pad, y, lw, 18), "설명");
            var descProp = _selectedSerializedObject.FindProperty("description");
            descProp.stringValue = EditorGUI.TextArea(
                new Rect(pad + lw, y, fw - lw, 56), descProp.stringValue);
            y += 64f;

            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            // artwork (SerializedObject + 미리보기)
            var artworkProp = _selectedSerializedObject.FindProperty("artwork");
            var currentSprite = (Sprite)artworkProp.objectReferenceValue;

            GUI.Label(new Rect(pad, y, lw, 18), "아트워크");
            var newSprite = (Sprite)EditorGUI.ObjectField(
                new Rect(pad + lw, y, fw - lw, 18),
                currentSprite, typeof(Sprite), false);
            if (newSprite != currentSprite)
                artworkProp.objectReferenceValue = newSprite;
            y += 22f;

            // 중복 경고
            var dupCard = FindDuplicateArtwork(newSprite, _selectedCard);
            if (dupCard != null)
            {
                DrawInfoBox(new Rect(pad, y, fw, 28), $"⚠ 이미 '{dupCard.cardName}'에서 사용 중인 아이콘입니다.", true);
                y += 32f;
            }

            // artwork 미리보기
            if (newSprite != null)
            {
                var previewTex = AssetPreview.GetAssetPreview(newSprite);
                if (previewTex != null)
                {
                    GUI.DrawTexture(new Rect(pad + lw, y, 128, 128), previewTex, ScaleMode.ScaleToFit);
                    y += 132f;
                }
                else y += 4f;
            }

            y = DrawEffectSlotsSection(pad, y, fw);

            // presentationData (읽기 전용 링크)
            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;
            GUI.Label(new Rect(pad, y, lw, 18), "Presentation");
            EditorGUI.ObjectField(
                new Rect(pad + lw, y, fw - lw, 18),
                _selectedCard.presentationData, typeof(SkillPresentationDataSO), false);
            y += 28f;

            // ── DB 관리 ──────────────────────────────────────────────
            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;
            GUI.Label(new Rect(pad, y, fw, 18), "DB 관리", _subHeaderStyle ?? EditorStyles.boldLabel);
            y += 26f;

            y = DrawDbRow(pad, y, fw, "전체 DB", _cardDb,
                db => db.allCards.Contains(_selectedCard),
                db => { db.allCards.Add(_selectedCard); },
                db => { db.allCards.Remove(_selectedCard); });

            y += 4f;

            y = DrawDbRow(pad, y, fw, "시작 덱 DB", _startDeckDb,
                db => db.allCards.Contains(_selectedCard),
                db => { db.allCards.Add(_selectedCard); },
                db => { db.allCards.Remove(_selectedCard); });

            _editFormContentHeight = Mathf.Max(900f, y + 20f);

            GUI.EndScrollView();

            // 변경사항 반영 (SaveAssets는 OnDisable 또는 명시적 액션에서만 호출)
            if (_selectedSerializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selectedCard);
        }

        // artwork 필드 공통 (신규 생성용)
        private float DrawArtworkField(float pad, float y, float fw, float lw,
            ref Sprite artwork, CardDataSO exclude)
        {
            GUI.Label(new Rect(pad, y, lw, 18), "아트워크");
            var newSprite = (Sprite)EditorGUI.ObjectField(
                new Rect(pad + lw, y, fw - lw, 18), artwork, typeof(Sprite), false);
            artwork = newSprite;
            y += 22f;

            var dupCard = FindDuplicateArtwork(artwork, exclude);
            if (dupCard != null)
            {
                DrawInfoBox(new Rect(pad, y, fw, 28), $"⚠ 이미 '{dupCard.cardName}'에서 사용 중인 아이콘입니다.", true);
                y += 32f;
            }

            if (artwork != null)
            {
                var previewTex = AssetPreview.GetAssetPreview(artwork);
                if (previewTex != null)
                {
                    GUI.DrawTexture(new Rect(pad + lw, y, 128, 128), previewTex, ScaleMode.ScaleToFit);
                    y += 132f;
                }
            }

            return y;
        }

        // SerializedObject 기반 필드 한 줄 그리기
        private float DrawSerializedField(string propName, string label,
            float pad, float y, float fw, float lw)
        {
            var prop = _selectedSerializedObject.FindProperty(propName);
            if (prop == null) return y;

            GUI.Label(new Rect(pad, y, lw, 18), label);
            EditorGUI.PropertyField(new Rect(pad + lw, y, fw - lw, 18), prop, GUIContent.none);
            return y + 22f;
        }

        // ─────────────────────────────────────────────────────────────
        //  카드 생성
        // ─────────────────────────────────────────────────────────────
        private void CreateCard()
        {
            if (string.IsNullOrWhiteSpace(_newCardName))
            {
                EditorUtility.DisplayDialog("오류", "카드 이름을 입력하세요.", "확인");
                return;
            }

            string fileName   = BuildFileName(_newElementType, _newCardName);
            string cardFolder = GetCardFolder(_newElementType);
            EnsureFolder(cardFolder);

            string cardPath = $"{cardFolder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<CardDataSO>(cardPath) != null)
            {
                EditorUtility.DisplayDialog("중복 오류",
                    $"이미 동일한 파일이 존재합니다:\n{cardPath}", "확인");
                return;
            }

            // CardDataSO 생성
            var card = ScriptableObject.CreateInstance<CardDataSO>();
            card.cardId        = fileName;
            card.cardName      = _newCardName.Trim();
            card.cost          = _newCost;
            card.targetType    = _newTargetType;
            card.disposePolicy = _newDisposePolicy;
            card.elementType   = _newElementType;
            card.grade         = _newGrade;
            card.cardType      = _newCardType;
            card.description   = _newDescription;
            card.artwork       = _newArtwork;

            // 복제 모드: effectSlots 복사 후 GUID 재발급
            if (_isDuplicateMode && _duplicateSource != null)
            {
                var temp = ScriptableObject.CreateInstance<CardDataSO>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_duplicateSource), temp);
                card.effectSlots = temp.effectSlots ?? new List<CardEffectSlot>();
                DestroyImmediate(temp);
                card.EnsureEffectSlotIds();
            }
            else
            {
                card.effectSlots = new List<CardEffectSlot>();
            }

            AssetDatabase.CreateAsset(card, cardPath);

            // SkillPresentationDataSO 자동 생성
            string presFolder = GetPresentationFolder(_newElementType);
            EnsureFolder(presFolder);
            var pres     = ScriptableObject.CreateInstance<SkillPresentationDataSO>();
            var presPath = $"{presFolder}/{fileName}_Presentation.asset";
            AssetDatabase.CreateAsset(pres, presPath);

            card.presentationData = pres;
            EditorUtility.SetDirty(card);

            // DB 등록
            if (_addToCardDb && _cardDb != null)
            {
                _cardDb.allCards.Add(card);
                EditorUtility.SetDirty(_cardDb);
            }
            if (_addToStartDeck && _startDeckDb != null)
            {
                _startDeckDb.allCards.Add(card);
                EditorUtility.SetDirty(_startDeckDb);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadAllCards();
            _selectedCard             = card;
            _selectedSerializedObject = new SerializedObject(card);
            _isCreatingNew            = false;
            _isDuplicateMode          = false;
            _duplicateSource          = null;
            _renameBuffer             = card.cardId;
            _rightScrollPos           = Vector2.zero;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────
        //  이펙트 슬롯 커스텀 UI
        // ─────────────────────────────────────────────────────────────
        private float DrawEffectSlotsSection(float pad, float y, float fw)
        {
            EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.25f, 0.25f, 0.25f));
            y += 8f;

            var effectSlotsProp = _selectedSerializedObject.FindProperty("effectSlots");
            int count           = effectSlotsProp.arraySize;

            GUI.Label(new Rect(pad, y, fw - 64f, 18),
                $"이펙트 슬롯 ({count})", _subHeaderStyle ?? EditorStyles.boldLabel);
            if (GUI.Button(new Rect(pad + fw - 60f, y, 60f, 18), "+ 추가"))
                ShowAddEffectMenu();
            y += 24f;

            int toDelete = -1;

            for (int i = 0; i < count; i++)
            {
                var slotProp   = effectSlotsProp.GetArrayElementAtIndex(i);
                var effectProp = slotProp.FindPropertyRelative("effect");
                var idProp     = slotProp.FindPropertyRelative("effectSlotId");

                string effectTypeName = effectProp.managedReferenceValue?.GetType().Name ?? "(없음)";

                EditorGUI.DrawRect(new Rect(pad, y, fw, 20f), new Color(0.12f, 0.12f, 0.12f, 0.9f));
                GUI.Label(new Rect(pad + 4, y + 1f, fw - 60f, 18),
                    $"[{i}]  {effectTypeName}", _subHeaderStyle ?? EditorStyles.boldLabel);
                if (GUI.Button(new Rect(pad + fw - 52f, y + 2f, 50f, 16f), "삭제"))
                    toDelete = i;
                y += 22f;

                if (effectProp.managedReferenceValue != null)
                {
                    float slotH = EditorGUI.GetPropertyHeight(effectProp, true);
                    EditorGUI.PropertyField(
                        new Rect(pad + 12, y, fw - 12, slotH),
                        effectProp, GUIContent.none, true);
                    y += slotH + 4f;
                }

                var prevColor = GUI.color;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(pad + 4, y, fw, 12f),
                    $"ID: {idProp.stringValue}", EditorStyles.miniLabel);
                GUI.color = prevColor;
                y += 16f;

                EditorGUI.DrawRect(new Rect(pad, y, fw, 1), new Color(0.2f, 0.2f, 0.2f));
                y += 6f;
            }

            if (toDelete >= 0)
            {
                effectSlotsProp.DeleteArrayElementAtIndex(toDelete);
                _selectedSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_selectedCard);
                Repaint();
            }

            return y;
        }

        private void ShowAddEffectMenu()
        {
            var menu  = new GenericMenu();
            var types = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try   { return a.GetTypes(); }
                    catch { return new System.Type[0]; }
                })
                .Where(t => typeof(CardEffect).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var capturedType = type;
                menu.AddItem(new GUIContent(capturedType.Name), false, () =>
                {
                    if (_selectedSerializedObject == null) return;
                    _selectedSerializedObject.Update();
                    var prop = _selectedSerializedObject.FindProperty("effectSlots");
                    int idx  = prop.arraySize;
                    prop.InsertArrayElementAtIndex(idx);
                    var newSlot = prop.GetArrayElementAtIndex(idx);
                    newSlot.FindPropertyRelative("effectSlotId").stringValue =
                        System.Guid.NewGuid().ToString();
                    newSlot.FindPropertyRelative("effect").managedReferenceValue =
                        System.Activator.CreateInstance(capturedType);
                    _selectedSerializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_selectedCard);
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        // ─────────────────────────────────────────────────────────────
        //  카드 리네임
        // ─────────────────────────────────────────────────────────────
        private void RenameCard(CardDataSO card, string newFileName)
        {
            newFileName = newFileName.Trim();
            if (string.IsNullOrEmpty(newFileName)) return;

            string cardPath   = AssetDatabase.GetAssetPath(card);
            string cardFolder = System.IO.Path.GetDirectoryName(cardPath)?.Replace('\\', '/');

            // 중복 체크
            if (AssetDatabase.LoadAssetAtPath<CardDataSO>($"{cardFolder}/{newFileName}.asset") != null)
            {
                EditorUtility.DisplayDialog("중복 오류",
                    $"이미 동일한 파일이 존재합니다:\n{cardFolder}/{newFileName}.asset", "확인");
                return;
            }

            // 카드 에셋 리네임
            string error = AssetDatabase.RenameAsset(cardPath, newFileName);
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("오류", $"파일명 변경 실패:\n{error}", "확인");
                return;
            }

            // cardId 갱신
            card.cardId = newFileName;
            EditorUtility.SetDirty(card);

            // presentationData 파일명 변경
            if (card.presentationData != null)
            {
                string presPath   = AssetDatabase.GetAssetPath(card.presentationData);
                string presFolder = System.IO.Path.GetDirectoryName(presPath)?.Replace('\\', '/');
                string newPresName = $"{newFileName}_Presentation";

                if (AssetDatabase.LoadAssetAtPath<SkillPresentationDataSO>(
                        $"{presFolder}/{newPresName}.asset") == null)
                {
                    AssetDatabase.RenameAsset(presPath, newPresName);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadAllCards();
            _selectedCard             = card;
            _selectedSerializedObject = new SerializedObject(card);
            _renameBuffer             = newFileName;
            Repaint();
        }
    }
}
