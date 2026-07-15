using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using DelayType = Cysharp.Threading.Tasks.DelayType;

namespace Battle.Presentation
{
    public sealed class SkillCasterExecutionService
    {
        public async UniTask PlayAsync(SkillSingleTrackData casterTrack, SkillPresentationPlaybackContext context, float effectiveDuration, CancellationToken token)
        {
            if (casterTrack?.keyframes == null || casterTrack.keyframes.Count == 0) return;

            var t = context.Caster?.transform;
            if (t == null) return;

            Vector3 basePosition = t.position;
            Vector3 baseEuler    = t.eulerAngles;

            try
            {
                var posKeys = casterTrack.GetCached(SkillKeyframeProperty.CasterPosition);
                var rotKeys = casterTrack.GetCached(SkillKeyframeProperty.CasterRotation);

                if (posKeys.Count > 0 || rotKeys.Count > 0)
                    await UniTask.WhenAll(
                        posKeys.Count > 0 ? PlayPositionAsync(t, posKeys, basePosition, context, token) : UniTask.CompletedTask,
                        rotKeys.Count > 0 ? PlayRotationAsync(t, rotKeys, baseEuler, context, token)    : UniTask.CompletedTask);

                float elapsed = 0f;
                foreach (var k in casterTrack.keyframes)
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
                if (t != null)
                {
                    t.position    = basePosition;
                    t.eulerAngles = baseEuler;
                }
            }
        }

        private async UniTask PlayPositionAsync(Transform t, List<SkillKeyframeData> keys, Vector3 basePosition, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            float   elapsed   = 0f;
            Vector3 prevDelta = Vector3.zero;

            try
            {
                foreach (var key in keys)
                {
                    float   seg     = key.timeSeconds - elapsed;
                    Vector3 fromPos = basePosition + prevDelta;
                    Vector3 toPos   = basePosition + key.position;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            t.position = toPos;
                        }
                        else
                        {
                            await LMotion.Create(fromPos, toPos, seg)
                                .WithEase(key.easing)
                                .Bind(v => t.position = v)
                                .ToUniTask(token);
                        }
                    }
                    else
                    {
                        t.position = toPos;
                    }
                    elapsed   = key.timeSeconds;
                    prevDelta = key.position;
                    await context.WaitForResumeAsync(token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask PlayRotationAsync(Transform t, List<SkillKeyframeData> keys, Vector3 baseEuler, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            float   elapsed   = 0f;
            Vector3 prevDelta = Vector3.zero;

            try
            {
                foreach (var key in keys)
                {
                    float   seg       = key.timeSeconds - elapsed;
                    Vector3 fromEuler = baseEuler + prevDelta;
                    Vector3 toEuler   = baseEuler + key.rotationEuler;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            t.eulerAngles = toEuler;
                        }
                        else
                        {
                            await LMotion.Create(fromEuler, toEuler, seg)
                                .WithEase(key.easing)
                                .Bind(v => t.eulerAngles = v)
                                .ToUniTask(token);
                        }
                    }
                    else
                    {
                        t.eulerAngles = toEuler;
                    }
                    elapsed   = key.timeSeconds;
                    prevDelta = key.rotationEuler;
                    await context.WaitForResumeAsync(token);
                }
            }
            catch (OperationCanceledException) { }
        }

    }
}
