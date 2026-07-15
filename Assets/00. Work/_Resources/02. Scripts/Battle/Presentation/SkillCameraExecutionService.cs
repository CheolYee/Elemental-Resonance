using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using Unity.Cinemachine;
using UnityEngine;
using DelayType = Cysharp.Threading.Tasks.DelayType;

namespace Battle.Presentation
{
    public sealed class SkillCameraExecutionService
    {
        private readonly CinemachineCamera        _skillCamera;
        private readonly CinemachineImpulseSource _impulseSource;

        public SkillCameraExecutionService(CinemachineCamera skillCamera, CinemachineImpulseSource impulseSource)
        {
            _skillCamera   = skillCamera;
            _impulseSource = impulseSource;
        }

        public async UniTask PlayAsync(SkillSingleTrackData cameraTrack, SkillPresentationPlaybackContext context, float effectiveDuration, CancellationToken token)
        {
            if (cameraTrack?.keyframes == null || cameraTrack.keyframes.Count == 0) return;

            bool needsSkillCamera = false;
            foreach (var k in cameraTrack.keyframes)
            {
                if (k.property is SkillKeyframeProperty.CamPosition
                               or SkillKeyframeProperty.CamRotation
                               or SkillKeyframeProperty.CamZoom)
                {
                    needsSkillCamera = true;
                    break;
                }
            }

            // Base transform 캡처 (Priority 올리기 전)
            Vector3    basePosition = _skillCamera != null ? _skillCamera.transform.position         : Vector3.zero;
            Quaternion baseRotation = _skillCamera != null ? _skillCamera.transform.rotation         : Quaternion.identity;
            float      baseFov      = _skillCamera != null ? _skillCamera.Lens.FieldOfView           : 60f;

            if (needsSkillCamera && _skillCamera != null)
                _skillCamera.Priority = 20;

            try
            {
                var posKeys   = cameraTrack.GetCached(SkillKeyframeProperty.CamPosition);
                var rotKeys   = cameraTrack.GetCached(SkillKeyframeProperty.CamRotation);
                var zoomKeys  = cameraTrack.GetCached(SkillKeyframeProperty.CamZoom);
                var shakeKeys = cameraTrack.GetCached(SkillKeyframeProperty.CamShake);

                if (posKeys.Count > 0 || rotKeys.Count > 0 || zoomKeys.Count > 0 || shakeKeys.Count > 0)
                    await UniTask.WhenAll(
                        posKeys.Count   > 0 ? PlayPositionAsync(posKeys, basePosition, context, token) : UniTask.CompletedTask,
                        rotKeys.Count   > 0 ? PlayRotationAsync(rotKeys, baseRotation, context, token) : UniTask.CompletedTask,
                        zoomKeys.Count  > 0 ? PlayZoomAsync(zoomKeys, baseFov, context, token)         : UniTask.CompletedTask,
                        shakeKeys.Count > 0 ? PlayShakeAsync(shakeKeys, context, token)                : UniTask.CompletedTask);

                // 마지막 키프레임 이후 EndMarker 시각까지 Priority = 20 유지
                float elapsed = 0f;
                foreach (var k in cameraTrack.keyframes)
                    if (k != null && k.timeSeconds > elapsed) elapsed = k.timeSeconds;

                float remaining = Mathf.Max(0f, effectiveDuration - elapsed);
                if (remaining > 0.001f)
                {
                    await context.WaitForResumeAsync(token);
                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                if (needsSkillCamera && _skillCamera != null)
                {
                    _skillCamera.transform.position = basePosition;
                    _skillCamera.transform.rotation = baseRotation;
                    SetFov(_skillCamera, baseFov);
                    _skillCamera.Priority = 0;
                }
            }
        }

        // ── Position ───────────────────────────────────────────────────────────

        private async UniTask PlayPositionAsync(List<SkillKeyframeData> keys, Vector3 basePosition, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            if (_skillCamera == null) return;
            float   elapsed   = 0f;
            Vector3 prevDelta = Vector3.zero;

            try
            {
                foreach (var key in keys)
                {
                    float   seg      = key.timeSeconds - elapsed;
                    Vector3 fromPos  = basePosition + prevDelta;
                    Vector3 toPos    = basePosition + key.cameraPosition;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            _skillCamera.transform.position = toPos;
                        }
                        else
                        {
                            var cam = _skillCamera;
                            await LMotion.Create(fromPos, toPos, seg)
                                .WithEase(key.easing)
                                .Bind(v => cam.transform.position = v)
                                .ToUniTask(token);
                        }
                    }
                    else
                    {
                        _skillCamera.transform.position = toPos;
                    }
                    elapsed   = key.timeSeconds;
                    prevDelta = key.cameraPosition;
                    await context.WaitForResumeAsync(token);
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Rotation ───────────────────────────────────────────────────────────

        private async UniTask PlayRotationAsync(List<SkillKeyframeData> keys, Quaternion baseRotation, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            if (_skillCamera == null) return;
            float   elapsed      = 0f;
            Vector3 prevDelta    = Vector3.zero;
            Vector3 baseEuler    = baseRotation.eulerAngles;

            try
            {
                foreach (var key in keys)
                {
                    float   seg      = key.timeSeconds - elapsed;
                    Vector3 fromEuler = baseEuler + prevDelta;
                    Vector3 toEuler   = baseEuler + key.cameraRotationEuler;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            _skillCamera.transform.rotation = Quaternion.Euler(toEuler);
                        }
                        else
                        {
                            var cam = _skillCamera;
                            await LMotion.Create(fromEuler, toEuler, seg)
                                .WithEase(key.easing)
                                .Bind(v => cam.transform.rotation = Quaternion.Euler(v))
                                .ToUniTask(token);
                        }
                    }
                    else
                    {
                        _skillCamera.transform.rotation = Quaternion.Euler(toEuler);
                    }
                    elapsed   = key.timeSeconds;
                    prevDelta = key.cameraRotationEuler;
                    await context.WaitForResumeAsync(token);
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Zoom ───────────────────────────────────────────────────────────────

        private async UniTask PlayZoomAsync(List<SkillKeyframeData> keys, float baseFov, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            if (_skillCamera == null) return;
            float elapsed = 0f;
            float prev    = baseFov;

            try
            {
                foreach (var key in keys)
                {
                    float seg = key.timeSeconds - elapsed;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            SetFov(_skillCamera, key.fieldOfView);
                        }
                        else
                        {
                            var cam = _skillCamera;
                            await LMotion.Create(prev, key.fieldOfView, seg)
                                .WithEase(key.easing)
                                .Bind(v => SetFov(cam, v))
                                .ToUniTask(token);
                        }
                    }
                    else
                    {
                        SetFov(_skillCamera, key.fieldOfView);
                    }
                    elapsed = key.timeSeconds;
                    prev    = key.fieldOfView;
                    await context.WaitForResumeAsync(token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private static void SetFov(CinemachineCamera cam, float fov)
        {
            var lens = cam.Lens;
            lens.FieldOfView = fov;
            cam.Lens = lens;
        }

        // ── Shake ──────────────────────────────────────────────────────────────

        private async UniTask PlayShakeAsync(List<SkillKeyframeData> keys, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            float elapsed = 0f;
            try
            {
                foreach (var key in keys)
                {
                    float wait = key.timeSeconds - elapsed;
                    if (wait > 0.001f)
                        await UniTask.Delay(TimeSpan.FromSeconds(wait), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                    elapsed = key.timeSeconds;
                    await context.WaitForResumeAsync(token);
                    _impulseSource?.GenerateImpulse(key.amplitude);
                }
            }
            catch (OperationCanceledException) { }
        }

    }
}
