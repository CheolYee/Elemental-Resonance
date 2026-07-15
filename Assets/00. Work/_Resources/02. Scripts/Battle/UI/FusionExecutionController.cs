using System;
using Battle.Enums;
using Battle.Events;
using Battle.Fusion;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class FusionExecutionController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private HandLayoutController handLayoutController;
        [SerializeField] private DeckController deckController;
        [SerializeField] private CardFlyAnimator cardFlyAnimator;
        [SerializeField] private FusionPanel fusionPanel;

        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds      fusionSound;

        [Inject] private FusionRecipeService _fusionRecipeService;
        [Inject] private FusionResultPicker  _fusionResultPicker;

        private void OnEnable()  => battleEventChannel.AddListener<CardFusionRequestedEvent>(OnFusionRequested);
        private void OnDisable() => battleEventChannel.RemoveListener<CardFusionRequestedEvent>(OnFusionRequested);

        private void OnFusionRequested(CardFusionRequestedEvent evt) => ExecuteFusionAsync(evt).Forget();

        private async UniTaskVoid ExecuteFusionAsync(CardFusionRequestedEvent evt)
        {
            // 1. 레시피 검증
            if (!_fusionRecipeService.TryGetResult(
                    evt.DraggedCard.data.elementType,
                    evt.TargetCard.data.elementType,
                    out var resultElement))
            {
                battleEventChannel.RaiseEvent(new CardReturnToHandEvent(evt.DraggedCard));
                return;
            }

            // 2. 손패 입력 잠금
            handLayoutController.SetAllCardsInteractable(false);

            // 3. 뷰 분리 (풀 반환 없이 참조만 획득)
            if (!handLayoutController.TryDetachFusionMaterials(
                    evt.DraggedCard, evt.TargetCard,
                    out var draggedView, out var targetView,
                    out var draggedPos, out var targetPos))
            {
                handLayoutController.SetAllCardsInteractable(true);
                return;
            }

            // 4. 덱 데이터에서 재료 소모
            deckController.ConsumeFusionMaterial(evt.DraggedCard);
            deckController.ConsumeFusionMaterial(evt.TargetCard);

            // 5. 팝 → 수축 연출 (두 카드 동시)
            soundChannel?.RaiseEvent(new PlaySoundEvent(fusionSound, Vector3.zero));
            await UniTask.WhenAll(
                draggedView.PlayFusionDepartureAsync(destroyCancellationToken),
                targetView.PlayFusionDepartureAsync(destroyCancellationToken));

            // 6. 뷰 풀 반환
            handLayoutController.ReturnViewToPool(draggedView);
            handLayoutController.ReturnViewToPool(targetView);

            // 7. FlyingCard → Grave (두 카드 동시)
            await UniTask.WhenAll(
                cardFlyAnimator.FlyToGraveAsync(draggedPos),
                cardFlyAnimator.FlyToGraveAsync(targetPos));

            // 8. 결과 카드 추첨
            var maxGrade = (CardGrade)Mathf.Max(
                (int)evt.DraggedCard.data.grade,
                (int)evt.TargetCard.data.grade);
            var resultCard = _fusionResultPicker.Pick(resultElement, maxGrade);

            if (resultCard == null)
            {
                Debug.LogWarning("[FusionExecutionController] 결과 카드 없음 — 합성 취소");
                handLayoutController.SetAllCardsInteractable(true);
                return;
            }

            // 10. 합성 패널 표시 및 닫힘 대기
            await fusionPanel.ShowAsync(resultCard, destroyCancellationToken);

            // 11. 결과 카드 손패에 추가
            deckController.AddFusionCard(resultCard);
            var fusionView = handLayoutController.AddCard(resultCard);

            // 12. 손패 입력 잠금 해제
            handLayoutController.SetAllCardsInteractable(true);
            // 카드 레이아웃 트윈이 끝난 후 발행 — CardRect로 정확한 합성 카드 위치 전달
            await UniTask.Delay(
                TimeSpan.FromSeconds(handLayoutController.TweenDuration),
                ignoreTimeScale: true,
                cancellationToken: destroyCancellationToken);
            battleEventChannel.RaiseEvent(new FusionCompletedEvent(fusionView.GetComponent<RectTransform>()));
        }
    }
}
