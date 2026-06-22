using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using Battle.Effects;
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

            SkillPresentationTimeline timeline = context.PresentationData.GetTimeline();
            if (timeline == null || timeline.IsEmpty) return;

            SkillPresentationPlaybackContext runtimeContext = context.WithTimeline(timeline);
            IReadOnlyList<SkillPresentationScheduledBatch> schedule = _sampler.CreateSchedule(timeline);

            PreResolveHitTargets(runtimeContext, timeline);

            runtimeContext.CancellationToken.ThrowIfCancellationRequested();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                runtimeContext.CancellationToken,
                runtimeContext.FizzleToken.CancellationToken);

            try
            {
                await UniTask.WhenAll(
                    RunKeyframeScheduleAsync(runtimeContext, schedule, timeline, linkedCts.Token),
                    RunCameraAsync(runtimeContext, timeline, linkedCts.Token),
                    RunCasterAsync(runtimeContext, timeline, linkedCts.Token),
                    RunVfxAsync(runtimeContext, timeline, linkedCts.Token));
            }
            catch (OperationCanceledException) when (runtimeContext.FizzleToken.IsFizzled)
            {
                // Fizzle로 인한 취소 — 상위로 전파하지 않음
            }
        }

        private async UniTask RunKeyframeScheduleAsync(
            SkillPresentationPlaybackContext context,
            IReadOnlyList<SkillPresentationScheduledBatch> schedule,
            SkillPresentationTimeline timeline,
            CancellationToken token)
        {
            float startTime         = Time.time;
            int   currentBatchIndex = 0;
            var   executedKeyframes = new HashSet<SkillKeyframeData>();

            while (currentBatchIndex < schedule.Count)
            {
                SkillPresentationScheduledBatch batch = schedule[currentBatchIndex];
                float waitSeconds = Mathf.Max(0f, startTime + batch.TimeSeconds - Time.time);
                if (waitSeconds > 0f)
                    await DelayAsync(waitSeconds, token);

                token.ThrowIfCancellationRequested();

                for (int i = 0; i < batch.Keyframes.Count; i++)
                {
                    SkillKeyframeData keyframe = batch.Keyframes[i];
                    if (keyframe == null || !executedKeyframes.Add(keyframe)) continue;
                    await _keyframeExecutor.ExecuteAsync(context, keyframe);
                    if (context.FizzleToken.IsFizzled) return;
                }

                currentBatchIndex++;
            }

            float remainingDuration = Mathf.Max(0f, timeline.GetEffectiveDuration() - (Time.time - startTime));
            if (remainingDuration > 0f)
                await DelayAsync(remainingDuration, token);
        }

        private UniTask RunCameraAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline, CancellationToken token)
        {
            if (_cameraService == null
                || timeline.cameraTrack?.keyframes == null
                || timeline.cameraTrack.keyframes.Count == 0)
                return UniTask.CompletedTask;

            return _cameraService.PlayAsync(timeline.cameraTrack, context, timeline.GetEffectiveDuration(), token);
        }

        private UniTask RunCasterAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline, CancellationToken token)
        {
            if (_casterService == null
                || timeline.casterTrack?.keyframes == null
                || timeline.casterTrack.keyframes.Count == 0)
                return UniTask.CompletedTask;

            return _casterService.PlayAsync(timeline.casterTrack, context, timeline.GetEffectiveDuration(), token);
        }

        private UniTask RunVfxAsync(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline, CancellationToken token)
        {
            if (_vfxService == null || timeline.vfxObjects == null || timeline.vfxObjects.Count == 0)
                return UniTask.CompletedTask;

            return RunVfxObjectsAsync(context, timeline.vfxObjects, token);
        }

        private async UniTask RunVfxObjectsAsync(
            SkillPresentationPlaybackContext context,
            List<SkillVfxObjectData> vfxObjects,
            CancellationToken token)
        {
            var sorted = new List<SkillVfxObjectData>(vfxObjects);
            sorted.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

            float elapsed = 0f;
            foreach (var vfx in sorted)
            {
                if (vfx == null) continue;
                float wait = Mathf.Max(0f, vfx.spawnTime - elapsed);
                if (wait > 0f)
                    await DelayAsync(wait, token);

                token.ThrowIfCancellationRequested();
                await context.WaitForResumeAsync(token);
                elapsed = vfx.spawnTime;
                _vfxService.SpawnAndPlay(vfx, context, token);
            }
        }

        private static void PreResolveHitTargets(SkillPresentationPlaybackContext context, SkillPresentationTimeline timeline)
        {
            if (context.RandomTargetResolver == null) return;

            var effectKeyframes = timeline.effectTrack?.keyframes;
            if (effectKeyframes == null || effectKeyframes.Count == 0) return;

            var slots = context.CardInstance?.data?.effectSlots;
            if (slots == null || slots.Count == 0) return;

            int count = 0;
            foreach (var kf in effectKeyframes)
            {
                if (kf == null || kf.property != SkillKeyframeProperty.EffectSlot) continue;
                if (string.IsNullOrEmpty(kf.effectSlotId)) continue;

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot != null && slot.effectSlotId == kf.effectSlotId && slot.effect is DamageEffect)
                    {
                        count++;
                        break;
                    }
                }
            }

            if (count == 0) return;

            var targets = new Agent[count];
            for (int i = 0; i < count; i++)
                targets[i] = context.RandomTargetResolver();

            context.SetPreResolvedHitTargets(targets);
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
