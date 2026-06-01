using _00._Work._Resources._02._Scripts.Systems.EnumSystem;
using UnityEditor;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.EnumSystem.Editor
{
    [CustomEditor(typeof(EnumListSO))]
    public class EnumListSOEditor : UnityEditor.Editor
    {
        private EnumListSO _target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            _target = (EnumListSO)target;

            EditorGUILayout.Space();

            string displayPath = string.IsNullOrEmpty(_target.generatePath)
                ? "선택된 폴더 없음"
                : FileUtil.GetProjectRelativePath(_target.generatePath);
            EditorGUILayout.LabelField("생성 경로", displayPath);

            if (GUILayout.Button("폴더 선택"))
                HandleFolderSelectBtn();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_target.generatePath));
            if (GUILayout.Button("C# 파일 생성"))
                HandleGenerateBtn();
            EditorGUI.EndDisabledGroup();
        }

        private void HandleFolderSelectBtn()
        {
            string selected = EditorUtility.OpenFolderPanel("폴더를 선택", _target.generatePath ?? "", "");
            if (string.IsNullOrEmpty(selected)) return;

            _target.generatePath = selected;
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }

        private void HandleGenerateBtn()
        {
            if (string.IsNullOrEmpty(_target.enumName) || string.IsNullOrEmpty(_target.namespaceName))
            {
                EditorUtility.DisplayDialog("입력 오류", "enumName과 namespaceName을 입력해주세요.", "OK");
                return;
            }

            EnumCodeGenerator.WriteEnumFile(
                _target.generatePath,
                _target.namespaceName,
                _target.enumName,
                _target.valueNames);
        }
    }
}
