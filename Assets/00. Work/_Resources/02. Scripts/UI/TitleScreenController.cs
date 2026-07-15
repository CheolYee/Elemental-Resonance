using System;
using Battle.Tutorial;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private Button _tutorialButton;

        [Header("Settings Panel")]
        [SerializeField] private SettingsPanelController _settingsPanel;

        [Header("New Game Popup")]
        [SerializeField] private NewGamePopupController _newGamePopup;

        [Header("Tutorial")]
        [SerializeField] private TutorialRecommendPanel _tutorialRecommendPanel;

        [Header("Slide-In")]
        [SerializeField] private RectTransform[] _slideInTargets;
        [SerializeField] private float           _slideOffsetX  = -600f;
        [SerializeField] private float           _slideDuration  = 0.45f;
        [SerializeField] private float           _staggerDelay   = 0.08f;

        [Header("Scene Names")]
        [SerializeField] private string _mainSceneName         = "Main";

        [Header("Dev")]
        [SerializeField] private bool _devShortcutsEnabled;

        private const string KeyTutorialPending  = "tutorial_pending";
        private const string KeyTutorialDeclined = "tutorial_declined";

        private void Start()
        {
            PlayBgm();
            SetupButtons();
            SlideInAsync().Forget();
            CheckFirstRun();
        }

        private void Update()
        {
            if (!_devShortcutsEnabled) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if ((kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) && kb.qKey.wasPressedThisFrame)
                StartDevTestRun();
        }

        private void StartDevTestRun()
        {
            PlayerPrefs.SetInt(NewGamePopupController.KeyUseCustom, 0);
            PlayerPrefs.Save();
            FadeManager.Instance?.GoToScene(_mainSceneName);
        }

        private void OnDestroy()
        {
            _newGameButton ?.onClick.RemoveListener(OnNewGame);
            _continueButton?.onClick.RemoveListener(OnContinue);
            _settingsButton?.onClick.RemoveListener(OnSettings);
            _quitButton    ?.onClick.RemoveListener(OnQuit);
            _tutorialButton?.onClick.RemoveListener(OnTutorial);
            if (_tutorialRecommendPanel != null)
            {
                _tutorialRecommendPanel.OnConfirmed -= OnTutorial;
                _tutorialRecommendPanel.OnDeclined  -= OnTutorialDeclined;
            }
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
            _tutorialButton?.onClick.AddListener(OnTutorial);

            if (_tutorialRecommendPanel != null)
            {
                _tutorialRecommendPanel.OnConfirmed += OnTutorial;
                _tutorialRecommendPanel.OnDeclined  += OnTutorialDeclined;
            }
        }

        private void CheckFirstRun()
        {
            bool declined  = PlayerPrefs.GetInt(KeyTutorialDeclined, 0) == 1;
            if (!TutorialController.IsTutorialCompleted() && !declined)
                _tutorialRecommendPanel?.Show();
        }

        private void OnTutorialDeclined()
        {
            PlayerPrefs.SetInt(KeyTutorialDeclined, 1);
            PlayerPrefs.Save();
        }

        private void OnNewGame()  => _newGamePopup?.Show();
        private void OnContinue() { SaveController.SetPendingLoad(); FadeManager.Instance?.GoToScene(_mainSceneName); }
        private void OnSettings() => _settingsPanel?.Toggle();
        private void OnQuit()     => Application.Quit();

        private void OnTutorial()
        {
            PlayerPrefs.SetInt(KeyTutorialPending, 1);
            PlayerPrefs.Save();
            FadeManager.Instance?.GoToScene(_mainSceneName);
        }

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
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
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
