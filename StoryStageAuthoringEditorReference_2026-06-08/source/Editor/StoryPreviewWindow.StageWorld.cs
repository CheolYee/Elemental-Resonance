using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ── Stage World 초기화 / Transform ────────────

        private void OnStageWrapperResized(GeometryChangedEvent e)
        {
            if (e.newRect is { width: > 0, height: > 0 })
                InitWorldView(e.newRect.width, e.newRect.height);
        }

        /// <summary>
        /// 창 크기에 맞게 zoom 자동 설정 후 카메라 프레임을 중앙에 배치한다.
        /// 이후 pan/zoom 조작은 ApplyWorldTransform() 만 호출하면 된다.
        /// </summary>
        private void InitWorldView(float wrapW, float wrapH)
        {
            if (_stageWorld == null || wrapW <= 0 || wrapH <= 0) return;
            if (_isTransitionPreviewing) return;

            FitCameraFrameToWrapper(wrapW, wrapH, IsRuntimePreviewMode ? RuntimeFitFill : AuthoringFitFill);
        }

        private void FitRuntimePreviewToWrapper()
        {
            float ww = _stageWrapper?.resolvedStyle.width ?? 0f;
            float wh = _stageWrapper?.resolvedStyle.height ?? 0f;
            if (ww <= 0f || wh <= 0f)
                return;

            FitCameraFrameToWrapper(ww, wh, RuntimeFitFill);
        }

        private void FitCameraFrameToWrapper(float wrapW, float wrapH, float fill)
        {
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();

            // 카메라 프레임이 래퍼 안에 항상 들어오는 최대 zoom.
            float fitZoom  = Mathf.Min((wrapW * fill) / DefaultUnitPixels, (wrapH * fill) / camH);
            fitZoom = Mathf.Min(fitZoom, 1f);
            _stageZoom     = Mathf.Clamp(fitZoom, MinZoom, MaxZoom);

            // 카메라 프레임 시각적 중심 = 래퍼 중심
            _stagePanOffset = new Vector2(
                wrapW * 0.5f - DefaultUnitPixels * _stageZoom * 0.5f,
                wrapH * 0.5f - camH              * _stageZoom * 0.5f
            );

            ApplyWorldTransform();
            UpdateCameraFrameGeometry();
        }

        private void ApplyWorldTransform()
        {
            if (_stageWorld == null) return;
            _stageWorld.style.translate = new Vector3(_stagePanOffset.x, _stagePanOffset.y, 0);
            _stageWorld.style.scale    = new Vector3(_stageZoom, _stageZoom, 1);
            UpdateLetterboxOverlay();
        }

        /// <summary>GameView 해상도 변경 시 카메라 프레임 가이드와 배경 레이어 크기를 갱신한다.</summary>
        private void UpdateCameraFrameGeometry()
        {
            float camH = DefaultUnitPixels / GetStoryVisibleAspect();

            if (_stageWorld != null)
            {
                _stageWorld.style.width = DefaultUnitPixels;
                _stageWorld.style.height = camH;
            }

            if (_cameraFrameGuide != null)
            {
                _cameraFrameGuide.style.width  = DefaultUnitPixels;
                _cameraFrameGuide.style.height = camH;
            }
            if (_backgroundLayer != null)
            {
                _backgroundLayer.style.left = 0;
                _backgroundLayer.style.top = 0;
                _backgroundLayer.style.width = DefaultUnitPixels;
                _backgroundLayer.style.height = camH;
            }
            if (_cameraGizmoLayer != null)
            {
                _cameraGizmoLayer.style.left = 0;
                _cameraGizmoLayer.style.top = 0;
                _cameraGizmoLayer.style.width = DefaultUnitPixels;
                _cameraGizmoLayer.style.height = camH;
            }

            ApplyCameraFrameModeStyles();
            RefreshBackgroundLayer();
        }

        private void ApplyCameraFrameModeStyles()
        {
            bool isRuntime = IsRuntimePreviewMode;

            if (_stageWrapper != null)
                _stageWrapper.style.backgroundColor = new StyleColor(isRuntime ? Color.black : new Color(0.04f, 0.04f, 0.05f));

            if (_stageWorld != null)
                _stageWorld.style.overflow = isRuntime ? Overflow.Hidden : Overflow.Visible;

            if (_cameraFrameGuide != null)
            {
                _cameraFrameGuide.style.overflow = isRuntime ? Overflow.Hidden : Overflow.Visible;
                // 파란 Reference Frame 제거: border 항상 0
                _cameraFrameGuide.style.borderTopWidth    = 0f;
                _cameraFrameGuide.style.borderRightWidth  = 0f;
                _cameraFrameGuide.style.borderBottomWidth = 0f;
                _cameraFrameGuide.style.borderLeftWidth   = 0f;
            }

            if (_authoringGridLayer != null)
                _authoringGridLayer.style.display = isRuntime ? DisplayStyle.None : DisplayStyle.Flex;

            UpdateLetterboxOverlay();
        }

        private void RefreshFocusPreviewGuide() { }

        private float GetPhysicalAspect()
        {
            if (_renderResolution.x <= 0 || _renderResolution.y <= 0)
                return (float)FallbackRenderWidth / FallbackRenderHeight;

            return Mathf.Clamp((float)_renderResolution.x / _renderResolution.y, 0.2f, 4f);
        }

        private float GetStoryVisibleAspect()
        {
            float physical = GetPhysicalAspect();
            if (_previewAspectSettings == null)
                return physical;
            return physical * _previewAspectSettings.VisibleWidthRatio;
        }

        // ── Pan / Zoom 이벤트 ─────────────────────────

        private void RegisterPanZoomCallbacks()
        {
            _stageWrapper.RegisterCallback<WheelEvent>(OnStageWheel);
            _stageWrapper.RegisterCallback<PointerDownEvent>(OnStagePanDown);
            _stageWrapper.RegisterCallback<PointerMoveEvent>(OnStagePanMove);
            _stageWrapper.RegisterCallback<PointerUpEvent>(OnStagePanUp);
            // 빈 공간 클릭 시 selection clear (actor/camera/bg 가 StopPropagation 하면 여기 안 옴)
            _stageWrapper.RegisterCallback<PointerDownEvent>(OnStageEmptyClick);
        }

        private void OnStageCameraGizmoPointerDown(PointerDownEvent e)
        {
            if (previewMode != PreviewMode.StageAuthoring || e.button != 0)
                return;

            if (IsPointerTargetWithinActor(e.target) || !IsCameraGizmoHandleHit(e.position))
                return;

            SetInteractionContext(InteractionContext.Stage);
            FocusStageWorkspace();
            if (TryBeginCameraGizmoDrag(_stageWrapper, e.pointerId, e.position))
                e.StopPropagation();
        }

        private void OnStageCameraGizmoPointerMove(PointerMoveEvent e)
        {
            if (!IsCameraGizmoDragPointer(e.pointerId))
                return;

            UpdateCameraGizmoDragVisual(e.position);
            e.StopPropagation();
        }

        private void OnStageCameraGizmoPointerUp(PointerUpEvent e)
        {
            if (!IsCameraGizmoDragPointer(e.pointerId))
                return;

            EndCameraGizmoDrag(_stageWrapper, e.pointerId, e.position);
            e.StopPropagation();
        }

        private void OnStageCameraGizmoPointerCaptureOut(PointerCaptureOutEvent _)
        {
            if (!_isDraggingCamera)
                return;

            CancelCameraGizmoDrag(_stageWrapper, _cameraDragPointerId);
        }

        private void OnStageEmptyClick(PointerDownEvent e)
        {
            if (previewMode != PreviewMode.StageAuthoring || e.button != 0) return;
            SetInteractionContext(InteractionContext.Stage);
            FocusStageWorkspace();
            ClearStageSelection();
            HighlightSelectedActor();
            RefreshActorInspector();
            Repaint();
        }

        private void OnStageWheel(WheelEvent e)
        {
            if (previewMode != PreviewMode.StageAuthoring) return;
            SetInteractionContext(InteractionContext.Stage);

            if (e.ctrlKey)
            {
                // 마우스 위치 기준 zoom
                Vector2 mouseLocal = e.localMousePosition;
                Vector2 mouseWorld = (mouseLocal - _stagePanOffset) / _stageZoom;
                float   delta      = -e.delta.y * ZoomStep;
                _stageZoom      = Mathf.Clamp(_stageZoom + delta, MinZoom, MaxZoom);
                _stagePanOffset = mouseLocal - mouseWorld * _stageZoom;
            }
            else
            {
                _stagePanOffset -= (Vector2)e.delta * 2f;
            }

            ApplyWorldTransform();
            e.StopPropagation();
        }

        private void OnStagePanDown(PointerDownEvent e)
        {
            if (previewMode != PreviewMode.StageAuthoring) return;
            if (e.button != 2) return;

            SetInteractionContext(InteractionContext.Stage);
            FocusStageWorkspace();
            _isPanDragging      = true;
            _panDragStartMouse  = e.localPosition;
            _panDragStartOffset = _stagePanOffset;
            _stageWrapper.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnStagePanMove(PointerMoveEvent e)
        {
            if (!_isPanDragging) return;
            _stagePanOffset = _panDragStartOffset + ((Vector2)e.localPosition - _panDragStartMouse);
            ApplyWorldTransform();
            e.StopPropagation();
        }

        private void OnStagePanUp(PointerUpEvent e)
        {
            if (!_isPanDragging || e.button != 2) return;
            _isPanDragging = false;
            _stageWrapper.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }

        /// <summary>panel 좌표(PointerEvent.position)를 stage world 좌표로 변환한다.</summary>
        internal Vector2 PanelToWorld(Vector2 panelPos) =>
            (panelPos - _stagePanOffset) / _stageZoom;

        // ── GameView 해상도 감지 ───────────────────────

        private void RefreshRenderAreaFromGameView()
        {
            if (!TryGetGameViewReferenceSize(out Vector2Int size, out string source))
            {
                size   = new Vector2Int(FallbackRenderWidth, FallbackRenderHeight);
                source = "Fallback";
            }

            bool changed        = size != _renderResolution || source != _renderResolutionSource;
            _renderResolution       = size;
            _renderResolutionSource = source;
            RefreshRenderAreaLabel();

            if (!changed) return;

            UpdateCameraFrameGeometry();

            float ww = _stageWrapper?.resolvedStyle.width  ?? 0;
            float wh = _stageWrapper?.resolvedStyle.height ?? 0;
            if (ww > 0 && wh > 0) InitWorldView(ww, wh);

            RebuildActorLayer();
            RefreshAspectMetricsDisplay();
            Repaint();
        }

        private void RefreshRenderAreaLabel()
        {
            if (_renderAreaLabel != null)
                _renderAreaLabel.text = $"Render {_renderResolution.x}x{_renderResolution.y} ({_renderResolutionSource})";
        }

        // ── GameView 크기 반사 ─────────────────────────

        private static bool TryGetGameViewReferenceSize(out Vector2Int size, out string source)
        {
            size = default; source = string.Empty;
            try
            {
                Type gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                if (gvType == null) return false;

                const BindingFlags f = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                MethodInfo getSize = gvType.GetMethod("GetSizeOfMainGameView", f);
                if (getSize != null && TryReadVector2(getSize.Invoke(null, null), out Vector2 ms))
                    if (TryUseSize(ms, "Game View", out size, out source)) return true;

                MethodInfo getMain = gvType.GetMethod("GetMainGameView", f);
                object     gv      = getMain?.Invoke(null, null);
                if (gv == null) return false;

                PropertyInfo curSizeProp = gvType.GetProperty("currentGameViewSize", f);
                if (TryReadGameViewSizeObject(curSizeProp?.GetValue(gv), out size))
                { source = "Game View Size"; return true; }

                PropertyInfo tgtSizeProp = gvType.GetProperty("targetSize", f);
                return TryReadVector2(tgtSizeProp?.GetValue(gv), out Vector2 ts)
                    && TryUseSize(ts, "Game View Target", out size, out source);
            }
            catch { return false; }
        }

        private static bool TryReadVector2(object value, out Vector2 result)
        { if (value is Vector2 v) { result = v; return true; } result = default; return false; }

        private static bool TryUseSize(Vector2 raw, string rawSrc, out Vector2Int size, out string source)
        {
            size = default; source = string.Empty;
            int w = Mathf.RoundToInt(raw.x), h = Mathf.RoundToInt(raw.y);
            if (w <= 0 || h <= 0) return false;
            size = new Vector2Int(w, h); source = rawSrc; return true;
        }

        private static bool TryReadGameViewSizeObject(object gvs, out Vector2Int size)
        {
            size = default;
            if (gvs == null) return false;
            Type t = gvs.GetType();
            int w = ReadIntMember(gvs, t, "width"),  h = ReadIntMember(gvs, t, "height");
            if (w <= 0 || h <= 0) { w = ReadIntMember(gvs, t, "baseWidth"); h = ReadIntMember(gvs, t, "baseHeight"); }
            if (w <= 0 || h <= 0) return false;
            size = new Vector2Int(w, h); return true;
        }

        private static int ReadIntMember(object target, Type type, string name)
        {
            const BindingFlags flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.GetProperty(name, flag) is { } p && p.GetIndexParameters().Length == 0 && p.GetValue(target) is int pi) return pi;
            if (type.GetField(name, flag)    is { } f && f.GetValue(target) is int fi) return fi;
            return 0;
        }
    }
}
