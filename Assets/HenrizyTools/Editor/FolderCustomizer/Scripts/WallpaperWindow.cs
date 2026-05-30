using System.IO;
using UnityEditor;
using UnityEngine;

namespace Henrizy.Editor.ColoredFolder
{ 
    public class WallpaperWindow : EditorWindow
    {
        private const string DefaultSaveRelativePath = "Assets/HenrizyTools/Editor/FolderCustomizer/Resources/FCData/folder_customizer_save.fc";
        private static string _currentSavePath;
        private static Vector2 _windowSize = new Vector2(260, 120);
        private static Vector2 _previewMaxSize = new Vector2(200, 200);

        [System.Serializable]
        private class WallpaperSaveData
        {
            public string texturePath;
            public float alpha;
        }

        private Texture2D _texture;
        private float _alpha = 0.2f;

        private static string SavePath
        {
            get
            {
                if (string.IsNullOrEmpty(_currentSavePath))
                {
                    _currentSavePath = EditorPrefs.GetString(
                        "Henrizy.FolderCustomizer.WallpaperSavePath",
                        DefaultSaveRelativePath);
                }
                return _currentSavePath;
            }
            set
            {
                _currentSavePath = value;
                EditorPrefs.SetString("Henrizy.FolderCustomizer.WallpaperSavePath", _currentSavePath);
            }
        }

        // Shortcut: Ctrl Alt A
        // % = Ctrl, & = Alt, a = key
        [MenuItem("Tools/HenrizyTools/Browser wallpaper %&a")] 
        public static void Open()
        {
            var window = GetWindow<WallpaperWindow>("Browser Wallpaper");
            window.minSize = _windowSize;
            window.Load();
            window.Show();
        }

        private void OnGUI()
        {
            GUIContent titleContent = new GUIContent("Browser Wallpaper");
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(titleContent, titleStyle);
            EditorGUILayout.Space(10);

            // Save path field + browse button
            EditorGUILayout.LabelField("Save Path (choose a path to save your wallpaper setting)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            SavePath = EditorGUILayout.TextField(SavePath);
            GUI.enabled = true;
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    dir = Application.dataPath;

                string fileName = Path.GetFileName(SavePath);
                if (string.IsNullOrEmpty(fileName))
                    fileName = "folder_customizer_save.fc";

                string fullPath = EditorUtility.SaveFilePanel(
                    "Select save file",
                    dir,
                    fileName,
                    "fc");

                if (!string.IsNullOrEmpty(fullPath))
                {
                    if (fullPath.StartsWith(Application.dataPath))
                    {
                        string rel = "Assets" + fullPath.Substring(Application.dataPath.Length);
                        SavePath = rel.Replace('\\', '/');
                    }
                    else
                    {
                        SavePath = fullPath.Replace('\\', '/');
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview", GUILayout.Width(60));

            float previewWidth = _previewMaxSize.x;
            float previewHeight = _previewMaxSize.y;

            if (_texture != null)
            {
                float aspect = (float)_texture.width / Mathf.Max(1, _texture.height);
                if (aspect >= 1f)
                {
                    previewWidth = _previewMaxSize.x;
                    previewHeight = _previewMaxSize.x / aspect;
                }
                else
                {
                    previewHeight = _previewMaxSize.y;
                    previewWidth = _previewMaxSize.y * aspect;
                }
            }

            _texture = (Texture2D)EditorGUILayout.ObjectField(
                _texture,
                typeof(Texture2D),
                false,
                GUILayout.Width(previewWidth),
                GUILayout.Height(previewHeight)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            _alpha = EditorGUILayout.Slider("Alpha", _alpha, 0.05f, 0.2f);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                Save();
            }
            if (GUILayout.Button("Clear", GUILayout.Width(80)))
            {
                ClearWallpaper();
            }
            EditorGUILayout.EndHorizontal();
            DrawCredit();
        }
        private void DrawCredit()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Wallpaper feature by HenrizyTools", EditorStyles.centeredGreyMiniLabel);
        }
        private void Save()
        {
            SaveFromTexture(_texture, _alpha);
        }

        private static void ClearWallpaper()
        {
            // Remove saved file and reset in-memory values
            string path = SavePath;
            if (File.Exists(path))
            {
                File.Delete(path);
                AssetDatabase.Refresh();
            }
            _currentSavePath = DefaultSaveRelativePath;
        }

        private static void SaveFromTexture(Texture2D texture, float alpha)
        {
            if (texture == null)
                return;

            var data = new WallpaperSaveData
            {
                texturePath = AssetDatabase.GetAssetPath(texture),
                alpha = Mathf.Clamp(alpha, 0.05f, 0.2f)
            };

            string json = JsonUtility.ToJson(data, true);

            string path = SavePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        private void Load()
        {
            string path = SavePath;
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
                return;

            var data = JsonUtility.FromJson<WallpaperSaveData>(json);
            if (data == null)
                return;

            _alpha = Mathf.Clamp(data.alpha <= 0f ? 0.2f : data.alpha, 0.05f, 0.2f);

            if (!string.IsNullOrEmpty(data.texturePath))
            {
                _texture = AssetDatabase.LoadAssetAtPath<Texture2D>(data.texturePath);
            }
        }

        public static string GetSavedBackgroundTexturePath()
        {
            var data = LoadData();
            return data != null ? data.texturePath : null;
        }

        public static float GetSavedBackgroundAlpha(float defaultAlpha = 0.2f)
        {
            var data = LoadData();
            if (data == null || data.alpha <= 0f)
                return defaultAlpha;
            return Mathf.Clamp(data.alpha, 0.05f, 0.2f);
        }

        private static WallpaperSaveData LoadData()
        {
            string path = SavePath;
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonUtility.FromJson<WallpaperSaveData>(json);
        }

        // Context menu API: set selected texture as wallpaper
        [MenuItem("Assets/Set as browser wallpaper", true)]
        private static bool ValidateSetAsBrowserWallpaper()
        {
            var tex = Selection.activeObject as Texture2D;
            return tex != null;
        }

        [MenuItem("Assets/Set as browser wallpaper", false, 2000)]
        private static void SetAsBrowserWallpaper()
        {
            var tex = Selection.activeObject as Texture2D;
            if (tex == null)
                return;

            float alpha = GetSavedBackgroundAlpha();
            SaveFromTexture(tex, alpha);
        }
    }
}
