# File Map

## Agent Guide

- `CLAUDE_CODE_STAGE_AUTHORING_WALKTHROUGH.md`
  - detailed explanation of selection, timeline row creation, key editing, and inspector rebuild flow
- `scripts/Show-StageAuthoringFlow.ps1`
  - prints the current file/line map for the main entry points and editing paths

## Core Editor Window

- `source/Editor/StoryPreviewWindow.cs`
  - shared state, lifecycle, selection context, undo routing, external entry points
- `source/Editor/StoryPreviewWindow.Layout.cs`
  - UI Toolkit hierarchy, stage wrapper, timeline panel placement, inspector layout
- `source/Editor/StoryPreviewWindow.StageWorld.cs`
  - pan/zoom, visible frame sizing, GameView size sampling, camera-frame geometry
- `source/Editor/StoryPreviewWindow.Actors.cs`
  - actor rendering, selection, drag, scale handles, camera gizmo interactions
- `source/Editor/StoryPreviewWindow.Background.cs`
  - background rendering and direct manipulation
- `source/Editor/StoryPreviewWindow.Inspector.cs`
  - authoring tools, actor/background/camera inspector builders, track and snapshot persistence helpers
- `source/Editor/StoryPreviewWindow.Playback.cs`
  - line selection playback, accumulated stage-state rebuild, transition preview
- `source/Editor/StoryPreviewWindow.Timeline.cs`
  - timeline UI, key rendering, selection, copy/paste, motion presets, sampling, shortcuts
- `source/Editor/StoryPreviewWindow.Sound.cs`
  - sound rows, BGM/SFX key inspectors, sound proxy-key mapping

## Optional Editor Integration

- `source/Editor/StoryMotionPresetLibraryWindow.cs`
  - preset asset browser used by the timeline
- `source/Editor/StoryGraphEditorWindow.cs`
  - graph window entry that opens the preview and forwards line selection
- `source/Editor/StoryGraphInspectorPanel.cs`
  - graph-side line inspector
- `source/Editor/StoryLineSOEditor.cs`
  - inspector-side line helper
- `source/Editor/StoryLineIdHelperGUI.cs`
  - shared line-id helper UI
- `source/Editor/StoryEditorUtility.cs`
  - editor-side asset mutation helpers used by the story editor

## Authoring Data Model

- `source/Data/StoryLineSO.cs`
- `source/Data/StoryEpisodeSO.cs`
- `source/Data/StoryModuleSO.cs`
- `source/Data/CharacterDefinitionSO.cs`
- `source/Data/BackgroundDefinitionSO.cs`

## Stage Module And Track Types

- `source/Data/Modules/StoryStageLayoutModuleSO.cs`
- `source/Data/Modules/StoryActorStateData.cs`
- `source/Data/Modules/StoryBackgroundStateData.cs`
- `source/Data/Modules/StoryCameraStateData.cs`
- `source/Data/Modules/StoryActorTrackData.cs`
- `source/Data/Modules/StoryBackgroundTrackData.cs`
- `source/Data/Modules/StoryCameraTrackData.cs`
- `source/Data/Modules/StoryActorKeyframeData.cs`
- `source/Data/Modules/StorySoundTrackData.cs`
- `source/Data/Modules/StoryMotionPresetSO.cs`
- `source/Data/Modules/StoryActorMotionProfileData.cs`
- `source/Data/Modules/StoryCharacterEnterModuleSO.cs`

## Shared Logic

- `source/Shared/StoryTransitionSampler.cs`
- `source/Shared/Aspect/StoryAspectSettingsSO.cs`
- `source/Shared/Camera/StoryCameraInitSettingsSO.cs`
- `source/Shared/Attributes/StoryModuleMetadataAttribute.cs`
- `source/Shared/Types/*`

## Key Relationships

- `StoryPreviewWindow.*` reads and writes `StoryStageLayoutModuleSO`.
- `StoryStageLayoutModuleSO` stores both snapshot state and track state.
- `StoryTransitionSampler` evaluates tracks at the current playhead time.
- `StoryMotionPresetLibraryWindow` calls back into `StoryPreviewWindow`.
- `StoryGraphEditorWindow` calls `StoryPreviewWindow.NotifyLineSelected(...)`.
