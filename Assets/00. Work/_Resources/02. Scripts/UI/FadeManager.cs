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

            await LMotion.Create(0f, 1f, _fadeDuration)
                .WithEase(Ease.InCubic)
                .Bind(a => _overlay.alpha = a)
                .ToUniTask(destroyCancellationToken);

            await SceneManager.LoadSceneAsync(sceneName);

            await LMotion.Create(1f, 0f, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .Bind(a => _overlay.alpha = a)
                .ToUniTask(destroyCancellationToken);

            _overlay.blocksRaycasts = false;
            _isTransitioning        = false;
        }
    }
}
