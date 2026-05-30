using System;
using System.Collections.Generic;
using System.IO;
using Gamelib.ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gamelib.ObjectPool.Editor
{
    public class PoolManagerEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        [SerializeField] private PoolManagerSo poolManager = default;
        [SerializeField] private VisualTreeAsset itemAsset = default;

        private string _rootFolder = "Asset/ObjectPool";

        private Button _createBtn;
        private ScrollView _itemView;

        private List<PoolItemView> _itemList;
        private PoolItemView _selectedItem;

        private UnityEditor.Editor _cachedEditor;
        private VisualElement _inspector;

        [MenuItem("Tools/PoolManager")]
        public static void OpenWindow()
        {
            PoolManagerEditor wnd = GetWindow<PoolManagerEditor>();
            wnd.titleContent = new GUIContent("PoolManagerEditor");
        }

        private string GetCurrentDirectory()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            return Path.GetDirectoryName(scriptPath);
        }

        private void InitializeRootFolder()
        {
            string dirName = GetCurrentDirectory();
            DirectoryInfo parentDirectory = Directory.GetParent(dirName);
            Debug.Assert(parentDirectory != null, $"Parent directory is null! Check path : {dirName}");
            
            string dataPath = Application.dataPath;
            _rootFolder = parentDirectory.FullName.Replace('\\', '/');
            if (_rootFolder.StartsWith(dataPath))
            {
                _rootFolder = "Assets" + _rootFolder.Substring(dataPath.Length);
            }
        }
        
        public void CreateGUI()
        {
            InitializeRootFolder();
            
            VisualElement root = rootVisualElement;

            if (editorView == null)
            {
                string dirName = GetCurrentDirectory();
                editorView = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/PoolManagerEditor.uxml");
            }

            editorView.CloneTree(root);

            InitializeItems(root);
            GeneratePoolingItemUI();
        }

        private void InitializeItems(VisualElement root)
        {
            _createBtn = root.Q<Button>("CreateBtn");
            _createBtn.clicked += HandleCreateBtn;
            _itemView = root.Q<ScrollView>("ItemView");
            
            _itemList = new List<PoolItemView>();
            _inspector = root.Q<VisualElement>("InspectorView");
        }

        private void GeneratePoolingItemUI()
        {
            _itemView.Clear();
            _itemList.Clear();
            _inspector.Clear();

            if (poolManager == null)
            {
                string poolManagerFilePath = $"{_rootFolder}/PoolManager.asset";
                poolManager = AssetDatabase.LoadAssetAtPath<PoolManagerSo>(poolManagerFilePath);
                if (poolManager == null)
                {
                    Debug.LogError("풀메니저가 없어 자동 생성합니다.");
                    poolManager = CreateInstance<PoolManagerSo>();
                    AssetDatabase.CreateAsset(poolManager, poolManagerFilePath);
                }
            }

            if (itemAsset == null)
            {
                string dirName = GetCurrentDirectory();
                itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/PoolItemView.uxml");
            }

            foreach (PoolItemSo item in poolManager.itemList)
            {
                TemplateContainer itemUi = itemAsset.Instantiate();
                PoolItemView poolItemView = new PoolItemView(itemUi, item);
                
                _itemView.Add(itemUi);
                _itemList.Add(poolItemView);

                poolItemView.Name = item.name;
                poolItemView.IsEmpty = item.prefab == null;
                poolItemView.IsActive = false;

                poolItemView.OnSelectEvent += HandleSelectEvent;
                poolItemView.OnDeleteEvent += HandleDeleteEvent;
            }
        }

        private void HandleSelectEvent(PoolItemView targetView)
        {
            if (_selectedItem != null)
                _selectedItem.IsActive = false;
            
            _selectedItem = targetView;
            _selectedItem.IsActive = true;
            
            _inspector.Clear();
            UnityEditor.Editor.CreateCachedEditor(_selectedItem.TargetItem, null, ref _cachedEditor);
            VisualElement inspectorElement = _cachedEditor.CreateInspectorGUI();
            
            SerializedObject serializedObject = new SerializedObject(_selectedItem.TargetItem);
            inspectorElement.Bind(serializedObject);
            
            inspectorElement.TrackSerializedObjectValue(serializedObject, so =>
            {
                _selectedItem.Name = so.FindProperty("poolingName").stringValue;
                _selectedItem.IsEmpty = so.FindProperty("prefab").objectReferenceValue == null;
            });
            
            _inspector.Add(inspectorElement);
        }

        private void HandleDeleteEvent(PoolItemView targetView)
        {
            poolManager.itemList.Remove(targetView.TargetItem);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(targetView.TargetItem));
            EditorUtility.SetDirty(poolManager);
            
            AssetDatabase.SaveAssets();

            if (targetView == _selectedItem)
            {
                _selectedItem = null;
            }
            GeneratePoolingItemUI();
        }

        private void HandleCreateBtn()
        {
            Guid itemGuid = Guid.NewGuid();
            PoolItemSo newItem = CreateInstance<PoolItemSo>();
            newItem.poolingName = itemGuid.ToString();
            
            if (Directory.Exists($"{_rootFolder}/Items") == false)
            {
                Directory.CreateDirectory($"{_rootFolder}/Items");
            }
            
            AssetDatabase.CreateAsset(newItem, $"{_rootFolder}/Items/{newItem.poolingName}.asset");
            poolManager.itemList.Add(newItem);
            
            EditorUtility.SetDirty(poolManager);
            AssetDatabase.SaveAssets();
            
            GeneratePoolingItemUI();
        }
    }
}
