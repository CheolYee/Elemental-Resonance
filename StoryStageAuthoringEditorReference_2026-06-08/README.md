# Story Stage Authoring Editor Reference

Curated on `2026-06-08` from `Bar_Tycoon`.

## Start Here

If you are handing this bundle to another coding agent, start with:

- `CLAUDE_CODE_STAGE_AUTHORING_WALKTHROUGH.md`
- `scripts/Show-StageAuthoringFlow.ps1`

Script example:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section All
```

## Goal

This package extracts the editor-side code that drives the Story stage authoring workflow, with focus on:

- timeline UI and playback
- keyframe storage and editing
- right-side inspector behavior
- stage preview interaction for actor, background, camera, and sound

This is a reference bundle, not a guaranteed drop-in package. Some project-specific types and third-party dependencies remain intentionally unmodified so the original architecture is easier to study.

## What Is The Core System

The core authoring surface is `StoryPreviewWindow`, implemented as a partial `EditorWindow`.

- `StoryPreviewWindow.cs` owns lifecycle, selection context, undo context, and shared fields.
- `StoryPreviewWindow.Layout.cs` builds the UI Toolkit hierarchy.
- `StoryPreviewWindow.StageWorld.cs` manages pan, zoom, visible aspect, and camera-frame geometry.
- `StoryPreviewWindow.Actors.cs` and `StoryPreviewWindow.Background.cs` handle direct manipulation in the preview.
- `StoryPreviewWindow.Inspector.cs` builds the selection-driven inspector and writes changes back to authored data.
- `StoryPreviewWindow.Timeline.cs` builds the timeline, edits keys, drives playhead sampling, and handles timeline shortcuts.
- `StoryPreviewWindow.Sound.cs` adapts BGM/SFX key data into the same timeline selection model.
- `StoryPreviewWindow.Playback.cs` reconstructs stage state for the selected line and line-to-line previews.

## Source Of Truth

The authoring system does not keep a separate preview-only stage asset.

- Line-level authored data lives in `StoryLineSO`.
- Stage data lives in `StoryStageLayoutModuleSO`, inside `StoryLineSO.modules`.
- Absolute state at line entry is stored in snapshot data:
  - `StoryActorStateData`
  - `StoryBackgroundStateData`
  - `StoryCameraTrackData.defaultState`
- Intra-line animation is stored in tracks:
  - `StoryActorTrackData`
  - `StoryBackgroundTrackData`
  - `StoryCameraTrackData`
  - `StorySoundTrackData`
- Generic visual keyframes use `StoryActorKeyframeData`.
- Sound rows keep native BGM/SFX key types, then map them to temporary `StoryActorKeyframeData` proxies for unified timeline selection.

## Main Design Patterns

- Selection-driven UI: the inspector and timeline both rebuild from `_selectionKind`.
- Shared sampler: `StoryTransitionSampler` evaluates preview values so editor and runtime sampling logic stay aligned.
- Context-aware undo: stage edits and timeline edits are separated with `InteractionContext`.
- Partial-class decomposition: one window, multiple focused files, instead of one giant monolith.
- Event-style editor flow: UI callbacks mutate source data, refresh the preview, then rebuild dependent panels.

## Architecture Summary

1. `StoryGraphEditorWindow` or `StoryLineSOEditor` selects a line.
2. `StoryPreviewWindow.NotifyLineSelected(...)` updates the preview target.
3. `StoryPreviewWindow.Playback.cs` rebuilds accumulated stage state up to that line.
4. Stage selection chooses one authoring target: actor, background, camera, or sound.
5. Inspector edits write back into `StoryStageLayoutModuleSO` snapshot data or track data.
6. Timeline edits update keyframes in the same module.
7. `ApplyTimelinePlayheadSample()` resamples authored data and refreshes the preview scene.

## Files In This Bundle

See `FILE_MAP.md` for the full grouped file list.

## Porting Notes

- The preview is built with UI Toolkit, not IMGUI, except for a few legacy helper panels.
- The timeline is custom; it does not use Unity Timeline or Playables.
- The system assumes one `StoryStageLayoutModuleSO` per line as the stage source of truth.
- Sound authoring depends on `Gamelib.SoundSystem` enums and `Gamelib.EventSystem`.
- Some scriptable definitions such as `CharacterDefinitionSO` and `BackgroundDefinitionSO` are included because the editor depends on them for preview rendering and keys.

## Recommended Read Order

1. `source/Editor/StoryPreviewWindow.cs`
2. `source/Editor/StoryPreviewWindow.Layout.cs`
3. `source/Editor/StoryPreviewWindow.StageWorld.cs`
4. `source/Editor/StoryPreviewWindow.Playback.cs`
5. `source/Editor/StoryPreviewWindow.Inspector.cs`
6. `source/Editor/StoryPreviewWindow.Timeline.cs`
7. `source/Shared/StoryTransitionSampler.cs`
8. `source/Data/Modules/*`

## Porting Checklist

- Replace project-specific asset definitions first:
  - `CharacterDefinitionSO`
  - `BackgroundDefinitionSO`
  - `StoryEpisodeSO`
  - `StoryLineSO`
- Decide whether you want snapshot-plus-track authoring, or a pure track model.
- Preserve the split between:
  - line entry snapshot state
  - intra-line keyframe state
- Keep the sampler pure if you want editor/runtime parity.
- If you port sound rows, either keep the proxy-key pattern or give sound its own timeline selection model.

## Known Project-Specific Dependencies

- `Gamelib.SoundSystem`
- `Gamelib.EventSystem`
- Story-specific enums under `Shared/Types`
- Existing story graph/editor windows that open the preview and pass selected lines
