using UnityEngine;

namespace Battle.UI
{
    // FlyingCardView의 이동 로직과 분리된 트레일/파티클 시각 연출 전담 컴포넌트.
    public class CardFlyTrailEffect : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem trailParticle;
        [SerializeField] private ParticleSystem arrivalBurst;
        [SerializeField] private int arrivalBurstCount = 18;

        // 풀에서 꺼낸 직후 순간이동 시 trail이 그 구간을 기록하는 현상을 막기 위해 일시 비활성화
        public void SuppressForTeleport()
        {
            if (trailRenderer != null) trailRenderer.enabled = false;
        }

        public void ResumeAfterTeleport()
        {
            if (trailRenderer != null) trailRenderer.enabled = true;
            if (trailParticle != null) trailParticle.Play();
        }

        // 도착 시점: 지속 파티클은 신규 방출만 멈추고(기존 입자는 자연 소멸), 트레일은 그대로 남겨 잔광을 만든다.
        public void PlayArrival()
        {
            if (trailParticle != null) trailParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (arrivalBurst != null)
            {
                arrivalBurst.Play();
                arrivalBurst.Emit(arrivalBurstCount);
            }
        }

        public void ResetEffect()
        {
            if (trailRenderer != null)
            {
                trailRenderer.enabled = true;
                trailRenderer.Clear();
            }
            if (trailParticle != null)
                trailParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (arrivalBurst != null)
                arrivalBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
