using System.Collections.Generic;
using UnityEditor;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── 재생 버튼 콜백 ──────────────────────────────────────────────────────

        private void OnPlay()
        {
            EnsureEpisodeForLine(_pendingFromLine ?? _currentLine);
            if (episode == null) return;
            if (!episode.TryGetLine(episode.EntryLineId, out var entry)) return;

            _isPlaying    = true;
            _isLineSample = false;
            ShowLineSnapshot(entry);
            RefreshButtons();
        }

        private void OnFromHere()
        {
            if (_pendingFromLine == null) return;
            EnsureEpisodeForLine(_pendingFromLine);

            _isPlaying    = true;
            _isLineSample = false;
            ShowLineSnapshot(_pendingFromLine);
            RefreshButtons();
        }

        private void OnSampleLine()
        {
            if (_pendingFromLine == null) return;
            EnsureEpisodeForLine(_pendingFromLine);

            _isLineSample = true;
            ShowLineSnapshot(_pendingFromLine);
            RefreshButtons();
        }

        private void OnStop() => StopPlayback();

        private void OnNext()
        {
            EnsureEpisodeForLine(_currentLine);
            if (_currentLine == null || string.IsNullOrEmpty(_currentLine.NextLineId)) return;
            if (episode == null || !episode.TryGetLine(_currentLine.NextLineId, out var next)) return;

            ShowLineSnapshot(next);
            RefreshButtons();
        }

        private void OnPreviousLine()
        {
            if (_currentLine == null) return;
            EnsureEpisodeForLine(_currentLine);
            if (!TryGetPreviousLine(_currentLine, out var previous)) return;

            _pendingFromLine = previous;
            _isLineSample    = true;
            ShowLineSnapshot(previous);
            RefreshButtons();
        }

        private void OnNextLine()
        {
            if (_currentLine == null) return;
            EnsureEpisodeForLine(_currentLine);
            if (!TryGetNextLine(_currentLine, out var next)) return;

            _pendingFromLine = next;
            _isLineSample    = true;
            ShowLineSnapshot(next);
            RefreshButtons();
        }

        // ── 재생 상태 정지 ─────────────────────────────────────────────────────

        private void StopPlayback()
        {
            StopTransitionPreview(applyTargetState: false);
            _isPlaying    = false;
            _isLineSample = false;
            _currentLine  = null;
            _stageState.Clear();
            _bgState = null;
            RebuildActorLayer();
            RefreshDialogue();
            RefreshChoices();
            RefreshButtons();
        }

        // ── 라인 스냅샷 표시 ──────────────────────────────────────────────────

        /// <summary>
        /// 재생 없이 지정 라인의 스테이지 상태를 즉시 또는 전환 애니메이션으로 표시한다.
        /// RuntimePreview 모드에서 재생 중이면 자동으로 전환 애니메이션을 시작한다.
        /// </summary>
        private void ShowLineSnapshot(StoryLineSO line)
        {
            StopTransitionPreview(applyTargetState: false);
            EnsureEpisodeForLine(line);
            _currentLine = line;
            ClearPreviewCameraSampleState();

            // RuntimePreview 재생 중 자동 전환: 기존 스테이지 상태가 있을 때만 적용
            bool autoTransition = IsRuntimePreviewMode
                && _isPlaying
                && (_stageState.Count > 0 || _bgState != null);

            if (autoTransition)
            {
                StartLineTransitionPreview(line);
            }
            else
            {
                BuildStageStateAt(line);
            }

            ValidateStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshDialogue();
            RefreshChoices();
        }

        // ── 스테이지 상태 누적 ────────────────────────────────────────────────

        /// <summary>
        /// episode 진입 라인에서 targetLine 까지 nextLineId 체인을 따라
        /// StoryStageLayoutModuleSO 를 적용해 _stageState / _bgState 를 구축한다.
        /// targetLine 이 체인에 없으면 에피소드 라인 순서로 fallback.
        /// </summary>
        private void BuildStageStateAt(StoryLineSO targetLine)
        {
            _stageState.Clear();
            _bgState = null;

            EnsureEpisodeForLine(targetLine);
            if (targetLine == null) return;
            if (episode == null)
            {
                ApplyStageModulesToState(targetLine, _stageState, ref _bgState);
                return;
            }

            string currentId = episode.EntryLineId;
            const int maxSteps = 500;
            int steps = 0;

            bool found = false;
            while (!string.IsNullOrEmpty(currentId) && steps < maxSteps)
            {
                if (!episode.TryGetLine(currentId, out var line)) break;

                ApplyStageModulesToState(line, _stageState, ref _bgState);

                if (line == targetLine) { found = true; break; }

                currentId = line.NextLineId;
                steps++;
            }

            if (!found)
            {
                _stageState.Clear();
                _bgState = null;
                if (!TryBuildStageStateByEpisodeOrder(targetLine, includeTargetLine: true, _stageState, ref _bgState))
                    ApplyStageModulesToState(targetLine, _stageState, ref _bgState);
            }
        }

        private bool TryBuildStageStateBeforeLine(
            StoryLineSO targetLine,
            out Dictionary<string, StoryActorStateData> actors,
            out StoryBackgroundStateData background)
        {
            actors     = new Dictionary<string, StoryActorStateData>();
            background = null;

            EnsureEpisodeForLine(targetLine);
            if (targetLine == null || episode == null)
                return false;

            string currentId = episode.EntryLineId;
            const int maxSteps = 500;
            int steps = 0;

            while (!string.IsNullOrEmpty(currentId) && steps < maxSteps)
            {
                if (!episode.TryGetLine(currentId, out var line)) break;
                if (line == targetLine) return true;

                ApplyStageModulesToState(line, actors, ref background);
                currentId = line.NextLineId;
                steps++;
            }

            return TryBuildStageStateByEpisodeOrder(targetLine, includeTargetLine: false, actors, ref background);
        }

        private bool TryBuildStageStateByEpisodeOrder(
            StoryLineSO targetLine,
            bool includeTargetLine,
            Dictionary<string, StoryActorStateData> actors,
            ref StoryBackgroundStateData background)
        {
            if (episode == null || targetLine == null || actors == null)
                return false;

            int targetIndex = FindLineIndex(targetLine);
            if (targetIndex < 0) return false;

            int lastIndex = includeTargetLine ? targetIndex : targetIndex - 1;
            for (int i = 0; i <= lastIndex; i++)
                ApplyStageModulesToState(episode.Lines[i], actors, ref background);

            return true;
        }

        private static void ApplyStageModulesToState(
            StoryLineSO line,
            Dictionary<string, StoryActorStateData> actors,
            ref StoryBackgroundStateData background)
        {
            if (line == null) return;

            foreach (var module in line.Modules)
            {
                if (module is not StoryStageLayoutModuleSO layout) continue;

                if (layout.HasBackground)
                    background = layout.Background.ShallowClone();

                foreach (var actorData in layout.Actors)
                {
                    if (actorData == null) continue;

                    StoryActorStateData clone = actorData.ShallowClone();
                    string actorKey = clone.ResolvedActorKey;
                    if (string.IsNullOrWhiteSpace(actorKey)) continue;

                    actors[actorKey] = clone;
                }
            }
        }

        private void EnsureEpisodeForLine(StoryLineSO line)
        {
            if (line == null) return;
            if (episode != null && ContainsLine(episode, line)) return;
            if (!TryResolveEpisodeForLine(line, out var resolved)) return;

            episode = resolved;
            _episodeField?.SetValueWithoutNotify(resolved);
        }

        private static bool TryResolveEpisodeForLine(StoryLineSO line, out StoryEpisodeSO resolved)
        {
            resolved = null;
            if (line == null) return false;

            string linePath = AssetDatabase.GetAssetPath(line);
            string[] guids  = AssetDatabase.FindAssets("t:StoryEpisodeSO", new[] { "Assets/00. Work/CheolYee" });
            foreach (string guid in guids)
            {
                string path      = AssetDatabase.GUIDToAssetPath(guid);
                var candidate    = AssetDatabase.LoadAssetAtPath<StoryEpisodeSO>(path);
                if (candidate == null) continue;

                foreach (StoryLineSO candidateLine in candidate.Lines)
                {
                    if (candidateLine == line
                        || (!string.IsNullOrEmpty(linePath)
                            && AssetDatabase.GetAssetPath(candidateLine) == linePath))
                    {
                        resolved = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsLine(StoryEpisodeSO targetEpisode, StoryLineSO line)
        {
            if (targetEpisode == null || line == null) return false;
            foreach (StoryLineSO candidate in targetEpisode.Lines)
                if (candidate == line) return true;
            return false;
        }

        private int FindLineIndex(StoryLineSO line)
        {
            if (episode == null || line == null) return -1;
            for (int i = 0; i < episode.Lines.Count; i++)
                if (episode.Lines[i] == line) return i;
            return -1;
        }

        private bool TryGetPreviousLine(StoryLineSO line, out StoryLineSO previous)
        {
            previous = null;
            if (line == null || episode == null) return false;

            foreach (StoryLineSO candidate in episode.Lines)
            {
                if (candidate != null && candidate.NextLineId == line.LineId)
                {
                    previous = candidate;
                    return true;
                }
            }

            int index = FindLineIndex(line);
            if (index > 0) previous = episode.Lines[index - 1];
            return previous != null;
        }

        private bool TryGetNextLine(StoryLineSO line, out StoryLineSO next)
        {
            next = null;
            if (line == null || episode == null) return false;

            if (!string.IsNullOrEmpty(line.NextLineId) && episode.TryGetLine(line.NextLineId, out next))
                return true;

            int index = FindLineIndex(line);
            if (index >= 0 && index + 1 < episode.Lines.Count)
                next = episode.Lines[index + 1];

            return next != null;
        }

        private static Dictionary<string, StoryActorStateData> CloneActorStateMap(
            Dictionary<string, StoryActorStateData> source)
        {
            var result = new Dictionary<string, StoryActorStateData>();
            foreach (var pair in source)
            {
                if (pair.Value == null) continue;
                StoryActorStateData clone = pair.Value.ShallowClone();
                clone.EnsureActorInstanceKey(pair.Key);
                result[pair.Key] = clone;
            }
            return result;
        }

        private static StoryBackgroundStateData CloneBackgroundState(StoryBackgroundStateData source) =>
            source != null ? source.ShallowClone() : null;

        // ── Line transition preview ────────────────────────────────────────────

        private void StartLineTransitionPreview(StoryLineSO line)
        {
            if (line == null) return;

            StopTransitionPreview(applyTargetState: false);

            TryBuildStageStateBeforeLine(line, out var fromActors, out var fromBackground);

            BuildStageStateAt(line);
            var toActors     = CloneActorStateMap(_stageState);
            var toBackground = CloneBackgroundState(_bgState);

            _transitionFromActors.Clear();
            _transitionToActors.Clear();
            _transitionActorTracks.Clear();
            _transitionCameraFocusTarget = "";
            foreach (var pair in fromActors) _transitionFromActors[pair.Key] = pair.Value.ShallowClone();
            foreach (var pair in toActors)   _transitionToActors[pair.Key]   = pair.Value.ShallowClone();

            if (FindCurrentStageLayout() is { } currentLayout)
            {
                foreach (StoryActorTrackData track in currentLayout.ActorTracks)
                {
                    if (track == null || string.IsNullOrWhiteSpace(track.actorInstanceKey))
                        continue;

                    _transitionActorTracks[track.actorInstanceKey] = track;
                }

                _transitionBackgroundTrack = currentLayout.BackgroundTrack;
                _transitionCameraTrack = currentLayout.CameraTrackEditable;
            }

            _transitionFromBackground   = CloneBackgroundState(fromBackground);
            _transitionToBackground     = CloneBackgroundState(toBackground);
            _transitionPreviewDuration  = Mathf.Max(0.05f, CalculateTransitionDuration());
            _transitionPreviewStartedAt = EditorApplication.timeSinceStartup;
            _transitionPreviewElapsed = 0f;
            _transitionActorsInitialized = false;
            _isTransitionPreviewing     = true;

            ApplyTransitionPreviewFrame(0f);
        }

        private void UpdateTransitionPreview()
        {
            if (!_isTransitionPreviewing) return;

            float previousElapsed = _transitionPreviewElapsed;
            float elapsed = (float)(EditorApplication.timeSinceStartup - _transitionPreviewStartedAt);
            _transitionPreviewElapsed = elapsed;
            ApplyTransitionPreviewFrame(elapsed);

            if (elapsed >= _transitionPreviewDuration)
                StopTransitionPreview(applyTargetState: true);
        }

        private void StopTransitionPreview(bool applyTargetState)
        {
            if (!_isTransitionPreviewing) return;

            _isTransitionPreviewing      = false;
            _transitionActorsInitialized = false;

            if (applyTargetState)
            {
                _stageState.Clear();
                foreach (var pair in _transitionToActors)
                {
                    StoryActorStateData target = pair.Value.ShallowClone();
                    if (_transitionActorTracks.TryGetValue(pair.Key, out var track))
                        target = StoryTransitionSampler.SampleActorTrackAtTime(target, track, _transitionPreviewDuration);
                    if (target != null)
                        _stageState[pair.Key] = target;
                }
                _bgState = StoryTransitionSampler.SampleBackgroundTrackAtTime(
                    CloneBackgroundState(_transitionToBackground),
                    _transitionBackgroundTrack,
                    _transitionPreviewDuration);
                SetPreviewCameraSampleState(StoryTransitionSampler.SampleCameraTrackAtTime(
                    _transitionCameraTrack,
                    _transitionCameraFocusTarget,
                    _transitionPreviewDuration));
                RebuildActorLayer(refreshInspectorLists: false);
                RefreshActorInspector();
                RefreshAuthoringControls();
            }
            else
            {
                ClearPreviewCameraSampleState();
            }

            _transitionFromActors.Clear();
            _transitionToActors.Clear();
            _transitionActorTracks.Clear();
            _transitionBackgroundTrack = null;
            _transitionCameraTrack = null;
            _transitionFromBackground = null;
            _transitionToBackground   = null;
            _transitionCameraFocusTarget = "";
            _transitionPreviewElapsed = 0f;
        }

        private float CalculateTransitionDuration()
        {
            float duration = 0.05f;
            var keys = new HashSet<string>(_transitionFromActors.Keys);
            keys.UnionWith(_transitionToActors.Keys);

            foreach (string key in keys)
            {
                _transitionFromActors.TryGetValue(key, out var from);
                _transitionToActors.TryGetValue(key, out var to);
                duration = Mathf.Max(duration, StoryTransitionSampler.ActorTransitionDuration(from, to));
            }

            duration = Mathf.Max(
                duration,
                StoryTransitionSampler.BackgroundTransitionDuration(_transitionFromBackground, _transitionToBackground));
            duration = Mathf.Max(duration, StoryTransitionSampler.GetBackgroundTrackDuration(_transitionBackgroundTrack));
            duration = Mathf.Max(duration, StoryTransitionSampler.GetCameraTrackDuration(_transitionCameraTrack));
            return duration;
        }

        /// <summary>
        /// Samples the transition at <paramref name="elapsed"/> seconds and updates the
        /// stage display. Uses fast in-place style updates after the first full rebuild,
        /// avoiding per-frame DOM recreation for the common case of static actor sets.
        /// </summary>
        private void ApplyTransitionPreviewFrame(float elapsed)
        {
            _stageState.Clear();

            var keys = new HashSet<string>(_transitionFromActors.Keys);
            keys.UnionWith(_transitionToActors.Keys);

            foreach (string key in keys)
            {
                _transitionFromActors.TryGetValue(key, out var from);
                _transitionToActors.TryGetValue(key, out var to);
                StoryActorStateData sample = StoryTransitionSampler.SampleActor(key, from, to, elapsed);
                if (sample != null && _transitionActorTracks.TryGetValue(key, out var track))
                    sample = StoryTransitionSampler.SampleActorTrackAtTime(sample, track, elapsed);

                if (sample != null)
                    _stageState[key] = sample;
            }

            _bgState = StoryTransitionSampler.SampleBackground(
                _transitionFromBackground, _transitionToBackground, elapsed);
            _bgState = StoryTransitionSampler.SampleBackgroundTrackAtTime(_bgState, _transitionBackgroundTrack, elapsed);
            StoryCameraStateData cameraSample = StoryTransitionSampler.SampleCameraTrackAtTime(
                _transitionCameraTrack,
                _transitionCameraFocusTarget,
                elapsed);
            SetPreviewCameraSampleState(cameraSample);

            // First frame: full DOM rebuild to initialise _actorElements for this transition.
            // Subsequent frames: fast style-only update (no Clear/re-add).
            if (!_transitionActorsInitialized)
            {
                RebuildActorLayer(refreshInspectorLists: false);
                _transitionActorsInitialized = true;
            }
            else
            {
                UpdateActorLayerPositions();
            }

            UpdateCameraGizmoVisual(cameraSample);
            RefreshFocusPreviewGuide();
            Repaint();
        }

        // ── 대화 / 선택지 텍스트 갱신 ─────────────────────────────────────────

        private void RefreshDialogue()
        {
            string speaker = _currentLine?.GetResolvedSpeakerName() ?? "";
            string text    = _currentLine?.DialogueText ?? "";

            if (_speakerLabel  != null) _speakerLabel.text  = speaker;
            if (_dialogueLabel != null) _dialogueLabel.text = text;

            if (_renderSpeakerLabel  != null) _renderSpeakerLabel.text  = speaker;
            if (_renderDialogueLabel != null) _renderDialogueLabel.text = text;

            bool hasContent = !string.IsNullOrEmpty(text);
            SetElementVisible(_renderDialoguePanel, hasContent && ShouldShowRenderDialogue());
            SetElementVisible(_dialoguePanel, ShouldShowEditorDialogue());
        }

        private void RefreshChoices()
        {
            _choiceArea?.Clear();
            _renderChoiceArea?.Clear();

            IStoryChoiceLikeModule choiceLike = FindChoiceLikeModule(_currentLine);
            if (choiceLike != null)
                BuildChoiceButtons(choiceLike);

            ApplyPreviewModeVisibility();
        }

        private static IStoryChoiceLikeModule FindChoiceLikeModule(StoryLineSO line)
        {
            if (line == null) return null;
            foreach (var module in line.Modules)
                if (module is IStoryChoiceLikeModule choiceLike) return choiceLike;
            return null;
        }

        private void BuildChoiceButtons(IStoryChoiceLikeModule choiceLike)
        {
            if (choiceLike.Options == null || choiceLike.Options.Count == 0) return;

            _choiceArea?.Add(new Label("Choices")
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(1f, 0.78f, 0.35f)),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 6,
                    marginTop = 3
                }
            });

            foreach (IStoryChoiceOption option in choiceLike.Options)
            {
                string reactionId = option.ReactionStartLineId;
                string text = string.IsNullOrWhiteSpace(option.Text) ? "(Empty Choice)" : option.Text;

                var bottomButton = new Button(() => OnChoiceSelected(reactionId))
                {
                    text = text,
                    style =
                    {
                        marginRight = 6,
                        marginBottom = 4,
                        paddingLeft = 8,
                        paddingRight = 8,
                        whiteSpace = WhiteSpace.Normal,
                        maxWidth = 220
                    }
                };
                _choiceArea?.Add(bottomButton);

                var overlayButton = new Button(() => OnChoiceSelected(reactionId))
                {
                    text = text,
                    style =
                    {
                        marginBottom = 5,
                        paddingLeft = 10,
                        paddingRight = 10,
                        minHeight = 28,
                        whiteSpace = WhiteSpace.Normal,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.1f, 0.9f)),
                        color = new StyleColor(new Color(0.94f, 0.94f, 0.96f))
                    }
                };
                _renderChoiceArea?.Add(overlayButton);
            }
        }

        private void OnChoiceSelected(string reactionStartLineId)
        {
            if (string.IsNullOrWhiteSpace(reactionStartLineId))
            {
                StopPlayback();
                return;
            }

            if (episode == null || !episode.TryGetLine(reactionStartLineId, out var next))
                return;

            if (!_isPlaying) _isLineSample = true;

            ShowLineSnapshot(next);
            RefreshButtons();
        }

        // ── 버튼 활성화 / 상태 갱신 ──────────────────────────────────────────

        private void RefreshButtons()
        {
            EnsureEpisodeForLine(_currentLine ?? _pendingFromLine);

            bool isRuntime      = previewMode == PreviewMode.RuntimePreview;
            bool hasEpisode     = episode != null;
            bool isAuthoring    = IsStageAuthoringMode;
            bool hasCurrentLine = _currentLine != null;

            SetElementVisible(_playBtn,              isRuntime && !_isPlaying && hasEpisode);
            SetElementVisible(_fromHereBtn,          isRuntime && !_isPlaying && hasEpisode && _pendingFromLine != null);
            SetElementVisible(_stopBtn,              isRuntime && _isPlaying);
            SetElementVisible(_nextBtn,              isRuntime && _isPlaying);
            SetElementVisible(_statusLabel,          isRuntime && !_previewRuntimeUiCollapsed);
            SetElementVisible(_sampleLineBtn,        hasEpisode);
            SetElementVisible(_prevLineBtn,          isAuthoring);
            SetElementVisible(_nextLineAuthoringBtn, isAuthoring);

            if (_nextBtn != null)
                _nextBtn.SetEnabled(_currentLine != null && !string.IsNullOrEmpty(_currentLine.NextLineId));

            if (_prevLineBtn != null)
                _prevLineBtn.SetEnabled(isAuthoring && hasCurrentLine && TryGetPreviousLine(_currentLine, out _));

            if (_nextLineAuthoringBtn != null)
                _nextLineAuthoringBtn.SetEnabled(isAuthoring && hasCurrentLine && TryGetNextLine(_currentLine, out _));

            if (_statusLabel != null)
            {
                _statusLabel.text = _isPlaying
                    ? (_isLineSample ? "Sample" : "Playing")
                    : "Stopped";
            }
        }

        // ── 모드 전환 가시성 갱신 ──────────────────────────────────────────────

        /// <summary>RuntimePreview / StageAuthoring 모드에 따라 UI 요소 가시성을 결정한다.</summary>
        private void ApplyPreviewModeVisibility()
        {
            bool isRuntime    = IsRuntimePreviewMode;
            bool showRuntimeUi = isRuntime && !_previewRuntimeUiCollapsed;

            ApplyInspectorPanelVisibility();
            ApplyCameraFrameModeStyles();
            RefreshFocusPreviewGuide();
            if (isRuntime) FitRuntimePreviewToWrapper();

            SetElementVisible(_dialoguePanel, ShouldShowEditorDialogue());
            SetElementVisible(_choiceArea,    ShouldShowEditorDialogue());
            SetElementVisible(_authoringToolsRoot, !isRuntime);
            SetElementVisible(_authoringGridLayer, !isRuntime);
            SetElementVisible(_cameraGizmoLayer,   !isRuntime);
            SetElementVisible(_timelinePanel, !isRuntime);
            SetElementVisible(_timelineResizeHandle, !isRuntime);
            SetEmptyStageVisible(!isRuntime && _stageState.Count == 0);

            SetElementVisible(_renderDialoguePanel,
                ShouldShowRenderDialogue()
                && _renderDialoguePanel != null
                && !string.IsNullOrEmpty(_renderDialogueLabel?.text));
            SetElementVisible(_renderChoiceArea,  ShouldShowRenderDialogue());
            SetElementVisible(_dialogueDisplayField, isRuntime && showRuntimeUi);

            RefreshButtons();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private bool ShouldShowEditorDialogue() =>
            IsRuntimePreviewMode
            && !_previewRuntimeUiCollapsed
            && dialogueDisplayMode is DialogueDisplayMode.EditorOnly or DialogueDisplayMode.Both;

        private bool ShouldShowRenderDialogue() =>
            IsRuntimePreviewMode
            && !_previewRuntimeUiCollapsed
            && dialogueDisplayMode is DialogueDisplayMode.RenderOnly or DialogueDisplayMode.Both;
    }
}
