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
        private readonly PoolManagerSo _pool;
        private readonly PoolItemSo    _containerItem;

        public SkillVfxExecutionService(PoolManagerSo pool, PoolItemSo containerItem)
        {
            _pool          = pool;
            _containerItem = containerItem;
        }

        public void SpawnAndPlay(SkillVfxObjectData vfxObject, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            if (vfxObject == null || vfxObject.vfxDefinition == null) return;
            if (_pool == null || _containerItem == null)
            {
                Debug.LogWarning("[SkillVfxExecution] PoolManagerSo 또는 containerItem이 설정되지 않았습니다.");
                return;
            }
            PlayVfxAsync(vfxObject, context, token).Forget();
        }

        private async UniTaskVoid PlayVfxAsync(SkillVfxObjectData vfxObject, SkillPresentationPlaybackContext context, CancellationToken token)
        {
            SkillVfxContainer container = _pool.Pop<SkillVfxContainer>(_containerItem);
            if (container == null)
            {
                Debug.LogWarning("[SkillVfxExecution] 컨테이너 풀에서 SkillVfxContainer를 가져오지 못했습니다.");
                return;
            }

            Vector3 spawnPos = ResolveSpawnPosition(vfxObject, context) + vfxObject.spawnPositionOffset;
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
                        AnimateActiveAsync(container, vfxObject.vfxDefinition.key, activeKeys, vfxObject.spawnTime, token)
                    };
                    if (vfxObject.keyframes != null && vfxObject.keyframes.Count > 0)
                        tasks.Add(AnimateTransformAsync(container.transform, vfxObject.keyframes, vfxObject.spawnTime, token));
                    await UniTask.WhenAll(tasks);
                }
                else
                {
                    container.Play(vfxObject.vfxDefinition.key);
                    if (vfxObject.keyframes != null && vfxObject.keyframes.Count > 0)
                        AnimateTransformAsync(container.transform, vfxObject.keyframes, vfxObject.spawnTime, token).Forget(); // fire-and-forget in legacy path

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
                container.Stop();
                _pool.Push(container);
            }
        }

        private async UniTask AnimateActiveAsync(
            SkillVfxContainer container,
            SkillVfxKey key,
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
                        container.Play(key);
                    else
                        container.Stop();
                }
            }
            catch (OperationCanceledException) { }
        }

        // keyframes는 VfxPosition / VfxRotation / VfxScale property 키를 혼합 보유
        // timeSeconds는 스킬 절대 시간 기준 → spawnTime 오프셋으로 상대 시간 계산
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

        private static Vector3 ResolveSpawnPosition(SkillVfxObjectData vfxObject, SkillPresentationPlaybackContext context)
        {
            if (vfxObject.spawnTarget == SkillVfxSpawnTarget.Target && context.Target == null)
                return context.Caster?.transform.position ?? Vector3.zero;

            return vfxObject.spawnTarget switch
            {
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
