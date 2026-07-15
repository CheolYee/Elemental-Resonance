using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Tutorial
{
    // Sort Order 99 캔버스에 부착. 반투명 어둠 패널 제어.
    // ClickToContinue 단계: blockingButton이 전체 화면을 덮어 클릭을 수신.
    // WaitForEvent 단계: raycastTarget=false로 하단 UI 입력을 허용.
    public class TutorialOverlayView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Button blockingButton;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration  = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private float overlayAlpha    = 0.75f;

        private Action _onClicked;

        private void Awake()
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            SetInteractable(false);
            blockingButton.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy() => blockingButton.onClick.RemoveListener(HandleClicked);

        public async UniTask ShowAsync(bool blockInput, Action onClicked, CancellationToken ct)
        {
            _onClicked = onClicked;
            SetInteractable(blockInput);

            // WaitForEvent 단계(blockInput=false)에서는 딤 오버레이를 표시하지 않는다.
            // 게임 UI가 오버레이 위에서 자연스럽게 클릭될 수 있도록 투명 유지.
            if (!blockInput)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                return;
            }

            overlayGroup.blocksRaycasts = true;
            await LMotion.Create(overlayGroup.alpha, overlayAlpha, fadeInDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => overlayGroup.alpha = a)
                .ToUniTask(cancellationToken: ct);
        }

        public async UniTask HideAsync(CancellationToken ct)
        {
            _onClicked = null;
            SetInteractable(false);
            overlayGroup.blocksRaycasts = false;
            if (overlayGroup.alpha <= 0f) return;
            await LMotion.Create(overlayGroup.alpha, 0f, fadeOutDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => overlayGroup.alpha = a)
                .ToUniTask(cancellationToken: ct);
        }

        public void HideImmediate()
        {
            _onClicked = null;
            overlayGroup.alpha = 0f;
            SetInteractable(false);
        }

        private void SetInteractable(bool value)
        {
            blockingButton.image.raycastTarget = value;
            blockingButton.interactable = value;
        }

        private void HandleClicked() => _onClicked?.Invoke();
    }
}
