using System;
using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Battle.Effects;
using Battle.Enums;
using LitMotion;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
    {
        // ── 계층 / 키프레임 인스펙터 컨테이너 ───────────────────────────────────────
        private VisualElement _objectListRoot;
        private VisualElement _keyframeInspectorContainer;

        // ── Object List (Hierarchy) ───────────────────────────────────────────────

        internal void RefreshObjectList()
        {
            if (_objectListRoot == null) return;
            _objectListRoot.Clear();

            var addBtn = new Button(ShowAddObjectMenu)
            {
                text  = "+ Add Object",
                style = { height = 22, marginBottom = 4, fontSize = 10 }
            };
            addBtn.SetEnabled(_target != null);
            _objectListRoot.Add(addBtn);

            var tl = GetEditableTimeline();
            if (tl == null) return;

            foreach (SkillObjectKind kind in tl.addedObjects)
                _objectListRoot.Add(BuildObjectRow(kind, -1));

            for (int i = 0; i < (tl.vfxObjects?.Count ?? 0); i++)
                _objectListRoot.Add(BuildObjectRow(SkillObjectKind.Vfx, i));
        }

        private VisualElement BuildObjectRow(SkillObjectKind kind, int vfxIndex)
        {
            bool selected = _selectedObjectKind == kind &&
                            (kind != SkillObjectKind.Vfx || _selectedVfxIndex == vfxIndex);
            string vfxName = kind == SkillObjectKind.Vfx
                ? (GetEditableTimeline()?.vfxObjects?[vfxIndex]?.vfxDefinition?.name ?? "NONE")
                : null;
            string label = kind == SkillObjectKind.Vfx ? vfxName : kind.ToString();

            var row = new VisualElement
            {
                style =
                {
                    flexDirection    = FlexDirection.Row, alignItems = Align.Center,
                    paddingLeft      = 4, paddingRight  = 4,
                    paddingTop       = 2, paddingBottom = 2, marginBottom = 1,
                    backgroundColor  = new StyleColor(selected
                        ? new Color(0.22f, 0.34f, 0.52f)
                        : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius    = 2, borderTopRightRadius    = 2,
                    borderBottomLeftRadius = 2, borderBottomRightRadius = 2
                }
            };

            row.Add(new VisualElement
            {
                style =
                {
                    width = 5, height = 14, marginRight = 6, flexShrink = 0,
                    backgroundColor        = new StyleColor(GetObjectKindColor(kind)),
                    borderTopLeftRadius    = 1, borderTopRightRadius    = 1,
                    borderBottomLeftRadius = 1, borderBottomRightRadius = 1
                }
            });
            row.Add(new Label(label)
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            SkillObjectKind capturedKind  = kind;
            int             capturedIndex = vfxIndex;
            row.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0) { SelectObject(capturedKind, capturedIndex); e.StopPropagation(); }
                if (e.button == 1) { ShowRemoveObjectMenu(capturedKind, capturedIndex); e.StopPropagation(); }
            });
            return row;
        }

        private void ShowAddObjectMenu()
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;

            var menu = new GenericMenu();
            foreach (SkillObjectKind kind in new[]
            {
                SkillObjectKind.Animation, SkillObjectKind.Effect,
                SkillObjectKind.Camera,    SkillObjectKind.Caster,
                SkillObjectKind.Ui,        SkillObjectKind.Sfx,
            })
            {
                SkillObjectKind captured = kind;
                if (tl.addedObjects.Contains(kind))
                    menu.AddDisabledItem(new GUIContent(kind.ToString()));
                else
                    menu.AddItem(new GUIContent(kind.ToString()), false, () => AddObject(captured));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("VFX"), false, () => AddObject(SkillObjectKind.Vfx));
            menu.AddSeparator("");
            if (tl.addedObjects.Contains(SkillObjectKind.EndMarker))
                menu.AddDisabledItem(new GUIContent("EndMarker"));
            else
                menu.AddItem(new GUIContent("EndMarker"), false, () => AddObject(SkillObjectKind.EndMarker));
            menu.ShowAsContext();
        }

        private void AddObject(SkillObjectKind kind)
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            if (kind != SkillObjectKind.Vfx && tl.addedObjects.Contains(kind)) return;

            Undo.RecordObject(_target, "Add Object");
            int vfxIndex = -1;
            if (kind == SkillObjectKind.Vfx)
            {
                tl.vfxObjects.Add(new SkillVfxObjectData());
                vfxIndex = tl.vfxObjects.Count - 1;
            }
            else if (kind == SkillObjectKind.EndMarker)
            {
                float autoTime = tl.GetEffectiveDuration();
                tl.endMarkerKeyframe = new SkillKeyframeData
                {
                    property    = SkillKeyframeProperty.TimelineEndTime,
                    timeSeconds = autoTime
                };
                tl.addedObjects.Add(kind);
            }
            else
                tl.addedObjects.Add(kind);

            EditorUtility.SetDirty(_target);
            SelectObject(kind, vfxIndex);
        }

        private void ShowRemoveObjectMenu(SkillObjectKind kind, int vfxIndex)
        {
            var menu = new GenericMenu();
            if (kind == SkillObjectKind.Vfx)
                menu.AddItem(new GUIContent("복제"), false, () => DuplicateVfxObject(vfxIndex));
            menu.AddItem(new GUIContent("Remove"), false, () => RemoveObject(kind, vfxIndex));
            menu.ShowAsContext();
        }

        private void DuplicateVfxObject(int srcIndex)
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;
            if (srcIndex < 0 || tl.vfxObjects == null || srcIndex >= tl.vfxObjects.Count) return;

            Undo.RecordObject(_target, "Duplicate VFX Object");

            var src = tl.vfxObjects[srcIndex];
            var clone = new SkillVfxObjectData
            {
                vfxDefinition           = src.vfxDefinition,
                spawnTime               = src.spawnTime,
                lifeTime                = src.lifeTime,
                simulationSpeed         = src.simulationSpeed,
                startLifetimeMultiplier = src.startLifetimeMultiplier,
                spawnTarget             = src.spawnTarget,
                spawnPositionOffset     = src.spawnPositionOffset,
                spawnRotationEuler      = src.spawnRotationEuler,
                keyframes               = new List<SkillKeyframeData>()
            };
            foreach (var k in src.keyframes)
            {
                var kClone = CloneKeyframe(k);
                if (kClone != null) clone.keyframes.Add(kClone);
            }

            tl.vfxObjects.Add(clone);
            int newIndex = tl.vfxObjects.Count - 1;

            EditorUtility.SetDirty(_target);
            SelectObject(SkillObjectKind.Vfx, newIndex);
        }

        private void RemoveObject(SkillObjectKind kind, int vfxIndex)
        {
            var tl = GetEditableTimeline();
            if (tl == null || _target == null) return;

            Undo.RecordObject(_target, "Remove Object");
            if (kind == SkillObjectKind.Vfx)
            {
                if (vfxIndex >= 0 && vfxIndex < tl.vfxObjects.Count)
                    tl.vfxObjects.RemoveAt(vfxIndex);

                if (_selectedObjectKind == SkillObjectKind.Vfx)
                {
                    if (_selectedVfxIndex == vfxIndex)     ClearSelection();
                    else if (_selectedVfxIndex > vfxIndex) _selectedVfxIndex--;
                }
            }
            else
            {
                tl.addedObjects.Remove(kind);
                ClearObjectTrackKeyframes(tl, kind);
                if (_selectedObjectKind == kind) ClearSelection();
            }

            EditorUtility.SetDirty(_target);
            RefreshObjectList();
            RefreshTimeline();
            RefreshInspector();
        }

        private static void ClearObjectTrackKeyframes(SkillPresentationTimeline tl, SkillObjectKind kind)
        {
            switch (kind)
            {
                case SkillObjectKind.Animation:  tl.animationTrack?.keyframes?.Clear(); break;
                case SkillObjectKind.Effect:     tl.effectTrack?.keyframes?.Clear();    break;
                case SkillObjectKind.Camera:     tl.cameraTrack?.keyframes?.Clear();    break;
                case SkillObjectKind.Caster:     tl.casterTrack?.keyframes?.Clear();    break;
                case SkillObjectKind.Ui:         tl.uiTrack?.keyframes?.Clear();        break;
                case SkillObjectKind.Sfx:        tl.sfxTrack?.keyframes?.Clear();       break;
                case SkillObjectKind.EndMarker:  tl.endMarkerKeyframe = null;           break;
            }
        }

        private static Color GetObjectKindColor(SkillObjectKind kind) => kind switch
        {
            SkillObjectKind.Animation => new Color(0.38f, 0.62f, 1.00f),
            SkillObjectKind.Effect    => new Color(0.95f, 0.62f, 0.30f),
            SkillObjectKind.Vfx       => new Color(0.38f, 0.82f, 0.42f),
            SkillObjectKind.Camera    => new Color(0.95f, 0.72f, 0.28f),
            SkillObjectKind.Caster    => new Color(0.42f, 0.88f, 0.75f),
            SkillObjectKind.Ui        => new Color(0.72f, 0.42f, 0.95f),
            SkillObjectKind.Sfx       => new Color(0.40f, 0.82f, 0.95f),
            SkillObjectKind.EndMarker => new Color(0.95f, 0.90f, 0.40f),
            _                         => Color.gray
        };

        // ── Inspector ────────────────────────────────────────────────────────────

        internal void BuildInspectorPlaceholder()
        {
            if (_keyframeInspectorContainer == null) return;
            _keyframeInspectorContainer.Clear();
            _keyframeInspectorContainer.Add(MakeInspectorInfoLabel("키프레임을 선택하면 표시됩니다."));
        }

        internal void RefreshInspector()
        {
            if (_keyframeInspectorContainer == null) return;

            RefreshVfxObjectSection();

            if (_selectedKeyframe == null) { BuildInspectorPlaceholder(); return; }

            _keyframeInspectorContainer.Clear();

            var content = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, paddingLeft = 12, paddingRight = 12, paddingTop = 12, paddingBottom = 12 }
            };

            if (_isEasingLineSelected)
            {
                BuildEasingLineInspector(content, _selectedKeyframe);
                _keyframeInspectorContainer.Add(content);
                return;
            }

            if (SelectedKeyframes.Count > 1)
                content.Add(MakeInspectorInfoLabel($"{SelectedKeyframes.Count}개 선택됨", new Color(0.82f, 0.82f, 0.66f)));

            content.Add(CreateTimeField(_selectedKeyframe));

            switch (_selectedKeyframe.property)
            {
                case SkillKeyframeProperty.AnimParam:
                    content.Add(CreateAnimParamField(_selectedKeyframe));
                    break;

                case SkillKeyframeProperty.EffectSlot:
                    var previewUpdater = AddEffectInspector(content, _selectedKeyframe);
                    content.Add(CreateFloatField("Value Multiplier", _selectedKeyframe.valueMultiplier,
                        v => _selectedKeyframe.valueMultiplier = v,
                        v => previewUpdater?.Invoke(v)));
                    break;

                case SkillKeyframeProperty.CamPosition:
                case SkillKeyframeProperty.CamRotation:
                case SkillKeyframeProperty.CamZoom:
                case SkillKeyframeProperty.CamShake:
                    AddCameraKeyframeInspector(content, _selectedKeyframe);
                    break;

                case SkillKeyframeProperty.CasterPosition:
                case SkillKeyframeProperty.CasterRotation:
                    AddCasterKeyframeInspector(content, _selectedKeyframe);
                    break;

                case SkillKeyframeProperty.UiAction:
                    content.Add(CreateEnumField("Action", _selectedKeyframe.uiAction, v => _selectedKeyframe.uiAction = v));
                    break;

                case SkillKeyframeProperty.SfxId:
                    if (Enum.GetValues(typeof(Gamelib.SoundSystem.SfxSounds)).Length == 0)
                        content.Add(MakeInspectorInfoLabel("효과음이 존재하지 않습니다"));
                    else
                        content.Add(CreateEnumField("Sfx Sound", _selectedKeyframe.sfxSound,
                            v => _selectedKeyframe.sfxSound = v));
                    break;

                case SkillKeyframeProperty.VfxActive:
                    content.Add(CreateEnumField("Action", _selectedKeyframe.vfxActiveAction,
                        v => _selectedKeyframe.vfxActiveAction = (SkillVfxActiveAction)v));
                    break;

                case SkillKeyframeProperty.VfxPosition:
                case SkillKeyframeProperty.VfxRotation:
                case SkillKeyframeProperty.VfxScale:
                    AddVfxTransformKeyframeInspector(content, _selectedKeyframe);
                    break;
            }

            _keyframeInspectorContainer.Add(content);
        }

        // ── VFX Object Inspector ─────────────────────────────────────────────────

        private void RefreshVfxObjectSection()
        {
            if (_vfxObjectSection == null) return;
            _vfxObjectSection.Clear();

            var tl = GetEditableTimeline();
            bool show = _selectedObjectKind == SkillObjectKind.Vfx
                        && _selectedVfxIndex >= 0
                        && tl?.vfxObjects != null
                        && _selectedVfxIndex < tl.vfxObjects.Count;

            SetVisible(_vfxObjectSection, show);
            if (!show) return;

            BuildVfxObjectSection(_vfxObjectSection, tl.vfxObjects[_selectedVfxIndex]);
        }

        private void BuildVfxObjectSection(VisualElement container, SkillVfxObjectData vfxObj)
        {
            var section = new VisualElement
            {
                style =
                {
                    flexDirection     = FlexDirection.Column,
                    paddingLeft       = 12, paddingRight  = 12,
                    paddingTop        = 10, paddingBottom = 10,
                    borderBottomWidth = 1,
                    borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.20f))
                }
            };

            section.Add(new Label($"VFX [{_selectedVfxIndex}]")
            {
                style =
                {
                    fontSize                = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color                   = new StyleColor(new Color(0.38f, 0.82f, 0.42f)),
                    marginBottom            = 8
                }
            });

            // vfxDefinition
            var defField = new ObjectField("VFX Definition")
            {
                objectType        = typeof(VfxDefinitionSO),
                allowSceneObjects = false,
                value             = vfxObj.vfxDefinition
            };
            StyleInspectorField(defField);
            defField.RegisterValueChangedCallback(evt =>
            {
                var def = evt.newValue as VfxDefinitionSO;
                ApplyVfxObjectChange(() =>
                {
                    vfxObj.vfxDefinition = def;
                    if (def != null) vfxObj.lifeTime = def.defaultLifeTime;
                });
                RefreshInspector();
            });
            section.Add(defField);

            // spawnTime
            var spawnTimeField = new FloatField("Spawn Time") { value = vfxObj.spawnTime };
            StyleInspectorField(spawnTimeField);
            spawnTimeField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.spawnTime = evt.newValue));
            section.Add(spawnTimeField);

            // lifeTime
            var lifeTimeField = new FloatField("Life Time") { value = vfxObj.lifeTime };
            StyleInspectorField(lifeTimeField);
            lifeTimeField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.lifeTime = evt.newValue));
            section.Add(lifeTimeField);

            // spawnTarget
            var spawnTargetField = new EnumField("Spawn Target");
            spawnTargetField.Init(vfxObj.spawnTarget);
            spawnTargetField.value = vfxObj.spawnTarget;
            StyleInspectorField(spawnTargetField);

            bool isRandomEnemy = _referenceCard != null && _referenceCard.targetType == CardTargetType.RandomEnemy;
            bool showHitIndex = isRandomEnemy &&
                                 (vfxObj.spawnTarget == SkillVfxSpawnTarget.Target ||
                                  vfxObj.spawnTarget == SkillVfxSpawnTarget.BetweenCasterAndTarget);
            var hitIndexContainer = BuildHitIndexField(vfxObj);
            hitIndexContainer.style.display = showHitIndex ? DisplayStyle.Flex : DisplayStyle.None;

            spawnTargetField.RegisterValueChangedCallback(evt =>
            {
                var newTarget = (SkillVfxSpawnTarget)evt.newValue;
                ApplyVfxObjectChange(() => vfxObj.spawnTarget = newTarget);
                hitIndexContainer.style.display =
                    (isRandomEnemy &&
                     (newTarget == SkillVfxSpawnTarget.Target || newTarget == SkillVfxSpawnTarget.BetweenCasterAndTarget))
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            });
            section.Add(spawnTargetField);
            section.Add(hitIndexContainer);

            // spawnPositionOffset
            var posField = new Vector3Field("Position Offset") { value = vfxObj.spawnPositionOffset };
            StyleInspectorField(posField);
            posField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.spawnPositionOffset = evt.newValue));
            section.Add(posField);

            // spawnRotationEuler
            var rotField = new Vector3Field("Rotation Euler") { value = vfxObj.spawnRotationEuler };
            StyleInspectorField(rotField);
            rotField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.spawnRotationEuler = evt.newValue));
            section.Add(rotField);

            // simulationSpeed
            var simSpeedField = new FloatField("Simulation Speed") { value = vfxObj.simulationSpeed };
            StyleInspectorField(simSpeedField);
            simSpeedField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.simulationSpeed = evt.newValue));
            section.Add(simSpeedField);

            // startLifetimeMultiplier
            var lifetimeField = new FloatField("Lifetime Multiplier") { value = vfxObj.startLifetimeMultiplier };
            StyleInspectorField(lifetimeField);
            lifetimeField.RegisterValueChangedCallback(evt => ApplyVfxObjectChange(() => vfxObj.startLifetimeMultiplier = evt.newValue));
            section.Add(lifetimeField);

            container.Add(section);
        }

        private VisualElement BuildHitIndexField(SkillVfxObjectData vfxObj)
        {
            var container = new VisualElement();
            int damageCount = CountDamageEffectsInCurrentTimeline();
            int choiceCount = Mathf.Max(1, damageCount);   // 0개여도 최소 "Hit 0" 표시

            var choices = new List<string>();
            for (int i = 0; i < choiceCount; i++)
                choices.Add($"Hit {i}");

            int clampedIdx = Mathf.Clamp(vfxObj.hitIndex, 0, choiceCount - 1);
            var dropdown = new DropdownField("Hit Index", choices, clampedIdx);
            StyleInspectorField(dropdown);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int idx = choices.IndexOf(evt.newValue);
                ApplyVfxObjectChange(() => vfxObj.hitIndex = idx < 0 ? 0 : idx);
            });
            container.Add(dropdown);
            return container;
        }

        private int CountDamageEffectsInCurrentTimeline()
        {
            if (_referenceCard == null) return 0;
            var timeline = GetEditableTimeline();
            var keyframes = timeline?.effectTrack?.keyframes;
            if (keyframes == null) return 0;

            var slots = _referenceCard.effectSlots;
            if (slots == null) return 0;

            int count = 0;
            foreach (var kf in keyframes)
            {
                if (kf == null || kf.property != SkillKeyframeProperty.EffectSlot) continue;
                if (string.IsNullOrEmpty(kf.effectSlotId)) continue;
                for (int i = 0; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s != null && s.effectSlotId == kf.effectSlotId && s.effect is DamageEffect)
                    {
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        private void ApplyVfxObjectChange(Action change)
        {
            if (_target == null) return;
            Undo.RecordObject(_target, "Edit VFX Object");
            change();
            EditorUtility.SetDirty(_target);
            RebuildPreview();
        }

        // ── Easing Line Inspector ─────────────────────────────────────────────────

        private void BuildEasingLineInspector(VisualElement content, SkillKeyframeData keyframe)
        {
            content.Add(new Label("Transition")
            {
                style =
                {
                    fontSize                = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color                   = new StyleColor(new Color(0.85f, 0.85f, 0.90f)),
                    marginBottom            = 8
                }
            });

            var options = new List<string> { "Hold" };
            foreach (LitMotion.Ease e in System.Enum.GetValues(typeof(LitMotion.Ease)))
                options.Add(e.ToString());

            string currentValue  = keyframe.isHold ? "Hold" : keyframe.easing.ToString();
            int    currentIndex  = options.IndexOf(currentValue);
            if (currentIndex < 0) currentIndex = 0;

            var dropdown = new PopupField<string>("Transition", options, currentIndex);
            StyleInspectorField(dropdown);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                ApplyKeyframeChange(() =>
                {
                    if (evt.newValue == "Hold")
                    {
                        keyframe.isHold = true;
                    }
                    else
                    {
                        keyframe.isHold = false;
                        if (System.Enum.TryParse<LitMotion.Ease>(evt.newValue, out var ease))
                            keyframe.easing = ease;
                    }
                });
                RefreshTimeline();
                RefreshInspector();
            });
            content.Add(dropdown);
        }

        // ── VFX Transform Keyframe Inspector ──────────────────────────────────────

        private void AddVfxTransformKeyframeInspector(VisualElement content, SkillKeyframeData key)
        {
            content.Add(CreateReadOnlyTextField("Property", key.property.ToString()));

            switch (key.property)
            {
                case SkillKeyframeProperty.VfxPosition:
                    content.Add(CreateVector3Field("Position", key.position, v => key.position = v));
                    break;
                case SkillKeyframeProperty.VfxRotation:
                    content.Add(CreateVector3Field("Rotation Euler", key.rotationEuler, v => key.rotationEuler = v));
                    break;
                case SkillKeyframeProperty.VfxScale:
                    content.Add(CreateVector3Field("Scale", key.scale, v => key.scale = v));
                    break;
            }
        }

        // ── Camera Keyframe Inspector ──────────────────────────────────────────

        private void AddCameraKeyframeInspector(VisualElement content, SkillKeyframeData key)
        {
            content.Add(CreateReadOnlyTextField("Property", key.property.ToString()));

            switch (key.property)
            {
                case SkillKeyframeProperty.CamPosition:
                    content.Add(CreateVector3Field("Position", key.cameraPosition, v => key.cameraPosition = v));
                    break;
                case SkillKeyframeProperty.CamRotation:
                    content.Add(CreateVector3Field("Rotation Euler", key.cameraRotationEuler, v => key.cameraRotationEuler = v));
                    break;
                case SkillKeyframeProperty.CamZoom:
                    content.Add(CreateFloatField("Field Of View", key.fieldOfView, v => key.fieldOfView = v));
                    break;
                case SkillKeyframeProperty.CamShake:
                    content.Add(CreateFloatField("Amplitude",     key.amplitude,     v => key.amplitude     = v));
                    content.Add(CreateFloatField("Shake Duration", key.shakeDuration, v => key.shakeDuration = v));
                    break;
            }
        }

        // ── Caster Keyframe Inspector ──────────────────────────────────────────

        private void AddCasterKeyframeInspector(VisualElement content, SkillKeyframeData key)
        {
            content.Add(CreateReadOnlyTextField("Property", key.property.ToString()));

            switch (key.property)
            {
                case SkillKeyframeProperty.CasterPosition:
                    content.Add(CreateVector3Field("Position Offset", key.position, v => key.position = v));
                    break;
                case SkillKeyframeProperty.CasterRotation:
                    content.Add(CreateVector3Field("Rotation Euler Offset", key.rotationEuler, v => key.rotationEuler = v));
                    break;
            }
        }

        // ── Time Field ────────────────────────────────────────────────────────

        private FloatField CreateTimeField(SkillKeyframeData keyframe)
        {
            var field = new FloatField("Time Seconds") { value = keyframe.timeSeconds };
            StyleInspectorField(field);
            field.RegisterValueChangedCallback(evt =>
            {
                ApplyKeyframeChange(() => keyframe.timeSeconds = evt.newValue);
                SortSelectedRowByTime();
                RefreshTimeline();
                RebuildPreview();
            });
            return field;
        }

        // ── Animation Param ───────────────────────────────────────────────────

        private VisualElement CreateAnimParamField(SkillKeyframeData keyframe)
        {
            var container = new VisualElement();

            var paramField = new ObjectField("Anim Param")
            {
                objectType        = typeof(AnimParamSO),
                allowSceneObjects = false,
                value             = keyframe.animParam
            };
            StyleInspectorField(paramField);
            paramField.RegisterValueChangedCallback(evt =>
                ApplyKeyframeChange(() => keyframe.animParam = evt.newValue as AnimParamSO));

            var clipField = new ObjectField("Anim Clip")
            {
                objectType        = typeof(AnimationClip),
                allowSceneObjects = false,
                value             = keyframe.animClip
            };
            StyleInspectorField(clipField);

            var durationLabel = new Label();
            durationLabel.style.color       = new UnityEngine.Color(0.6f, 0.8f, 0.6f);
            durationLabel.style.fontSize    = 10;
            durationLabel.style.paddingLeft = 4;
            durationLabel.style.paddingTop  = 2;

            void UpdateDuration(AnimationClip clip)
            {
                durationLabel.text = clip != null ? $"클립 길이: {clip.length:F2}s" : "";
            }

            clipField.RegisterValueChangedCallback(evt =>
            {
                var clip = evt.newValue as AnimationClip;
                ApplyKeyframeChange(() => keyframe.animClip = clip);
                UpdateDuration(clip);
            });

            UpdateDuration(keyframe.animClip);

            container.Add(paramField);
            container.Add(clipField);
            container.Add(durationLabel);
            return container;
        }

        // ── Effect Inspector ──────────────────────────────────────────────────

        private Action<float> AddEffectInspector(VisualElement content, SkillKeyframeData keyframe)
        {
            if (_referenceCard == null)
            {
                content.Add(MakeInspectorInfoLabel("Reference Card를 선택하세요."));
                return null;
            }

            if (_referenceCard.EnsureEffectSlotIds())
                EditorUtility.SetDirty(_referenceCard);

            var effectSlots = _referenceCard.effectSlots;
            if (effectSlots == null || effectSlots.Count == 0)
            {
                content.Add(MakeInspectorInfoLabel("이 카드에는 effectSlots가 없습니다."));
                return null;
            }

            var optionLabels = new List<string>(effectSlots.Count);
            int selectedIndex = -1;

            for (int i = 0; i < effectSlots.Count; i++)
            {
                CardEffectSlot slot = effectSlots[i];
                optionLabels.Add(BuildEffectSlotLabel(slot, i));
                if (slot != null && slot.effectSlotId == keyframe.effectSlotId)
                    selectedIndex = i;
            }

            if (!string.IsNullOrEmpty(keyframe.effectSlotId) && selectedIndex < 0)
                content.Add(MakeInspectorInfoLabel("현재 Reference Card에 없는 Effect Slot입니다.", new Color(0.90f, 0.72f, 0.32f)));

            var dropdown = new PopupField<string>("Effect Slot", optionLabels, selectedIndex >= 0 ? selectedIndex : 0);
            if (selectedIndex >= 0) dropdown.SetValueWithoutNotify(optionLabels[selectedIndex]);
            StyleInspectorField(dropdown);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                int newIndex = optionLabels.IndexOf(evt.newValue);
                if (newIndex < 0) return;
                CardEffectSlot slot = effectSlots[newIndex];
                if (slot == null) return;
                ApplyKeyframeChange(() => keyframe.effectSlotId = slot.effectSlotId);
                RefreshInspector();
            });
            content.Add(dropdown);

            if (selectedIndex < 0) return null;
            CardEffectSlot selected = effectSlots[selectedIndex];
            if (selected?.effect == null) { content.Add(MakeInspectorInfoLabel("선택된 슬롯에 effect 데이터가 없습니다.")); return null; }
            return AddEffectValuePreview(content, selected.effect, keyframe.valueMultiplier);
        }

        private Action<float> AddEffectValuePreview(VisualElement content, CardEffect effect, float multiplier)
        {
            switch (effect)
            {
                case DamageEffect damage:
                {
                    content.Add(CreateReadOnlyIntField("Base Damage",  damage.damage));
                    var final = CreateReadOnlyIntField("Final Damage", CardEffectValueCalculator.Calculate(damage.damage, multiplier));
                    content.Add(final);
                    return v => final.SetValueWithoutNotify(CardEffectValueCalculator.Calculate(damage.damage, v));
                }
                case BlockEffect block:
                {
                    content.Add(CreateReadOnlyIntField("Base Block",  block.block));
                    var final = CreateReadOnlyIntField("Final Block", CardEffectValueCalculator.Calculate(block.block, multiplier));
                    content.Add(final);
                    return v => final.SetValueWithoutNotify(CardEffectValueCalculator.Calculate(block.block, v));
                }
                case CostGainEffect costGain:
                {
                    content.Add(CreateReadOnlyIntField("Base Cost Gain",  costGain.costGain));
                    var final = CreateReadOnlyIntField("Final Cost Gain", CardEffectValueCalculator.Calculate(costGain.costGain, multiplier));
                    content.Add(final);
                    content.Add(CreateReadOnlyTextField("Condition",       costGain.condition.ToString()));
                    content.Add(CreateReadOnlyIntField("Condition Value",  costGain.conditionValue));
                    return v => final.SetValueWithoutNotify(CardEffectValueCalculator.Calculate(costGain.costGain, v));
                }
                default:
                {
                    content.Add(CreateReadOnlyTextField("Effect Type", effect.GetType().Name));
                    content.Add(CreateReadOnlyIntField("Base Value",   effect.BaseValue));
                    var final = CreateReadOnlyIntField("Final Value",  CardEffectValueCalculator.Calculate(effect.BaseValue, multiplier));
                    content.Add(final);
                    return v => final.SetValueWithoutNotify(CardEffectValueCalculator.Calculate(effect.BaseValue, v));
                }
            }
        }

        private static string BuildEffectSlotLabel(CardEffectSlot slot, int index)
            => $"{index} - {slot?.effect?.GetType().Name ?? "None"}";

        // ── Field Creators ────────────────────────────────────────────────────

        private TextField CreateTextField(string label, string value, Action<string> onChanged)
        {
            var field = new TextField(label) { value = value ?? string.Empty };
            StyleInspectorField(field);
            field.RegisterValueChangedCallback(evt => ApplyKeyframeChange(() => onChanged(evt.newValue)));
            return field;
        }

        private FloatField CreateFloatField(string label, float value, Action<float> onChanged, Action<float> afterChanged = null)
        {
            var field = new FloatField(label) { value = value };
            StyleInspectorField(field);
            field.RegisterValueChangedCallback(evt => { ApplyKeyframeChange(() => onChanged(evt.newValue)); afterChanged?.Invoke(evt.newValue); });
            return field;
        }

        private VisualElement CreateVector3Field(string label, Vector3 value, Action<Vector3> onChanged)
        {
            var field = new Vector3Field(label) { value = value };
            StyleInspectorField(field);
            field.RegisterValueChangedCallback(evt => ApplyKeyframeChange(() => onChanged(evt.newValue)));
            return field;
        }

        private TextField CreateReadOnlyTextField(string label, string value)
        {
            var field = new TextField(label) { value = value ?? string.Empty };
            field.SetEnabled(false);
            StyleInspectorField(field);
            return field;
        }

        private IntegerField CreateReadOnlyIntField(string label, int value)
        {
            var field = new IntegerField(label) { value = value };
            field.SetEnabled(false);
            StyleInspectorField(field);
            return field;
        }

        private EnumField CreateEnumField<TEnum>(string label, TEnum value, Action<TEnum> onChanged) where TEnum : Enum
        {
            var field = new EnumField(label);
            field.Init(value);
            field.value = value;
            StyleInspectorField(field);
            field.RegisterValueChangedCallback(evt => ApplyKeyframeChange(() => onChanged((TEnum)evt.newValue)));
            return field;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ApplyKeyframeChange(Action change)
        {
            if (_target == null) return;
            Undo.RecordObject(_target, "Edit Keyframe");
            change();
            EditorUtility.SetDirty(_target);
            _lastSampledTime = float.MinValue;
            Repaint();
        }

        private void SortSelectedRowByTime()
        {
            var tl = GetEditableTimeline();
            if (tl == null) return;
            string tag = _selectedRowTag ?? GetRowTagForKeyframe(_selectedKeyframe, tl);
            if (tag == null) return;

            if (tag.StartsWith("camera_")) { SortByTime(tl.cameraTrack.keyframes); return; }
            switch (tag)
            {
                case "animation": SortByTime(tl.animationTrack.keyframes); break;
                case "effect":    SortByTime(tl.effectTrack.keyframes);    break;
                case "ui":        SortByTime(tl.uiTrack.keyframes);        break;
                case "sfx":       SortByTime(tl.sfxTrack.keyframes);       break;
            }
        }

        internal static Label MakeInspectorInfoLabel(string text, Color? color = null) => new(text)
        {
            style = { color = new StyleColor(color ?? new Color(0.50f, 0.50f, 0.55f)), marginBottom = 8, fontSize = 11, whiteSpace = WhiteSpace.Normal }
        };

        private static void StyleInspectorField(VisualElement field) => field.style.marginBottom = 8;
    }
}
