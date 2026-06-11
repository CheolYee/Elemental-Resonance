using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Motion;
using LitMotion;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Motion.Editor
{
    [CustomEditor(typeof(UIMotionPlayer))]
    public sealed class UIMotionPlayerEditor : UnityEditor.Editor
    {
        private SerializedProperty _canvasGroup;
        private SerializedProperty _motionTarget;
        private SerializedProperty _sequences;

        private readonly Dictionary<int, bool> _seqFoldouts = new();
        private int _selectedSeqIdx = -1;
        private int _selectedKfIdx = -1;
        private bool _templatesFoldout = true;

        // ── 스타일/색상 ────────────────────────
        private static readonly Color TrackBg = new(0.5f, 0.5f, 0.5f, 0.12f);
        private static readonly Color TrackFill = new(0.35f, 0.6f, 0.9f, 0.25f);
        private static readonly Color KfNormal = new(0.3f, 0.55f, 0.85f, 1f);
        private static readonly Color KfSelected = new(0.95f, 0.6f, 0.2f, 1f);
        private static readonly Color HeaderBg = new(0.22f, 0.22f, 0.22f, 0.15f);
        private static readonly Color PropBg = new(0.5f, 0.5f, 0.5f, 0.06f);

        private GUIStyle _timeStyle;
        private GUIStyle _propLabelStyle;

        private void OnEnable()
        {
            _canvasGroup = serializedObject.FindProperty("canvasGroup");
            _motionTarget = serializedObject.FindProperty("motionTarget");
            _sequences = serializedObject.FindProperty("sequences");
        }

        private void EnsureStyles()
        {
            _timeStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 9,
            };

            _propLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fixedWidth = 48,
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureStyles();

            // ── Targets ────────────────────────
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_canvasGroup);
            EditorGUILayout.PropertyField(_motionTarget);
            EditorGUILayout.Space(10);

            // ── Sequences ──────────────────────
            EditorGUILayout.LabelField("Sequences", EditorStyles.boldLabel);

            for (int i = 0; i < _sequences.arraySize; i++)
            {
                DrawSequence(i);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 시퀀스 추가", GUILayout.Height(24)))
            {
                AddEmptySequence("New Motion");
            }

            EditorGUILayout.Space(14);

            // ── Templates ──────────────────────
            DrawTemplateSection();

            serializedObject.ApplyModifiedProperties();
        }

        // ═══════════════════════════════════════
        //  시퀀스 렌더링
        // ═══════════════════════════════════════

        private void DrawSequence(int seqIdx)
        {
            SerializedProperty seq = _sequences.GetArrayElementAtIndex(seqIdx);
            SerializedProperty labelProp = seq.FindPropertyRelative("label");
            SerializedProperty kfsProp = seq.FindPropertyRelative("keyframes");

            _seqFoldouts.TryAdd(seqIdx, true);

            // ── 헤더 바 ────────────────────────
            Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width + 20, 22), HeaderBg);

            _seqFoldouts[seqIdx] = EditorGUILayout.Foldout(_seqFoldouts[seqIdx], "", true);
            labelProp.stringValue = EditorGUILayout.TextField(labelProp.stringValue, EditorStyles.boldLabel, GUILayout.MinWidth(60));

            GUILayout.FlexibleSpace();

            float dur = 0f;
            for (int k = 0; k < kfsProp.arraySize; k++)
            {
                float t = kfsProp.GetArrayElementAtIndex(k).FindPropertyRelative("time").floatValue;
                if (t > dur) dur = t;
            }
            GUILayout.Label($"{kfsProp.arraySize} kf · {dur:F2}s", EditorStyles.miniLabel);

            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                _sequences.DeleteArrayElementAtIndex(seqIdx);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                if (_selectedSeqIdx == seqIdx) { _selectedSeqIdx = -1; _selectedKfIdx = -1; }
                return;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            if (!_seqFoldouts[seqIdx])
            {
                EditorGUILayout.Space(2);
                return;
            }

            // ── 타임라인 ───────────────────────
            DrawTimeline(kfsProp, seqIdx, dur);

            // ── 선택된 키프레임 편집 ────────────
            if (_selectedSeqIdx == seqIdx && _selectedKfIdx >= 0 && _selectedKfIdx < kfsProp.arraySize)
            {
                DrawKeyframeEditor(kfsProp.GetArrayElementAtIndex(_selectedKfIdx), _selectedKfIdx);
            }

            // ── 키프레임 추가 버튼 ──────────────
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button("+ 키프레임 추가", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                float newTime = dur + 0.1f;
                int newIdx = kfsProp.arraySize;
                kfsProp.InsertArrayElementAtIndex(newIdx);
                SerializedProperty nkf = kfsProp.GetArrayElementAtIndex(newIdx);
                nkf.FindPropertyRelative("time").floatValue = newTime;
                nkf.FindPropertyRelative("alpha").floatValue = 1f;
                nkf.FindPropertyRelative("scale").floatValue = 1f;
                nkf.FindPropertyRelative("positionX").floatValue = 0f;
                nkf.FindPropertyRelative("positionY").floatValue = 0f;
                nkf.FindPropertyRelative("rotation").floatValue = 0f;
                nkf.FindPropertyRelative("ease").enumValueIndex = (int)Ease.OutCubic;
                _selectedSeqIdx = seqIdx;
                _selectedKfIdx = newIdx;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
        }

        // ═══════════════════════════════════════
        //  비주얼 타임라인
        // ═══════════════════════════════════════

        private void DrawTimeline(SerializedProperty kfsProp, int seqIdx, float totalDuration)
        {
            float displayDuration = Mathf.Max(totalDuration, 0.5f);

            // 트랙 영역 확보
            Rect area = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(48));
            float pad = 30f;
            Rect track = new Rect(area.x + pad, area.y + 8, area.width - pad * 2, 20);

            // 트랙 배경
            EditorGUI.DrawRect(track, TrackBg);

            // 채워진 부분
            EditorGUI.DrawRect(new Rect(track.x, track.y, track.width, track.height), TrackFill);

            // 눈금 (0.1초 단위)
            int tickCount = Mathf.CeilToInt(displayDuration / 0.1f);
            for (int t = 0; t <= tickCount; t++)
            {
                float ratio = (t * 0.1f) / displayDuration;
                if (ratio > 1f) break;
                float x = track.x + ratio * track.width;
                bool isMajor = t % 5 == 0;
                float tickH = isMajor ? 8f : 4f;
                EditorGUI.DrawRect(new Rect(x, track.y + track.height - tickH, 1, tickH),
                    new Color(0.4f, 0.4f, 0.4f, isMajor ? 0.5f : 0.25f));

                if (isMajor)
                {
                    GUI.Label(new Rect(x - 15, track.y + track.height + 1, 30, 14),
                        $"{t * 0.1f:F1}s", _timeStyle);
                }
            }

            // 키프레임 마커
            for (int i = 0; i < kfsProp.arraySize; i++)
            {
                float t = kfsProp.GetArrayElementAtIndex(i).FindPropertyRelative("time").floatValue;
                float ratio = displayDuration > 0 ? Mathf.Clamp01(t / displayDuration) : 0f;
                float cx = track.x + ratio * track.width;
                float cy = track.y + track.height * 0.5f;

                bool isSel = (_selectedSeqIdx == seqIdx && _selectedKfIdx == i);
                Color col = isSel ? KfSelected : KfNormal;

                // 다이아몬드 형태
                float sz = isSel ? 7f : 5.5f;
                Vector3[] diamond = new Vector3[]
                {
                    new(cx, cy - sz, 0),
                    new(cx + sz, cy, 0),
                    new(cx, cy + sz, 0),
                    new(cx - sz, cy, 0),
                };
                Handles.color = col;
                Handles.DrawAAConvexPolygon(diamond);

                // 테두리
                Handles.color = new Color(col.r * 0.7f, col.g * 0.7f, col.b * 0.7f, 1f);
                Handles.DrawAAPolyLine(2f, diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]);

                // 클릭 히트 영역
                Rect hitRect = new Rect(cx - 10, cy - 10, 20, 20);
                if (Event.current.type == EventType.MouseDown && hitRect.Contains(Event.current.mousePosition))
                {
                    _selectedSeqIdx = seqIdx;
                    _selectedKfIdx = i;
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        // ═══════════════════════════════════════
        //  키프레임 상세 편집
        // ═══════════════════════════════════════

        private void DrawKeyframeEditor(SerializedProperty kf, int kfIdx)
        {
            Rect bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(bgRect.x + 16, bgRect.y, bgRect.width - 16, bgRect.height), PropBg);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.LabelField($"Keyframe #{kfIdx}", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.FlexibleSpace();

            GUI.color = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button("키프레임 삭제", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                kf.serializedObject.FindProperty(kf.propertyPath
                    .Substring(0, kf.propertyPath.LastIndexOf('.')));
                // Navigate up to get the array
                string arrPath = kf.propertyPath;
                int bracket = arrPath.LastIndexOf('[');
                if (bracket >= 0)
                {
                    string arrPropPath = arrPath.Substring(0, bracket - ".Array.data".Length);
                    SerializedProperty arrProp = serializedObject.FindProperty(arrPropPath);
                    arrProp.DeleteArrayElementAtIndex(kfIdx);
                    _selectedKfIdx = -1;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 프로퍼티 행들
            DrawPropRow("time", kf, "time", "s");
            DrawPropRow("alpha", kf, "alpha", "");
            DrawPropRow("scale", kf, "scale", "");
            DrawPropRow("pos X", kf, "positionX", "px");
            DrawPropRow("pos Y", kf, "positionY", "px");
            DrawPropRow("rot", kf, "rotation", "°");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("ease", _propLabelStyle);
            EditorGUILayout.PropertyField(kf.FindPropertyRelative("ease"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawPropRow(string label, SerializedProperty kf, string fieldName, string unit)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(label, _propLabelStyle);
            SerializedProperty prop = kf.FindPropertyRelative(fieldName);
            prop.floatValue = EditorGUILayout.FloatField(prop.floatValue, GUILayout.Width(70));
            if (!string.IsNullOrEmpty(unit))
                GUILayout.Label(unit, EditorStyles.miniLabel, GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════
        //  템플릿 섹션
        // ═══════════════════════════════════════

        private void DrawTemplateSection()
        {
            _templatesFoldout = EditorGUILayout.Foldout(_templatesFoldout, "프리셋 템플릿으로 시퀀스 추가", true, EditorStyles.foldoutHeader);
            if (!_templatesFoldout) return;

            EditorGUILayout.HelpBox(
                "버튼을 누르면 선택한 모션의 Show/Hide (또는 Press/Release) 시퀀스가 추가됩니다.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("팝업\n(Fade+Scale)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 0.85f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.25f, 1f, 1f, 0, 0, 0, Ease.OutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.18f, 0f, 0.85f, 0, 0, 0, Ease.InCubic),
                });
            }
            if (GUILayout.Button("슬라이드 업\n(Fade+PosY)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 1f, 0, -60, 0, Ease.Linear),
                    new UIMotionKeyframe(0.30f, 1f, 1f, 0, 0, 0, Ease.OutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.20f, 0f, 1f, 0, -60, 0, Ease.InCubic),
                });
            }
            if (GUILayout.Button("페이드\n(Alpha)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.25f, 1f, 1f, 0, 0, 0, Ease.OutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.18f, 0f, 1f, 0, 0, 0, Ease.InCubic),
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("슬라이드 우측\n(Fade+PosX)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 1f, -80, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.30f, 1f, 1f, 0, 0, 0, Ease.OutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.20f, 0f, 1f, -80, 0, 0, Ease.InCubic),
                });
            }
            if (GUILayout.Button("버튼 프레스\n(Scale)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Press", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.06f, 1f, 0.92f, 0, 0, 0, Ease.OutCubic),
                });
                AddSequenceFromKfs("Release", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 0.92f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.12f, 1f, 1f, 0, 0, 0, Ease.OutBack),
                });
            }
            if (GUILayout.Button("바운스\n(Scale+Back)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 0.6f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.35f, 1f, 1f, 0, 0, 0, Ease.OutBack),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.18f, 0f, 0.6f, 0, 0, 0, Ease.InBack),
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("3단계 모션", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("팝 바운스\n(3kf overshoot)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 0.7f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.18f, 1f, 1.08f, 0, 0, 0, Ease.OutCubic),
                    new UIMotionKeyframe(0.28f, 1f, 1f, 0, 0, 0, Ease.InOutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.06f, 1f, 1.05f, 0, 0, 0, Ease.OutCubic),
                    new UIMotionKeyframe(0.20f, 0f, 0.7f, 0, 0, 0, Ease.InCubic),
                });
            }
            if (GUILayout.Button("슬라이드+바운스\n(3kf PosY)", GUILayout.Height(38)))
            {
                AddSequenceFromKfs("Show", new[]
                {
                    new UIMotionKeyframe(0f, 0f, 1f, 0, -80, 0, Ease.Linear),
                    new UIMotionKeyframe(0.22f, 1f, 1f, 0, 8, 0, Ease.OutCubic),
                    new UIMotionKeyframe(0.32f, 1f, 1f, 0, 0, 0, Ease.InOutCubic),
                });
                AddSequenceFromKfs("Hide", new[]
                {
                    new UIMotionKeyframe(0f, 1f, 1f, 0, 0, 0, Ease.Linear),
                    new UIMotionKeyframe(0.20f, 0f, 1f, 0, -80, 0, Ease.InCubic),
                });
            }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════
        //  헬퍼
        // ═══════════════════════════════════════

        private void AddEmptySequence(string label)
        {
            int idx = _sequences.arraySize;
            _sequences.InsertArrayElementAtIndex(idx);
            SerializedProperty seq = _sequences.GetArrayElementAtIndex(idx);
            seq.FindPropertyRelative("label").stringValue = label;
            seq.FindPropertyRelative("keyframes").ClearArray();
            serializedObject.ApplyModifiedProperties();
        }

        private void AddSequenceFromKfs(string label, UIMotionKeyframe[] keyframes)
        {
            int idx = _sequences.arraySize;
            _sequences.InsertArrayElementAtIndex(idx);
            SerializedProperty seq = _sequences.GetArrayElementAtIndex(idx);
            seq.FindPropertyRelative("label").stringValue = label;

            SerializedProperty kfsProp = seq.FindPropertyRelative("keyframes");
            kfsProp.ClearArray();

            for (int i = 0; i < keyframes.Length; i++)
            {
                UIMotionKeyframe kf = keyframes[i];
                kfsProp.InsertArrayElementAtIndex(i);
                SerializedProperty p = kfsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("time").floatValue = kf.time;
                p.FindPropertyRelative("alpha").floatValue = kf.alpha;
                p.FindPropertyRelative("scale").floatValue = kf.scale;
                p.FindPropertyRelative("positionX").floatValue = kf.positionX;
                p.FindPropertyRelative("positionY").floatValue = kf.positionY;
                p.FindPropertyRelative("rotation").floatValue = kf.rotation;
                p.FindPropertyRelative("ease").enumValueIndex = (int)kf.ease;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}