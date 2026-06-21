using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using UnityEngine;

namespace Battle.Presentation
{
    public sealed class SkillVfxExecutionService
    {
        private readonly PoolManagerSo       _pool;
        private readonly PoolItemSo          _containerItem;
        private readonly SkillPreviewLayoutSO _layout;

        public SkillVfxExecutionService(PoolManagerSo pool, PoolItemSo containerItem, SkillPreviewLayoutSO layout)
        {
            _pool          = pool;
            _containerItem = containerItem;
            _layout        = layout;
        }

        public void SpawnAndPlay(SkillVfxObjectData vfxObject, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            if (vfxObject == null || vfxObject.vfxDefinition == null) return;
            if (_pool == null || _containerItem == null)
            {
                Debug.LogWarning("[SkillVfxExecution] PoolManagerSo 또는 containerItem이 설정되지 않았습니다.");
                return;
            }

            if (context.RandomTargetResolver != null &&
                (vfxObject.spawnTarget == SkillVfxSpawnTarget.Target ||
                 vfxObject.spawnTarget == SkillVfxSpawnTarget.BetweenCasterAndTarget))
            {
                var resolvedTarget = context.GetPreResolvedHitTarget(vfxObject.hitIndex);
                if (resolvedTarget != null)
                {
                    Vector3 pos = vfxObject.spawnTarget == SkillVfxSpawnTarget.BetweenCasterAndTarget
                        ? Vector3.Lerp(context.Caster?.transform.position ?? Vector3.zero, resolvedTarget.transform.position, 0.5f)
                        : resolvedTarget.transform.position;
                    PlayVfxAsync(vfxObject, pos + vfxObject.spawnPositionOffset, token).Forget();
                    return;
                }
            }

            if (vfxObject.spawnTarget == SkillVfxSpawnTarget.Target && context.Targets.Count > 0)
            {
                foreach (var target in context.Targets)
                    PlayVfxAsync(vfxObject, target.transform.position + vfxObject.spawnPositionOffset, token).Forget();
                return;
            }

            Vector3 spawnPos = ResolveSpawnPosition(vfxObject, context) + vfxObject.spawnPositionOffset;
            PlayVfxAsync(vfxObject, spawnPos, token).Forget();
        }

