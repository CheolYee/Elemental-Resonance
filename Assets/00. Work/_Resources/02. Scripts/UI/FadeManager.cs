using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battle.UI
{
    public class FadeManager : MonoBehaviour
    {
        public static FadeManager Instance { get; private set; }

        [SerializeField] private CanvasGroup _overlay;
        [SerializeField] private float       _fadeDuration = 0.3f;

        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _overlay.alpha          = 0f;
            _overlay.blocksRaycasts = false;
        }

        public void GoToScene(string sceneName)
        {
            if (_isTransitioning) return;
            TransitionAsync(sceneName).Forget();
        }

        private async UniTask TransitionAsync(string sceneName)
        {
            _isTransitioning        = true;
            _overlay.blocksRaycasts = true;

            // 1. fade-in 완료 (씬 로드 전 — 로딩 부하가 모션에 영향 없도록)
            await LMotion.Create(0f, 1f, _fadeDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => _overlay.alpha = a)
                .ToUniTask(destroyCancellationToken);
            _overlay.alpha = 1f;

            // 2. 화면이 검은 상태에서 씬 로드 — 준비 완료 후 활성화
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;
            await UniTask.WaitUntil(() => asyncOp.progress >= 0.9f, cancellationToken: destroyCancellationToken);
            asyncOp.allowSceneActivation = true;
            await UniTask.WaitUntil(() => asyncOp.isDone, cancellationToken: destroyCancellationToken);

            // 3. fade-out
            await LMotion.Create(1f, 0f, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => _overlay.alpha = a)
                .ToUniTask(destroyCancellationToken);
            _overlay.alpha          = 0f;
            _overlay.blocksRaycasts = false;
            _isTransitioning        = false;
        }
    }
}
