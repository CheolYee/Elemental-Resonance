using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Battle.Presentation
{
    public sealed class SkillPresentationPlayer : ISkillPresentationPlayer
    {
        private readonly ISkillPresentationSampler          _sampler;
        private readonly ISkillPresentationKeyframeExecutor _keyframeExecutor;
        private readonly SkillCameraExecutionService        _cameraService;
        private readonly SkillCasterExecutionService        _casterService;
        private readonly SkillVfxExecutionService           _vfxService;

        public SkillPresentationPlayer(
            ISkillPresentationSampler          sampler,
            ISkillPresentationKeyframeExecutor keyframeExecutor,
            SkillCameraExecutionService        cameraService,
            SkillCasterExecutionService        casterService,
            SkillVfxExecutionService           vfxService)
        {
            _sampler          = sampler;
            _keyframeExecutor = keyframeExecutor;
            _cameraService    = cameraService;
            _casterService    = casterService;
            _vfxService       = vfxService;
        }

        public async UniTask PlayAsync(SkillPresentationPlaybackContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.PresentationData == null) return;

            SkillPresentationTimeline timeline = context.PresentationData.GetTimeline(context.Grade);
            if (timeline == null || timeline.IsEmpty) return;

            SkillPresentationPlaybackContext runtimeContext = context.WithTimeline(timeline);
            IReadOnlyList<SkillPresentationScheduledBatch> schedule = _sampler.CreateSchedule(timeline);

            runtimeContext.CancellationToken.ThrowIfCancellationRequested();

            await UniTask.WhenAll(
                RunKeyframeScheduleAsync(runtimeContext, schedule, timeline),
                RunCameraAsync(runtimeContext, timeline),
                RunCasterAsync(runtimeContext, timeline),
                RunVfxAsync(runtimeContext, timeline));
        }

        private async UniTask RunKeyframeScheduleAsync(
            SkillPresentationPlaybackContext context,
            IReadOnlyList<SkillPresentationScheduledBatch> schedule,
            SkillPresentationTimeline timeline)
        {
            float elapsed           = 0f;
            int   currentBatchIndex = 0;
            var   executedKeyframes = new HashSet<SkillKeyframeData>();

            while (currentBatchIndex < schedule.Count)
            {
                SkillPresentationScheduledBatch batch = schedule[currentBatchIndex];
                float waitSeconds = Mathf.Max(0f, batch.TimeSeconds - elapsed);
                if (waitSeconds > 0f)
                    await DelayAsync(waitSeconds, context.CancellationToken);

                context.CancellationToken.ThrowIfCancellationRequested();
                elapsed = batch.TimeSeconds;

                for (int i = 0; i < batch.Keyframes.Count; i++)
                {
                    SkillKeyframeData keyframe = batch.Keyframes[i];
                    if (keyframe == null || !executedKeyframes.Add(keyframe)) continue;
                    await _keyframeExecutor.ExecuteAsync(context, keyframe);
                }

                currentBatchIndex++;
            }

            float remainingDuration = Mathf.Max(0f, timeline.GetEffectiveDuration() - elapsed);
            if (remainingDuration > 0f)
                await DelayAsync(remainingDuration, context.CancellationToken);
        }

        private UniTask RunCameraAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline)
        {
            if (_cameraService == null
                || timeline.cameraTrack?.keyframes == null
                || timeline.cameraTrack.keyframes.Count == 0)
                return UniTask.CompletedTask;

            return _cameraService.PlayAsync(timeline.cameraTrack, context, timeline.GetEffectiveDuration(), context.CancellationToken);
        }

        private UniTask RunCasterAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline)
        {
            if (_casterService == null
                || timeline.casterTrack?.keyframes == null
                || timeline.casterTrack.keyframes.Count == 0)
                return UniTask.CompletedTask;

            return _casterService.PlayAsync(timeline.casterTrack, context, timeline.GetEffectiveDuration(), context.CancellationToken);
        }

        private UniTask RunVfxAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline)
        {
            if (_vfxService == null || timeline.vfxObjects == null || timeline.vfxObjects.Count == 0)
                return UniTask.CompletedTask;

            return RunVfxObjectsAsync(context, timeline.vfxObjects);
        }

        private async UniTask RunVfxObjectsAsync(
            SkillPresentationPlaybackContext context,
            List<SkillVfxObjectData> vfxObjects)
        {
            var sorted = new List<SkillVfxObjectData>(vfxObjects);
            sorted.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

            float elapsed = 0f;
            foreach (var vfx in sorted)
            {
                if (vfx == null) continue;
                float wait = Mathf.Max(0f, vfx.spawnTime - elapsed);
                if (wait > 0f)
                    await DelayAsync(wait, context.CancellationToken);

                context.CancellationToken.ThrowIfCancellationRequested();
                elapsed = vfx.spawnTime;
                _vfxService.SpawnAndPlay(vfx, context, context.CancellationToken);
            }
        }

        private static UniTask DelayAsync(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f) return UniTask.CompletedTask;
            return UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                DelayType.DeltaTime,
                PlayerLoopTiming.Update,
                cancellationToken);
        }
    }
}
