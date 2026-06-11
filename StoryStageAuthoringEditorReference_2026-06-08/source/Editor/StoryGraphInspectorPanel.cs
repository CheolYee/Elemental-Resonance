using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 선택된 StoryLineSO 필드를 그래프 에디터 우측에서 직접 편집하는 패널.
    /// PropertyField + SerializedObject 바인딩으로 Undo 자동 지원.
    /// nextLineId는 episode 기반 드롭다운으로 선택 가능.
    /// </summary>
    public sealed class StoryGraphInspectorPanel : VisualElement
    {
        public event Action LineChanged;

        private StoryLineSO      _line;
        private StoryEpisodeSO   _episode;
        private SerializedObject _so;

        private readonly VisualElement _content;

        // Line ID Helper 상태 — Rebuild()를 거쳐도 suffix/foldout 유지
        private readonly StoryLineIdHelperGUI.State _idState = new();

        // ── 생성자 ───────────────────────────────────

        public StoryGraphInspectorPanel()
        {
            style.width           = 300;
            style.minWidth        = 220;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            style.backgroundColor = new StyleColor(new Color(0.19f, 0.19f, 0.19f));
            style.flexShrink      = 0;

            var header = new Label("Line 편집")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    paddingLeft = 10,
                    paddingTop = 8,
                    paddingBottom = 6,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f))
                }
            };
            Add(header);

            var scroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1
                }
            };
            scroll.contentContainer.style.paddingLeft  = 10;
            scroll.contentContainer.style.paddingRight = 10;
            scroll.contentContainer.style.paddingTop   = 8;
            _content = scroll.contentContainer;
            Add(scroll);
        }

        // ── 공개 API ─────────────────────────────────

        public void SetLine(StoryLineSO line, StoryEpisodeSO episode = null)
        {
            _line    = line;
            _episode = episode ?? _episode;   // episode가 null이면 기존 유지
            _so      = line != null ? new SerializedObject(line) : null;
            Rebuild();
        }

        /// <summary>다중 선택 상태일 때 편집 불가 메시지를 표시한다.</summary>
        public void SetMultipleSelection(int count)
        {
            _line = null;
            _so   = null;
            _content.Unbind();
            _content.Clear();

            var msg = new Label($"여러 개의 라인을 선택 중입니다. ({count}개)")
            {
                style =
                {
                    color = new StyleColor(new Color(0.6f, 0.6f, 0.6f)),
                    whiteSpace = WhiteSpace.Normal,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = 20,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };
            _content.Add(msg);

            var hint = new Label("단일 노드를 선택하면 편집할 수 있습니다.")
            {
                style =
                {
                    color = new StyleColor(new Color(0.4f, 0.4f, 0.4f)),
                    fontSize = 10,
                    whiteSpace = WhiteSpace.Normal,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginTop = 6,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };
            _content.Add(hint);
        }

        public void SetEpisode(StoryEpisodeSO episode)
        {
            _episode = episode;
            Rebuild();
        }

        /// <summary>Undo/Redo 후 패널을 최신 SO 값으로 갱신한다.</summary>
        public void Reload()
        {
            if (_so == null) return;
            _so.Update();
            Rebuild();   // 드롭다운 포함 전체 재구성
        }

        // ── 내부 구성 ─────────────────────────────────

        private void Rebuild()
        {
            _content.Unbind();
            _content.Clear();

            if (_line == null || _so == null)
            {
                var hint = new Label("노드를 선택하세요")
                {
                    style =
                    {
                        color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                        unityTextAlign = TextAnchor.MiddleCenter,
                        marginTop = 20
                    }
                };
                _content.Add(hint);
                return;
            }

            // 기본 섹션
            AddSectionLabel("기본");
            BuildLineIdSection();
            AddField("speaker",      "Speaker");
            AddField("nameOverride", "Name Override");
            AddField("dialogueText", "Dialogue Text");

            // 흐름 섹션
            AddSectionLabel("흐름");
            AddField("logVisible",         "Log Visible");
            AddField("allowTapToComplete", "Allow Tap");

            // 다음 라인 섹션 (드롭다운 헬퍼)
            AddSectionLabel("다음 라인");
            BuildNextLineSection();

            _content.Bind(_so);
        }

        // ── Line ID 헬퍼 (IMGUIContainer로 공용 유틸 호출) ──────

        private void BuildLineIdSection()
        {
            // StoryLineSOEditor 와 완전히 동일한 UI/동작을 IMGUIContainer 로 임베드
            var container = new IMGUIContainer(() =>
            {
                if (_line == null || _so == null) return;
                _so.Update();
                StoryLineIdHelperGUI.Draw(_line, _episode, _so, _idState, () =>
                {
                    LineChanged?.Invoke();
                    // 다음 프레임에 패널 갱신 (IMGUI 렌더 중 UIToolkit 재빌드 방지)
                    EditorApplication.delayCall += Reload;
                });
            });
            container.style.paddingBottom = 4;
            _content.Add(container);
        }

        // ── Next Line 헬퍼 ─────────────────────────────

        private void BuildNextLineSection()
        {
            // nextLineId PropertyField (직접 입력도 허용)
            AddField("nextLineId", "Next Line ID");

            if (_episode == null)
            {
                var noEp = new Label("에피소드 없음 — 드롭다운 사용 불가")
                {
                    style =
                    {
                        fontSize = 10,
                        color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                        marginTop = 4
                    }
                };
                _content.Add(noEp);
                return;
            }

            // 경고
            BuildNextLineWarning();

            // 드롭다운
            var (labels, ids, selectedIdx) = BuildDropdownChoices();

            var dropdown = new DropdownField("드롭다운 선택", labels, selectedIdx)
            {
                style =
                {
                    marginTop = 4
                }
            };
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = labels.IndexOf(evt.newValue);
                if (idx < 0) return;
                string newId = ids[idx];

                Undo.RecordObject(_line, "Set nextLineId");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = newId;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);

                LineChanged?.Invoke();
            });
            _content.Add(dropdown);

            // 버튼 행: 순서 다음 + Clear
            var btnRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 4
                }
            };

            // 순서 다음 버튼
            int myIdx       = FindIndexInEpisode(_line, _episode);
            StoryLineSO nextInList = null;
            if (myIdx >= 0)
            {
                for (int i = myIdx + 1; i < _episode.Lines.Count; i++)
                    if (_episode.Lines[i] != null) { nextInList = _episode.Lines[i]; break; }
            }

            var nextBtn = new Button(() =>
            {
                if (nextInList == null) return;
                Undo.RecordObject(_line, "Set nextLineId (순서 다음)");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = nextInList.LineId;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);
                LineChanged?.Invoke();
                Reload();
            })
            {
                text = nextInList != null ? $"순서 다음 → {Trunc(nextInList.LineId, 14)}" : "순서 다음 없음"
            };
            nextBtn.SetEnabled(nextInList != null);
            nextBtn.style.flexGrow    = 1;
            nextBtn.style.height      = 20;
            nextBtn.style.fontSize    = 10;
            nextBtn.style.paddingLeft = 4;
            nextBtn.style.paddingRight = 4;
            btnRow.Add(nextBtn);

            var clearBtn = new Button(() =>
            {
                Undo.RecordObject(_line, "Clear nextLineId");
                var so = new SerializedObject(_line);
                so.FindProperty("nextLineId").stringValue = "";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(_line);
                LineChanged?.Invoke();
                Reload();
            })
            {
                text = "Clear",
                style =
                {
                    width = 48,
                    height = 20,
                    fontSize = 10,
                    marginLeft = 4
                }
            };
            btnRow.Add(clearBtn);

            _content.Add(btnRow);
        }

        private void BuildNextLineWarning()
        {
            string nextId = _so.FindProperty("nextLineId").stringValue;

            if (string.IsNullOrWhiteSpace(nextId)) return;

            // 자기 참조
            if (nextId == _so.FindProperty("lineId").stringValue)
            {
                AddWarningLabel("⚠ 자기 자신 참조 (무한 루프)");
                return;
            }

            // 에피소드에 없음
            bool found = false;
            foreach (StoryLineSO epLine in _episode.Lines)
                if (epLine != null && epLine.LineId == nextId)
                { found = true; break; }

            if (!found)
                AddWarningLabel($"⚠ \"{Trunc(nextId, 20)}\" 에피소드에 없음");
        }

        private (List<string> labels, List<string> ids, int selectedIdx) BuildDropdownChoices()
        {
            var labels = new List<string> { "— 없음 (종료) —" };
            var ids    = new List<string> { "" };
            int selected   = 0;

            string currentNextId = _so.FindProperty("nextLineId").stringValue;

            for (int i = 0; i < _episode.Lines.Count; i++)
            {
                var l = _episode.Lines[i];
                if (l == null || l == _line) continue;

                string label = string.IsNullOrWhiteSpace(l.LineId) ? $"(#{i} ID없음)" : l.LineId;
                if (l.Speaker != null) label += $"  [{l.Speaker.DisplayName}]";
                if (!string.IsNullOrEmpty(l.DialogueText))
                    label += $"  \"{Trunc(l.DialogueText, 16)}\"";

                labels.Add(label);
                ids.Add(l.LineId);

                if (l.LineId == currentNextId) selected = labels.Count - 1;
            }

            return (labels, ids, selected);
        }

        // ── 공통 헬퍼 ─────────────────────────────────

        private void AddSectionLabel(string text)
        {
            var lbl = new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 10,
                    color = new StyleColor(new Color(0.55f, 0.55f, 0.55f)),
                    marginTop = 10,
                    marginBottom = 2,
                    paddingBottom = 2,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f))
                }
            };
            _content.Add(lbl);
        }

        private void AddField(string propName, string labelText)
        {
            var prop = _so.FindProperty(propName);
            if (prop == null) return;
            var field = new PropertyField(prop, labelText);
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ => LineChanged?.Invoke());
            _content.Add(field);
        }

        private void AddWarningLabel(string text)
        {
            var lbl = new Label(text)
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(1f, 0.6f, 0.2f)),
                    marginTop = 2
                }
            };
            _content.Add(lbl);
        }

        private static int FindIndexInEpisode(StoryLineSO line, StoryEpisodeSO ep)
        {
            for (int i = 0; i < ep.Lines.Count; i++)
                if (ep.Lines[i] == line) return i;
            return -1;
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }

        // ── 텍스트 필드 포커스 감지 ──────────────────

        public static bool IsFocusedOnTextField(VisualElement root)
        {
            var el = root?.focusController?.focusedElement as VisualElement;
            while (el != null)
            {
                if (el is TextField or IntegerField or FloatField or LongField) return true;
                el = el.parent;
            }
            return false;
        }
    }
}
