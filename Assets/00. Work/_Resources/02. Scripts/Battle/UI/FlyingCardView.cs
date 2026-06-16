using System.Threading;
using Cysharp.Threading.Tasks;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace Battle.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlyingCardView : PoolableMono
    {
        [SerializeField] private CardFlyTrailEffect trailEffect;

        private SpriteRenderer _spriteRenderer;

        private void Awake() => _spriteRenderer = GetComponent<SpriteRenderer>();

        // Trail 비활성화 → 위치 이동 → 한 프레임 대기 → Trail 활성화
        // 풀에서 꺼낸 직후 poolRoot 위치에서 목표 위치로 순간이동 시 trail이 그 구간을 기록하는 현상 방지
        public async UniTask PlaceAt(Vector3 worldPos, CancellationToken ct)
        {
            trailEffect?.SuppressForTeleport();
            transform.position = worldPos;
            await UniTask.NextFrame(ct);
            trailEffect?.ResumeAfterTeleport();
        }

        // 더미 도착: 카드 스프라이트를 숨겨 "흡수됨"을 표현하고, 도착 버스트를 재생한다.
        // 트레일은 끄지 않아 풀 반환 전까지 잔광이 자연스럽게 남는다.
        public void PlayArrivalEffect()
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
            trailEffect?.PlayArrival();
        }

        public override void ResetItem()
        {
            trailEffect?.ResetEffect();
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
