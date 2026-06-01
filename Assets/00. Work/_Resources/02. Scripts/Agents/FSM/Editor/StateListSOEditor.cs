using System.Linq;
using System.Text;
using _00._Work._Resources._02._Scripts.Systems.EnumSystem.Editor;
using Agents.FSM;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work._Resources._02._Scripts.Agents.FSM.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSOEditor : UnityEditor.Editor
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
            //기본에디터를 채워주는 내용.
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            editorView.CloneTree(root);
            
            _folderBtn = root.Q<Button>("FolderBtn");
            _generateBtn = root.Q<Button>("GenerateBtn");
            _folderPathLabel = root.Q<Label>("SelectedFolderLabel");
            _folderPathLabel.text = "No folder selected";

            _targetData = target as StateListSO;
            _folderBtn.clicked += HandleFolderSelectBtn;
            _generateBtn.clicked += HandleGenenrateBtn;

            if (_targetData != null && !string.IsNullOrEmpty(_targetData.generatePath))
            {
                _folderPath = _targetData.generatePath;
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_targetData.generatePath);
            }

            return root;
        }

        private void HandleGenenrateBtn()
        {
            int index = 0;
            string[] stateNames = _targetData.states.Select(so =>
            {
                so.assetIndex = index++;
                EditorUtility.SetDirty(so);
                return so.stateName;
            }).ToArray();

            string relativePath = FileUtil.GetProjectRelativePath(_folderPath).Substring("Assets/".Length);
            if (relativePath.StartsWith("Scripts/"))
                relativePath = relativePath.Substring("Scripts/".Length);

            string nameSpace = string.Join(".",
                relativePath.Split('/')
                    .Select(SanitizeNamespaceSegment)
                    .Where(s => !string.IsNullOrEmpty(s)));

            EnumCodeGenerator.WriteEnumFile(_folderPath, nameSpace, _targetData.enumName, stateNames);
        }

        private static string SanitizeNamespaceSegment(string segment)
        {
            var sb = new StringBuilder();
            foreach (char c in segment)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }
            if (sb.Length == 0) return string.Empty;
            if (char.IsDigit(sb[0]))
                sb.Insert(0, '_');
            return sb.ToString();
        }

        private void HandleFolderSelectBtn()
        {
            _folderPath = EditorUtility.OpenFolderPanel("폴더를 선택", _folderPath, "");

            if (!string.IsNullOrEmpty(_folderPath))
            {
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_folderPath);
                _targetData.generatePath = _folderPath;
                EditorUtility.SetDirty(_targetData);
                AssetDatabase.SaveAssets(); //더러워진 거 전부 저장.
            }
        }
    }
}