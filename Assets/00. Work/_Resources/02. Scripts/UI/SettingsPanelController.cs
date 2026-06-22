using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Battle.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        private const string KeyMaster     = "Setting_MasterVolume";
        private const string KeyBgm        = "Setting_BgmVolume";
        private const string KeySfx        = "Setting_SfxVolume";
        private const string KeyResolution = "Setting_Resolution";
        private const string KeyFullscreen = "Setting_Fullscreen";

        private static readonly (int w, int h)[] Resolutions = { (1280, 720), (1920, 1080) };

        [SerializeField] private AudioMixer _audioMixer;

        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button      _closeButton;
        [SerializeField] private Button      _returnToTitleButton;
        [SerializeField] private bool        _showReturnButton;
        [SerializeField] private float       _fadeDuration = 0.2f;

        [Header("Save (Main Scene Only)")]
        [SerializeField] private SaveController _saveController;

        [Header("Volume")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Display")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle   _fullscreenToggle;

        private bool _isOpen;

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable   = false;

            if (_returnToTitleButton != null)
                _returnToTitleButton.gameObject.SetActive(_showReturnButton);

            LoadAndApplyAll();
            RegisterListeners();
        }

        private void OnDestroy() => UnregisterListeners();

        // ── 공개 API ────────────────────────────────────────────

        public void Show()
        {
            if (_isOpen) return;
            _isOpen = true;
            gameObject.SetActive(true);
            FadeAsync(1f).Forget();
        }

        public void Hide()
        {
            if (!_isOpen) return;
            _isOpen = false;
            FadeAsync(0f).Forget();
        }

        public void Toggle() { if (_isOpen) Hide(); else Show(); }

        // ── 초기화 ──────────────────────────────────────────────

        private void LoadAndApplyAll()
        {
            float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            float bgm    = PlayerPrefs.GetFloat(KeyBgm,    1f);
            float sfx    = PlayerPrefs.GetFloat(KeySfx,    1f);
            int   res    = PlayerPrefs.GetInt(KeyResolution, 1);
            bool  full   = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;

            _masterSlider.SetValueWithoutNotify(master);
            _bgmSlider   .SetValueWithoutNotify(bgm);
            _sfxSlider   .SetValueWithoutNotify(sfx);

            SetupResolutionDropdown(res);
            _fullscreenToggle.SetIsOnWithoutNotify(full);

            ApplyVolume("MasterVolume", master);
            ApplyVolume("BGMVolume",    bgm);
            ApplyVolume("SFXVolume",    sfx);
            Screen.SetResolution(Resolutions[res].w, Resolutions[res].h, full);
        }

        private void SetupResolutionDropdown(int selected)
        {
            _resolutionDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var r in Resolutions)
                options.Add($"{r.w} × {r.h}");
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(selected);
        }

        // ── 이벤트 연결 ─────────────────────────────────────────

        private void RegisterListeners()
        {
            _masterSlider.onValueChanged.AddListener(OnMasterChanged);
            _bgmSlider   .onValueChanged.AddListener(OnBgmChanged);
            _sfxSlider   .onValueChanged.AddListener(OnSfxChanged);
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            _fullscreenToggle  .onValueChanged.AddListener(OnFullscreenChanged);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);
            if (_returnToTitleButton != null)
                _returnToTitleButton.onClick.AddListener(OnReturnToTitle);
        }

        private void UnregisterListeners()
        {
            _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            _bgmSlider   .onValueChanged.RemoveListener(OnBgmChanged);
            _sfxSlider   .onValueChanged.RemoveListener(OnSfxChanged);
            _resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            _fullscreenToggle  .onValueChanged.RemoveListener(OnFullscreenChanged);
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Hide);
            if (_returnToTitleButton != null)
                _returnToTitleButton.onClick.RemoveListener(OnReturnToTitle);
        }

        private void OnReturnToTitle() => _saveController?.SaveAndReturnToTitle();

        // ── 슬라이더 핸들러 ─────────────────────────────────────

        private void OnMasterChanged(float v)
        {
            ApplyVolume("MasterVolume", v);
            PlayerPrefs.SetFloat(KeyMaster, v);
            PlayerPrefs.Save();
        }

        private void OnBgmChanged(float v)
        {
            ApplyVolume("BGMVolume", v);
            PlayerPrefs.SetFloat(KeyBgm, v);
            PlayerPrefs.Save();
        }

        private void OnSfxChanged(float v)
        {
            ApplyVolume("SFXVolume", v);
            PlayerPrefs.SetFloat(KeySfx, v);
            PlayerPrefs.Save();
        }

        // ── 해상도 / 전체화면 ────────────────────────────────────

        private void OnResolutionChanged(int index)
        {
            bool full = _fullscreenToggle.isOn;
            Screen.SetResolution(Resolutions[index].w, Resolutions[index].h, full);
            PlayerPrefs.SetInt(KeyResolution, index);
            PlayerPrefs.Save();
        }

        private void OnFullscreenChanged(bool full)
        {
            int index = _resolutionDropdown.value;
            Screen.SetResolution(Resolutions[index].w, Resolutions[index].h, full);
            PlayerPrefs.SetInt(KeyFullscreen, full ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ── 유틸 ────────────────────────────────────────────────

        private void ApplyVolume(string parameter, float linearValue)
        {
            float db = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
            _audioMixer.SetFloat(parameter, db);
        }

        private async UniTaskVoid FadeAsync(float target)
        {
            _canvasGroup.blocksRaycasts = target > 0f;
            _canvasGroup.interactable   = target > 0f;

            await LMotion.Create(_canvasGroup.alpha, target, _fadeDuration)
                .WithEase(Ease.OutCubic)
                .Bind(a => _canvasGroup.alpha = a)
                .ToUniTask(destroyCancellationToken);

            if (target <= 0f)
                gameObject.SetActive(false);
        }
    }
}
