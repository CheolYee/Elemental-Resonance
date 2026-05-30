using System.IO;
using System.Linq;
using _00._Work._Resources._02._Scripts.Systems;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work._Resources._02._Scripts.Agents.FSM.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSoEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        private Button _folderBtn;
        private Button _generateBtn;
        private Label _folderPathLabel;
        
        private string _folderPath;
        private StateListSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            editorView.CloneTree(root);
            
            _folderBtn = root.Q<Button>("FolderBtn");
            _generateBtn = root.Q<Button>("GenerateBtn");
            _folderPathLabel = root.Q<Label>("SelectedFolderLabel");
            _folderPathLabel.text = "No Folder Selected";
            
            _targetData = (StateListSO)target;
            _folderBtn.clicked += HandleFolderSelectBtn;
            _generateBtn.clicked += HandleGenerateBtn;

            if (_targetData != null && !string.IsNullOrEmpty(_targetData.generatePath))
            {
                _folderPath = _targetData.generatePath;
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_targetData.generatePath);
            }
            
            return root;
        }

        private void HandleGenerateBtn()
        {
            if (string.IsNullOrEmpty(_folderPath) || !Directory.Exists(_folderPath))
            {
                EditorUtility.DisplayDialog("폴더를 찾을 수 없습니다.", "경로 설정이 올바르지 않습니다.", "OK");
                return;
            }

            int index = 0;
            string enumString = string.Join(", ", _targetData.states.Select(so =>
            {
                so.stateIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));
            
            string nameSpace = FileUtil.GetProjectRelativePath(_folderPath).Substring("Assets/".Length);
            if (nameSpace.StartsWith("Scripts/"))
            {
                nameSpace = nameSpace.Substring("Scripts/".Length);
            }
            
            nameSpace = nameSpace.Replace("/", ".");
            
            string code = string.Format(CodeFormat.EnumFormat, nameSpace, _targetData.enumName, enumString);
            File.WriteAllText($"{_folderPath}/{_targetData.enumName}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void HandleFolderSelectBtn()
        {
            _folderPath = EditorUtility.OpenFolderPanel("폴더를 선택하세요", _folderPath, "");

            if (!string.IsNullOrEmpty(_folderPath))
            {
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_folderPath);
                _targetData.generatePath = _folderPath;
                EditorUtility.SetDirty(_targetData);
                AssetDatabase.SaveAssets();
            }
        }
    }
}