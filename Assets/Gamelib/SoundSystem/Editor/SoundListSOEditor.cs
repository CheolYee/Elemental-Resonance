using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gamelib.SoundSystem.Editor
{
    [CustomEditor(typeof(SoundListSo))]
    public class SoundListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            editorView.CloneTree(root);

            root.Q<Button>("GenerateButton").clicked += HandleGenerateEnumClick;
            
            return root;
        }
        
        private string ToEnumName(string enumName)
        {
            return enumName.Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
        }

        private void HandleGenerateEnumClick()
        {
            SoundListSo listData = target as SoundListSo;
            
            Debug.Assert(listData != null, "Target data is null check editor");
            
            int index = 0;
            string enumString = string.Join($",\n\t\t", listData.sounds.Select(so =>
            {
                so.soundIndex = index;
                EditorUtility.SetDirty(so);
                return $"{ToEnumName(so.soundName)} = {index++}";
            }));
            
            string code = string.Format(SoundCodeFormat.EnumFormat, "Gamelib.SoundSystem", listData.enumName, enumString);

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directoryName = Path.GetDirectoryName(scriptPath); //이 코드가 있는 디렉토리를 가져와.
            Debug.Assert(directoryName != null, "Parent directory is null");
            
            DirectoryInfo parentDirectory = Directory.GetParent(directoryName); //이 코드의 디렉토리의 부모 디렉토리 가져와
            Debug.Assert(parentDirectory != null, "Parent directory is null");
            
            string path = parentDirectory.FullName;
            File.WriteAllText($"{path}/{listData.enumName}.cs", code);
            
            AssetDatabase.SaveAssets(); //Ctrl+s키랑 같은 기능
            AssetDatabase.Refresh(); //갱신 -> 이걸 해야 새롭게 컴파일을 한다.
        }
    }
}