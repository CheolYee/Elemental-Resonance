using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gamelib.SoundSystem.Editor
{
    public class SoundManagerWindow : EditorWindow
    {
        private const string PrefPrefix     = "SoundManagerWindow_";
        private const string PrefSfxAudio   = PrefPrefix + "SfxAudioFolder";
        private const string PrefBgmAudio   = PrefPrefix + "BgmAudioFolder";
        private const string PrefSfxSo      = PrefPrefix + "SfxSoFolder";
        private const string PrefBgmSo      = PrefPrefix + "BgmSoFolder";

        [MenuItem("Window/Sound Manager")]
        private static void Open() => GetWindow<SoundManagerWindow>("Sound Manager");

        // Settings
        private string _sfxAudioFolder;
        private string _bgmAudioFolder;
        private string _sfxSoFolder;
        private string _bgmSoFolder;
        private SoundListSo _sfxList;
        private SoundListSo _bgmList;
        private bool _settingsFoldout = true;

        // Tab
        private int _selectedTab;
        private readonly string[] _tabNames = { "SFX", "BGM" };

        // List + edit panel
        private SoundClipSo _selectedSo;
        private SerializedObject _selectedSoSerialized;
        private Vector2 _listScrollPos;
        private Vector2 _editScrollPos;
        private Texture2D _selectedBg;

        // Playback
        private bool  _isPlaying;
        private int   _pausedSamplePos;
        private float _previewTime;

        private void OnEnable()
        {
            _sfxAudioFolder = EditorPrefs.GetString(PrefSfxAudio, "Assets/00. Work/CheolYee/10. Sounds/SFX");
            _bgmAudioFolder = EditorPrefs.GetString(PrefBgmAudio, "Assets/00. Work/CheolYee/10. Sounds/BGM");
            _sfxSoFolder    = EditorPrefs.GetString(PrefSfxSo,    "Assets/00. Work/CheolYee/10. Sounds/SFXSO");
            _bgmSoFolder    = EditorPrefs.GetString(PrefBgmSo,    "Assets/00. Work/CheolYee/10. Sounds/BGMSO");
            AutoFindSoundLists();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SoundEditorUtils.StopEditorClip();
            _isPlaying = false;
            if (_selectedBg != null)
                DestroyImmediate(_selectedBg);
        }

        private void OnDestroy() => SoundEditorUtils.StopEditorClip();

        private void OnEditorUpdate()
        {
            if (!_isPlaying) return;
            if (!SoundEditorUtils.IsPreviewPlaying())
            {
                // 클립 재생 자연 종료
                _isPlaying      = false;
                _pausedSamplePos = 0;
                _previewTime    = 0f;
                Repaint();
                return;
            }
            var clip = _selectedSo?.clip;
            if (clip != null && clip.samples > 0)
                _previewTime = (float)SoundEditorUtils.GetPreviewSamplePosition() / clip.samples * clip.length;
            Repaint();
        }

        private void AutoFindSoundLists()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundListSo");
            foreach (string guid in guids)
            {
                SoundListSo so = AssetDatabase.LoadAssetAtPath<SoundListSo>(AssetDatabase.GUIDToAssetPath(guid));
                if (so == null) continue;
                if (so.enumName == "SfxSounds" && _sfxList == null) _sfxList = so;
                else if (so.enumName == "BgmSounds" && _bgmList == null) _bgmList = so;
            }
        }

        private void OnGUI()
        {
            EnsureSelectedBg();
            DrawSettings();
            EditorGUILayout.Space(6);

            int prevTab = _selectedTab;
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            if (_selectedTab != prevTab)
                ClearSelection();

            EditorGUILayout.Space(4);
            DrawTabContent(
                _selectedTab == 0 ? _sfxList : _bgmList,
                _selectedTab == 0 ? _sfxAudioFolder : _bgmAudioFolder,
                _selectedTab == 0 ? _sfxSoFolder : _bgmSoFolder,
                _selectedTab == 0 ? AudioTypes.SFX : AudioTypes.MUSIC);
        }

        // ── Settings ────────────────────────────────────────────────────────

        private void DrawSettings()
        {
            _settingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_settingsFoldout, "Settings");
            if (_settingsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawFolderField("SFX Audio Folder",     ref _sfxAudioFolder, PrefSfxAudio);
                DrawFolderField("BGM Audio Folder",     ref _bgmAudioFolder, PrefBgmAudio);
                DrawFolderField("SFX SO Output Folder", ref _sfxSoFolder,    PrefSfxSo);
                DrawFolderField("BGM SO Output Folder", ref _bgmSoFolder,    PrefBgmSo);

                _sfxList = (SoundListSo)EditorGUILayout.ObjectField("SFX Sound List", _sfxList, typeof(SoundListSo), false);
                _bgmList = (SoundListSo)EditorGUILayout.ObjectField("BGM Sound List", _bgmList, typeof(SoundListSo), false);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawFolderField(string label, ref string value, string prefKey)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            string next = EditorGUILayout.TextField(value);
            if (next != value)
            {
                value = next;
                EditorPrefs.SetString(prefKey, value);
            }
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string picked = EditorUtility.OpenFolderPanel("폴더 선택", value, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    string dataPath = Application.dataPath;
                    if (picked.StartsWith(dataPath))
                        picked = "Assets" + picked.Substring(dataPath.Length);
                    value = picked;
                    EditorPrefs.SetString(prefKey, value);
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab content ──────────────────────────────────────────────────────

        private void DrawTabContent(SoundListSo list, string audioFolder, string soFolder, AudioTypes audioType)
        {
            // Sync button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = list != null;
            if (GUILayout.Button("Sync & Generate Enum", GUILayout.Height(24), GUILayout.Width(190)))
                SyncAndGenerate(list, audioFolder, soFolder, audioType);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            // Split layout
            float listWidth = Mathf.Floor(position.width * 0.50f) - 8f;
            EditorGUILayout.BeginHorizontal();
            DrawSoundList(list, listWidth);
            EditorGUILayout.Space(4);
            DrawEditPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ── List panel ───────────────────────────────────────────────────────

        private void DrawSoundList(SoundListSo list, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            if (list == null)
            {
                EditorGUILayout.HelpBox("Settings에서 SoundListSo를 지정하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            SoundClipSo[] sounds = list.sounds ?? Array.Empty<SoundClipSo>();

            EditorGUILayout.LabelField($"등록됨: {sounds.Length}개", EditorStyles.miniLabel);
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos);

            for (int i = 0; i < sounds.Length; i++)
            {
                SoundClipSo so = sounds[i];
                if (so == null) continue;

                bool selected = _selectedSo == so;
                GUIStyle rowStyle = selected ? SelectedRowStyle() : GUIStyle.none;

                Rect rowRect = EditorGUILayout.BeginHorizontal(rowStyle, GUILayout.Height(18));
                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    SelectSo(so);
                    Event.current.Use();
                }

                string loopMark = so.loop ? " ↻" : "";
                string noteMark = !string.IsNullOrEmpty(so.note) ? " ✎" : "";
                EditorGUILayout.LabelField(
                    $"[{i}]",
                    GUILayout.Width(28));
                EditorGUILayout.LabelField(
                    $"{so.soundName}{loopMark}{noteMark}",
                    GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(
                    $"vol {so.volume:0.0}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(46));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Edit panel ───────────────────────────────────────────────────────

        private void DrawEditPanel()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedSo == null)
            {
                EditorGUILayout.HelpBox("왼쪽 리스트에서 항목을 선택하세요.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_selectedSoSerialized == null || _selectedSoSerialized.targetObject != _selectedSo)
                _selectedSoSerialized = new SerializedObject(_selectedSo);

            _editScrollPos = EditorGUILayout.BeginScrollView(_editScrollPos);
            _selectedSoSerialized.Update();

            EditorGUILayout.LabelField(_selectedSo.soundName, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawProp("clip",                 "Clip");
            DrawProp("note",                 "Note");
            DrawProp("audioType",            "Audio Type");
            DrawProp("loop",                 "Loop");
            DrawProp("volume",               "Volume");
            DrawProp("pitch",                "Pitch");
            DrawProp("randomizePitch",       "Randomize Pitch");
            if (_selectedSo.randomizePitch)
                DrawProp("randomPitchModifier", "Pitch Modifier");

            if (_selectedSoSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selectedSo);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("에셋 선택 (Project)", GUILayout.Height(22)))
                EditorGUIUtility.PingObject(_selectedSo);

            EditorGUILayout.EndScrollView();

            DrawPlaybackPanel(_selectedSo);

            EditorGUILayout.EndVertical();
        }

        private void DrawPlaybackPanel(SoundClipSo so)
        {
            var clip = so?.clip;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 구분선

            GUI.enabled = clip != null;

            // 시크바
            float duration   = clip != null ? clip.length : 1f;
            float newTime    = EditorGUILayout.Slider(_previewTime, 0f, duration);
            if (!Mathf.Approximately(newTime, _previewTime) && clip != null)
            {
                _previewTime     = newTime;
                _pausedSamplePos = Mathf.RoundToInt(newTime / duration * clip.samples);
                if (_isPlaying) PlayPreview(); // 드래그 중이면 해당 위치부터 즉시 이어 재생
            }

            // 시간 표시
            EditorGUILayout.LabelField(
                $"{FormatTime(_previewTime)} / {FormatTime(duration)}",
                EditorStyles.centeredGreyMiniLabel);

            // 버튼 행
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string playLabel = _isPlaying ? "⏸ Pause" : "▶ Play";
            if (GUILayout.Button(playLabel, GUILayout.Width(80), GUILayout.Height(24)))
            {
                if (_isPlaying) PausePreview();
                else            PlayPreview();
            }

            if (GUILayout.Button("■ Stop", GUILayout.Width(70), GUILayout.Height(24)))
                StopPreview();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            GUI.enabled = true;
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            int ms = (int)((seconds - Mathf.Floor(seconds)) * 10);
            return $"{m}:{s:00}.{ms}";
        }

        private void DrawProp(string propName, string label)
        {
            SerializedProperty prop = _selectedSoSerialized.FindProperty(propName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }

        // ── Sync & Generate ──────────────────────────────────────────────────

        private void SyncAndGenerate(SoundListSo list, string audioFolder, string soFolder, AudioTypes audioType)
        {
            if (list == null) return;

            // 1. 오디오 폴더의 모든 AudioClip 수집
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioFolder });
            List<AudioClip> allClips = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .ToList();

            // 2. 이미 등록된 clip 레퍼런스 집합 구성
            var registered = new HashSet<AudioClip>();
            if (list.sounds != null)
            {
                foreach (SoundClipSo so in list.sounds)
                    if (so != null && so.clip != null)
                        registered.Add(so.clip);
            }

            // 3. 미등록 clip → SoundClipSo 생성 또는 기존 SO 재활용
            EnsureDirectory(soFolder);
            var toAdd = new List<SoundClipSo>();
            int createdCount = 0;

            foreach (AudioClip clip in allClips)
            {
                if (registered.Contains(clip)) continue;

                string soPath = $"{soFolder}/{clip.name}.asset";
                SoundClipSo existing = AssetDatabase.LoadAssetAtPath<SoundClipSo>(soPath);
                if (existing != null)
                {
                    // 경로에 SO가 있지만 리스트에 없는 경우 — 추가만
                    if (list.sounds == null || !list.sounds.Contains(existing))
                        toAdd.Add(existing);
                    continue;
                }

                SoundClipSo newSo = CreateInstance<SoundClipSo>();
                newSo.soundName = clip.name;
                newSo.clip      = clip;
                newSo.audioType = audioType;
                newSo.volume    = 1f;
                newSo.pitch     = 1f;

                AssetDatabase.CreateAsset(newSo, soPath);
                toAdd.Add(newSo);
                createdCount++;
            }

            // 4. 리스트에 추가
            if (toAdd.Count > 0)
            {
                Undo.RecordObject(list, "Sync Sound List");
                List<SoundClipSo> all = new List<SoundClipSo>(list.sounds ?? Array.Empty<SoundClipSo>());
                foreach (SoundClipSo so in toAdd)
                    if (!all.Contains(so)) all.Add(so);
                list.sounds = all.ToArray();
                EditorUtility.SetDirty(list);
            }

            // 5. Enum 재생성
            GenerateEnum(list);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = createdCount > 0
                ? $"[Sound Manager] Sync 완료 — SO {createdCount}개 생성, 총 {toAdd.Count}개 추가, enum 재생성"
                : $"[Sound Manager] Sync 완료 — 신규 없음, enum 재생성";
            Debug.Log(msg);
            Repaint();
        }

        private void GenerateEnum(SoundListSo listData)
        {
            IEnumerable<SoundClipSo> valid = (listData.sounds ?? Array.Empty<SoundClipSo>())
                .Where(so => so != null);

            int index = 0;
            string enumString = string.Join(",\n\t\t", valid.Select(so =>
            {
                so.soundIndex = index;
                EditorUtility.SetDirty(so);
                return $"{ToEnumName(so.soundName)} = {index++}";
            }));

            string code = string.Format(SoundCodeFormat.EnumFormat,
                "Gamelib.SoundSystem", listData.enumName, enumString);

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string editorDir  = Path.GetDirectoryName(scriptPath);
            DirectoryInfo parent = Directory.GetParent(editorDir);
            if (parent == null)
            {
                Debug.LogError("[Sound Manager] Enum 출력 디렉토리를 찾을 수 없습니다.");
                return;
            }
            File.WriteAllText(Path.Combine(parent.FullName, $"{listData.enumName}.cs"), code, System.Text.Encoding.UTF8);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void EnsureDirectory(string assetRelativePath)
        {
            // assetRelativePath = "Assets/..." → full system path
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (projectRoot == null) return;
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetRelativePath));
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        private static string ToEnumName(string name) =>
            name.Replace(" ", "_").Replace("-", "_").ToUpperInvariant();

        private void SelectSo(SoundClipSo so)
        {
            StopPreview();
            _selectedSo           = so;
            _selectedSoSerialized = so != null ? new SerializedObject(so) : null;
        }

        // ── Playback helpers ─────────────────────────────────────────────────

        private void PlayPreview()
        {
            var clip = _selectedSo?.clip;
            if (clip == null) return;
            SoundEditorUtils.StopEditorClip(); // 기존 재생 반드시 먼저 정지
            int startSample = Mathf.Clamp(_pausedSamplePos, 0, clip.samples - 1);
            SoundEditorUtils.PlayEditorClipFromSample(clip, startSample);
            _isPlaying = true;
        }

        private void PausePreview()
        {
            _pausedSamplePos = SoundEditorUtils.GetPreviewSamplePosition();
            var clip = _selectedSo?.clip;
            if (clip != null && clip.samples > 0)
                _previewTime = (float)_pausedSamplePos / clip.samples * clip.length;
            SoundEditorUtils.StopEditorClip();
            _isPlaying = false;
        }

        private void StopPreview()
        {
            SoundEditorUtils.StopEditorClip();
            _isPlaying       = false;
            _pausedSamplePos = 0;
            _previewTime     = 0f;
        }

        private void ClearSelection()
        {
            _selectedSo           = null;
            _selectedSoSerialized = null;
        }

        private void EnsureSelectedBg()
        {
            if (_selectedBg != null) return;
            _selectedBg = new Texture2D(1, 1);
            _selectedBg.SetPixel(0, 0, new Color(0.24f, 0.37f, 0.58f, 1f));
            _selectedBg.Apply();
        }

        private GUIStyle SelectedRowStyle()
        {
            var style = new GUIStyle(GUIStyle.none);
            style.normal.background = _selectedBg;
            return style;
        }
    }
}
