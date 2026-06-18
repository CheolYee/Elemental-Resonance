#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Battle.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VfxRegistrationWindow : EditorWindow
{
    // ── 경로 상수 ──────────────────────────────────────────────────────────────

    private const string VfxDefinitionSODir  = "Assets/05. SO/Battle/VFX";
    private const string VfxContainerPrefab  = "Assets/08. Prefabs/VFX/VfxPrefab.prefab";
    private const string EnumSourcePath      = "Assets/00. Work/_Resources/02. Scripts/Battle/Presentation/SkillTimelineKeyframes.cs";
    private const string EnumBlockIdentifier = "public enum SkillVfxKey";

    // ── 레이아웃 (리사이즈 가능) ────────────────────────────────────────────────

    private const float HandleThickness  = 5f;
    private const float LeftPanelMin     = 120f;
    private const float LeftPanelMax     = 400f;
    private const float FormHeightMin    = 120f;
    private const float FormHeightMax    = 400f;

    private float _leftPanelWidth  = 200f;
    private float _formAreaHeight  = 180f;
    private bool  _draggingLeft;
    private bool  _draggingTop;

    // ── 상태 ──────────────────────────────────────────────────────────────────

    private string      _newName        = "";
    private GameObject  _newPrefab;
    private SkillVfxKey _selectedKey;
    private bool        _hasSelection;
    private Vector2     _listScroll;
    private string      _statusMessage  = "";
    private Color       _statusColor    = Color.white;

    // ── 프리뷰 ────────────────────────────────────────────────────────────────

    private PreviewRenderUtility _preview;
    private GameObject           _previewInstance;
    private GameObject           _floorInstance;
    private Material             _floorMaterial;
    private Texture2D            _floorGridTexture;
    private double               _previewStartTime;
    private float                _previewLoopDuration = 3f;

    // 카메라 오비트 상태
    private float   _camYaw      = 30f;
    private float   _camPitch    = 20f;
    private float   _camDistance = 3.5f;
    private Vector3 _camTarget   = new Vector3(0f, 0.5f, 0f);
    private const float CamDistanceMin  = 0.5f;
    private const float CamDistanceMax  = 80f;
    private bool  _orbitDragging;
    private bool  _panDragging;
    private Vector2 _lastMousePos;

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/VFX 등록")]
    public static void Open() => GetWindow<VfxRegistrationWindow>("VFX 등록");

    private void OnEnable()
    {
        InitPreview();
        EditorApplication.update += OnEditorUpdate;
        minSize = new Vector2(500f, 400f);
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        CleanupPreview();
    }

    // ── 프리뷰 초기화 ──────────────────────────────────────────────────────────

    private void InitPreview()
    {
        _preview = new PreviewRenderUtility();
        ApplyCameraTransform();
        _preview.camera.nearClipPlane   = 0.05f;
        _preview.camera.farClipPlane    = 200f;
        _preview.camera.backgroundColor = new Color(0.15f, 0.15f, 0.17f);
        _preview.camera.clearFlags      = CameraClearFlags.SolidColor;
        _preview.lights[0].intensity    = 1f;
        _preview.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        CreateFloor();
    }

    private void CreateFloor()
    {
        _floorInstance = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _floorInstance.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

        var col = _floorInstance.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        _floorGridTexture = CreateGridTexture();

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader != null)
        {
            _floorMaterial = new Material(shader);

            // 격자 텍스처 (라인만 반투명, 배경 완전 투명)
            _floorMaterial.SetTexture("_BaseMap", _floorGridTexture); // URP
            _floorMaterial.SetTexture("_MainTex", _floorGridTexture); // Standard fallback
            _floorMaterial.SetColor("_BaseColor", Color.white);
            _floorMaterial.SetColor("_Color",     Color.white);

            // URP 투명 설정
            _floorMaterial.SetFloat("_Surface", 1f);
            _floorMaterial.SetFloat("_Blend", 0f);
            _floorMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _floorMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _floorMaterial.SetFloat("_ZWrite", 0f);
            _floorMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            _floorMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Standard 투명 fallback
            _floorMaterial.SetFloat("_Mode", 3f);
            _floorMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            _floorInstance.GetComponent<Renderer>().sharedMaterial = _floorMaterial;
        }

        SceneManager.MoveGameObjectToScene(_floorInstance, _preview.camera.scene);
    }

    private static Texture2D CreateGridTexture()
    {
        const int size      = 256;
        const int cellCount = 8;
        const int lineWidth = 3;
        int cellSize = size / cellCount;

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        var transparent = new Color32(0,   0,   0,   0);
        var lineColor   = new Color32(160, 160, 180, 180);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool onLine = (x % cellSize < lineWidth) || (y % cellSize < lineWidth);
            pixels[y * size + x] = onLine ? lineColor : transparent;
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }

    private void CleanupPreview()
    {
        ClearPreviewInstance();
        _floorInstance = null;
        if (_floorMaterial    != null) { DestroyImmediate(_floorMaterial);    _floorMaterial    = null; }
        if (_floorGridTexture != null) { DestroyImmediate(_floorGridTexture); _floorGridTexture = null; }
        _preview?.Cleanup();
        _preview = null;
    }

    private void ApplyCameraTransform()
    {
        if (_preview == null) return;
        float rad     = _camDistance;
        float pitchR  = _camPitch * Mathf.Deg2Rad;
        float yawR    = _camYaw   * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            rad * Mathf.Cos(pitchR) * Mathf.Sin(yawR),
            rad * Mathf.Sin(pitchR),
            rad * Mathf.Cos(pitchR) * Mathf.Cos(yawR));
        _preview.camera.transform.position = _camTarget + offset;
        _preview.camera.transform.LookAt(_camTarget);
    }

    // ── 프리뷰 업데이트 ────────────────────────────────────────────────────────

    private void OnEditorUpdate()
    {
        if (_previewInstance == null || _preview == null) return;
        double elapsed = EditorApplication.timeSinceStartup - _previewStartTime;
        float  t       = (float)(elapsed % _previewLoopDuration);

        // 루트 PS만 withChildren=true로 시뮬레이션.
        // 개별로 돌리면 sub-emitter 이벤트 연계와 부모-자식 순서가 깨져 일부 파티클이 누락됨.
        foreach (var ps in _previewInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.transform.parent != null &&
                ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                continue; // 부모 계층에 PS가 있으면 부모가 대신 처리
            ps.Simulate(t, true, true);
        }
        Repaint();
    }

    // ── GUI ───────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        HandleResizeDrags();

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        DrawLeftPanel();
        DrawVerticalHandle();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    // ── 좌측 패널 ──────────────────────────────────────────────────────────────

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.Label("등록된 VFX", EditorStyles.boldLabel);
        GUILayout.Space(4f);

        // ExpandHeight는 스크롤 뷰를 컨텐츠 크기만큼 늘려버려 클리핑이 없어짐.
        // 창 높이에서 헤더 높이를 뺀 명시적 높이를 줘야 스크롤바가 동작함.
        float headerH  = EditorGUIUtility.singleLineHeight + 12f;
        float scrollH  = Mathf.Max(10f, position.height - headerH);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(scrollH));
        foreach (SkillVfxKey key in Enum.GetValues(typeof(SkillVfxKey)))
        {
            if (key == SkillVfxKey.None) continue;
            bool isSelected = _hasSelection && _selectedKey == key;
            var  style      = isSelected ? EditorStyles.toolbarButton : EditorStyles.miniButton;
            if (GUILayout.Button(key.ToString(), style))
                SelectVfx(key);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ── 리사이즈 핸들 (수직 구분선) ────────────────────────────────────────────

    private void DrawVerticalHandle()
    {
        var rect = GUILayoutUtility.GetRect(HandleThickness, float.MaxValue,
            GUILayout.Width(HandleThickness), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        { _draggingLeft = true; Event.current.Use(); }
    }

    // ── 우측 패널 ──────────────────────────────────────────────────────────────

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        // 폼 영역
        EditorGUILayout.BeginVertical(GUILayout.Height(_formAreaHeight));
        DrawForm();
        EditorGUILayout.EndVertical();

        // 수평 리사이즈 핸들
        DrawHorizontalHandle();

        // 프리뷰 영역
        float previewH = position.height - _formAreaHeight - HandleThickness * 2f;
        float previewW = position.width - _leftPanelWidth - HandleThickness;
        if (previewH > 10f && previewW > 10f)
        {
            var previewRect = GUILayoutUtility.GetRect(previewW, previewH,
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            DrawPreviewWithControls(previewRect);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawForm()
    {
        GUILayout.Label("새 VFX 추가", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _newName = EditorGUILayout.TextField("이름 (PascalCase)", _newName);
        if (EditorGUI.EndChangeCheck()) ValidateName();

        EditorGUI.BeginChangeCheck();
        _newPrefab = (GameObject)EditorGUILayout.ObjectField(
            "VFX 프리팹", _newPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && _newPrefab != null)
        { _hasSelection = false; LoadPreview(_newPrefab); }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var prev = GUI.color;
            GUI.color = _statusColor;
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
            GUI.color = prev;
        }

        bool canAdd = IsValidEnumName(_newName) && _newPrefab != null
                      && !IsAlreadyRegistered(_newName);
        using (new EditorGUI.DisabledScope(!canAdd))
        {
            if (GUILayout.Button("추가", GUILayout.Height(26f)))
                Register(_newName, _newPrefab);
        }
    }

    private void DrawHorizontalHandle()
    {
        var rect = GUILayoutUtility.GetRect(
            float.MaxValue, HandleThickness,
            GUILayout.ExpandWidth(true), GUILayout.Height(HandleThickness));
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        { _draggingTop = true; Event.current.Use(); }
    }

    // ── 리사이즈 드래그 처리 ──────────────────────────────────────────────────

    private void HandleResizeDrags()
    {
        var e = Event.current;

        if (_draggingLeft || _draggingTop)
        {
            if (e.type == EventType.MouseUp)
            { _draggingLeft = _draggingTop = false; e.Use(); }
            else if (e.type == EventType.MouseDrag)
            {
                if (_draggingLeft)
                {
                    _leftPanelWidth = Mathf.Clamp(
                        e.mousePosition.x, LeftPanelMin, LeftPanelMax);
                }
                if (_draggingTop)
                {
                    _formAreaHeight = Mathf.Clamp(
                        e.mousePosition.y, FormHeightMin, FormHeightMax);
                }
                e.Use();
                Repaint();
            }
        }
    }

    // ── 프리뷰 + 카메라 조작 ──────────────────────────────────────────────────

    private void DrawPreviewWithControls(Rect rect)
    {
        // Layout 이벤트에서는 rect가 0,0으로 채워짐 — BeginPreview 호출 차단
        if (Event.current.type == EventType.Layout) return;
        if (rect.width <= 1f || rect.height <= 1f) return;
        HandleCameraInput(rect);

        if (_preview == null) return;

        if (_previewInstance == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.17f));
            GUI.Label(rect, "프리팹을 선택하면 프리뷰가 표시됩니다",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
            DrawCameraHint(rect);
            return;
        }

        _preview.BeginPreview(rect, GUIStyle.none);
        _preview.camera.Render();
        var tex = _preview.EndPreview();
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);
        DrawCameraHint(rect);
    }

    private void DrawCameraHint(Rect rect)
    {
        var hintRect = new Rect(rect.x + 4f, rect.yMax - 18f, rect.width - 8f, 16f);
        GUI.Label(hintRect,
            "우클릭 드래그: 오비트   Alt+우클릭 드래그: 팬   스크롤: 줌",
            EditorStyles.centeredGreyMiniLabel);
    }

    private void HandleCameraInput(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition) && !_orbitDragging && !_panDragging)
            return;

        // 스크롤 줌
        if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
        {
            _camDistance = Mathf.Clamp(
                _camDistance + e.delta.y * _camDistance * 0.12f, CamDistanceMin, CamDistanceMax);
            ApplyCameraTransform();
            e.Use(); Repaint();
        }

        // 드래그 시작
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            bool isPan = e.button == 2 || (e.button == 1 && e.alt);
            bool isOrbit = e.button == 1 && !e.alt;
            if (isPan)   { _panDragging   = true; _lastMousePos = e.mousePosition; e.Use(); }
            if (isOrbit) { _orbitDragging = true; _lastMousePos = e.mousePosition; e.Use(); }
        }

        // 드래그 종료
        if (e.type == EventType.MouseUp)
        {
            _orbitDragging = _panDragging = false;
        }

        // 드래그 중
        if (e.type == EventType.MouseDrag)
        {
            Vector2 delta = e.mousePosition - _lastMousePos;
            _lastMousePos = e.mousePosition;

            if (_orbitDragging)
            {
                _camYaw   += delta.x * 0.5f;
                _camPitch  = Mathf.Clamp(_camPitch - delta.y * 0.5f, -89f, 89f);
                ApplyCameraTransform();
                e.Use(); Repaint();
            }
            else if (_panDragging)
            {
                // 화면 공간 델타 → 월드 공간 이동
                float panSpeed = _camDistance * 0.001f;
                Vector3 right = _preview.camera.transform.right;
                Vector3 up    = _preview.camera.transform.up;
                _camTarget -= right * (delta.x * panSpeed);
                _camTarget += up    * (delta.y * panSpeed);
                ApplyCameraTransform();
                e.Use(); Repaint();
            }
        }
    }

    // ── 프리뷰 오브젝트 관리 ──────────────────────────────────────────────────

    private void SelectVfx(SkillVfxKey key)
    {
        _hasSelection = true;
        _selectedKey  = key;
        _newPrefab    = null;

        var guids = AssetDatabase.FindAssets("t:VfxDefinitionSO", new[] { VfxDefinitionSODir });
        foreach (var guid in guids)
        {
            var so = AssetDatabase.LoadAssetAtPath<VfxDefinitionSO>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (so != null && so.key == key && so.prefab != null)
            {
                LoadPreview(so.prefab);
                EditorGUIUtility.PingObject(so.prefab);
                return;
            }
        }
        ClearPreviewInstance();
    }

    private void LoadPreview(GameObject prefab)
    {
        if (_preview == null) InitPreview();
        ClearPreviewInstance();

        _previewInstance  = _preview.InstantiatePrefabInScene(prefab);
        _previewStartTime = EditorApplication.timeSinceStartup;

        float maxDuration = 0f;
        foreach (var ps in _previewInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            float d = ps.main.duration + ps.main.startLifetime.constantMax;
            if (d > maxDuration) maxDuration = d;
        }
        _previewLoopDuration = maxDuration > 0.1f ? maxDuration : 3f;

        foreach (var ps in _previewInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.transform.parent != null &&
                ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                continue;
            ps.Simulate(0f, true, true);
        }
    }

    private void ClearPreviewInstance()
    {
        if (_previewInstance != null)
        { DestroyImmediate(_previewInstance); _previewInstance = null; }
    }

    // ── 등록 로직 ──────────────────────────────────────────────────────────────

    private void Register(string name, GameObject prefab)
    {
        int newValue = Enum.GetValues(typeof(SkillVfxKey)).Cast<int>().Max() + 1;

        if (!TryAddToEnum(name))
        { SetStatus($"enum 추가 실패: {name}", Color.red); return; }

        CreateVfxDefinitionSO(name, prefab, newValue);
        AddToVfxContainerPrefab(newValue, prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _newName   = "";
        _newPrefab = null;
        ClearPreviewInstance();
        SetStatus($"'{name}' 등록 완료 (key={newValue}). 재컴파일 후 사용 가능.", Color.green);
        Debug.Log($"[VfxRegistration] 완료: {name} (value={newValue})");
    }

    private bool TryAddToEnum(string name)
    {
        var enc    = System.Text.Encoding.UTF8;
        var source = File.ReadAllText(EnumSourcePath, enc);

        int declPos   = source.IndexOf(EnumBlockIdentifier, StringComparison.Ordinal);
        if (declPos < 0) return false;
        int openBrace = source.IndexOf('{', declPos);
        if (openBrace < 0) return false;
        int closeBrace = source.IndexOf('}', openBrace);
        if (closeBrace < 0) return false;

        string body        = source.Substring(openBrace + 1, closeBrace - openBrace - 1);
        string trimmedBody = body.TrimEnd();
        string separator   = trimmedBody.EndsWith(",") ? "\n        " : ",\n        ";

        string newSource = source.Substring(0, openBrace + 1)
                         + trimmedBody + separator + name
                         + "\n    " + source.Substring(closeBrace);

        File.WriteAllText(EnumSourcePath, newSource, enc);
        AssetDatabase.ImportAsset(EnumSourcePath);
        return true;
    }

    private void CreateVfxDefinitionSO(string name, GameObject prefab, int enumValue)
    {
        var so = CreateInstance<VfxDefinitionSO>();
        so.prefab          = prefab;
        so.defaultLifeTime = CalculateLifeTime(prefab);

        var path = $"{VfxDefinitionSODir}/{name}.asset";
        AssetDatabase.CreateAsset(so, path);

        var serialized = new SerializedObject(so);
        serialized.FindProperty("key").intValue = enumValue;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);
    }

    private void AddToVfxContainerPrefab(int enumValue, GameObject vfxPrefab)
    {
        var contents = PrefabUtility.LoadPrefabContents(VfxContainerPrefab);
        try
        {
            var container = contents.GetComponent<SkillVfxContainer>();
            if (container == null)
            { Debug.LogError("[VfxRegistration] SkillVfxContainer를 찾을 수 없습니다."); return; }

            var vfxGo = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab, contents.transform);
            vfxGo.SetActive(false);

            var so      = new SerializedObject(container);
            var entries = so.FindProperty("entries");
            int idx     = entries.arraySize;
            entries.InsertArrayElementAtIndex(idx);
            var entry   = entries.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("key").intValue             = enumValue;
            entry.FindPropertyRelative("vfx").objectReferenceValue = vfxGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, VfxContainerPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    private static float CalculateLifeTime(GameObject prefab)
    {
        if (prefab == null) return 1f;
        var ps = prefab.GetComponentInChildren<ParticleSystem>();
        if (ps == null) return 1f;
        return ps.main.duration + ps.main.startLifetime.constantMax;
    }

    private bool IsValidEnumName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        char first = name[0];
        // 영문 대문자(PascalCase) 또는 한글 첫 글자 허용
        if (!char.IsUpper(first) && !IsKoreanChar(first)) return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static bool IsKoreanChar(char c) =>
        (c >= '가' && c <= '힣') || // 완성형 한글
        (c >= 'ᄀ' && c <= 'ᇿ');    // 자모

    private bool IsAlreadyRegistered(string name) =>
        Enum.GetNames(typeof(SkillVfxKey)).Any(n => n == name);

    private void ValidateName()
    {
        if (string.IsNullOrWhiteSpace(_newName)) { _statusMessage = ""; return; }
        char first = string.IsNullOrEmpty(_newName) ? '\0' : _newName[0];
        if (!char.IsUpper(first) && !IsKoreanChar(first))
            SetStatus("영문 대문자(PascalCase) 또는 한글로 시작해야 합니다.", Color.yellow);
        else if (!_newName.All(c => char.IsLetterOrDigit(c) || c == '_'))
            SetStatus("영문자, 한글, 숫자, 밑줄(_)만 사용 가능합니다.", Color.yellow);
        else if (IsAlreadyRegistered(_newName))
            SetStatus($"'{_newName}'은 이미 등록된 이름입니다.", Color.yellow);
        else
            SetStatus($"'{_newName}' — 유효한 이름입니다.", Color.green);
    }

    private void SetStatus(string msg, Color color)
    { _statusMessage = msg; _statusColor = color; }
}
#endif
