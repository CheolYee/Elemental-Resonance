using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Enums;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using DelayType = Cysharp.Threading.Tasks.DelayType;

namespace Battle.UI
{
    public class CardFlyAnimator : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private PileAnchorView pileAnchorView;
        [SerializeField] private PoolItemSo flyingCardPoolItem;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private Camera pileCamera;

        [Header("Card Size")]
        [SerializeField] private float cardScale = 0.15f;
        [SerializeField] private float cardUsedShrinkDuration = 0.2f;

        [Header("Arc")]
        [SerializeField] private float flyDuration = 0.45f;
        [SerializeField] private Ease flyEase = Ease.OutQuad;
        [SerializeField] private float arcHeightRatio = 0.5f;
        [SerializeField] private float minArcHeightPixels = 80f;
        [SerializeField] private float flyDepth = 5f;

        [Header("Arc Variance")]
        [SerializeField] private float arcHeightVarianceRatio = 0.3f;
        [SerializeField] private float lateralOffsetVariancePixels = 80f;
        [SerializeField] private float durationVarianceRatio = 0.1f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeedDegPerSec = 540f;

        [Header("Refill (C-6)")]
        [SerializeField] private int maxRefillAnimCards = 8;
        [SerializeField] private float refillStagger = 0.05f;

        [Header("Arrival")]
        [SerializeField] private float trailLingerDuration = 0.2f;

        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds      cardUseSound;
        [SerializeField] private SfxSounds      arrivalSound;

        private Pool _pool;

        private void Awake()
        {
            _pool = new Pool(flyingCardPoolItem, poolRoot, flyingCardPoolItem.initCount);
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.AddListener<DrawPileRefillEvent>(OnDrawPileRefill);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardDroppedOnTargetEvent>(OnCardDropped);
            battleEventChannel.RemoveListener<DrawPileRefillEvent>(OnDrawPileRefill);
        }

        // C-8: 카드 사용 → 버림패/무덤패 (shrink 연출 후 fly 시작)
        private void OnCardDropped(CardDroppedOnTargetEvent evt)
        {
            soundChannel?.RaiseEvent(new PlaySoundEvent(cardUseSound, Vector3.zero));
            var target = evt.CardInstance.data.disposePolicy == CardDisposePolicy.Grave
                ? PileDisplayTarget.Grave
                : PileDisplayTarget.Discard;
            FlyWithDelayAsync(evt.CardScreenPosition, target, cardUsedShrinkDuration).Forget();
        }

        // C-6: 버림패 → 가짐패 보충
        private void OnDrawPileRefill(DrawPileRefillEvent evt)
        {
            Vector2 from = pileAnchorView.GetScreenPosition(PileDisplayTarget.Discard);
            int count = Mathf.Min(evt.RefillCount, maxRefillAnimCards);
            for (int i = 0; i < count; i++)
                FlyWithDelayAsync(from, PileDisplayTarget.Draw, i * refillStagger).Forget();
        }

        // 합성 재료 카드 비행 — FusionExecutionController에서 직접 호출
        public UniTask FlyToDiscardAsync(Vector2 fromScreen)
            => FlyCardAsync(fromScreen, PileDisplayTarget.Discard);

        public UniTask FlyToGraveAsync(Vector2 fromScreen)
            => FlyCardAsync(fromScreen, PileDisplayTarget.Grave);

        public void FlyToCurrentDeck(Vector2 fromScreen)
            => FlyCardAsync(fromScreen, PileDisplayTarget.CurrentDeck).Forget();

        // C-10: HandLayoutController가 직접 호출 — 턴 종료 손패 전체 이동
        public void FlyAllToDiscard(List<Vector2> screenPositions)
        {
            foreach (var pos in screenPositions)
                FlyCardAsync(pos, PileDisplayTarget.Discard).Forget();
        }

