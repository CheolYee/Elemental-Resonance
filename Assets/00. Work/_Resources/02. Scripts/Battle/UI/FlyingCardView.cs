using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace Battle.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlyingCardView : PoolableMono
    {
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem trailParticle;

        // Trail 비활성화 → 위치 이동 → 한 프레임 대기 → Trail 활성화
        // 풀에서 꺼낸 직후 poolRoot 위치에서 목표 위치로 순간이동 시 trail이 그 구간을 기록하는 현상 방지
        public async UniTask PlaceAt(Vector3 worldPos, CancellationToken ct)
        {
            if (trailRenderer != null) trailRenderer.enabled = false;
            transform.position = worldPos;
            await UniTask.NextFrame(ct);
            if (trailRenderer != null) trailRenderer.enabled = true;
        }

        public override void ResetItem()
        {
            if (trailRenderer != null)
            {
                trailRenderer.enabled = true;
                trailRenderer.Clear();
            }
            if (trailParticle != null)
                trailParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
