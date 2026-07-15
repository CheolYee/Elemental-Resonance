using Battle.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class NewGamePopupController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup    _canvasGroup;
        [SerializeField] private Button         _normalPlayButton;
        [SerializeField] private Button         _seedPlayButton;
        [SerializeField] private Button         _closeButton;
        [SerializeField] private TMP_InputField _seedInput;
        [SerializeField] private float          _fadeDuration = 0.2f;
        [SerializeField] private string         _deckBuildingSceneName = "DeckBuilding";

        internal const string KeyPendingValue = "Seed_PendingValue";
        internal const string KeyUseCustom    = "Seed_UseCustom";

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable   = false;
            gameObject.SetActive(false);

            _seedInput.contentType       = TMP_InputField.ContentType.IntegerNumber;
            _seedPlayButton.interactable = false;

            _normalPlayButton.onClick.AddListener(OnNormalPlay);
            _seedPlayButton  .onClick.AddListener(OnSeedPlay);
            _closeButton     .onClick.AddListener(Hide);
            _seedInput.onValueChanged.AddListener(OnSeedInputChanged);
        }

        private void OnDestroy()
        {
            _normalPlayButton.onClick.RemoveListener(OnNormalPlay);
            _seedPlayButton  .onClick.RemoveListener(OnSeedPlay);
            _closeButton     .onClick.RemoveListener(Hide);
            _seedInput.onValueChanged.RemoveListener(OnSeedInputChanged);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _seedInput.text              = "";
            _seedPlayButton.interactable = false;
            FadeAsync(1f).Forget();
        }

        public void Hide() => FadeAsync(0f).Forget();

        private void OnSeedInputChanged(string value) =>
            _seedPlayButton.interactable = int.TryParse(value, out _);

        private void OnNormalPlay()
        {
            PlayerPrefs.SetInt(KeyUseCustom, 0);
            PlayerPrefs.Save();
            FadeManager.Instance?.GoToScene(_deckBuildingSceneName);
        }

        private void OnSeedPlay()
        {
            if (!int.TryParse(_seedInput.text, out int seed)) return;
            PlayerPrefs.SetInt(KeyPendingValue, seed);
            PlayerPrefs.SetInt(KeyUseCustom, 1);
            PlayerPrefs.Save();
            FadeManager.Instance?.GoToScene(_deckBuildingSceneName);
        }

        private async UniTaskVoid FadeAsync(float target)
        {
            _canvasGroup.blocksRaycasts = target > 0f;
            _canvasGroup.interactable   = target > 0f;

            await LMotion.Create(_canvasGroup.alpha, target, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => _canvasGroup.alpha = a)
                .ToUniTask(destroyCancellationToken);

            if (target <= 0f) gameObject.SetActive(false);
        }
    }
}
