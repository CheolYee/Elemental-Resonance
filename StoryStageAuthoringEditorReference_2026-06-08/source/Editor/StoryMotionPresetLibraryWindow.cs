using System.Collections.Generic;
using System.IO;
using System.Text;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryMotionPresetLibraryWindow : EditorWindow
    {
        private const string DefaultPresetFolder = "Assets/00. Work/CheolYee/05. SO/Story/MotionPresets";

        private readonly List<StoryMotionPresetSO> _presets = new();
        private Vector2 _listScroll;
        private Vector2 _detailsScroll;
        private StoryMotionPresetSO _selectedPreset;
        private string _search = "";
        private string _renameValue = "";
        private bool _showList = true;
        private bool _showDetails = true;

        [MenuItem("Tools/Story/Motion Preset Library")]
        public static void Open()
        {
            var window = GetWindow<StoryMotionPresetLibraryWindow>("Motion Presets");
            window.minSize = new Vector2(360f, 360f);
            window.RefreshPresetList();
        }

        internal static void RefreshOpenWindows()
        {
            foreach (StoryMotionPresetLibraryWindow window in Resources.FindObjectsOfTypeAll<StoryMotionPresetLibraryWindow>())
            {
                if (window == null)
                    continue;

                window.RefreshPresetList();
                window.Repaint();
            }
        }

        private void OnEnable()
        {
            RefreshPresetList();
        }

        private void OnProjectChange()
        {
            RefreshPresetList();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4f);

            _showList = EditorGUILayout.BeginFoldoutHeaderGroup(_showList, $"Preset List ({_presets.Count})");
            if (_showList)
                DrawPresetList();
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4f);
            _showDetails = EditorGUILayout.BeginFoldoutHeaderGroup(_showDetails, "Selected Preset");
            if (_showDetails)
                DrawSelectedPresetDetails();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.MinWidth(90f));
                if (GUILayout.Button("x", GUI.skin.FindStyle("ToolbarSearchCancelButton"), GUILayout.Width(20f)))
                    _search = "";

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Save Selected", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                    SaveSelectedPreviewKeysAsPreset();
                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                    CreateNewPresetAsset();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    RefreshPresetList();
            }
        }

        private void DrawPresetList()
        {
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MinHeight(150f));
            foreach (StoryMotionPresetSO preset in _presets)
            {
                if (preset == null || !MatchesSearch(preset))
                    continue;

                DrawPresetRow(preset);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetRow(StoryMotionPresetSO preset)
        {
            Rect row = GUILayoutUtility.GetRect(1f, 30f, GUILayout.ExpandWidth(true));
            bool selected = preset == _selectedPreset;
            EditorGUI.DrawRect(row, selected ? new Color(0.22f, 0.28f, 0.38f) : new Color(0.16f, 0.16f, 0.18f));

            Rect labelRect = new Rect(row.x + 6f, row.y + 4f, Mathf.Max(40f, row.width - 176f), 22f);
            GUI.Label(labelRect, $"{preset.name}  [{BuildPropertySummary(preset)}]  keys:{preset.KeyCount}  len:{CalculatePresetDuration(preset):0.00}s");

            Rect applyRect = new Rect(row.xMax - 166f, row.y + 4f, 48f, 22f);
            Rect pingRect = new Rect(row.xMax - 114f, row.y + 4f, 42f, 22f);
            Rect delRect = new Rect(row.xMax - 68f, row.y + 4f, 60f, 22f);

            if (GUI.Button(applyRect, "Apply"))
                ApplyPresetToCurrentPreview(preset);
            if (GUI.Button(pingRect, "Ping"))
                PingPreset(preset);
            if (GUI.Button(delRect, "Delete"))
            {
                DeletePreset(preset);
                return;
            }

            Event evt = Event.current;
            if (!row.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                SelectPreset(preset);
                evt.Use();
            }
            else if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                SelectPreset(preset);
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { preset };
                DragAndDrop.StartDrag($"Motion Preset: {preset.name}");
                evt.Use();
            }
        }

        private void DrawSelectedPresetDetails()
        {
            if (_selectedPreset == null)
            {
                EditorGUILayout.HelpBox("Select a preset from the list, or create a new one.", MessageType.Info);
                return;
            }

            _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll, GUILayout.MinHeight(160f));
            using (new EditorGUILayout.HorizontalScope())
            {
                _renameValue = EditorGUILayout.TextField("Asset Name", _renameValue);
                if (GUILayout.Button("Rename", GUILayout.Width(70f)))
                    RenameSelectedPreset();
            }

            var serialized = new SerializedObject(_selectedPreset);
            serialized.Update();
            EditorGUILayout.HelpBox(
                $"Properties: {BuildPropertySummary(_selectedPreset)}\nKeys: {_selectedPreset.KeyCount}\nLength: {CalculatePresetDuration(_selectedPreset):0.00}s",
                MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(StoryMotionPresetSO.presetId)));
            EditorGUILayout.PropertyField(serialized.FindProperty(nameof(StoryMotionPresetSO.keyframes)), includeChildren: true);
            if (serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(_selectedPreset);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply To Current Track"))
                    ApplyPresetToCurrentPreview(_selectedPreset);
                if (GUILayout.Button("Select Asset"))
                    Selection.activeObject = _selectedPreset;
                if (GUILayout.Button("Ping"))
                    PingPreset(_selectedPreset);
            }
            EditorGUILayout.EndScrollView();
        }

        private bool MatchesSearch(StoryMotionPresetSO preset)
        {
            if (string.IsNullOrWhiteSpace(_search))
                return true;

            string needle = NormalizeSearchText(_search);
            if (string.IsNullOrWhiteSpace(needle))
                return true;

            return SearchContains(preset.name, needle)
                || SearchContains(preset.presetId, needle)
                || SearchContains(BuildPropertySummary(preset), needle)
                || SearchContains(AssetDatabase.GetAssetPath(preset), needle);
        }

        private void SelectPreset(StoryMotionPresetSO preset)
        {
            _selectedPreset = preset;
            _renameValue = preset != null ? preset.name : "";
            Repaint();
        }

        private void ApplyPresetToCurrentPreview(StoryMotionPresetSO preset)
        {
            if (preset == null)
                return;

            if (!StoryPreviewWindow.TryGetOpenPreviewWindow(out StoryPreviewWindow preview)
                || !preview.ApplyMotionPresetToCurrentTrack(preset))
            {
                ShowNotification(new GUIContent("Select an actor/property in Story Preview first."));
            }
        }

        private void SaveSelectedPreviewKeysAsPreset()
        {
            if (!StoryPreviewWindow.TryGetOpenPreviewWindow(out StoryPreviewWindow preview)
                || !preview.SaveSelectedTimelineKeysAsMotionPreset())
            {
                ShowNotification(new GUIContent("Select timeline keys in Story Preview first."));
                return;
            }

            RefreshPresetList();
        }

        private void CreateNewPresetAsset()
        {
            EnsureDefaultPresetFolder();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Motion Preset",
                "MotionPreset",
                "asset",
                "Choose where to save the new motion preset asset.",
                DefaultPresetFolder);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                EditorUtility.DisplayDialog("Cannot Create Preset", "An asset already exists at the selected path.", "OK");
                return;
            }

            StoryMotionPresetSO preset = CreateInstance<StoryMotionPresetSO>();
            preset.presetId = Path.GetFileNameWithoutExtension(path);
            preset.property = StoryActorKeyframeProperty.Position;
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshPresetList();
            SelectPreset(preset);
        }

        private void RenameSelectedPreset()
        {
            if (_selectedPreset == null || string.IsNullOrWhiteSpace(_renameValue))
                return;

            string path = AssetDatabase.GetAssetPath(_selectedPreset);
            string error = AssetDatabase.RenameAsset(path, _renameValue.Trim());
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("Rename Failed", error, "OK");
                return;
            }

            _selectedPreset.presetId = _renameValue.Trim();
            EditorUtility.SetDirty(_selectedPreset);
            AssetDatabase.SaveAssets();
            RefreshPresetList();
        }

        private void DeletePreset(StoryMotionPresetSO preset)
        {
            string path = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!EditorUtility.DisplayDialog("Delete Motion Preset", $"Delete {preset.name}?", "Delete", "Cancel"))
                return;

            if (_selectedPreset == preset)
                SelectPreset(null);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshPresetList();
        }

        private static void PingPreset(StoryMotionPresetSO preset)
        {
            if (preset == null)
                return;

            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
        }

        private void RefreshPresetList()
        {
            _presets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:StoryMotionPresetSO", new[] { "Assets/00. Work/CheolYee" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StoryMotionPresetSO preset = AssetDatabase.LoadAssetAtPath<StoryMotionPresetSO>(path);
                if (preset != null)
                    _presets.Add(preset);
            }

            _presets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private static string BuildPropertySummary(StoryMotionPresetSO preset)
        {
            if (preset?.keyframes == null || preset.keyframes.Count == 0)
                return preset != null ? "Empty" : "-";

            var properties = new SortedSet<string>();
            foreach (StoryActorKeyframeData key in preset.keyframes)
            {
                if (key != null)
                    properties.Add(key.property.ToString());
            }

            return properties.Count > 0 ? string.Join("+", properties) : "Empty";
        }

        private static bool SearchContains(string source, string normalizedNeedle)
        {
            string normalizedSource = NormalizeSearchText(source);
            return normalizedSource.Contains(normalizedNeedle);
        }

        private static string NormalizeSearchText(string value)
        {
            return (value ?? "")
                .Normalize(NormalizationForm.FormKC)
                .Trim()
                .ToLowerInvariant();
        }

        private static float CalculatePresetDuration(StoryMotionPresetSO preset)
        {
            if (preset?.keyframes == null)
                return 0f;

            float duration = 0f;
            foreach (StoryActorKeyframeData key in preset.keyframes)
            {
                if (key != null)
                    duration = Mathf.Max(duration, StoryTransitionSampler.GetKeyTime(key));
            }

            return duration;
        }

        private static void EnsureDefaultPresetFolder()
        {
            string[] parts = DefaultPresetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
