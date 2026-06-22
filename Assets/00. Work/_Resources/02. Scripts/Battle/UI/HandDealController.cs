using System;
using System.Collections.Generic;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace Battle.UI
{
    public class HandDealController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandLayoutController handLayoutController;
        [SerializeField] private CardFlyAnimator cardFlyAnimator;
        [SerializeField] private int initialDrawCount = 5;
        [SerializeField] private float dealStaggerDelay = 0.12f;

        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.AddListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEndRequest);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<SkillDrawCardsRequestEvent>(OnSkillDrawCardsRequest);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PlayerTurnStartEvent>(OnPlayerTurnStart);
            battleEventChannel.RemoveListener<PlayerTurnEndRequestEvent>(OnPlayerTurnEndRequest);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<SkillDrawCardsRequestEvent>(OnSkillDrawCardsRequest);
        }

        private void OnSessionStart(BattleSessionStartEvent _) => _battleEnded = false;

        private void OnPlayerTurnStart(PlayerTurnStartEvent _)
        {
            if (_battleEnded) return;
            DealStartingCardsAsync().Forget();
        }

        private void OnPlayerTurnEndRequest(PlayerTurnEndRequestEvent _) => DiscardHand();

        private void OnWaveClear(WaveClearEvent _) => SilentDiscardHand();

        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        private async UniTaskVoid DealStartingCardsAsync()
        {
            battleEventChannel.RaiseEvent(new CardDrawStartEvent());

            // SlideIn 애니메이션 완료 대기 후 카드 딜 시작
            await UniTask.Delay(TimeSpan.FromSeconds(handLayoutController.SlideDuration), cancellationToken: destroyCancellationToken);

            var cards = deckController.DrawCards(initialDrawCount);

            foreach (var cardInstance in cards)
            {
                var view = handLayoutController.AddCard(cardInstance);
                view.SetInteractable(false);
                soundChannel?.RaiseEvent(new PlaySoundEvent(SfxSounds.CARD_DROW, Vector3.zero));
                await UniTask.Delay(TimeSpan.FromSeconds(dealStaggerDelay), cancellationToken: destroyCancellationToken);
            }

            // 마지막 카드 트윈 완료 대기
            await UniTask.Delay(TimeSpan.FromSeconds(handLayoutController.TweenDuration), cancellationToken: destroyCancellationToken);

            var handCards = handLayoutController.HandCards;
            for (int i = 0; i < handCards.Count; i++)
                handCards[i].SetInteractable(true);

            battleEventChannel.RaiseEvent(new CardDrawEndEvent());
        }

        private void DiscardHand()
        {
            handLayoutController.ForceReturnDraggedCard();
            var handCards = handLayoutController.HandCards;
            if (cardFlyAnimator != null && handCards.Count > 0)
            {
                var positions = new List<Vector2>(handCards.Count);
                foreach (var view in handCards)
                    positions.Add(view.transform.position);
                cardFlyAnimator.FlyAllToDiscard(positions);
            }
            deckController.DiscardAllHand();
            handLayoutController.ReturnAllToPool();
        }

        // Wave 전환 시 조용히 손패 제거 — 비행 연출 없음
        private void SilentDiscardHand()
        {
            deckController.DiscardAllHand();
            handLayoutController.ReturnAllToPool();
        }

        private void OnSkillDrawCardsRequest(SkillDrawCardsRequestEvent evt)
        {
            if (_battleEnded) { evt.Tcs.TrySetResult(); return; }
            DealSkillCardsAsync(evt, evt.Tcs).Forget();
        }

        private async UniTaskVoid DealSkillCardsAsync(SkillDrawCardsRequestEvent evt, UniTaskCompletionSource tcs)
        {
            // PreDrawnCards가 있으면 DeckController가 이미 Hand로 이동 완료 → AddCard 애니메이션만 수행
            IReadOnlyList<CardInstance> cards = evt.PreDrawnCards != null
                ? evt.PreDrawnCards
                : deckController.DrawCards(evt.DrawCount);

            foreach (var cardInstance in cards)
            {
                var view = handLayoutController.AddCard(cardInstance);
                view.SetInteractable(false);
                soundChannel?.RaiseEvent(new PlaySoundEvent(SfxSounds.CARD_DROW, Vector3.zero));
                await UniTask.Delay(TimeSpan.FromSeconds(dealStaggerDelay), cancellationToken: destroyCancellationToken);
            }
            await UniTask.Delay(TimeSpan.FromSeconds(handLayoutController.TweenDuration), cancellationToken: destroyCancellationToken);

            if (!_battleEnded)
            {
                var handCards = handLayoutController.HandCards;
                for (int i = 0; i < handCards.Count; i++)
                    handCards[i].SetInteractable(true);
            }

            tcs.TrySetResult();
        }
    }
}
