using System.Collections.Generic;
using System.IO;
using System.Linq;
using _00._Work._Resources._02._Scripts.Systems;
using UnityEditor;

namespace _00._Work._Resources._02._Scripts.Systems.EnumSystem.Editor
{
    public static class EnumCodeGenerator
    {
        public static void WriteEnumFile(string folderPath, string namespaceName, string enumName, IEnumerable<string> valueNames)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                EditorUtility.DisplayDialog("경로 오류", "올바른 폴더 경로를 먼저 선택해주세요.", "OK");
                return;
            }

            string enumBody = string.Join(", ", valueNames.Select((name, i) => $"{name} = {i}"));
            string code = string.Format(CodeFormat.EnumFormat, namespaceName, enumName, enumBody);
            File.WriteAllText($"{folderPath}/{enumName}.cs", code);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
