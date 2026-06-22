using System;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private BgmSounds      _titleBgm      = BgmSounds.START;
        [SerializeField] private float          _bgmFadeIn     = 1f;

        [Header("Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Settings Panel")]
        [SerializeField] private SettingsPanelController _settingsPanel;

        [Header("Slide-In")]
        [SerializeField] private RectTransform[] _slideInTargets;
        [SerializeField] private float           _slideOffsetX  = -600f;
        [SerializeField] private float           _slideDuration  = 0.45f;
        [SerializeField] private float           _staggerDelay   = 0.08f;

        [Header("Scene Names")]
        [SerializeField] private string _mainSceneName         = "Main";
        [SerializeField] private string _deckBuildingSceneName = "DeckBuilding";

        private void Start()
        {
            PlayBgm();
            SetupButtons();
            SlideInAsync().Forget();
        }

        private void OnDestroy()
        {
            _newGameButton ?.onClick.RemoveListener(OnNewGame);
            _continueButton?.onClick.RemoveListener(OnContinue);
            _settingsButton?.onClick.RemoveListener(OnSettings);
            _quitButton    ?.onClick.RemoveListener(OnQuit);
        }

        // ── BGM ─────────────────────────────────────────────────

        private void PlayBgm()
        {
            _soundChannel.RaiseEvent(new PlayManagedSoundEvent(
                _titleBgm, Vector3.zero, SoundChannelId.Bgm,
                fadeInDuration:  _bgmFadeIn,
                fadeOutDuration: 0f,
                crossfadeExisting: true));
        }

        // ── 버튼 ────────────────────────────────────────────────

        private void SetupButtons()
        {
            if (_continueButton != null)
                _continueButton.interactable = SaveController.HasSaveData();

            _newGameButton ?.onClick.AddListener(OnNewGame);
            _continueButton?.onClick.AddListener(OnContinue);
            _settingsButton?.onClick.AddListener(OnSettings);
            _quitButton    ?.onClick.AddListener(OnQuit);
        }

        private void OnNewGame()  => FadeManager.Instance?.GoToScene(_deckBuildingSceneName);
        private void OnContinue() { SaveController.SetPendingLoad(); FadeManager.Instance?.GoToScene(_mainSceneName); }
        private void OnSettings() => _settingsPanel?.Toggle();
        private void OnQuit()     => Application.Quit();

        // ── 슬라이드 인 ─────────────────────────────────────────

        private async UniTaskVoid SlideInAsync()
        {
            if (_slideInTargets == null || _slideInTargets.Length == 0) return;

            var ct = destroyCancellationToken;

            var originals = new Vector2[_slideInTargets.Length];
            for (int i = 0; i < _slideInTargets.Length; i++)
            {
                originals[i] = _slideInTargets[i].anchoredPosition;
                _slideInTargets[i].anchoredPosition =
                    new Vector2(originals[i].x + _slideOffsetX, originals[i].y);
            }

            for (int i = 0; i < _slideInTargets.Length; i++)
            {
                int    idx    = i;
                float  fromX  = _slideInTargets[idx].anchoredPosition.x;
                float  toX    = originals[idx].x;
                float  fixedY = originals[idx].y;

                LMotion.Create(fromX, toX, _slideDuration)
                    .WithEase(Ease.OutBack)
                    .Bind(x => _slideInTargets[idx].anchoredPosition = new Vector2(x, fixedY))
                    .ToUniTask(ct)
                    .Forget();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_staggerDelay),
                    cancellationToken: ct);
            }
        }
    }
}
