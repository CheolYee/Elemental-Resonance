using System;
using System.Threading;
using Battle.Enums;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.UI
{
    public class FusionPanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Image glowImage;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardSilhouette;
        [SerializeField] private CardView resultCardView;
        [SerializeField] private Image whiteFlashOverlay;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration      = 0.3f;
        [SerializeField] private float gradeStepDelay      = 0.4f;
        [SerializeField] private float gradePopDuration    = 0.2f;
        [SerializeField] private float flashDuration       = 0.25f;
        [SerializeField] private float whiteFlashInDuration  = 0.1f;
        [SerializeField] private float whiteFlashOutDuration = 0.25f;
        [SerializeField] private float revealDuration      = 0.4f;
        [SerializeField] private float fadeOutDuration     = 0.25f;

        [Header("Glow")]
        [SerializeField] private float glowAlpha = 0.8f;

        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [Tooltip("인덱스 = CardGrade 순서 (Normal=0, Rare=1, Epic=2, Legendary=3)")]
        [SerializeField] private SfxSounds[] gradePopSounds;
        [SerializeField] private SfxSounds   revealSound;

        private bool _isAnimating;
        private CancellationTokenSource _skipCts;
        private UniTaskCompletionSource _closeTcs;
        private Vector3 _resultCardOriginalScale;

        private void Awake()
        {
            // Inspector에서 설정한 ResultCardView 스케일 보존
            if (resultCardView != null)
                _resultCardOriginalScale = resultCardView.transform.localScale;

            // 초기 상태: 투명 + 인터랙션 차단 없음
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable   = false;
        }

        public async UniTask ShowAsync(CardInstance result, CancellationToken destroyCt)
        {
            // 패널 표시 시작 — 인터랙션 차단 활성
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable   = true;

            cardSilhouette.SetActive(true);
            cardContainer.localScale = Vector3.one;
            resultCardView.gameObject.SetActive(false);
            resultCardView.Setup(result);
            resultCardView.SetInteractable(false);

            // 패널 페이드 인
            await LMotion.Create(0f, 1f, fadeInDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => panelCanvasGroup.alpha = a)
                .ToUniTask(cancellationToken: destroyCt);

            // 등급 공개 애니메이션
            _isAnimating = true;
            _skipCts     = CancellationTokenSource.CreateLinkedTokenSource(destroyCt);

            var resultGrade = result.data.grade;
            bool skipped    = false;

            try
            {
                for (int g = 0; g <= (int)resultGrade; g++)
                {
                    if (_skipCts.Token.IsCancellationRequested) break;

                    if (gradePopSounds != null && g < gradePopSounds.Length)
                        PlaySfx(gradePopSounds[g]);

                    var gradeColor = CardColorUtility.GradeColors[(CardGrade)g];
                    glowImage.color = new Color(gradeColor.r, gradeColor.g, gradeColor.b, glowAlpha);

                    // 스케일 팝 (cardContainer + glowImage 동시)
                    await LMotion.Create(1f, 1.2f, gradePopDuration * 0.5f)
                        .WithEase(Ease.OutQuad)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(s =>
                        {
                            cardContainer.localScale        = Vector3.one * s;
                            glowImage.transform.localScale  = Vector3.one * s;
                        })
                        .ToUniTask(cancellationToken: _skipCts.Token);

                    await LMotion.Create(1.2f, 1f, gradePopDuration * 0.5f)
                        .WithEase(Ease.InQuad)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(s =>
                        {
                            cardContainer.localScale        = Vector3.one * s;
                            glowImage.transform.localScale  = Vector3.one * s;
                        })
                        .ToUniTask(cancellationToken: _skipCts.Token);

                    if (g < (int)resultGrade)
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(gradeStepDelay),
                            cancellationToken: _skipCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                skipped = true;
            }

            if (destroyCt.IsCancellationRequested) return;

            // 최종 등급 색으로 즉시 보정
            var finalColor = CardColorUtility.GradeColors[resultGrade];
            glowImage.color                = new Color(finalColor.r, finalColor.g, finalColor.b, glowAlpha);
            cardContainer.localScale       = Vector3.one;
            glowImage.transform.localScale = Vector3.one;
            _isAnimating             = false;

            // 번쩍임 (스킵하지 않은 경우만)
            if (!skipped)
            {
                var capturedColor = glowImage.color;
                await LMotion.Create(glowAlpha, 1f, flashDuration * 0.5f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => glowImage.color = new Color(capturedColor.r, capturedColor.g, capturedColor.b, a))
                    .ToUniTask(cancellationToken: destroyCt);
                await LMotion.Create(1f, glowAlpha, flashDuration * 0.5f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => glowImage.color = new Color(capturedColor.r, capturedColor.g, capturedColor.b, a))
                    .ToUniTask(cancellationToken: destroyCt);
            }

            // 흰색 플래시 — in
            if (whiteFlashOverlay != null)
            {
                whiteFlashOverlay.color = new Color(1f, 1f, 1f, 0f);
                await LMotion.Create(0f, 1f, whiteFlashInDuration)
                    .WithEase(Ease.OutQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => whiteFlashOverlay.color = new Color(1f, 1f, 1f, a))
                    .ToUniTask(cancellationToken: destroyCt);
            }

            // 플래시 정점에서 카드 교체
            cardSilhouette.SetActive(false);
            resultCardView.gameObject.SetActive(true);
            resultCardView.transform.localScale = Vector3.zero;
            PlaySfx(revealSound);

            // 흰색 플래시 — out + 카드 reveal 동시 진행
            if (whiteFlashOverlay != null)
            {
                LMotion.Create(1f, 0f, whiteFlashOutDuration)
                    .WithEase(Ease.InQuad)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => whiteFlashOverlay.color = new Color(1f, 1f, 1f, a))
                    .ToUniTask(cancellationToken: destroyCt)
                    .Forget();
            }

            await LMotion.Create(0f, 1f, revealDuration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => resultCardView.transform.localScale = _resultCardOriginalScale * s)
                .ToUniTask(cancellationToken: destroyCt);

            // 클릭 대기 (패널 닫기)
            _closeTcs = new UniTaskCompletionSource();
            await _closeTcs.Task.AttachExternalCancellation(destroyCt);

            // 패널 페이드 아웃
            await LMotion.Create(1f, 0f, fadeOutDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => panelCanvasGroup.alpha = a)
                .ToUniTask(cancellationToken: destroyCt);

            // 인터랙션 차단 해제 (SetActive 없이 CanvasGroup으로 제어)
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable   = false;
        }

        private void PlaySfx(SfxSounds sound)
        {
            if (soundChannel == null) return;
            soundChannel.RaiseEvent(new PlaySoundEvent(sound, Vector3.zero));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isAnimating)
                _skipCts?.Cancel();
            else
                _closeTcs?.TrySetResult();
        }
    }
}
