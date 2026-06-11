using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public static class StoryEditorUtility
    {
        // ── Line ID 생성 ─────────────────────────────

        /// <summary>에피소드의 기존 lineId와 중복되지 않는 다음 ID를 반환합니다.</summary>
        public static string GenerateUniqueLineId(StoryEpisodeSO episode)
        {
            var used = new HashSet<string>();
            for (int i = 0; i < episode.Lines.Count; i++)
                if (episode.Lines[i] != null && !string.IsNullOrWhiteSpace(episode.Lines[i].LineId))
                    used.Add(episode.Lines[i].LineId);

            int n = episode.Lines.Count + 1;
            string id;
            do { id = $"line_{n:D3}"; n++; } while (used.Contains(id));
            return id;
        }

        // ── StoryLineSO 생성 ─────────────────────────

        /// <summary>
        /// 지정 폴더(또는 에피소드와 같은 폴더)에 StoryLineSO asset을 생성하고,
        /// episode.lines에 추가한 뒤 반환합니다.
        /// </summary>
        public static StoryLineSO CreateAndAddLine(StoryEpisodeSO episode, string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                string episodePath = AssetDatabase.GetAssetPath(episode);
                folder = Path.GetDirectoryName(episodePath);
            }
            string lineId = GenerateUniqueLineId(episode);
            string assetPath   = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{lineId}.asset");

            // asset 생성
            var line = ScriptableObject.CreateInstance<StoryLineSO>();
            AssetDatabase.CreateAsset(line, assetPath);

            // lineId 기입
            SetLineId(line, lineId);

            // episode.lines에 추가 (SerializedObject → undo 자동 기록)
            AddToEpisode(episode, line);

            AssetDatabase.SaveAssets();
            return line;
        }

        // ── episode.lines 조작 ───────────────────────

        public static void AddToEpisode(StoryEpisodeSO episode, StoryLineSO line)
        {
            var so   = new SerializedObject(episode);
            var prop = so.FindProperty("lines");
            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = line;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(episode);
        }

        public static void RemoveFromEpisode(StoryEpisodeSO episode, StoryLineSO line)
        {
            var so   = new SerializedObject(episode);
            var prop = so.FindProperty("lines");

            for (int i = prop.arraySize - 1; i >= 0; i--)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == line)
                {
                    // InsertArrayElementAtIndex 방식 null 패턴으로 삭제
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    prop.DeleteArrayElementAtIndex(i);
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(episode);
        }

        // ── 에디터 노드 위치/크기 저장 ───────────────

        /// <param name="recordUndo">true: Undo.RecordObject 사용 (ApplyModifiedPropertiesWithoutUndo). false: ApplyModifiedProperties.</param>
        public static void SetEditorNodePosition(StoryLineSO line, Vector2 pos, bool recordUndo = false)
        {
            if (recordUndo) Undo.RecordObject(line, "Move Node");
            var so = new SerializedObject(line);
            so.FindProperty("editorNodePosition").vector2Value = pos;
            if (recordUndo) so.ApplyModifiedPropertiesWithoutUndo();
            else            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);
        }

        /// <param name="recordUndo">true: Undo.RecordObject 사용 (ApplyModifiedPropertiesWithoutUndo). false: ApplyModifiedProperties.</param>
        public static void SetEditorNodeSize(StoryLineSO line, Vector2 size, bool recordUndo = false)
        {
            if (recordUndo) Undo.RecordObject(line, "Resize Node");
            var so = new SerializedObject(line);
            so.FindProperty("editorNodeSize").vector2Value = size;
            if (recordUndo) so.ApplyModifiedPropertiesWithoutUndo();
            else            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);
        }

        // ── 에피소드 에디터 저장 폴더 ─────────────────

        public static void SetEditorLineSaveFolder(StoryEpisodeSO episode, string folder)
        {
            var so = new SerializedObject(episode);
            so.FindProperty("editorLineSaveFolder").stringValue = folder ?? "";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(episode);
        }

        // ── 모듈 관리 ─────────────────────────────────

        /// <summary>moduleType 인스턴스를 sub-asset으로 생성 후 line.modules에 추가합니다.</summary>
        public static StoryModuleSO AddModule(StoryLineSO line, Type moduleType)
        {
            if (moduleType == typeof(StoryFadeModuleSO) && line != null)
            {
                foreach (StoryModuleSO existing in line.Modules)
                {
                    if (existing is StoryFadeModuleSO fadeModule)
                    {
                        Debug.LogWarning("[StoryEditorUtility] Fade module is limited to one per line. Reusing existing module.", fadeModule);
                        return fadeModule;
                    }
                }
            }

            var module = (StoryModuleSO)ScriptableObject.CreateInstance(moduleType);
            module.name = moduleType.Name;

            string linePath = AssetDatabase.GetAssetPath(line);
            if (!string.IsNullOrEmpty(linePath))
                AssetDatabase.AddObjectToAsset(module, line);

            var so   = new SerializedObject(line);
            var prop = so.FindProperty("modules");
            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = module;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(linePath))
                AssetDatabase.ImportAsset(linePath);

            return module;
        }

        /// <summary>line.modules에서 제거 후, sub-asset이면 asset도 삭제합니다.</summary>
        public static void RemoveModule(StoryLineSO line, StoryModuleSO module)
        {
            var so   = new SerializedObject(line);
            var prop = so.FindProperty("modules");
            for (int i = prop.arraySize - 1; i >= 0; i--)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == module)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    prop.DeleteArrayElementAtIndex(i);
                }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);

            string linePath   = AssetDatabase.GetAssetPath(line);
            string modulePath = AssetDatabase.GetAssetPath(module);

            if (!string.IsNullOrEmpty(linePath) && modulePath == linePath)
            {
                // sub-asset → asset에서 제거 후 즉시 파괴
                AssetDatabase.RemoveObjectFromAsset(module);
                UnityEngine.Object.DestroyImmediate(module, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(linePath);
            }
            else
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>modules 배열에서 fromIndex → toIndex로 이동합니다.</summary>
        public static void MoveModule(StoryLineSO line, int fromIndex, int toIndex)
        {
            var so = new SerializedObject(line);
            so.FindProperty("modules").MoveArrayElement(fromIndex, toIndex);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);
        }

        // ── 대화 필드 수정 ──────────────────────────

        public static void SetSpeaker(StoryLineSO line, CharacterDefinitionSO speaker)
        {
            var so = new SerializedObject(line);
            so.FindProperty("speaker").objectReferenceValue = speaker;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(line);
        }

        public static void SetNameOverride(StoryLineSO line, string nameOverride)
        {
            var so = new SerializedObject(line);
            so.FindProperty("nameOverride").stringValue = nameOverride ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(line);
        }

        public static void SetDialogueText(StoryLineSO line, string text)
        {
            var so = new SerializedObject(line);
            so.FindProperty("dialogueText").stringValue = text ?? "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(line);
        }

        // ── nextLineId 수정 ─────────────────────────

        public static void SetNextLineId(StoryLineSO line, string nextId)
        {
            var so = new SerializedObject(line);
            so.FindProperty("nextLineId").stringValue = nextId ?? "";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);
        }

        // ── lineId 수정 ──────────────────────────────

        public static void SetLineId(StoryLineSO line, string newId)
        {
            var so = new SerializedObject(line);
            so.FindProperty("lineId").stringValue = newId;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(line);
        }

        // ── 라인 복제 ────────────────────────────────

        /// <summary>
        /// sourceLine의 모든 필드와 모듈을 deep copy한 새 StoryLineSO를 생성하고 episode에 추가합니다.
        /// lineId는 새로 생성, nextLineId는 비웁니다. editorNodePosition은 별도로 설정하세요.
        /// </summary>
        public static StoryLineSO DuplicateLine(StoryEpisodeSO episode, StoryLineSO source, string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
                folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(episode));

            string lineId    = GenerateUniqueLineId(episode);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{lineId}.asset");

            var newLine = ScriptableObject.CreateInstance<StoryLineSO>();
            AssetDatabase.CreateAsset(newLine, assetPath);

            // 기본 필드 복사 (lineId·nextLineId·editorNode*·modules 제외)
            var srcSO = new SerializedObject(source);
            var dstSO = new SerializedObject(newLine);
            foreach (string field in new[]
                { "speaker", "nameOverride", "dialogueText", "logVisible",
                  "allowTapToComplete", "focusPolicy", "voice",
                  "useAutoAdvanceOverride", "autoAdvanceDelay" })
            {
                var srcProp = srcSO.FindProperty(field);
                if (srcProp != null) dstSO.CopyFromSerializedProperty(srcProp);
            }
            dstSO.FindProperty("lineId").stringValue    = lineId;
            dstSO.FindProperty("nextLineId").stringValue = "";
            dstSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(newLine);

            // 모듈 deep copy
            foreach (var module in source.Modules)
            {
                if (module == null) continue;

                var newModule = (StoryModuleSO)ScriptableObject.CreateInstance(module.GetType());
                newModule.name = module.name;
                EditorUtility.CopySerialized(module, newModule);
                AssetDatabase.AddObjectToAsset(newModule, newLine);

                var lineSO    = new SerializedObject(newLine);
                var modulesProp = lineSO.FindProperty("modules");
                modulesProp.InsertArrayElementAtIndex(modulesProp.arraySize);
                modulesProp.GetArrayElementAtIndex(modulesProp.arraySize - 1).objectReferenceValue = newModule;
                lineSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(newLine);
            }

            AddToEpisode(episode, newLine);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            return newLine;
        }

        // ── asset 삭제 ───────────────────────────────

        /// <summary>
        /// episode.lines에서 제거 후 asset 파일을 삭제합니다.
        /// 삭제는 되돌릴 수 없으므로 호출 전 확인 다이얼로그를 표시하세요.
        /// </summary>
        public static void DeleteLine(StoryEpisodeSO episode, StoryLineSO line)
        {
            RemoveFromEpisode(episode, line);

            string path = AssetDatabase.GetAssetPath(line);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.SaveAssets();
        }
    }
}
