using System;
using System.Collections.Generic;
using Battle.Enums;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class CardFlyAnimator : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private PileAnchorView pileAnchorView;
        [SerializeField] private PoolItemSo flyingCardPoolItem;
        [SerializeField] private Transform poolRoot;

        [Header("Card Size")]
        [SerializeField] private float cardScale = 0.15f;
        [SerializeField] private float cardUsedShrinkDuration = 0.2f;

        [Header("Arc")]
        [SerializeField] private float flyDuration = 0.45f;
        [SerializeField] private Ease flyEase = Ease.OutQuad;
        [SerializeField] private float arcHeightPixels = 180f;
        [SerializeField] private float flyDepth = 5f;

        [Header("Refill (C-6)")]
        [SerializeField] private int maxRefillAnimCards = 8;
        [SerializeField] private float refillStagger = 0.05f;

        private Pool _pool;
        private Camera _camera;

        private void Awake()
        {
            _pool = new Pool(flyingCardPoolItem, poolRoot, flyingCardPoolItem.initCount);
            _camera = Camera.main;
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
            var target = evt.CardInstance.data.disposePolicy == CardDisposePolicy.Grave
                ? PileDisplayTarget.Grave
                : PileDisplayTarget.Discard;
            FlyWithDelayAsync(evt.CardScreenPosition, pileAnchorView.GetScreenPosition(target),
                cardUsedShrinkDuration).Forget();
        }

        // C-6: 버림패 → 가짐패 보충
        private void OnDrawPileRefill(DrawPileRefillEvent evt)
        {
            Vector2 from = pileAnchorView.GetScreenPosition(PileDisplayTarget.Discard);
            Vector2 to   = pileAnchorView.GetScreenPosition(PileDisplayTarget.Draw);
            int count = Mathf.Min(evt.RefillCount, maxRefillAnimCards);
            for (int i = 0; i < count; i++)
                FlyWithDelayAsync(from, to, i * refillStagger).Forget();
        }

        // C-10: HandLayoutController가 직접 호출 — 턴 종료 손패 전체 이동
        public void FlyAllToDiscard(List<Vector2> screenPositions)
        {
            Vector2 to = pileAnchorView.GetScreenPosition(PileDisplayTarget.Discard);
            for (int i = 0; i < screenPositions.Count; i++)
                FlyCardAsync(screenPositions[i], to).Forget();
        }

        private async UniTaskVoid FlyWithDelayAsync(Vector2 from, Vector2 to, float delay)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            await FlyCardAsync(from, to);
        }

        private async UniTask FlyCardAsync(Vector2 fromScreen, Vector2 toScreen)
        {
            var card = (FlyingCardView)_pool.Pop();
            card.transform.localScale = Vector3.one * cardScale;

            // Trail 비활성화 → 시작 위치 이동 → 한 프레임 대기 → Trail 활성화 (trail 튀기 방지)
            await card.PlaceAt(ScreenToWorld(fromScreen), destroyCancellationToken);

            // 화면 공간에서 포물선 제어점 계산
            Vector2 controlScreen = (fromScreen + toScreen) * 0.5f + Vector2.up * arcHeightPixels;

            await LMotion.Create(0f, 1f, flyDuration)
                .WithEase(flyEase)
                .Bind(t =>
                {
                    float mt = 1f - t;

                    // 2차 베지어 곡선 위치
                    Vector2 pos = mt * mt * fromScreen
                                + 2f * mt * t * controlScreen
                                + t * t * toScreen;

                    // 접선 벡터 → 회전
                    Vector2 tangent = 2f * mt * (controlScreen - fromScreen)
                                   + 2f * t  * (toScreen - controlScreen);

                    card.transform.position = ScreenToWorld(pos);

                    if (tangent.sqrMagnitude > 0.01f)
                    {
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        card.transform.rotation =
                            Quaternion.LookRotation(-_camera.transform.forward, _camera.transform.up)
                            * Quaternion.Euler(0f, 0f, angle - 90f);
                    }
                })
                .ToUniTask(cancellationToken: destroyCancellationToken);

            _pool.Push(card);
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            if (_camera == null) _camera = Camera.main;
            return _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, flyDepth));
        }
    }
}