        private async UniTaskVoid FlyWithDelayAsync(Vector2 from, PileDisplayTarget to, float delay)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            await FlyCardAsync(from, to);
        }

        // 도착지는 PileDisplayTarget으로 받아 매 프레임 재조회한다 (UI 슬라이드 중에도 추적).
        private async UniTask FlyCardAsync(Vector2 fromScreen, PileDisplayTarget toTarget)
        {
            var card = (FlyingCardView)_pool.Pop();
            card.transform.localScale = Vector3.one * cardScale;

            // Trail 비활성화 → 시작 위치 이동 → 한 프레임 대기 → Trail 활성화 (trail 튀기 방지)
            await card.PlaceAt(ScreenToWorld(fromScreen), destroyCancellationToken);

            Vector2 toScreen = pileAnchorView.GetScreenPosition(toTarget);

            // 아치 높이는 이동 거리에 비례 (짧은 거리도 minArcHeightPixels로 최소 높이 보장)
            float distance = Vector2.Distance(fromScreen, toScreen);
            float baseArcHeight = Mathf.Max(distance * arcHeightRatio, minArcHeightPixels);

            // 비행마다 아치 높이/좌우 편향/길이를 랜덤 변주해 매번 다른 궤적을 만든다.
            float arcHeight = baseArcHeight * UnityEngine.Random.Range(1f - arcHeightVarianceRatio, 1f + arcHeightVarianceRatio);
            float lateralOffset = UnityEngine.Random.Range(-lateralOffsetVariancePixels, lateralOffsetVariancePixels);
            float duration = flyDuration * UnityEngine.Random.Range(1f - durationVarianceRatio, 1f + durationVarianceRatio);

            Vector2 dir = (toScreen - fromScreen).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 controlScreen = (fromScreen + toScreen) * 0.5f + Vector2.up * arcHeight + perp * lateralOffset;

            // 위치 이징과 분리된 회전 추적을 위해 시작 접선 각도로 초기화
            Vector2 initialTangent = controlScreen - fromScreen;
            float currentAngle = initialTangent.sqrMagnitude > 0.01f
                ? Mathf.Atan2(initialTangent.y, initialTangent.x) * Mathf.Rad2Deg
                : 0f;

            await LMotion.Create(0f, 1f, duration)
                .WithEase(flyEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(t =>
                {
                    float mt = 1f - t;

                    // 도착지를 매 프레임 재조회 (제어점은 비행 시작 시 고정값 사용)
                    Vector2 liveToScreen = pileAnchorView.GetScreenPosition(toTarget);

                    // 2차 베지어 곡선 위치
                    Vector2 pos = mt * mt * fromScreen
                                + 2f * mt * t * controlScreen
                                + t * t * liveToScreen;

                    // 접선 벡터 → 목표 회전 각도
                    Vector2 tangent = 2f * mt * (controlScreen - fromScreen)
                                   + 2f * t  * (liveToScreen - controlScreen);

                    card.transform.position = ScreenToWorld(pos);

                    if (tangent.sqrMagnitude > 0.01f)
                    {
                        float targetAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeedDegPerSec * Time.deltaTime);
                    }

                    card.transform.rotation =
                        Quaternion.LookRotation(-pileCamera.transform.forward, pileCamera.transform.up)
                        * Quaternion.Euler(0f, 0f, currentAngle - 90f);
                })
                .ToUniTask(cancellationToken: destroyCancellationToken);

            battleEventChannel.RaiseEvent(new CardArrivedAtPileEvent(toTarget, card.transform.position));
            soundChannel?.RaiseEvent(new PlaySoundEvent(arrivalSound, Vector3.zero));
            card.PlayArrivalEffect();

            // 카드/지속 파티클은 즉시 사라지지만, 트레일은 잔광으로 잠시 남겨둔 뒤 풀에 반환한다.
            await UniTask.Delay(TimeSpan.FromSeconds(trailLingerDuration), DelayType.UnscaledDeltaTime, cancellationToken: destroyCancellationToken);

            _pool.Push(card);
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
            => pileCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, flyDepth));
    }
}
