using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// Line ID Helper IMGUI 공용 드로어.
    /// StoryLineSOEditor 와 StoryGraphInspectorPanel(IMGUIContainer) 양쪽에서 동일하게 사용.
    /// </summary>
    public static class StoryLineIdHelperGUI
    {
        // ── 인스턴스별 UI 상태 ────────────────────────
        public sealed class State
        {
            public string suffix  = "";
            public bool   foldout = true;
        }

        // ── 메인 드로우 ───────────────────────────────

        /// <param name="line">편집 대상 StoryLineSO</param>
        /// <param name="episode">소속 에피소드 (null 가능)</param>
        /// <param name="so">line 의 SerializedObject (Update()는 호출자 책임)</param>
        /// <param name="state">UI 상태 (suffix, foldout). 인스턴스 필드로 유지.</param>
        /// <param name="onChanged">lineId 변경 완료 후 호출될 콜백 (null 가능)</param>
        public static void Draw(
            StoryLineSO      line,
            StoryEpisodeSO   episode,
            SerializedObject so,
            State            state,
            System.Action    onChanged = null)
        {
            if (line == null || so == null) return;

            var idProp = so.FindProperty("lineId");
            if (idProp == null) return;

            state.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(state.foldout, "── Line ID Helper ──");
            if (!state.foldout) { EditorGUILayout.EndFoldoutHeaderGroup(); return; }

            // ── 경고 ──────────────────────────────────
            DrawWarnings(line, episode, idProp);
            EditorGUILayout.Space(2);

            // ── 소속 에피소드 정보 ─────────────────────
            if (episode != null)
            {
                int myIdx = IndexInEpisode(line, episode);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("소속 에피소드", episode, typeof(StoryEpisodeSO), false);
                EditorGUILayout.LabelField("에피소드 내 순서",
                    myIdx >= 0 ? $"#{myIdx + 1}" : "미포함", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "소속 에피소드를 찾을 수 없습니다. 에피소드 Lines 목록에 추가하면 번호 기반 생성이 가능합니다.",
                    MessageType.None);
            }

            EditorGUILayout.Space(4);

            // ── Suffix 입력 ────────────────────────────
            state.suffix = EditorGUILayout.TextField("Suffix (선택)", state.suffix);
            EditorGUILayout.LabelField("", $"예: {PreviewId(line, episode, state.suffix)}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            // ── 버튼 행 ────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            bool isEmpty = string.IsNullOrWhiteSpace(idProp.stringValue);
            var  prevBg  = GUI.backgroundColor;
            GUI.backgroundColor = isEmpty ? new Color(0.5f, 0.9f, 0.5f) : new Color(1f, 0.75f, 0.4f);

            if (GUILayout.Button(isEmpty ? "ID 생성" : "ID 재생성"))
                TryGenerate(line, episode, so, idProp, isEmpty, state, onChanged);

            GUI.backgroundColor = prevBg;

            using (new EditorGUI.DisabledScope(isEmpty))
            {
                if (GUILayout.Button("ID 복사", GUILayout.Width(70)))
                    GUIUtility.systemCopyBuffer = idProp.stringValue;
            }

            EditorGUILayout.EndHorizontal();

            // 재생성 경고
            if (!isEmpty)
            {
                EditorGUILayout.HelpBox(
                    $"현재 lineId \"{idProp.stringValue}\" 를 참조하는 다른 라인이 있을 수 있습니다.\n재생성하면 해당 참조가 끊어집니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ── 내부: 생성 실행 ──────────────────────────

        private static void TryGenerate(
            StoryLineSO line, StoryEpisodeSO episode,
            SerializedObject so, SerializedProperty idProp,
            bool isEmpty, State state, System.Action onChanged)
        {
            string gen = BuildId(line, episode, state.suffix);
            if (gen == null)
            {
                EditorUtility.DisplayDialog("ID 생성 실패",
                    "소속 에피소드를 찾을 수 없어 번호를 결정할 수 없습니다.\n에피소드 Lines 목록에 이 라인을 추가하세요.",
                    "확인");
                return;
            }

            if (!isEmpty && !EditorUtility.DisplayDialog(
                    "ID 재생성 확인",
                    $"현재 ID \"{idProp.stringValue}\" 를 \"{gen}\" 로 변경합니다.\n" +
                    "기존 ID를 참조하는 nextLineId 들은 수동으로 수정해야 합니다.\n계속하시겠습니까?",
                    "변경", "취소"))
                return;

            Undo.RecordObject(line, "Generate lineId");
            idProp.stringValue = gen;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(line);
            onChanged?.Invoke();
        }

        // ── 내부: ID 계산 ────────────────────────────

        private static string BuildId(StoryLineSO line, StoryEpisodeSO episode, string suffix)
        {
            if (episode == null) return null;
            int idx = IndexInEpisode(line, episode);
            if (idx < 0) return null;
            string baseId = $"line_{idx + 1:D3}";
            suffix = suffix?.Trim();
            return string.IsNullOrEmpty(suffix) ? baseId : $"{baseId}_{suffix}";
        }

        private static string PreviewId(StoryLineSO line, StoryEpisodeSO episode, string suffix)
        {
            if (episode == null) return "에피소드 필요";
            int idx = IndexInEpisode(line, episode);
            if (idx < 0) return "에피소드 Lines에 추가 필요";
            string baseId = $"line_{idx + 1:D3}";
            suffix = suffix?.Trim();
            return string.IsNullOrEmpty(suffix) ? baseId : $"{baseId}_{suffix}";
        }

        // ── 내부: 경고 ──────────────────────────────

        private static void DrawWarnings(StoryLineSO line, StoryEpisodeSO episode, SerializedProperty idProp)
        {
            if (string.IsNullOrWhiteSpace(idProp.stringValue))
            {
                EditorGUILayout.HelpBox("lineId가 비어 있습니다. 아래 버튼으로 생성하세요.", MessageType.Warning);
                return;
            }

            if (episode != null)
            {
                int dup = 0;
                foreach (var l in episode.Lines)
                    if (l != null && l.LineId == line.LineId) dup++;
                if (dup > 1)
                    EditorGUILayout.HelpBox(
                        $"lineId \"{line.LineId}\" 가 에피소드에 {dup}개 존재합니다 (중복).", MessageType.Error);
            }

            if (!string.IsNullOrWhiteSpace(line.NextLineId) && line.NextLineId == line.LineId)
                EditorGUILayout.HelpBox("nextLineId가 자기 자신을 가리킵니다 (무한 루프).", MessageType.Error);
        }

        // ── 공개 유틸 ────────────────────────────────

        public static int IndexInEpisode(StoryLineSO line, StoryEpisodeSO episode)
        {
            if (episode == null || line == null) return -1;
            for (int i = 0; i < episode.Lines.Count; i++)
                if (episode.Lines[i] == line) return i;
            return -1;
        }
    }
}
