using Gamelib.ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gamelib.ObjectPool.Editor
{
    [CustomEditor(typeof(PoolItemSo))]
    public class PoolItemSoEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;

        private TextField _nameField;
        private Button _changeButton;
        private ObjectField _prefabField;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            editorView.CloneTree(root);
            
            _nameField = root.Q<TextField>("PoolingName");
            _changeButton = root.Q<Button>("ChangeBtn");
            _prefabField = root.Q<ObjectField>("PrefabField");

            _changeButton.clicked += HandleChangeButtonClick;
            _nameField.RegisterCallback<KeyDownEvent>(HandleKeydownEvent);
            _prefabField.RegisterValueChangedCallback(HandlePrefabChangeEvent);
            
            return root;
        }

        private void HandlePrefabChangeEvent(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null) return;
            
            GameObject newObject = evt.newValue as GameObject;
            Debug.Assert(newObject != null, "새롭게 할당된 프리팹은 게임오브젝트가 아닙니다.");
            
            PoolItemSo item = target as PoolItemSo;

            if (!newObject.TryGetComponent(out IPoolable poolable))
            {
                item.prefab = null;
                EditorUtility.SetDirty(item);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Error", "Poolable 컴포넌트를 찾을 수 없습니다.", "OK");
                return;
            }
            
            poolable.PoolItem = item;
            EditorUtility.SetDirty(newObject);
            AssetDatabase.SaveAssetIfDirty(newObject);
        }

        private void HandleChangeButtonClick()
        {
            string newName = _nameField.text;
            if (string.IsNullOrEmpty(newName))
            {
                EditorUtility.DisplayDialog("Error", "이름이 비었습니다.", "OK");
                return;
            }
            
            string assetPath = AssetDatabase.GetAssetPath(target);
            
            string message = AssetDatabase.RenameAsset(assetPath, newName);
            if (string.IsNullOrEmpty(message))
            {
                target.name = newName;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", message, "OK");
            }
        }
        private void HandleKeydownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                HandleChangeButtonClick();
            }
        }

    }
}