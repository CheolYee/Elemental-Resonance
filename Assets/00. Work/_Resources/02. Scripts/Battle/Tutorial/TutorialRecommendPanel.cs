using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Tutorial
{
    // 타이틀 씬에서 첫 실행 시 표시되는 권장 패널.
    // _panelRect: 팝 스케일 애니메이션 대상. 풀스크린 오버레이 안쪽 박스를 연결.
    // _canvasGroup: 전체 패널 알파 페이드 대상.
    public class TutorialRecommendPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private Button        _confirmButton;
        [SerializeField] private Button        _declineButton;

        [Header("Animation")]
        [SerializeField] private float _fadeInDuration  = 0.28f;
        [SerializeField] private float _fadeOutDuration = 0.18f;
        [SerializeField] private float _popFromScale    = 0.82f;

        public event Action OnConfirmed;
        public event Action OnDeclined;

        private CancellationTokenSource _animCts;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(OnConfirmClicked);
            _declineButton.onClick.AddListener(OnDeclineClicked);

            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            _declineButton.onClick.RemoveListener(OnDeclineClicked);
            CancelAnim();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RestartAnim();
            ShowAnimAsync(_animCts.Token).Forget();
        }

        public void Hide()
        {
            RestartAnim();
            HideAnimAsync(_animCts.Token).Forget();
        }

        // ── 애니메이션 ────────────────────────────────────────────

        private async UniTaskVoid ShowAnimAsync(CancellationToken ct)
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            if (_panelRect != null)
                _panelRect.localScale = new Vector3(_popFromScale, _popFromScale, 1f);

            await UniTask.WhenAll(
                LMotion.Create(0f, 1f, _fadeInDuration)
                    .WithEase(Ease.OutCubic)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => _canvasGroup.alpha = a)
                    .ToUniTask(cancellationToken: ct),
                _panelRect != null
                    ? LMotion.Create(_popFromScale, 1f, _fadeInDuration)
                        .WithEase(Ease.OutBack)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(s => _panelRect.localScale = new Vector3(s, s, 1f))
                        .ToUniTask(cancellationToken: ct)
                    : UniTask.CompletedTask
            );

            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private async UniTaskVoid HideAnimAsync(CancellationToken ct)
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            await LMotion.Create(_canvasGroup.alpha, 0f, _fadeOutDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => _canvasGroup.alpha = a)
                .ToUniTask(cancellationToken: ct);

            gameObject.SetActive(false);
        }

        // ── 버튼 ─────────────────────────────────────────────────

        private void OnConfirmClicked()
        {
            // 씬 전환이 바로 시작되므로 페이드 애니메이션 없이 즉시 이벤트 발행
            OnConfirmed?.Invoke();
        }

        private void OnDeclineClicked()
        {
            OnDeclined?.Invoke();
            Hide();
        }

        // ── 헬퍼 ─────────────────────────────────────────────────

        private void RestartAnim()
        {
            CancelAnim();
            _animCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        }

        private void CancelAnim()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            _animCts = null;
        }
    }
}