        private async UniTaskVoid PlayVfxAsync(SkillVfxObjectData vfxObject, Vector3 spawnPos, CancellationToken token)
        {
            SkillVfxContainer container = _pool.Pop<SkillVfxContainer>(_containerItem);
            if (container == null)
            {
                Debug.LogWarning("[SkillVfxExecution] 컨테이너 풀에서 SkillVfxContainer를 가져오지 못했습니다.");
                return;
            }

            container.transform.position   = spawnPos;
            container.transform.rotation   = Quaternion.Euler(vfxObject.spawnRotationEuler);
            container.transform.localScale = Vector3.one;

            var activeKeys = FilterAndSort(vfxObject.keyframes, SkillKeyframeProperty.VfxActive);
            bool useActiveKeyframes = activeKeys.Count > 0;

            try
            {
                if (useActiveKeyframes)
                {
                    var tasks = new List<UniTask>
                    {
                        AnimateActiveAsync(container, vfxObject, activeKeys, vfxObject.spawnTime, token)
                    };
                    if (vfxObject.keyframes != null && vfxObject.keyframes.Count > 0)
                        tasks.Add(AnimateTransformAsync(container.transform, vfxObject.keyframes, vfxObject.spawnTime, token));
                    await UniTask.WhenAll(tasks);
                }
                else
                {
                    container.Play(vfxObject.vfxDefinition.key, vfxObject.simulationSpeed, vfxObject.startLifetimeMultiplier);
                    if (vfxObject.keyframes != null && vfxObject.keyframes.Count > 0)
                        AnimateTransformAsync(container.transform, vfxObject.keyframes, vfxObject.spawnTime, token).Forget();

                    float waitTime = vfxObject.lifeTime > 0f
                        ? vfxObject.lifeTime
                        : container.GetMaxDuration(vfxObject.vfxDefinition.key);
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(Mathf.Max(0.05f, waitTime)),
                        Cysharp.Threading.Tasks.DelayType.DeltaTime,
                        PlayerLoopTiming.Update,
                        token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (container != null)
                {
                    container.Stop();
                    _pool.Push(container);
                }
            }
        }

        private async UniTask AnimateActiveAsync(
            SkillVfxContainer container,
            SkillVfxObjectData vfxObject,
            List<SkillKeyframeData> activeKeys,
            float spawnTime,
            CancellationToken token)
        {
            float elapsed = spawnTime;
            try
            {
                foreach (var k in activeKeys)
                {
                    float wait = k.timeSeconds - elapsed;
                    if (wait > 0.001f)
                        await UniTask.Delay(TimeSpan.FromSeconds(wait), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                    elapsed = k.timeSeconds;

                    if (k.vfxActiveAction == SkillVfxActiveAction.Play)
                        container.Play(vfxObject.vfxDefinition.key, vfxObject.simulationSpeed, vfxObject.startLifetimeMultiplier);
                    else
                        container.Stop();
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask AnimateTransformAsync(
            Transform t,
            List<SkillKeyframeData> keyframes,
            float spawnTime,
            CancellationToken token)
        {
            var posKeys   = FilterAndSort(keyframes, SkillKeyframeProperty.VfxPosition);
            var rotKeys   = FilterAndSort(keyframes, SkillKeyframeProperty.VfxRotation);
            var scaleKeys = FilterAndSort(keyframes, SkillKeyframeProperty.VfxScale);

            try
            {
                await UniTask.WhenAll(
                    AnimatePositionAsync(t, posKeys, spawnTime, token),
                    AnimateRotationAsync(t, rotKeys, spawnTime, token),
                    AnimateScaleAsync(t, scaleKeys, spawnTime, token));
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask AnimatePositionAsync(Transform t, List<SkillKeyframeData> keys, float spawnTime, CancellationToken token)
        {
            if (keys.Count == 0) return;
            float   elapsed   = spawnTime;
            Vector3 basePos   = t.position;
            Vector3 prevDelta = Vector3.zero;
            try
            {
                foreach (var key in keys)
                {
                    float   seg       = key.timeSeconds - elapsed;
                    Vector3 fromTotal = basePos + prevDelta;
                    Vector3 toTotal   = basePos + key.position;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            t.position = toTotal;
                        }
                        else
                        {
                            await LMotion.Create(fromTotal, toTotal, seg)
                                .WithEase(key.easing)
                                .Bind(v => t.position = v)
                                .ToUniTask(token);
                        }
                    }
                    else t.position = toTotal;
                    elapsed   = key.timeSeconds;
                    prevDelta = key.position;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask AnimateRotationAsync(Transform t, List<SkillKeyframeData> keys, float spawnTime, CancellationToken token)
        {
            if (keys.Count == 0) return;
            float   elapsed   = spawnTime;
            Vector3 baseRot   = t.eulerAngles;
            Vector3 prevDelta = Vector3.zero;
            try
            {
                foreach (var key in keys)
                {
                    float   seg       = key.timeSeconds - elapsed;
                    Vector3 fromTotal = baseRot + prevDelta;
                    Vector3 toTotal   = baseRot + key.rotationEuler;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            t.eulerAngles = toTotal;
                        }
                        else
                        {
                            await LMotion.Create(fromTotal, toTotal, seg)
                                .WithEase(key.easing)
                                .Bind(v => t.eulerAngles = v)
                                .ToUniTask(token);
                        }
                    }
                    else t.eulerAngles = toTotal;
                    elapsed   = key.timeSeconds;
                    prevDelta = key.rotationEuler;
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask AnimateScaleAsync(Transform t, List<SkillKeyframeData> keys, float spawnTime, CancellationToken token)
        {
            if (keys.Count == 0) return;
            float   elapsed   = spawnTime;
            Vector3 baseScale = t.localScale;
            Vector3 prevDelta = Vector3.zero;
            try
            {
                foreach (var key in keys)
                {
                    float   seg       = key.timeSeconds - elapsed;
                    Vector3 fromTotal = baseScale + prevDelta;
                    Vector3 toTotal   = baseScale + key.scale;
                    if (seg > 0.001f)
                    {
                        if (key.isHold)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(seg), Cysharp.Threading.Tasks.DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                            t.localScale = toTotal;
                        }
                        else
                        {
                            await LMotion.Create(fromTotal, toTotal, seg)
                                .WithEase(key.easing)
                                .Bind(v => t.localScale = v)
                                .ToUniTask(token);
                        }
                    }
                    else t.localScale = toTotal;
                    elapsed   = key.timeSeconds;
                    prevDelta = key.scale;
                }
            }
            catch (OperationCanceledException) { }
        }

        private static List<SkillKeyframeData> FilterAndSort(List<SkillKeyframeData> keys, SkillKeyframeProperty property)
        {
            var result = new List<SkillKeyframeData>();
            foreach (var k in keys)
                if (k != null && k.property == property) result.Add(k);
            result.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            return result;
        }

        private Vector3 ResolveSpawnPosition(SkillVfxObjectData vfxObject, SkillPresentationPlaybackContext context)
        {
            if (vfxObject.spawnTarget == SkillVfxSpawnTarget.Target && context.Target == null)
                return context.Caster?.transform.position ?? Vector3.zero;

            return vfxObject.spawnTarget switch
            {
                SkillVfxSpawnTarget.None     => _layout != null ? _layout.noneVfxSpawnPosition : Vector3.zero,
                SkillVfxSpawnTarget.Caster   => context.Caster?.transform.position ?? Vector3.zero,
                SkillVfxSpawnTarget.Target   => context.Target?.transform.position ?? Vector3.zero,
                SkillVfxSpawnTarget.BetweenCasterAndTarget =>
                    Vector3.Lerp(
                        context.Caster?.transform.position ?? Vector3.zero,
                        context.Target?.transform.position ?? Vector3.zero,
                        0.5f),
                _ => Vector3.zero
            };
        }
    }
}
