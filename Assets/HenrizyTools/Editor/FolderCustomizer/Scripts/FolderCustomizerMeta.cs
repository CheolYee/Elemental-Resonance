using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Henrizy.Editor.ColoredFolder
{
    public class UserMetaData
    {
        public Dictionary<string, string> MetaData;
        public UserMetaData()
        {
            MetaData = new Dictionary<string, string>();
        }
        public void AddMetaData(string key, string value)
        {
            if (MetaData.ContainsKey(key))
            {
                MetaData[key] = value;
            }
            else
            {
                MetaData.Add(key, value);
            }
        }
        public string ToUserData()
        {
            List<string> userdata = new List<string>();
            foreach (var item in MetaData)
            {
                userdata.Add($"{item.Key}:{item.Value}");
            }
            return string.Join(";", userdata);
        }
        public static UserMetaData FromUserData(string userData)
        {
            UserMetaData metaData = new UserMetaData();
            string[] data = userData.Split(';');
            foreach (var item in data)
            {
                string[] keyValue = item.Split(':');
                if (keyValue.Length == 2)
                    metaData.AddMetaData(keyValue[0], keyValue[1]);
            }
            return metaData;
        }
        public string GetValue(string key)
        {
            if (MetaData.ContainsKey(key))
            {
                return MetaData[key];
            }
            return null;
        }
    }
    public class FolderCustomizeData
    {
        public Color BackgroundColor;
        public Color TextColor;
        public string Tooltip;
        public int StyleIndex; // 0: Normal, 1: Bold, 2: Italic
        public string Path;
        public string Name;
        public bool ShowFolderSize;
        public Rect Rect;
        public FolderCustomizeData()
        {
            BackgroundColor = FolderCustomizerMeta.DefaultEditorColorMainList;
            TextColor = FolderCustomizerMeta.DefaultTextColorMainList;
            Tooltip = "";
            StyleIndex = 1;
            ShowFolderSize = false;
        }
        public static FolderCustomizeData Create(string path, Rect selectedRect)
        {
            return new FolderCustomizeData()
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path),
                Rect = selectedRect,
                BackgroundColor = FolderCustomizerMeta.LoadFolderColor(path),
                TextColor = FolderCustomizerMeta.LoadFolderTextColor(path),
                Tooltip = FolderCustomizerMeta.LoadFolderTooltip(path),
                StyleIndex = int.TryParse(FolderCustomizerMeta.LoadStyle(path), out int style) ? style : 0,
                ShowFolderSize = FolderCustomizerMeta.LoadShowFolderSize(path),
            };
        }
        public void ToMeta()
        {
            FolderCustomizerMeta.SetFolderMetadata(Path, FolderCustomizerMeta.CustomColorKey, $"{ColorUtility.ToHtmlStringRGBA(BackgroundColor)}-{BackgroundColor.a}");
            FolderCustomizerMeta.SetFolderMetadata(Path, FolderCustomizerMeta.CustomTextColorKey, $"{ColorUtility.ToHtmlStringRGBA(TextColor)}-{TextColor.a}");
            FolderCustomizerMeta.SetFolderMetadata(Path, FolderCustomizerMeta.CustomTooltipKey, Tooltip);
            FolderCustomizerMeta.SetFolderMetadata(Path, FolderCustomizerMeta.CustomStyleKey, StyleIndex.ToString());
            FolderCustomizerMeta.SetFolderMetadata(Path, FolderCustomizerMeta.CustomFolderSizeKey, ShowFolderSize ? "1" : "0");
        }
    }
    public class FolderCustomizerMeta : AssetPostprocessor
    {
        public static readonly string CustomColorKey = "FolderColor";
        public static readonly string CustomTextColorKey = "TextColor";
        public static readonly string CustomTooltipKey = "Tooltip";
        public static readonly string CustomStyleKey = "Style";
        public static readonly string CustomFolderSizeKey = "FolderSize";
        public static Color DefaultEditorColorMainList
        {
            get
            {
                // Dark: #3C3C3C, Light: #E1E1E1
                return EditorGUIUtility.isProSkin
                    ? new Color32(60, 60, 60, 255)
                    : new Color32(200, 200, 200, 255);
            }
        }
        public static Color DefaultEditorColorGridView
        {
            get
            {
                // Dark: #3C3C3C, Light: #E1E1E1
                return EditorGUIUtility.isProSkin
                    ? new Color32(51, 51, 51, 255)
                    : new Color32(190, 190, 190, 255);
            }
        }

        public static Color DefaultEditorColorNoAlphaMainList
        {
            get
            {
                var c = DefaultEditorColorMainList;
                c.a = 0f;
                return c;
            }
        }
        public static Color DefaultEditorColorNoAlphaGridView
        {
            get
            {
                var c = DefaultEditorColorGridView;
                c.a = 0f;
                return c;
            }
        }

        public static Color DefaultEditorSelectedColorMainList
        {
            get
            {
                // Dark: #2D5D87, Light: #C2D6EC
                return EditorGUIUtility.isProSkin
                    ? new Color32(45, 93, 135, 255)
                    : new Color32(194, 214, 236, 255);
            }
        }

        public static Color DefaultEditorSelectedColorGridView
        {
            get
            {
                // Dark: #2D5D87, Light: #C2D6EC
                return EditorGUIUtility.isProSkin
                    ? new Color32(45, 93, 135, 255)
                    : new Color32(58, 114, 176, 255);
            }
        }
        public static Color DefaultTextColorMainList
        {
            get
            {
                // Dark: #FFFFFF, Light: #222222
                return EditorGUIUtility.isProSkin
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(34, 34, 34, 255);
            }
        }
        public static Color DefaultTextColorGridView
        {
            get
            {
                // Dark: #FFFFFF, Light: #222222
                return EditorGUIUtility.isProSkin
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(34, 34, 34, 255);
            }
        }

        [MenuItem("Assets/Customize/Open Window", false, 1001)]
        private static void CustomizeFolder()
        {
            string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

            if (AssetDatabase.IsValidFolder(path))
            {
                FolderCustomizeWindow.Open(path);
            }
        }
        [MenuItem("Assets/Customize/Reset", false, 1002)]
        private static void ResetFolder()
        {
            string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

            if (AssetDatabase.IsValidFolder(path))
            {
                ResetFolderMetaData(path);

                AssetDatabase.Refresh();
            }
        }


        [MenuItem("Assets/Customize/Open Window", true)]
        private static bool ValidateCustomizeFolder()
        {
            if (Selection.assetGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return AssetDatabase.IsValidFolder(path);
            }
            return false;
        }
        [MenuItem("Assets/Customize/Reset", true)]
        private static bool ValidateResetFolder()
        {
            if (Selection.assetGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return AssetDatabase.IsValidFolder(path);
            }
            return false;
        }

        public static void SetFolderMetadata(string path, string key, string value)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                UserMetaData metaData = UserMetaData.FromUserData(importer.userData);
                metaData.AddMetaData(key, value);
                importer.userData = metaData.ToUserData();
                importer.SaveAndReimport();
            }
        }
        public static void ResetFolderMetaData(string path, string key)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                UserMetaData userMetaData = UserMetaData.FromUserData(importer.userData);
                userMetaData.MetaData.Remove(key);
                importer.userData = userMetaData.ToUserData();
                importer.SaveAndReimport();
            }
        }
        public static void ResetFolderMetaData(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                UserMetaData userMetaData = UserMetaData.FromUserData(importer.userData);
                userMetaData.MetaData = new Dictionary<string, string>();
                importer.userData = userMetaData.ToUserData();
                importer.SaveAndReimport();
            }
        }

        public static Color LoadFolderColor(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                string value = UserMetaData.FromUserData(importer.userData).GetValue(CustomColorKey);
                string[] splitValues = value?.Split('-');
                string hexValue = "000000";
                string alpha = "0";
                if (splitValues != null && splitValues.Length == 2)
                {
                    hexValue = splitValues[0];
                    alpha = splitValues[1];
                }
                if (!string.IsNullOrEmpty(value))
                {
                    if (ColorUtility.TryParseHtmlString("#" + hexValue, out Color color))
                    {
                        if (float.TryParse(alpha, out float alphaValue))
                        {
                            color.a = alphaValue;
                        }
                        return color;
                    }
                }
            }
            return DefaultEditorColorNoAlphaMainList;
        }
        public static Color LoadFolderTextColor(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                string value = UserMetaData.FromUserData(importer.userData).GetValue(CustomTextColorKey);
                string[] splitValues = value?.Split('-');
                string hexValue = "000000";
                string alpha = "0";
                if (splitValues != null && splitValues.Length == 2)
                {
                    hexValue = splitValues[0];
                    alpha = splitValues[1];
                }
                if (!string.IsNullOrEmpty(value))
                {
                    if (ColorUtility.TryParseHtmlString("#" + hexValue, out Color color))
                    {
                        if (float.TryParse(alpha, out float alphaValue))
                        {
                            color.a = alphaValue;
                        }
                        return color;
                    }
                }
            }
            return DefaultTextColorMainList;
        }
        public static string LoadFolderTooltip(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                string value = UserMetaData.FromUserData(importer.userData).GetValue(CustomTooltipKey);
                return value;
            }
            return "";
        }
        public static string LoadStyle(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                string value = UserMetaData.FromUserData(importer.userData).GetValue(CustomStyleKey);
                return value;
            }
            return "";
        }
        public static bool LoadShowFolderSize(string path)
        {
            // value is 0 or 1 for false or true
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null && !string.IsNullOrEmpty(importer.userData))
            {
                string value = UserMetaData.FromUserData(importer.userData).GetValue(CustomFolderSizeKey);
                if (int.TryParse(value, out int intValue))
                {
                    return intValue != 0;
                }
            }
            return false;
        }
        public static bool HasCustomized(string path)
        {
            //return true;
            var importer = AssetImporter.GetAtPath(path);
            return importer != null && !string.IsNullOrEmpty(importer.userData);
        }
    }
    public enum GradientDirection
    {
        Horizontal,
        Vertical
    }
    public class CachedTextureData
    {
        public Color Color;
        public Texture2D ForwardTexture;
        public Texture2D BackwardTexture;
        public Texture2D FillSmallGapTexture;
        public int Width;
        public int Height;
        public int Buffer;
    }
    [InitializeOnLoad]
    public class FolderIconOverlay
    {
        private static readonly Dictionary<string, CachedTextureData> _textureCache = new();
        private static readonly Dictionary<string, Texture2D> _colorizedIconCache = new();
        private static readonly Dictionary<string, string> _folderSizeCache = new();

        private static readonly string FolderIconBaseName = "FolderIcons/FolderIconBase";
        private static readonly string EmptyFolderIconBaseName = "FolderIcons/EmptyFolderIconBase";


        private static Assembly editorAssembly;
        private static Type _projectBrowserType;
        private static List<EditorWindow> _injectedWindows = new();
        private static Texture2D _cachedBackgroundTexture = null;
        private static string _currentBackgroundPath = "";

        static FolderIconOverlay()
        {
            editorAssembly ??= Assembly.GetAssembly(typeof(UnityEditor.Editor));
            _projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.delayCall += DelayCall;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.update += UpdateLoop;
        }

        private static void UpdateLoop()
        {
            var browsers = Resources.FindObjectsOfTypeAll(_projectBrowserType);
            foreach (EditorWindow win in browsers)
            {
                if (_injectedWindows.Contains(win))
                    continue;
                var imgui = new IMGUIContainer(DrawBrowserBackground);
                win.rootVisualElement.Add(imgui);
                _injectedWindows.Add(win);
            }
        }

        private static void DelayCall()
        {
            ComputeFolderSizesOnce();
        }
        public static void Apply()
        {
            ComputeFolderSizesOnce();
        }
        private static void DrawOverrideFolderLayout(FolderCustomizeData data, string guid, Rect selectionRect)
        {
            // Apply same background logic for all assets (not only folders)
            bool isSelected = Selection.assetGUIDs.Contains(guid);
            Color bgColor = isSelected ? FolderCustomizerMeta.DefaultEditorSelectedColorMainList : FolderCustomizerMeta.DefaultEditorColorMainList;
            Rect rect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height);
            EditorGUI.DrawRect(rect, bgColor);
        }
        private static void DrawOverrideFolderLayoutBigFolder(FolderCustomizeData data, string guid, Rect selectionRect)
        {
            // Apply grid background for all assets
            bool isSelected = Selection.assetGUIDs.Contains(guid);
            Color gridColor = FolderCustomizerMeta.DefaultEditorColorGridView;
            Color bgTextColor = isSelected ? FolderCustomizerMeta.DefaultEditorSelectedColorGridView : FolderCustomizerMeta.DefaultEditorColorGridView;
            Rect rect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height);
            string assetName = System.IO.Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid));
            float nameWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(assetName)).x + 15;
            nameWidth = Mathf.Min(nameWidth, selectionRect.width);
            Rect textRect = new Rect(selectionRect.x + (selectionRect.width - nameWidth) / 2f, selectionRect.yMax - 14, nameWidth, 16);

            if (data.BackgroundColor.a > 0)
            {
                EditorGUI.DrawRect(rect, gridColor);
            }
            EditorGUI.DrawRect(textRect, bgTextColor);
        }

        private static bool IsMainListAsset(Rect rect)
        {
            if (rect.height > 20)
            {
                return false;
            }
            return true;
        }
        private static void DrawCustomize(string path, string guid, Rect selectionRect)
        {
            FolderCustomizeData data = FolderCustomizeData.Create(path, selectionRect);
            DrawOverrideFolderLayout(data, guid, selectionRect);
            DrawCustomize(data);
            DrawFolderSize(data);
        }
        private static void DrawCustomizeBigFolder(string path, string guid, Rect selectionRect)
        {
            FolderCustomizeData data = FolderCustomizeData.Create(path, selectionRect);
            DrawOverrideFolderLayoutBigFolder(data, guid, selectionRect);
            DrawCustomizeBigFolder(data);
        }
        private static void DrawFolderSize(FolderCustomizeData data)
        {
            // Only draw in main list view and when enabled
            if (!IsMainListAsset(data.Rect) || !data.ShowFolderSize)
                return;

            if (!AssetDatabase.IsValidFolder(data.Path))
                return;

            // Use cached size (computed once after compile)
            if (!_folderSizeCache.TryGetValue(data.Path, out string sizeText) || string.IsNullOrEmpty(sizeText))
                return;

            // Calculate name rect and width to avoid overlap
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = data.StyleIndex switch
                {
                    0 => FontStyle.Normal,
                    1 => FontStyle.Bold,
                    2 => FontStyle.Italic,
                    3 => FontStyle.BoldAndItalic,
                    _ => FontStyle.Normal,
                },
                normal = { textColor = data.TextColor }
            };
            float nameX = data.Rect.x + 18f;
            float nameWidth = nameStyle.CalcSize(new GUIContent(data.Name)).x;
            float nameRight = nameX + nameWidth;

            // Draw at the right side of the row
            float padding = 6f;
            Vector2 textSize = EditorStyles.miniLabel.CalcSize(new GUIContent(sizeText));
            float sizeX = data.Rect.xMax - textSize.x - padding;

            // If overlapped with name, skip drawing
            if (sizeX <= nameRight + 4f)
                return;

            Rect r = new Rect(
                x: sizeX,
                y: data.Rect.y,
                width: textSize.x,
                height: data.Rect.height
            );
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            };
            GUI.Label(r, sizeText, style);
        }
        private static void OnAfterAssemblyReload()
        {
            EditorApplication.delayCall += ComputeFolderSizesOnce;
        }
        private static void ComputeFolderSizesOnce()
        {
            _folderSizeCache.Clear();
            try
            {
                var allPaths = AssetDatabase.GetAllAssetPaths();
                foreach (var path in allPaths)
                {
                    if (!AssetDatabase.IsValidFolder(path))
                        continue;
                    if (!FolderCustomizerMeta.LoadShowFolderSize(path))
                        continue;

                    string fullPath = Path.GetFullPath(path);
                    long sizeBytes = 0;
                    try
                    {
                        if (Directory.Exists(fullPath))
                        {
                            sizeBytes = GetDirectorySizeSafe(fullPath);
                        }
                    }
                    catch { }

                    _folderSizeCache[path] = FormatSize(sizeBytes);
                }
            }
            catch { }

            EditorApplication.RepaintProjectWindow();
        }
        private static long GetDirectorySizeSafe(string directory)
        {
            long total = 0;
            try
            {
                // Include files
                var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    try { var info = new FileInfo(f); total += info.Length; } catch { }
                }
                // Recurse into subdirectories
                var dirs = Directory.GetDirectories(directory);
                foreach (var d in dirs)
                {
                    total += GetDirectorySizeSafe(d);
                }
            }
            catch { }
            return total;
        }
        private static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = 1024 * KB;
            const long GB = 1024 * MB;
            if (bytes >= GB) return ($"{bytes / (float)GB:0.##} GB");
            if (bytes >= MB) return ($"{bytes / (float)MB:0.##} MB");
            if (bytes >= KB) return ($"{bytes / (float)KB:0.##} KB");
            return ($"{bytes} B");
        }
        private static void DrawCustomize(FolderCustomizeData data)
        {
            string path = data.Path;
            bool isEmpty = AssetDatabase.IsValidFolder(path) 
                            && !AssetDatabase.GetSubFolders(path).Any()
                           && !AssetDatabase.FindAssets(string.Empty, new[] { path })
                               .Any(guid => AssetDatabase.GUIDToAssetPath(guid) != path);

            float width = data.Rect.width;
            Rect rect = default;
            int buffer = (int)data.Rect.x;
            rect = new Rect(buffer, data.Rect.y, width, data.Rect.height);

            string cacheKey = data.Path;
            int height = (int)data.Rect.height;

            if (data.BackgroundColor.a > 0)
            {
                if (!_textureCache.TryGetValue(cacheKey, out var cached) ||
                    cached.Color != data.BackgroundColor ||
                    cached.Width != width ||
                    cached.Height != height ||
                    cached.Buffer != buffer)
                {
                    if (cached != null)
                    {
                        if (cached.ForwardTexture != null)
                            UnityEngine.Object.DestroyImmediate(cached.ForwardTexture);
                        if (cached.BackwardTexture != null)
                            UnityEngine.Object.DestroyImmediate(cached.BackwardTexture);
                    }

                    cached = new CachedTextureData
                    {
                        Color = data.BackgroundColor,
                        Width = (int)width,
                        Height = height,
                        Buffer = buffer,
                        ForwardTexture = EditorUtils.MakeGradientTex2D((int)width, height, Vector2.right, data.BackgroundColor, FolderCustomizerMeta.DefaultEditorColorNoAlphaMainList),
                        BackwardTexture = EditorUtils.MakeGradientTex2D(buffer, height, Vector2.left, data.BackgroundColor, FolderCustomizerMeta.DefaultEditorColorNoAlphaMainList),
                        FillSmallGapTexture = EditorUtils.MakeGradientTex2D(2, height, Vector2.right, data.BackgroundColor, data.BackgroundColor),

                    };
                    _textureCache[cacheKey] = cached;
                }
                Texture2D gradientTextureForward = cached.ForwardTexture;
                Texture2D gradientTextureBackward = cached.BackwardTexture;
                //Texture2D fillSmallGrapTexture = cached.FillSmallGapTexture;
                if (gradientTextureForward != null)
                {
                    // Draw the main area [buffer, buffer + width)
                    GUI.DrawTexture(rect, gradientTextureForward);
                }
                // Draw the left buffer area [0, buffer) without overlapping
                Rect leftRect = new Rect(rect.x - buffer, rect.y, buffer, rect.height);
                if (gradientTextureBackward != null && buffer > 0)
                {
                    GUI.DrawTexture(leftRect, gradientTextureBackward);
                }
            }


            int styleIndex = data.StyleIndex;
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = styleIndex switch
                {
                    0 => FontStyle.Normal,
                    1 => FontStyle.Bold,
                    2 => FontStyle.Italic,
                    3 => FontStyle.BoldAndItalic,
                    _ => FontStyle.Normal,
                },
                normal =
                {
                    textColor = data.TextColor
                },
            };
            // Get correct asset icon
            Texture iconTex = AssetDatabase.GetCachedIcon(path);
            Texture2D assetIcon = iconTex as Texture2D;
            if (assetIcon == null)
            {
                assetIcon = isEmpty ? EditorGUIUtility.FindTexture("FolderEmpty Icon") as Texture2D : EditorGUIUtility.FindTexture("Folder Icon") as Texture2D;
                if (assetIcon == null)
                    assetIcon = EditorGUIUtility.FindTexture("DefaultAsset Icon") as Texture2D;
            }
            Rect iconRect = new Rect(data.Rect.x, data.Rect.y, 16, 16);
            if (assetIcon != null)
            {
                GUI.DrawTexture(iconRect, assetIcon);
            }

            Rect labelRect = new Rect(data.Rect.x + 18, data.Rect.y, width, data.Rect.height);
            GUIContent content = new GUIContent(data.Name, data.Tooltip);

            EditorGUI.LabelField(labelRect, content, boldStyle);
        }
        private static void DrawCustomizeBigFolder(FolderCustomizeData data)
        {
            string path = data.Path;
            string guid = AssetDatabase.AssetPathToGUID(path);

            bool isSelected = Selection.assetGUIDs.Contains(guid);
            if (data.BackgroundColor.a > 0)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    // Folders: keep tinted base icon logic
                    bool isEmpty = !AssetDatabase.GetSubFolders(path).Any()
                                   && !AssetDatabase.FindAssets(string.Empty, new[] { path })
                                       .Any(guid => AssetDatabase.GUIDToAssetPath(guid) != path);
                    Texture2D folderIcon = Resources.Load<Texture2D>(isEmpty ? EmptyFolderIconBaseName : FolderIconBaseName);
                    if (folderIcon == null)
                        return;

                    string cacheKey = data.Path + data.BackgroundColor.ToString();
                    Texture2D colorizedIcon = GetColorizedIcon(folderIcon, data.BackgroundColor, cacheKey);
                    if (colorizedIcon == null)
                        return;

                    float iconSize = Mathf.Min(data.Rect.width, data.Rect.height) * 0.8f;
                    float iconX = data.Rect.x + (data.Rect.width - iconSize) / 2f;
                    float iconY = data.Rect.y + (data.Rect.height - iconSize) / 2f - 5;
                    Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

                    GUI.DrawTexture(iconRect, colorizedIcon, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    // Non-folder assets: colorize their cached icon
                    Texture iconTex = AssetDatabase.GetCachedIcon(path);
                    Texture2D baseIcon = iconTex as Texture2D;
                    if (baseIcon == null)
                    {
                        baseIcon = EditorGUIUtility.FindTexture("DefaultAsset Icon") as Texture2D;
                    }
                    if (baseIcon == null)
                        return;

                    string cacheKey = data.Path + data.BackgroundColor.ToString();
                    Texture2D colorizedIcon = GetColorizedIcon(baseIcon, data.BackgroundColor, cacheKey);
                    if (colorizedIcon == null)
                        return;

                    float iconSize = Mathf.Min(data.Rect.width, data.Rect.height) * 0.8f;
                    float iconX = data.Rect.x + (data.Rect.width - iconSize) / 2f;
                    float iconY = data.Rect.y + (data.Rect.height - iconSize) / 2f - 5;
                    Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

                    GUI.DrawTexture(iconRect, colorizedIcon, ScaleMode.ScaleToFit, true);
                }
            }


            int styleIndex = data.StyleIndex;
            float contrast = GetContrastRatio(data.TextColor, FolderCustomizerMeta.DefaultEditorSelectedColorGridView);
            Color labelColor = data.TextColor;
            if (contrast < 3f && isSelected)
            {
                labelColor = Color.white;
            }
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = styleIndex switch
                {
                    0 => FontStyle.Normal,
                    1 => FontStyle.Bold,
                    2 => FontStyle.Italic,
                    3 => FontStyle.BoldAndItalic,
                    _ => FontStyle.Normal,
                },
                normal =
                {
                    textColor = labelColor
                },
                alignment = TextAnchor.MiddleCenter,
            };
            Rect labelRect = new Rect(data.Rect.x, data.Rect.yMax - 14, data.Rect.width, 16);
            GUI.Label(labelRect, new GUIContent(data.Name, data.Tooltip), boldStyle);
        }
        private static Texture2D GetColorizedIcon(Texture2D baseIcon, Color color, string cacheKey)
        {
            if (baseIcon == null)
                return null;

            if (_colorizedIconCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            var width = baseIcon.width;
            var height = baseIcon.height;
            var pixels = baseIcon.GetPixels();

            var result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a > 0f)
                {
                    p.r *= color.r;
                    p.g *= color.g;
                    p.b *= color.b;
                }
                pixels[i] = p;
            }
            result.SetPixels(pixels);
            result.Apply();
            result.hideFlags = HideFlags.HideAndDontSave;
            _colorizedIconCache[cacheKey] = result;
            return result;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!FolderCustomizerMeta.HasCustomized(path))
            {
                return;
            }
            FolderCustomizeData data = FolderCustomizeData.Create(path, selectionRect);
            if (!IsMainListAsset(selectionRect))
            {
                DrawCustomizeBigFolder(path, guid, selectionRect);
                return;
            }
            DrawCustomize(path, guid, selectionRect);
        }
        private static void DrawBrowserBackground()
        {
            string backgroundPath = WallpaperWindow.GetSavedBackgroundTexturePath();
            float backgroundAlpha = WallpaperWindow.GetSavedBackgroundAlpha();

            Rect rect = GetRightPanelRect();
            rect.y += 24;
            EnsureTextureImporterSettingsForGUI(backgroundPath);

            if (_currentBackgroundPath != backgroundPath)
            {
                _currentBackgroundPath = backgroundPath;
                _cachedBackgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
            }
            if (_cachedBackgroundTexture != null)
            {
                _cachedBackgroundTexture.filterMode = FilterMode.Bilinear;
                _cachedBackgroundTexture.wrapMode = TextureWrapMode.Clamp;

                Color prev = GUI.color;
                GUI.color = new Color(prev.r, prev.g, prev.b, prev.a * backgroundAlpha);

                GUI.DrawTexture(
                    rect,
                    _cachedBackgroundTexture,
                    ScaleMode.ScaleAndCrop,   // <- preserves aspect, fills rect, crops overflow
                    true
                );

                GUI.color = prev;
            }
        }
        // * Auto detect constrast between text color and selected folder color
        // * so that we can auto adjust the text color while selecting.
        // * Ref: https://www.w3.org/TR/WCAG20/#relativeluminancedef
        private static float GetRelativeLuminance(Color c)
        {
            float R = c.r <= 0.03928f ? c.r / 12.92f : Mathf.Pow((c.r + 0.055f) / 1.055f, 2.4f);
            float G = c.g <= 0.03928f ? c.g / 12.92f : Mathf.Pow((c.g + 0.055f) / 1.055f, 2.4f);
            float B = c.b <= 0.03928f ? c.b / 12.92f : Mathf.Pow((c.b + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * R + 0.7152f * G + 0.0722f * B;
        }

        private static float GetContrastRatio(Color c1, Color c2)
        {
            float L1 = GetRelativeLuminance(c1);
            float L2 = GetRelativeLuminance(c2);
            return (Mathf.Max(L1, L2) + 0.05f) / (Mathf.Min(L1, L2) + 0.05f);
        }

        private static Rect GetRightPanelRect()
        {
            try
            {
                editorAssembly ??= Assembly.GetAssembly(typeof(UnityEditor.Editor));
                if (editorAssembly == null)
                    return default;

                var projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");
                if (projectBrowserType == null)
                    return default;

                var listAreaRectField = projectBrowserType.GetField("m_ListAreaRect", BindingFlags.NonPublic | BindingFlags.Instance);
                if (listAreaRectField == null)
                    return default;

                // Find all project browser instances
                var browsers = Resources.FindObjectsOfTypeAll(projectBrowserType);
                if (browsers == null || browsers.Length == 0)
                    return default;

                // Identify which browser is currently drawing by matching the current GUI group's screen origin
                Vector2 currentGuiScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
                EditorWindow targetWindow = null;
                object targetBrowser = null;
                foreach (var obj in browsers)
                {
                    var win = obj as EditorWindow;
                    if (win != null && win.position.Contains(currentGuiScreenOrigin))
                    {
                        targetWindow = win;
                        targetBrowser = obj;
                        break;
                    }
                }
                // Fallback to the first browser if we couldn't match by screen origin
                if (targetBrowser == null)
                {
                    targetBrowser = browsers[0];
                    targetWindow = browsers[0] as EditorWindow;
                }

                if (targetBrowser == null)
                    return default;

                object rawRect = listAreaRectField.GetValue(targetBrowser);
                if (!(rawRect is Rect listAreaRect))
                    return default;

                // Convert from window-local coordinates to the current GUI context coordinates
                if (targetWindow != null)
                {
                    Vector2 listAreaTopLeftScreen = new Vector2(targetWindow.position.x + listAreaRect.x, targetWindow.position.y + listAreaRect.y);
                    Vector2 guiLocalTopLeft = GUIUtility.ScreenToGUIPoint(listAreaTopLeftScreen);
                    return new Rect(guiLocalTopLeft.x, guiLocalTopLeft.y, listAreaRect.width, listAreaRect.height);
                }

                return listAreaRect;
            }
            catch
            {
                return default;
            }
        }

        // Configure the texture importer for crisp GUI rendering (restored method)
        private static void EnsureTextureImporterSettingsForGUI(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                changed = true;
            }
            if (importer.streamingMipmaps)
            {
                importer.streamingMipmaps = false;
                changed = true;
            }
            if (importer.maxTextureSize < 4096)
            {
                importer.maxTextureSize = 4096;
                changed = true;
            }
            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
