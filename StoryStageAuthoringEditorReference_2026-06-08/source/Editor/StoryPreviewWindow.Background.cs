using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        private void RefreshBackgroundLayer()
        {
            if (_backgroundLayer == null)
                return;

            _backgroundLayer.Clear();

            if (_bgState == null || !_bgState.HasBackground || !_bgState.visible)
                return;

            VisualElement bg = CreateBackgroundElement(_bgState);
            PositionBackgroundElement(bg, _bgState);
            _backgroundLayer.Add(bg);
        }

        private void RegisterBackgroundInteraction(VisualElement el)
        {
            el.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!IsStageAuthoringMode || e.button != 0) return;
                if (IsBackgroundRecordSelectionLocked()) return;
                if (_bgState == null || !_bgState.HasBackground) return;
                SetInteractionContext(InteractionContext.Stage);
                FocusStageWorkspace();
                if (_timelineIsPlaying) StopTimelinePlayback();
                SelectBackground();
                _isDraggingBackground = true;
                _bgDragStartPanelPos  = e.position;
                _bgDragStartStagePos  = _bgState.stageLocalPosition;
                el.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_isDraggingBackground) return;

                Vector2 panelDelta      = ((Vector2)e.position - _bgDragStartPanelPos) / _stageZoom;
                float   worldUnitsPerPx = ResolvePreviewCameraWorldWidth() / DefaultUnitPixels;
                _bgState.stageLocalPosition = new Vector2(
                    _bgDragStartStagePos.x + panelDelta.x * worldUnitsPerPx,
                    _bgDragStartStagePos.y - panelDelta.y * worldUnitsPerPx); // Y 반전

                PositionBackgroundElement(el, _bgState);
                e.StopPropagation();
            });

            el.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_isDraggingBackground) return;
                _isDraggingBackground = false;
                el.ReleasePointer(e.pointerId);

                Vector2 panelDelta      = ((Vector2)e.position - _bgDragStartPanelPos) / _stageZoom;
                float   worldUnitsPerPx = ResolvePreviewCameraWorldWidth() / DefaultUnitPixels;
                Vector2 finalPos = new Vector2(
                    _bgDragStartStagePos.x + panelDelta.x * worldUnitsPerPx,
                    _bgDragStartStagePos.y - panelDelta.y * worldUnitsPerPx);

                if (_timelineRecordEnabled && _selectionKind == StageSelectionKind.Background)
                {
                    var recordState = new StoryBackgroundStateData { stageLocalPosition = finalPos };
                    AddOrUpdateBackgroundKey(recordState, StoryActorKeyframeProperty.BackgroundPosition,
                        _timelinePlayheadTime, createIfMissing: true, selectKey: false, refreshInspector: false);
                    ApplyTimelinePlayheadSample();
                }
                else if (!TryApplySelectedTimelineKeyFromBackground(finalPos))
                {
                    SaveBackgroundStateToCurrent(bg => bg.stageLocalPosition = finalPos);
                }

                RefreshActorInspector();
                e.StopPropagation();
            });
        }

        private VisualElement CreateBackgroundElement(StoryBackgroundStateData state)
        {
            var pickMode = PickingMode.Ignore;

            var sprite = StoryStageVisualSizing.ResolveBackgroundSprite(state);
            if (sprite != null)
            {
                return new VisualElement
                {
                    pickingMode = pickMode,
                    style =
                    {
                        position = Position.Absolute,
                        backgroundImage = new StyleBackground(sprite),
                        backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover)),
                        backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center)),
                        backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center)),
                        unityBackgroundImageTintColor = new StyleColor(state.EffectiveTint)
                    }
                };
            }

            var placeholder = new VisualElement
            {
                pickingMode = pickMode,
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new StyleColor(new Color(0.12f, 0.13f, 0.15f, 0.65f)),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderRightColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderBottomColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f)),
                    borderLeftColor = new StyleColor(new Color(0.54f, 0.62f, 0.72f, 0.45f))
                }
            };

            placeholder.Add(new Label(state.ResolvedBackgroundKey)
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    color = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 0.65f))
                }
            });

            return placeholder;
        }

        private void PositionBackgroundElement(VisualElement el, StoryBackgroundStateData state)
        {
            float camW = DefaultUnitPixels;
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();
            float pixelsPerWorld = camW / ResolvePreviewCameraWorldWidth();

            Vector2 panelCenter = new Vector2(camW * 0.5f, camH * 0.5f);
            Vector2 parallaxBasePixel = panelCenter;

            float pf = state?.EffectiveParallaxFactor ?? 0f;
            if (pf > 0f)
            {
                Vector2 cameraStageLocal = ResolvePreviewCameraFocusOffset();
                Vector2 cameraPixel = new Vector2(
                    camW * 0.5f + cameraStageLocal.x * pixelsPerWorld,
                    camH * 0.5f - cameraStageLocal.y * pixelsPerWorld);
                parallaxBasePixel = Vector2.Lerp(panelCenter, cameraPixel, pf);
            }

            Rect rect = StoryStageVisualSizing.CalculateBackgroundPreviewRect(
                state,
                StoryStageVisualSizing.ResolveBackgroundSprite(state),
                parallaxBasePixel,
                pixelsPerWorld);

            el.style.width = rect.width;
            el.style.height = rect.height;
            el.style.left = rect.x;
            el.style.top = rect.y;
        }

        private static Vector2 ResolveNonZeroScale(Vector2 scale) =>
            Mathf.Approximately(scale.x, 0f) && Mathf.Approximately(scale.y, 0f)
                ? Vector2.one
                : scale;
    }
}
