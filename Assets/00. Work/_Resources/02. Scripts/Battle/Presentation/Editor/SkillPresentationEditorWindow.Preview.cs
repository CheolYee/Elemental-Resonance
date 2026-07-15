using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
    {
        [SerializeField] private string _previewPrefabFolder = "Assets";

        private SkillPreviewScene _previewScene;
        private ObjectField       _previewCasterField;
        private ObjectField[]     _previewTargetFields = new ObjectField[3];
        private float             _lastSampledTime     = float.MinValue;
        private Button            _recButton;
        private Button            _camFreeButton;
        private bool              _isCameraFree;

        // ── 녹화 ─────────────────────────────────────────────────────────────────
        private bool              _isRecording;
        private bool              _isDraggingCamera;
        private Vector2           _lastMousePos;
        private readonly HashSet<KeyCode> _heldKeys = new();
        private Vector3           _recordingCamPos;
        private Vector3           _recordingCamEuler;
        private double            _lastRecordingUpdateTime;

        private const float CamMoveSpeed      = 2f;
        private const float CamMoveSpeedFast  = 10f;
        private const float CamRotateSens     = 0.25f;

        // ── 생명주기 ─────────────────────────────────────────────────────────────

        partial void OnEnablePreview()
        {
            EditorApplication.playModeStateChanged  += OnPlayModeStateChanged;
            EditorApplication.delayCall             -= CreatePreviewSceneDeferred;
            EditorApplication.update                += OnCameraUpdate;
            _lastRecordingUpdateTime                 = EditorApplication.timeSinceStartup;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall += CreatePreviewSceneDeferred;
        }

        partial void OnDisablePreview()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall            -= CreatePreviewSceneDeferred;
            EditorApplication.update               -= OnCameraUpdate;
            CancelRecording();
        }

        partial void OnDestroyPreview()
        {
            CancelRecording();
            _previewScene?.Destroy();
            _previewScene = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    _previewScene?.Destroy();
                    _previewScene = null;
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall -= CreatePreviewSceneDeferred;
                    EditorApplication.delayCall += CreatePreviewSceneDeferred;
                    break;
            }
        }

        private void CreatePreviewSceneDeferred()
        {
            if (this == null) return;
            _previewScene?.Destroy();
            _previewScene = SkillPreviewScene.Create(_previewLayout);
            RebuildPreview();
        }

        // ── UI 구성 ───────────────────────────────────────────────────────────────

        partial void OnCreateGUIPreview()
        {
            if (RenderingView == null) return;

            var imgui = new IMGUIContainer(DrawPreview)
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0, right = 0, top = 0, bottom = 0
                }
            };
            RenderingView.Add(imgui);

            _camFreeButton = new Button(ToggleCameraFree)
            {
                text = "○ CAM",
                style =
                {
                    position            = Position.Absolute,
                    left = 8, top = 8,
                    width = 62, height  = 22,
                    fontSize            = 11,
                    color               = new StyleColor(Color.white),
                    backgroundColor     = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.85f)),
                    borderTopLeftRadius     = 3, borderTopRightRadius    = 3,
                    borderBottomLeftRadius  = 3, borderBottomRightRadius = 3,
                }
            };
            RenderingView.Add(_camFreeButton);

            _recButton = new Button(() => { if (_isRecording) StopRecording(); else StartRecording(); })
            {
                text = "○ REC",
                style =
                {
                    position            = Position.Absolute,
                    left = 8, top = 34,
                    width = 62, height  = 22,
                    fontSize            = 11,
                    color               = new StyleColor(Color.white),
                    backgroundColor     = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.85f)),
                    borderTopLeftRadius     = 3, borderTopRightRadius    = 3,
                    borderBottomLeftRadius  = 3, borderBottomRightRadius = 3,
                }
            };
            RenderingView.Add(_recButton);

            HierarchyPanel?.Insert(0, BuildPreviewCharacterSection());
        }

        private VisualElement BuildPreviewCharacterSection()
        {
            var section = new VisualElement
            {
                style =
                {
                    flexShrink        = 0,
                    flexDirection     = FlexDirection.Column,
                    paddingLeft       = 6, paddingRight  = 6,
                    paddingTop        = 4, paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.10f, 0.10f, 0.12f))
                }
            };

            section.Add(new Label("Characters")
            {
                style =
                {
                    fontSize       = 9,
                    color          = new StyleColor(new Color(0.55f, 0.55f, 0.60f)),
                    marginBottom   = 3,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            });

            VisualElement casterRow;
            (casterRow, _previewCasterField) = MakeCharacterRow("Caster", go =>
            {
                if (_previewLayout == null) return;
                _previewLayout.casterPrefab = go;
                EditorUtility.SetDirty(_previewLayout);
                _previewScene?.RebuildCharacters(_previewLayout);
                Repaint();
            });
            section.Add(casterRow);

            _previewTargetFields = new ObjectField[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                VisualElement row;
                (row, _previewTargetFields[i]) = MakeCharacterRow($"Target {i}", go =>
                {
                    if (_previewLayout?.targetPrefabs == null || idx >= _previewLayout.targetPrefabs.Length) return;
                    _previewLayout.targetPrefabs[idx] = go;
                    EditorUtility.SetDirty(_previewLayout);
                    _previewScene?.RebuildCharacters(_previewLayout);
                    Repaint();
                });
                section.Add(row);
            }

            RefreshPreviewCharacterSection();
            return section;
        }

        private (VisualElement row, ObjectField field) MakeCharacterRow(string label, System.Action<GameObject> onChange)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, height = 20, marginBottom = 2 }
            };

            var lbl = new Label(label) { style = { width = 50, fontSize = 10, flexShrink = 0 } };
            row.Add(lbl);

            var field = new ObjectField
            {
                objectType = typeof(GameObject),
                style      = { flexGrow = 1, height = 18, fontSize = 10 }
            };
            field.RegisterValueChangedCallback(evt => onChange(evt.newValue as GameObject));
            row.Add(field);

            var pickBtn = new Button(() => ShowAgentPicker(go =>
            {
                field.SetValueWithoutNotify(go);
                onChange(go);
            }))
            {
                text  = "▾",
                style = { width = 20, height = 18, fontSize = 10, flexShrink = 0, paddingLeft = 2, paddingRight = 2 }
            };
            row.Add(pickBtn);

            return (row, field);
        }

        private void ShowAgentPicker(System.Action<GameObject> onSelect)
        {
            var menu   = new GenericMenu();
            string folder = string.IsNullOrWhiteSpace(_previewPrefabFolder) ? "Assets" : _previewPrefabFolder;
            var guids  = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            bool any   = false;

            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<SkillPreviewAgentTag>() == null) continue;

                var captured = prefab;
                menu.AddItem(new GUIContent(prefab.name), false, () => onSelect(captured));
                any = true;
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent($"SkillPreviewAgentTag 프리팹 없음 ({folder})"));

            menu.ShowAsContext();
        }

        private void RefreshPreviewCharacterSection()
        {
            _previewCasterField?.SetValueWithoutNotify(_previewLayout?.casterPrefab);
            for (int i = 0; i < 3; i++)
            {
                var prefab = _previewLayout?.targetPrefabs != null && i < _previewLayout.targetPrefabs.Length
                    ? _previewLayout.targetPrefabs[i]
                    : null;
                _previewTargetFields[i]?.SetValueWithoutNotify(prefab);
            }
        }

        // ── 렌더 ─────────────────────────────────────────────────────────────────

        private void DrawPreview()
        {
            if (_previewScene == null) return;
            var rt = _previewScene.RenderTexture;
            if (rt == null) return;

            if (_currentTime != _lastSampledTime)
            {
                if (_isPlaying && _currentTime > _lastSampledTime && _lastSampledTime > float.MinValue)
                    TriggerSfxInRange(_lastSampledTime, _currentTime);

                _previewScene.Sample(_currentTime);
                _lastSampledTime = _currentTime;
            }

            _previewScene.RenderCamera();

            float w = RenderingView.resolvedStyle.width;
            float h = RenderingView.resolvedStyle.height;
            if (w > 0 && h > 0)
                GUI.DrawTexture(new Rect(0, 0, w, h), rt, ScaleMode.ScaleToFit, false);

            HandleCameraInput();
        }

        private void ToggleCameraFree()
        {
            _isCameraFree = !_isCameraFree;
            UpdateCamFreeButton();
            if (!_isCameraFree && !_isRecording)
            {
                _heldKeys.Clear();
                _isDraggingCamera = false;
                if (_previewScene != null) _previewScene.IsRecordingCamera = false;
                _lastSampledTime = float.MinValue;
            }
            Repaint();
        }

        private void UpdateCamFreeButton()
        {
            if (_camFreeButton == null) return;
            if (_isCameraFree)
            {
                _camFreeButton.text = "● CAM";
                _camFreeButton.style.backgroundColor = new StyleColor(new Color(0.15f, 0.55f, 0.85f, 0.95f));
            }
            else
            {
                _camFreeButton.text = "○ CAM";
                _camFreeButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.85f));
            }
        }

        private void UpdateRecButton()
        {
            if (_recButton == null) return;
            if (_isRecording)
            {
                _recButton.text = "● REC";
                _recButton.style.backgroundColor = new StyleColor(new Color(1f, 0.22f, 0.22f, 0.95f));
            }
            else
            {
                _recButton.text = "○ REC";
                _recButton.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.85f));
            }
        }

        private void HandleCameraInput()
        {
            if (!_isCameraFree && !_isRecording) return;
            var e = Event.current;
            if (e == null) return;

            switch (e.type)
            {
                case EventType.KeyDown:
                    if (_heldKeys.Count == 0 && !_isRecording && _previewScene != null)
                    {
                        _previewScene.GetCameraTransform(out _recordingCamPos, out _recordingCamEuler);
                        _previewScene.IsRecordingCamera = true;
                        _lastRecordingUpdateTime = EditorApplication.timeSinceStartup;
                    }
                    _heldKeys.Add(e.keyCode);
                    e.Use();
                    break;
                case EventType.KeyUp:
                    _heldKeys.Remove(e.keyCode);
                    if (!_isRecording && _heldKeys.Count == 0 && !_isDraggingCamera && _previewScene != null)
                        _previewScene.IsRecordingCamera = false;
                    e.Use();
                    break;
                case EventType.MouseDown when e.button == 1:
                    if (!_isRecording && _previewScene != null)
                    {
                        _previewScene.GetCameraTransform(out _recordingCamPos, out _recordingCamEuler);
                        _previewScene.IsRecordingCamera = true;
                    }
                    _isDraggingCamera = true;
                    _lastMousePos     = e.mousePosition;
                    e.Use();
                    break;
                case EventType.MouseDrag when e.button == 1 && _isDraggingCamera:
                    Vector2 delta        = e.mousePosition - _lastMousePos;
                    _recordingCamEuler.y += delta.x * CamRotateSens;
                    _recordingCamEuler.x += delta.y * CamRotateSens;
                    _lastMousePos         = e.mousePosition;
                    _previewScene.SetRecordingCameraTransform(_recordingCamPos, _recordingCamEuler);
                    e.Use();
                    break;
                case EventType.MouseUp when e.button == 1:
                    _isDraggingCamera = false;
                    if (!_isRecording && _heldKeys.Count == 0 && _previewScene != null)
                        _previewScene.IsRecordingCamera = false;
                    e.Use();
                    break;
            }
        }

        // ── 녹화 제어 ──────────────────────────────────────────────────────────────

        private void StartRecording()
        {
            if (_previewScene == null) return;
            _previewScene.GetCameraTransform(out _recordingCamPos, out _recordingCamEuler);
            _previewScene.IsRecordingCamera   = true;
            _isDraggingCamera                 = false;
            _heldKeys.Clear();
            _lastRecordingUpdateTime          = EditorApplication.timeSinceStartup;
            _isRecording = true;
            UpdateRecButton();
            Repaint();
        }

        private void StopRecording()
        {
            _isRecording      = false;
            _isDraggingCamera = false;
            _heldKeys.Clear();
            if (_previewScene != null) _previewScene.IsRecordingCamera = false;
            CommitCameraKeyframes();
            _lastSampledTime = float.MinValue;
            UpdateRecButton();
            Repaint();
        }

        private void CancelRecording()
        {
            if (!_isRecording) return;
            _isRecording      = false;
            _isDraggingCamera = false;
            _heldKeys.Clear();
            if (_previewScene != null) _previewScene.IsRecordingCamera = false;
            UpdateRecButton();
        }

        private void OnCameraUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Min((float)(now - _lastRecordingUpdateTime), 0.1f);
            _lastRecordingUpdateTime = now;

            if (_previewScene == null || (!_isCameraFree && !_isRecording)) return;
            if (_heldKeys.Count == 0) return;

            bool  fast  = _heldKeys.Contains(KeyCode.LeftShift) || _heldKeys.Contains(KeyCode.RightShift);
            float speed = (fast ? CamMoveSpeedFast : CamMoveSpeed) * deltaTime;

            Quaternion rot     = Quaternion.Euler(_recordingCamEuler);
            Vector3    forward = rot * Vector3.forward;
            Vector3    right   = rot * Vector3.right;

            if (_heldKeys.Contains(KeyCode.W)) _recordingCamPos += forward * speed;
            if (_heldKeys.Contains(KeyCode.S)) _recordingCamPos -= forward * speed;
            if (_heldKeys.Contains(KeyCode.A)) _recordingCamPos -= right   * speed;
            if (_heldKeys.Contains(KeyCode.D)) _recordingCamPos += right   * speed;
            if (_heldKeys.Contains(KeyCode.Q)) _recordingCamPos += Vector3.up * speed;
            if (_heldKeys.Contains(KeyCode.E)) _recordingCamPos -= Vector3.up * speed;

            _previewScene.SetRecordingCameraTransform(_recordingCamPos, _recordingCamEuler);
            Repaint();
        }

        private void CommitCameraKeyframes()
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null || _previewLayout == null) return;

            Undo.RecordObject(_target, "Record Camera Keyframes");

            if (!tl.addedObjects.Contains(SkillObjectKind.Camera))
                tl.addedObjects.Add(SkillObjectKind.Camera);

            Vector3 posDelta = _recordingCamPos   - _previewLayout.cameraPosition;
            Vector3 rotDelta = _recordingCamEuler - _previewLayout.cameraRotationEuler;

            UpsertCameraKeyframe(tl, SkillKeyframeProperty.CamPosition, _currentTime,
                kf => kf.cameraPosition = posDelta);
            UpsertCameraKeyframe(tl, SkillKeyframeProperty.CamRotation, _currentTime,
                kf => kf.cameraRotationEuler = rotDelta);

            EditorUtility.SetDirty(_target);
            RefreshObjectList();
            RefreshTimeline();
            RebuildPreview();
        }

        private static void UpsertCameraKeyframe(
            SkillPresentationTimeline tl,
            SkillKeyframeProperty     prop,
            float                     t,
            System.Action<SkillKeyframeData> applyValue)
        {
            var keys = tl.cameraTrack.keyframes;

            foreach (var kf in keys)
            {
                if (kf == null || kf.property != prop) continue;
                if (!Mathf.Approximately(kf.timeSeconds, t)) continue;
                applyValue(kf);
                return;
            }

            var newKey = new SkillKeyframeData { property = prop, timeSeconds = t };
            applyValue(newKey);
            keys.Add(newKey);
            SortByTime(keys);
        }

        private void TriggerSfxInRange(float fromTime, float toTime)
        {
            var tl   = GetEditableTimeline();
            var keys = tl?.sfxTrack?.keyframes;
            if (keys == null) return;

            foreach (var kf in keys)
            {
                if (kf == null) continue;
                if (kf.timeSeconds > fromTime && kf.timeSeconds <= toTime)
                {
                    var clip = FindSfxClip(kf.sfxSound);
                    if (clip != null) PlayEditorClip(clip);
                }
            }
        }

        private void RebuildPreview()
        {
            if (_previewScene == null) return;
            var tl = GetEditableTimeline();
            _previewScene.Rebuild(_previewLayout, tl);
            _lastSampledTime = float.MinValue;
            RefreshPreviewCharacterSection();
            Repaint();
        }
    }
}
