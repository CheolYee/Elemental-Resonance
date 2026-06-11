using System;
using System.Collections.Generic;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using UnityEditor.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        private enum SoundTimelineRowKind
        {
            Bgm,
            Sfx,
        }

        private SoundTimelineRowKind _selectedSoundRowKind = SoundTimelineRowKind.Bgm;
        private StoryBgmKeyframeData _selectedBgmKey;
        private StorySfxKeyframeData _selectedSfxKey;
        private readonly List<SoundClipboardEntry> _soundClipboardEntries = new();

        private sealed class SoundClipboardEntry
        {
            public SoundTimelineRowKind rowKind;
            public StoryBgmKeyframeData bgmKey;
            public StorySfxKeyframeData sfxKey;
            public float timeSeconds;
        }

        private static readonly Dictionary<string, SoundListSo> _soundListCache = new();

        private static SoundListSo GetCachedSoundList(string enumName)
        {
            if (_soundListCache.TryGetValue(enumName, out SoundListSo cached))
                return cached;

            string[] guids = AssetDatabase.FindAssets("t:SoundListSo");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SoundListSo so = AssetDatabase.LoadAssetAtPath<SoundListSo>(path);
                if (so != null && !string.IsNullOrEmpty(so.enumName))
                    _soundListCache[so.enumName] = so;
            }

            _soundListCache.TryGetValue(enumName, out SoundListSo result);
            return result;
        }

        private static string GetSoundNote(string enumName, int soundIndex)
        {
            SoundListSo list = GetCachedSoundList(enumName);
            if (list?.sounds == null)
                return null;

            foreach (SoundClipSo so in list.sounds)
            {
                if (so != null && so.soundIndex == soundIndex)
                    return so.note;
            }
            return null;
        }

        private static string BuildBgmTooltip(StoryBgmKeyframeData keyframe)
        {
            string text = keyframe.operation == StoryBgmKeyOperation.Stop
                ? $"BGM Stop {keyframe.timeSeconds:0.00}s"
                : $"BGM {keyframe.bgmSound} {keyframe.timeSeconds:0.00}s";
            if (keyframe.operation == StoryBgmKeyOperation.Stop)
                return text;
            string note = GetSoundNote("BgmSounds", (int)keyframe.bgmSound);
            return string.IsNullOrEmpty(note) ? text : $"{text}\n{note}";
        }

        private static string BuildSfxTooltip(StorySfxKeyframeData keyframe)
        {
            string text = $"SFX {keyframe.sfxSound} {keyframe.timeSeconds:0.00}s";
            string note = GetSoundNote("SfxSounds", (int)keyframe.sfxSound);
            return string.IsNullOrEmpty(note) ? text : $"{text}\n{note}";
        }

        private static bool HasEnumOptions<TEnum>() where TEnum : Enum =>
            Enum.GetValues(typeof(TEnum)).Length > 0;

        private void ClearSoundTimelineProxyCache()
        {
            _bgmTimelineKeyProxies.Clear();
            _sfxTimelineKeyProxies.Clear();
            _timelineProxyToBgmKeys.Clear();
            _timelineProxyToSfxKeys.Clear();
        }

        private void CopySelectedSoundTimelineKeys()
        {
            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(soundKeys);
            if (selected.Count == 0)
                return;

            selected.Sort((a, b) => StoryTransitionSampler.GetKeyTime(a).CompareTo(StoryTransitionSampler.GetKeyTime(b)));
            _soundClipboardEntries.Clear();
            foreach (StoryActorKeyframeData proxy in selected)
            {
                StoryBgmKeyframeData bgmKey = ResolveBgmKeyFromProxy(proxy);
                if (bgmKey != null)
                {
                    _soundClipboardEntries.Add(new SoundClipboardEntry
                    {
                        rowKind = SoundTimelineRowKind.Bgm,
                        bgmKey = bgmKey.ShallowClone(),
                        timeSeconds = bgmKey.timeSeconds
                    });
                    continue;
                }

                StorySfxKeyframeData sfxKey = ResolveSfxKeyFromProxy(proxy);
                if (sfxKey == null)
                    continue;

                _soundClipboardEntries.Add(new SoundClipboardEntry
                {
                    rowKind = SoundTimelineRowKind.Sfx,
                    sfxKey = sfxKey.ShallowClone(),
                    timeSeconds = sfxKey.timeSeconds
                });
            }

            _timelineClipboardSelectionKind = StageSelectionKind.Sound;
        }

        private void PasteSoundTimelineKeysAtPlayhead()
        {
            if (_soundClipboardEntries.Count == 0 || _timelineClipboardSelectionKind != StageSelectionKind.Sound)
                return;

            float firstTime = float.MaxValue;
            foreach (SoundClipboardEntry entry in _soundClipboardEntries)
                firstTime = Mathf.Min(firstTime, entry.timeSeconds);

            SaveSoundTrackToCurrent(track =>
            {
                _selectedTimelineKeys.Clear();
                foreach (SoundClipboardEntry entry in _soundClipboardEntries)
                {
                    float targetTime = _timelinePlayheadTime + Mathf.Max(0f, entry.timeSeconds - firstTime);
                    if (entry.rowKind == SoundTimelineRowKind.Bgm)
                    {
                        StoryBgmKeyframeData target = FindBgmKeyAtTime(track, targetTime);
                        StoryBgmKeyframeData source = entry.bgmKey;
                        if (source == null)
                            continue;

                        if (target == null)
                        {
                            target = source.ShallowClone();
                            track.bgmKeyframes.Add(target);
                        }
                        else
                        {
                            target.operation = source.operation;
                            target.bgmSound = source.bgmSound;
                            target.transitionMode = source.transitionMode;
                        }

                        target.timeSeconds = targetTime;
                        target.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                        StoryActorKeyframeData proxy = GetOrCreateSoundKeyProxy(target);
                        if (proxy != null)
                            _selectedTimelineKeys.Add(proxy);
                        continue;
                    }

                    StorySfxKeyframeData sfxTarget = FindSfxKeyAtTime(track, targetTime);
                    StorySfxKeyframeData sfxSource = entry.sfxKey;
                    if (sfxSource == null)
                        continue;

                    if (sfxTarget == null)
                    {
                        sfxTarget = sfxSource.ShallowClone();
                        track.sfxKeyframes.Add(sfxTarget);
                    }
                    else
                    {
                        sfxTarget.sfxSound = sfxSource.sfxSound;
                    }

                    sfxTarget.timeSeconds = targetTime;
                    sfxTarget.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                    StoryActorKeyframeData sfxProxy = GetOrCreateSoundKeyProxy(sfxTarget);
                    if (sfxProxy != null)
                        _selectedTimelineKeys.Add(sfxProxy);
                }

                StoryActorKeyframeData primarySelection = null;
                foreach (StoryActorKeyframeData key in _selectedTimelineKeys)
                {
                    primarySelection = key;
                    break;
                }

                _selectedTimelineProperty = primarySelection != null
                    ? primarySelection.property
                    : ResolveDefaultSoundTimelineProperty();
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
            }, refresh: true);

            NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
            UpdateSelectedSoundKeysFromTimelineSelection();
            RefreshTimelinePanel();
        }

        private void CleanupSoundTimelineProxies(StorySoundTrackData track)
        {
            var activeBgmKeys = new HashSet<StoryBgmKeyframeData>(
                track?.bgmKeyframes != null ? track.bgmKeyframes : Array.Empty<StoryBgmKeyframeData>());
            var activeSfxKeys = new HashSet<StorySfxKeyframeData>(
                track?.sfxKeyframes != null ? track.sfxKeyframes : Array.Empty<StorySfxKeyframeData>());

            foreach (var pair in new List<KeyValuePair<StoryBgmKeyframeData, StoryActorKeyframeData>>(_bgmTimelineKeyProxies))
            {
                if (pair.Key != null && activeBgmKeys.Contains(pair.Key))
                    continue;

                _bgmTimelineKeyProxies.Remove(pair.Key);
                _timelineProxyToBgmKeys.Remove(pair.Value);
            }

            foreach (var pair in new List<KeyValuePair<StorySfxKeyframeData, StoryActorKeyframeData>>(_sfxTimelineKeyProxies))
            {
                if (pair.Key != null && activeSfxKeys.Contains(pair.Key))
                    continue;

                _sfxTimelineKeyProxies.Remove(pair.Key);
                _timelineProxyToSfxKeys.Remove(pair.Value);
            }
        }

        private StoryActorKeyframeData GetOrCreateSoundKeyProxy(StoryBgmKeyframeData keyframe)
        {
            if (keyframe == null)
                return null;

            if (!_bgmTimelineKeyProxies.TryGetValue(keyframe, out StoryActorKeyframeData proxy))
            {
                proxy = new StoryActorKeyframeData();
                _bgmTimelineKeyProxies[keyframe] = proxy;
                _timelineProxyToBgmKeys[proxy] = keyframe;
            }

            proxy.property = StoryActorKeyframeProperty.SoundBgm;
            proxy.timeSeconds = keyframe.timeSeconds;
            proxy.normalizedTime = keyframe.normalizedTime;
            return proxy;
        }

        private StoryActorKeyframeData GetOrCreateSoundKeyProxy(StorySfxKeyframeData keyframe)
        {
            if (keyframe == null)
                return null;

            if (!_sfxTimelineKeyProxies.TryGetValue(keyframe, out StoryActorKeyframeData proxy))
            {
                proxy = new StoryActorKeyframeData();
                _sfxTimelineKeyProxies[keyframe] = proxy;
                _timelineProxyToSfxKeys[proxy] = keyframe;
            }

            proxy.property = StoryActorKeyframeProperty.SoundSfx;
            proxy.timeSeconds = keyframe.timeSeconds;
            proxy.normalizedTime = keyframe.normalizedTime;
            return proxy;
        }

        private IReadOnlyList<StoryActorKeyframeData> GetCurrentSoundTimelineKeyframes()
        {
            StorySoundTrackData track = FindCurrentStageLayout()?.SoundTrackEditable;
            if (track == null)
                return Array.Empty<StoryActorKeyframeData>();

            CleanupSoundTimelineProxies(track);

            var result = new List<StoryActorKeyframeData>();
            if (track.bgmKeyframes != null)
            {
                foreach (StoryBgmKeyframeData keyframe in track.bgmKeyframes)
                {
                    StoryActorKeyframeData proxy = GetOrCreateSoundKeyProxy(keyframe);
                    if (proxy != null)
                        result.Add(proxy);
                }
            }

            if (track.sfxKeyframes != null)
            {
                foreach (StorySfxKeyframeData keyframe in track.sfxKeyframes)
                {
                    StoryActorKeyframeData proxy = GetOrCreateSoundKeyProxy(keyframe);
                    if (proxy != null)
                        result.Add(proxy);
                }
            }

            return result;
        }

        private StoryBgmKeyframeData ResolveBgmKeyFromProxy(StoryActorKeyframeData proxy)
        {
            _timelineProxyToBgmKeys.TryGetValue(proxy, out StoryBgmKeyframeData keyframe);
            return keyframe;
        }

        private StorySfxKeyframeData ResolveSfxKeyFromProxy(StoryActorKeyframeData proxy)
        {
            _timelineProxyToSfxKeys.TryGetValue(proxy, out StorySfxKeyframeData keyframe);
            return keyframe;
        }

        private void UpdateSelectedSoundKeysFromTimelineSelection()
        {
            _selectedBgmKey = null;
            _selectedSfxKey = null;

            if (_selectionKind != StageSelectionKind.Sound)
                return;

            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(soundKeys);
            if (selected.Count != 1)
                return;

            StoryActorKeyframeData proxy = selected[0];
            _selectedBgmKey = ResolveBgmKeyFromProxy(proxy);
            _selectedSfxKey = ResolveSfxKeyFromProxy(proxy);
            if (_selectedBgmKey != null)
                _selectedSoundRowKind = SoundTimelineRowKind.Bgm;
            else if (_selectedSfxKey != null)
                _selectedSoundRowKind = SoundTimelineRowKind.Sfx;
        }

        private StoryActorKeyframeProperty ResolveDefaultSoundTimelineProperty()
        {
            StorySoundTrackData track = FindCurrentStageLayout()?.SoundTrackEditable;
            if (HasEnumOptions<BgmSounds>() && (track?.bgmKeyframes?.Count ?? 0) > 0)
                return StoryActorKeyframeProperty.SoundBgm;

            if (HasEnumOptions<SfxSounds>() && (track?.sfxKeyframes?.Count ?? 0) > 0)
                return StoryActorKeyframeProperty.SoundSfx;

            return HasEnumOptions<BgmSounds>()
                ? StoryActorKeyframeProperty.SoundBgm
                : StoryActorKeyframeProperty.SoundSfx;
        }

        private VisualElement BuildSoundSettingsPanels()
        {
            _soundSettingsPanelRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };

            _episodeSoundDefaultsRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };
            _soundSettingsPanelRoot.Add(_episodeSoundDefaultsRoot);
            return _soundSettingsPanelRoot;
        }

        private void RefreshSoundSettingsPanels()
        {
            if (_episodeSoundDefaultsRoot == null)
                return;

            bool visible = IsStageAuthoringMode;
            SetElementVisible(_soundSettingsPanelRoot, visible);
            if (!visible)
                return;

            _episodeSoundDefaultsRoot.Clear();
            BuildEpisodeSoundDefaultsPanel();
        }

        private void BuildEpisodeSoundDefaultsPanel()
        {
            _episodeSoundDefaultsRoot.Add(MakeBoldLabel("Episode Sound Defaults"));

            if (episode == null)
            {
                _episodeSoundDefaultsRoot.Add(new Label("Select an episode to edit default sound settings.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginTop = 4 }
                });
                return;
            }

            StorySoundSettingsData defaults = ResolveEpisodeSoundDefaultsForDisplay();

            var channelField = new ObjectField("Sound Channel")
            {
                objectType = typeof(EventChannelSO),
                allowSceneObjects = false,
                value = defaults.soundChannel,
                style = { marginBottom = 4 }
            };
            channelField.RegisterValueChangedCallback(e =>
            {
                SaveEpisodeSoundSettingsToCurrent(settings => settings.soundChannel = e.newValue as EventChannelSO);
            });
            _episodeSoundDefaultsRoot.Add(channelField);

            var bgmFadeField = new FloatField("BGM Fade Duration")
            {
                value = defaults.bgmFadeDuration,
                style = { marginBottom = 4 }
            };
            bgmFadeField.RegisterValueChangedCallback(e =>
            {
                SaveEpisodeSoundSettingsToCurrent(settings => settings.bgmFadeDuration = Mathf.Max(0f, e.newValue));
            });
            _episodeSoundDefaultsRoot.Add(bgmFadeField);

            var sfxFadeField = new FloatField("SFX Fade Duration")
            {
                value = defaults.sfxFadeDuration
            };
            sfxFadeField.RegisterValueChangedCallback(e =>
            {
                SaveEpisodeSoundSettingsToCurrent(settings => settings.sfxFadeDuration = Mathf.Max(0f, e.newValue));
            });
            _episodeSoundDefaultsRoot.Add(sfxFadeField);
        }

        private StorySoundSettingsData ResolveEpisodeSoundDefaultsForDisplay() =>
            StorySoundSettingsUtility.ResolveEpisodeDefaults(episode) ?? new StorySoundSettingsData();

        private void SaveEpisodeSoundSettingsToCurrent(Action<StorySoundSettingsData> setter)
        {
            if (episode == null)
                return;

            RecordStageUndo(episode, "Edit Episode Sound Defaults");
            if (!episode.HasExplicitDefaultSoundSettingsEditable)
                episode.DefaultSoundSettingsEditable.CopyFrom(ResolveEpisodeSoundDefaultsForDisplay());

            episode.HasExplicitDefaultSoundSettingsEditable = true;
            setter(episode.DefaultSoundSettingsEditable);
            EditorUtility.SetDirty(episode);
            RefreshSoundSettingsPanels();
            RefreshActorInspector();
            RefreshTimelinePanel();
        }

        private VisualElement BuildSoundRow()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StorySoundTrackData track = layout?.SoundTrackEditable;
            bool hasAnySound = (track?.bgmKeyframes?.Count ?? 0) > 0 || (track?.sfxKeyframes?.Count ?? 0) > 0;
            bool selected = _selectionKind == StageSelectionKind.Sound;

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginBottom = 1,
                    backgroundColor = new StyleColor(selected ? new Color(0.26f, 0.36f, 0.50f) : new Color(0.17f, 0.17f, 0.19f)),
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };

            row.Add(new Label("SND")
            {
                style =
                {
                    fontSize = 8,
                    marginRight = 5,
                    color = new StyleColor(hasAnySound ? new Color(0.44f, 0.84f, 1f) : new Color(0.45f, 0.45f, 0.45f))
                }
            });

            row.Add(new Label("Sound")
            {
                style = { fontSize = 10, flexGrow = 1, color = new StyleColor(Color.white) }
            });

            row.RegisterCallback<PointerDownEvent>(e =>
            {
                SelectSound();
                e.StopPropagation();
            });

            return row;
        }

        private void SelectSound()
        {
            if (!IsStageAuthoringMode)
                return;

            if (_timelineRecordEnabled)
                return;

            bool wasAlreadySound = _selectionKind == StageSelectionKind.Sound;
            _selectionKind = StageSelectionKind.Sound;
            _selectedActorKey = null;
            if (!wasAlreadySound)
                ClearTimelineSelection(refresh: false);
            RefreshActorList();
            HighlightSelectedActor();
            UpdateCameraGizmoVisual();
            RefreshActorInspector();
            RefreshAuthoringControls();
            RefreshTimelinePanel();
        }

        private void ClearSoundTimelineSelection()
        {
            _selectedBgmKey = null;
            _selectedSfxKey = null;
        }

        private bool HasSelectedSoundKey() => _selectedBgmKey != null || _selectedSfxKey != null;

        private void BuildSoundInspector()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StorySoundTrackData track = layout?.SoundTrackEditable;
            UpdateSelectedSoundKeysFromTimelineSelection();

            _inspectorRoot.Add(MakeBoldLabel("Sound"));
            _inspectorRoot.Add(MakeSeparator());

            if (HasTimelineMultiSelection)
            {
                _inspectorRoot.Add(new Label("Multiple sound keys selected. Select a single key to edit it.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginTop = 6 }
                });
                return;
            }

            if (_selectedBgmKey != null)
            {
                BuildSelectedBgmKeyInspector(track);
                if (_selectedBgmKey != null)
                    return;
            }

            if (_selectedSfxKey != null)
            {
                BuildSelectedSfxKeyInspector(track);
                if (_selectedSfxKey != null)
                    return;
            }

            _inspectorRoot.Add(new Label("Select a BGM or SFX key to edit it. Use Add Property to create the first sound key on this line.")
            {
                style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginTop = 6 }
            });
        }

        private void BuildSelectedBgmKeyInspector(StorySoundTrackData track)
        {
            if (track?.bgmKeyframes == null || _selectedBgmKey == null || !track.bgmKeyframes.Contains(_selectedBgmKey))
            {
                _selectedBgmKey = null;
                return;
            }

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected BGM Key"));

            var timeField = new FloatField("Time")
            {
                value = _selectedBgmKey.timeSeconds,
                style = { marginBottom = 3 }
            };
            timeField.RegisterValueChangedCallback(e =>
            {
                SaveSoundTrackToCurrent(soundTrack =>
                {
                    float targetTime = Mathf.Max(0f, e.newValue);
                    StoryBgmKeyframeData existing = FindBgmKeyAtTime(soundTrack, targetTime);
                    if (existing != null && !ReferenceEquals(existing, _selectedBgmKey))
                        targetTime = _selectedBgmKey.timeSeconds;

                    _selectedBgmKey.timeSeconds = targetTime;
                    _selectedBgmKey.normalizedTime = Mathf.Clamp01(_selectedBgmKey.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                }, refresh: true);
            });
            _inspectorRoot.Add(timeField);

            var operationField = new EnumField("Operation", _selectedBgmKey.operation)
            {
                style = { marginBottom = 3 }
            };
            operationField.RegisterValueChangedCallback(e =>
            {
                SaveSoundTrackToCurrent(soundTrack =>
                {
                    _selectedBgmKey.operation = (StoryBgmKeyOperation)e.newValue;
                }, refresh: true);
            });
            _inspectorRoot.Add(operationField);

            if (_selectedBgmKey.operation == StoryBgmKeyOperation.Play)
            {
                if (HasEnumOptions<BgmSounds>())
                {
                    var bgmField = new EnumField("BGM", _selectedBgmKey.bgmSound)
                    {
                        style = { marginBottom = 3 }
                    };
                    bgmField.RegisterValueChangedCallback(e =>
                    {
                        SaveSoundTrackToCurrent(soundTrack =>
                        {
                            _selectedBgmKey.bgmSound = (BgmSounds)e.newValue;
                        }, refresh: true);
                    });
                    _inspectorRoot.Add(bgmField);
                    string bgmNote = GetSoundNote("BgmSounds", (int)_selectedBgmKey.bgmSound);
                    if (!string.IsNullOrEmpty(bgmNote))
                    {
                        var noteBox = new HelpBox(bgmNote, HelpBoxMessageType.None);
                        noteBox.style.marginBottom = 3;
                        _inspectorRoot.Add(noteBox);
                    }
                }
                else
                {
                    _inspectorRoot.Add(new Label("No BgmSounds enum value is available.")
                    {
                        style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)), marginBottom = 3 }
                    });
                }

                var transitionField = new EnumField("Transition", _selectedBgmKey.transitionMode);
                transitionField.RegisterValueChangedCallback(e =>
                {
                    SaveSoundTrackToCurrent(soundTrack =>
                    {
                        _selectedBgmKey.transitionMode = (StoryBgmTransitionMode)e.newValue;
                    }, refresh: true);
                });
                _inspectorRoot.Add(transitionField);
            }
        }

        private void BuildSelectedSfxKeyInspector(StorySoundTrackData track)
        {
            if (track?.sfxKeyframes == null || _selectedSfxKey == null || !track.sfxKeyframes.Contains(_selectedSfxKey))
            {
                _selectedSfxKey = null;
                return;
            }

            _inspectorRoot.Add(MakeSeparator());
            _inspectorRoot.Add(MakeBoldLabel("Selected SFX Key"));

            var timeField = new FloatField("Time")
            {
                value = _selectedSfxKey.timeSeconds,
                style = { marginBottom = 3 }
            };
            timeField.RegisterValueChangedCallback(e =>
            {
                SaveSoundTrackToCurrent(soundTrack =>
                {
                    float targetTime = Mathf.Max(0f, e.newValue);
                    StorySfxKeyframeData existing = FindSfxKeyAtTime(soundTrack, targetTime);
                    if (existing != null && !ReferenceEquals(existing, _selectedSfxKey))
                        targetTime = _selectedSfxKey.timeSeconds;

                    _selectedSfxKey.timeSeconds = targetTime;
                    _selectedSfxKey.normalizedTime = Mathf.Clamp01(_selectedSfxKey.timeSeconds / Mathf.Max(1f, GetTimelineDuration()));
                }, refresh: true);
            });
            _inspectorRoot.Add(timeField);

            if (HasEnumOptions<SfxSounds>())
            {
                var sfxField = new EnumField("SFX", _selectedSfxKey.sfxSound);
                sfxField.RegisterValueChangedCallback(e =>
                {
                    SaveSoundTrackToCurrent(soundTrack =>
                    {
                        _selectedSfxKey.sfxSound = (SfxSounds)e.newValue;
                    }, refresh: true);
                });
                _inspectorRoot.Add(sfxField);
                string sfxNote = GetSoundNote("SfxSounds", (int)_selectedSfxKey.sfxSound);
                if (!string.IsNullOrEmpty(sfxNote))
                {
                    var noteBox = new HelpBox(sfxNote, HelpBoxMessageType.None);
                    noteBox.style.marginTop = 3;
                    _inspectorRoot.Add(noteBox);
                }
            }
            else
            {
                _inspectorRoot.Add(new Label("No SfxSounds enum value is available.")
                {
                    style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.5f, 0.5f)) }
                });
            }
        }

        private void BuildSoundTimelineRows()
        {
            StoryStageLayoutModuleSO layout = FindCurrentStageLayout();
            StorySoundTrackData track = layout?.SoundTrackEditable;
            if (track == null || !track.HasAnyKeyframes)
            {
                AddTimelineEmpty("No sound track. Use Add Property.");
                return;
            }

            float duration = Mathf.Max(2f, GetTimelineDuration() + 0.5f, _timelinePlayheadTime + 0.5f);
            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            bool hasVisibleRow = false;

            if ((track.bgmKeyframes?.Count ?? 0) > 0)
            {
                BuildBgmTimelineRow(track, duration, soundKeys);
                hasVisibleRow = true;
            }

            if ((track.sfxKeyframes?.Count ?? 0) > 0)
            {
                BuildSfxTimelineRow(track, duration, soundKeys);
                hasVisibleRow = true;
            }

            if (!hasVisibleRow)
                AddTimelineEmpty("No sound track. Use Add Property.");
        }

        private void BuildBgmTimelineRow(StorySoundTrackData track, float duration, IReadOnlyList<StoryActorKeyframeData> soundKeys)
        {
            var row = CreateTimelineRow("BGM");
            var lane = CreateTimelineLane(duration, TimelineRowHeight, StoryActorKeyframeProperty.SoundBgm);
            AddSoundTimelineMarkers(lane, duration, SoundTimelineRowKind.Bgm, soundKeys);
            row.Add(lane);
            _timelineRows.Add(row);
        }

        private void BuildSfxTimelineRow(StorySoundTrackData track, float duration, IReadOnlyList<StoryActorKeyframeData> soundKeys)
        {
            var row = CreateTimelineRow("SFX");
            var lane = CreateTimelineLane(duration, TimelineRowHeight, StoryActorKeyframeProperty.SoundSfx);
            AddSoundTimelineMarkers(lane, duration, SoundTimelineRowKind.Sfx, soundKeys);
            row.Add(lane);
            _timelineRows.Add(row);
        }

        private void AddSoundTimelineMarkers(
            VisualElement lane,
            float duration,
            SoundTimelineRowKind rowKind,
            IReadOnlyList<StoryActorKeyframeData> soundKeys)
        {
            if (FindCurrentStageLayout() is not { } layout)
                return;

            if (rowKind == SoundTimelineRowKind.Bgm)
            {
                if (layout.SoundTrackEditable?.bgmKeyframes != null)
                {
                    foreach (StoryBgmKeyframeData key in layout.SoundTrackEditable.bgmKeyframes)
                        AddBgmKeyMarker(lane, key, soundKeys);
                }
            }
            else if (layout.SoundTrackEditable?.sfxKeyframes != null)
            {
                foreach (StorySfxKeyframeData key in layout.SoundTrackEditable.sfxKeyframes)
                    AddSfxKeyMarker(lane, key, soundKeys);
            }

            AddTimelinePlayhead(lane, duration, TimelineRowHeight);
        }

        private void AddBgmKeyMarker(VisualElement lane, StoryBgmKeyframeData keyframe, IReadOnlyList<StoryActorKeyframeData> soundKeys)
        {
            if (keyframe == null)
                return;

            StoryActorKeyframeData proxy = GetOrCreateSoundKeyProxy(keyframe);
            int index = IndexOfTimelineKey(soundKeys, proxy);
            float x = keyframe.timeSeconds * _timelinePixelsPerSecond;
            bool selected = IsTimelineKeySelected(proxy, index, StoryActorKeyframeProperty.SoundBgm);
            Color color = keyframe.operation == StoryBgmKeyOperation.Stop
                ? new Color(0.88f, 0.38f, 0.38f)
                : new Color(0.36f, 0.78f, 0.95f);

            var marker = new VisualElement
            {
                tooltip = BuildBgmTooltip(keyframe),
                style =
                {
                    position = Position.Absolute,
                    left = x - TimelineMarkerSize * 0.5f,
                    top = (TimelineRowHeight - TimelineMarkerSize) * 0.5f,
                    width = TimelineMarkerSize,
                    height = TimelineMarkerSize,
                    backgroundColor = new StyleColor(selected ? new Color(1f, 0.82f, 0.22f) : color),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black)
                }
            };

            marker.RegisterCallback<PointerDownEvent>(e =>
            {
                HandleSoundKeyPointerDown(e, proxy, index, keyframe.timeSeconds, StoryActorKeyframeProperty.SoundBgm, SoundTimelineRowKind.Bgm);
            });

            lane.Add(marker);
            _timelineKeyElements[proxy] = marker;
        }

        private void AddSfxKeyMarker(VisualElement lane, StorySfxKeyframeData keyframe, IReadOnlyList<StoryActorKeyframeData> soundKeys)
        {
            if (keyframe == null)
                return;

            StoryActorKeyframeData proxy = GetOrCreateSoundKeyProxy(keyframe);
            int index = IndexOfTimelineKey(soundKeys, proxy);
            float x = keyframe.timeSeconds * _timelinePixelsPerSecond;
            bool selected = IsTimelineKeySelected(proxy, index, StoryActorKeyframeProperty.SoundSfx);
            var marker = new VisualElement
            {
                tooltip = BuildSfxTooltip(keyframe),
                style =
                {
                    position = Position.Absolute,
                    left = x - TimelineMarkerSize * 0.5f,
                    top = (TimelineRowHeight - TimelineMarkerSize) * 0.5f,
                    width = TimelineMarkerSize,
                    height = TimelineMarkerSize,
                    backgroundColor = new StyleColor(selected ? new Color(1f, 0.82f, 0.22f) : new Color(0.92f, 0.66f, 0.32f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black),
                    borderRightColor = new StyleColor(Color.black),
                    borderBottomColor = new StyleColor(Color.black),
                    borderLeftColor = new StyleColor(Color.black)
                }
            };

            marker.RegisterCallback<PointerDownEvent>(e =>
            {
                HandleSoundKeyPointerDown(e, proxy, index, keyframe.timeSeconds, StoryActorKeyframeProperty.SoundSfx, SoundTimelineRowKind.Sfx);
            });

            lane.Add(marker);
            _timelineKeyElements[proxy] = marker;
        }

        private void HandleSoundKeyPointerDown(
            PointerDownEvent e,
            StoryActorKeyframeData proxy,
            int index,
            float keyTime,
            StoryActorKeyframeProperty property,
            SoundTimelineRowKind rowKind)
        {
            if (e.button != 0)
                return;

            SetInteractionContext(InteractionContext.Timeline);
            _timelinePanel?.Focus();
            _selectedSoundRowKind = rowKind;

            if (e.ctrlKey || e.commandKey)
            {
                ToggleTimelineKeySelection(proxy, index, property);
                UpdateSelectedSoundKeysFromTimelineSelection();
                _timelinePlayheadTime = keyTime;
                ApplyTimelinePlayheadSample();
                RefreshActorInspector();
                RefreshTimelinePanel();
                e.StopPropagation();
                return;
            }

            if (_selectedTimelineKeys.Count > 1 && _selectedTimelineKeys.Contains(proxy))
            {
                BeginTimelineGroupKeyDrag(e);
            }
            else
            {
                SelectSingleTimelineKey(proxy, index, property);
                UpdateSelectedSoundKeysFromTimelineSelection();
                _isTimelineKeyDragging = true;
                _draggingTimelineActorKey = null;
                _draggingTimelineSelectionKind = _selectionKind;
                _draggingTimelineKeyIndex = index;
                _draggingTimelineKeyStartTime = keyTime;
                BeginTimelineDragUndo("Move Sound Timeline Key");
                _activeTimelinePointerId = e.pointerId;
                _timelinePanel?.CapturePointer(e.pointerId);
            }

            _timelinePlayheadTime = keyTime;
            ApplyTimelinePlayheadSample();
            RefreshActorInspector();
            RefreshTimelinePanel();
            e.StopPropagation();
        }

        private void SaveSoundTrackToCurrent(Action<StorySoundTrackData> setter, bool refresh = false)
        {
            if (!IsStageAuthoringMode || _currentLine == null)
                return;

            StoryStageLayoutModuleSO layout = GetOrCreateCurrentStageLayout("Edit Sound Timeline", InteractionContext.Timeline);
            if (layout == null)
                return;

            if (!_suppressTimelineUndoRecording)
                RecordTimelineUndo(layout, "Edit Sound Timeline");
            StorySoundTrackData track = layout.SoundTrackEditable;
            track.bgmKeyframes ??= new System.Collections.Generic.List<StoryBgmKeyframeData>();
            track.sfxKeyframes ??= new System.Collections.Generic.List<StorySfxKeyframeData>();
            setter(track);
            track.bgmKeyframes.RemoveAll(k => k == null);
            track.sfxKeyframes.RemoveAll(k => k == null);
            track.bgmKeyframes.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            track.sfxKeyframes.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            ClearSoundTimelineProxyCache();
            MarkLayoutDirty(layout, saveNow: false);

            if (_selectedBgmKey != null && !track.bgmKeyframes.Contains(_selectedBgmKey))
                _selectedBgmKey = null;
            if (_selectedSfxKey != null && !track.sfxKeyframes.Contains(_selectedSfxKey))
                _selectedSfxKey = null;

            if (_selectionKind == StageSelectionKind.Sound)
            {
                if (_selectedBgmKey != null || _selectedSfxKey != null)
                {
                    IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
                    StoryActorKeyframeData selectedProxy = _selectedBgmKey != null
                        ? GetOrCreateSoundKeyProxy(_selectedBgmKey)
                        : GetOrCreateSoundKeyProxy(_selectedSfxKey);
                    _selectedTimelineKeys.Clear();
                    _selectedTimelineKeyIndex = IndexOfTimelineKey(soundKeys, selectedProxy);
                    if (_selectedTimelineKeyIndex >= 0 && selectedProxy != null)
                    {
                _selectedTimelineProperty = selectedProxy.property;
                _selectedTimelineKeys.Add(selectedProxy);
            }
        }
                else
                {
                    NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
                    UpdateSelectedSoundKeysFromTimelineSelection();
                }
            }

            if (refresh)
            {
                RefreshSoundSettingsPanels();
                RefreshActorInspector();
                RefreshTimelinePanel();
            }
        }

        private void AddSoundKeyAtPlayhead(SoundTimelineRowKind rowKind, StoryBgmKeyOperation bgmOperation = StoryBgmKeyOperation.Play)
        {
            AddSoundKeyAtTime(rowKind, Mathf.Max(0f, _timelinePlayheadTime), bgmOperation);
        }

        private void ShowSoundRowKeyContextMenu(StoryActorKeyframeProperty property, float time)
        {
            var menu = new GenericMenu();
            SoundTimelineRowKind rowKind = property == StoryActorKeyframeProperty.SoundBgm
                ? SoundTimelineRowKind.Bgm
                : SoundTimelineRowKind.Sfx;

            bool canCreate = rowKind == SoundTimelineRowKind.Bgm
                ? HasEnumOptions<BgmSounds>()
                : HasEnumOptions<SfxSounds>();
            string label = $"Add/Select {(rowKind == SoundTimelineRowKind.Bgm ? "BGM" : "SFX")} Key @ {time:0.00}s";
            if (canCreate)
            {
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    _selectedTimelineProperty = property;
                    _timelinePlayheadTime = time;
                    AddSoundKeyAtTime(rowKind, time, StoryBgmKeyOperation.Play);
                    ApplyTimelinePlayheadSample();
                    RefreshActorInspector();
                    RefreshTimelinePanel();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(
                    rowKind == SoundTimelineRowKind.Bgm
                        ? "Add/Select BGM Key (No BgmSounds enum value)"
                        : "Add/Select SFX Key (No SfxSounds enum value)"));
            }
            menu.ShowAsContext();
        }

        private void AddSoundKeyAtTime(SoundTimelineRowKind rowKind, float time, StoryBgmKeyOperation bgmOperation = StoryBgmKeyOperation.Play)
        {
            SaveSoundTrackToCurrent(track =>
            {
                if (rowKind == SoundTimelineRowKind.Bgm)
                {
                    StoryBgmKeyframeData key = FindBgmKeyAtTime(track, time);
                    if (key == null)
                    {
                        key = new StoryBgmKeyframeData
                        {
                            timeSeconds = time,
                            normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration())),
                            operation = bgmOperation
                        };
                        track.bgmKeyframes.Add(key);
                    }

                    _selectedBgmKey = key;
                    _selectedSfxKey = null;
                    _selectedSoundRowKind = SoundTimelineRowKind.Bgm;
                }
                else
                {
                    StorySfxKeyframeData key = FindSfxKeyAtTime(track, time);
                    if (key == null)
                    {
                        key = new StorySfxKeyframeData
                        {
                            timeSeconds = time,
                            normalizedTime = Mathf.Clamp01(time / Mathf.Max(1f, GetTimelineDuration()))
                        };
                        track.sfxKeyframes.Add(key);
                    }

                    _selectedSfxKey = key;
                    _selectedBgmKey = null;
                    _selectedSoundRowKind = SoundTimelineRowKind.Sfx;
                }
            }, refresh: true);
        }

        private void MoveSoundTimelineGroupKeys(float currentPanelX)
        {
            float deltaTime = (currentPanelX - _timelineKeyDragStartPanelX) / Mathf.Max(1f, _timelinePixelsPerSecond);
            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            var proposedTimes = new Dictionary<StoryActorKeyframeData, float>();
            foreach (TimelineKeyDragState state in _timelineKeyDragStates)
                proposedTimes[state.key] = Mathf.Max(0f, state.startTime + deltaTime);

            bool hasCollision = HasTimelineKeyCollision(soundKeys, proposedTimes);
            SaveSoundTrackToCurrent(track =>
            {
                foreach (TimelineKeyDragState state in _timelineKeyDragStates)
                {
                    float targetTime = hasCollision ? state.startTime : proposedTimes[state.key];
                    StoryBgmKeyframeData bgmKey = ResolveBgmKeyFromProxy(state.key);
                    if (bgmKey != null)
                    {
                        bgmKey.timeSeconds = targetTime;
                        bgmKey.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                        continue;
                    }

                    StorySfxKeyframeData sfxKey = ResolveSfxKeyFromProxy(state.key);
                    if (sfxKey == null)
                        continue;

                    sfxKey.timeSeconds = targetTime;
                    sfxKey.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                }
            }, refresh: false);

            NormalizeTimelineSelectionFromSet(_selectedTimelineProperty);
            UpdateSelectedSoundKeysFromTimelineSelection();
            RefreshTimelinePanel();
        }

        private void SetSoundTimelineKeyTime(int keyIndex, float timeSeconds, bool refresh)
        {
            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            if (soundKeys == null || keyIndex < 0 || keyIndex >= soundKeys.Count)
                return;

            StoryActorKeyframeData proxy = soundKeys[keyIndex];
            float targetTime = Mathf.Max(0f, timeSeconds);
            if (HasTimelineKeyCollision(soundKeys, proxy, targetTime, null))
                targetTime = _draggingTimelineKeyStartTime;

            SaveSoundTrackToCurrent(track =>
            {
                StoryBgmKeyframeData bgmKey = ResolveBgmKeyFromProxy(proxy);
                if (bgmKey != null)
                {
                    bgmKey.timeSeconds = targetTime;
                    bgmKey.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
                    return;
                }

                StorySfxKeyframeData sfxKey = ResolveSfxKeyFromProxy(proxy);
                if (sfxKey == null)
                    return;

                sfxKey.timeSeconds = targetTime;
                sfxKey.normalizedTime = Mathf.Clamp01(targetTime / Mathf.Max(1f, GetTimelineDuration()));
            }, refresh);
        }

        private static StoryBgmKeyframeData FindBgmKeyAtTime(StorySoundTrackData track, float time)
        {
            if (track?.bgmKeyframes == null)
                return null;

            foreach (StoryBgmKeyframeData key in track.bgmKeyframes)
            {
                if (key != null && Mathf.Approximately(key.timeSeconds, time))
                    return key;
            }

            return null;
        }

        private static StorySfxKeyframeData FindSfxKeyAtTime(StorySoundTrackData track, float time)
        {
            if (track?.sfxKeyframes == null)
                return null;

            foreach (StorySfxKeyframeData key in track.sfxKeyframes)
            {
                if (key != null && Mathf.Approximately(key.timeSeconds, time))
                    return key;
            }

            return null;
        }

        private bool DeleteSelectedSoundKey()
        {
            if (_selectionKind != StageSelectionKind.Sound)
                return false;

            IReadOnlyList<StoryActorKeyframeData> soundKeys = GetCurrentSoundTimelineKeyframes();
            List<StoryActorKeyframeData> selected = GetSelectedTimelineKeys(soundKeys);
            if (selected.Count == 0)
                return false;

            bool removed = false;
            SaveSoundTrackToCurrent(track =>
            {
                foreach (StoryActorKeyframeData proxy in selected)
                {
                    StoryBgmKeyframeData bgmKey = ResolveBgmKeyFromProxy(proxy);
                    if (bgmKey != null)
                        removed |= track.bgmKeyframes.Remove(bgmKey);

                    StorySfxKeyframeData sfxKey = ResolveSfxKeyFromProxy(proxy);
                    if (sfxKey != null)
                        removed |= track.sfxKeyframes.Remove(sfxKey);
                }

                _selectedBgmKey = null;
                _selectedSfxKey = null;
                _selectedTimelineKeyIndex = -1;
                _selectedTimelineSegmentKeyIndex = -1;
                _selectedTimelineKeys.Clear();
            }, refresh: removed);

            if (removed)
            {
                ClearSoundTimelineProxyCache();
                RefreshTimelinePanel();
            }

            return removed;
        }

        private float GetSoundTrackDuration(StoryStageLayoutModuleSO layout)
        {
            if (layout?.SoundTrackEditable == null)
                return 0f;

            float duration = 0f;
            if (layout.SoundTrackEditable.bgmKeyframes != null)
            {
                foreach (StoryBgmKeyframeData key in layout.SoundTrackEditable.bgmKeyframes)
                {
                    if (key != null)
                        duration = Mathf.Max(duration, key.timeSeconds);
                }
            }

            if (layout.SoundTrackEditable.sfxKeyframes != null)
            {
                foreach (StorySfxKeyframeData key in layout.SoundTrackEditable.sfxKeyframes)
                {
                    if (key != null)
                        duration = Mathf.Max(duration, key.timeSeconds);
                }
            }

            return duration;
        }
    }
}
