using System;
using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.Map.Data;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Map.UI
{
    public class RestPanelController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _areaCamera;
        [SerializeField] private CinemachineCamera _interactCamera;
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private CanvasGroup _exitPanelGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private InteractableObject _interactable;

        [Header("Rest")]
        [SerializeField] private RestConfigSO      _restConfig;
        [SerializeField] private PlayerRunStateSO  _playerRunState;
        [SerializeField] private EventChannelSO    _battleEventChannel;
        [SerializeField] private Button            _healButton;
        [SerializeField] private TextMeshProUGUI   _healButtonText;
        [SerializeField] private TextMeshProUGUI   _hpText;
        [SerializeField] private TextMeshProUGUI   _healAmountText;

        [Header("Sound")]
        [SerializeField] private EventChannelSO _soundChannel;
        [SerializeField] private SfxSounds      _healSound;
        [SerializeField] private SfxSounds      _closeSound;
        [SerializeField] private SfxSounds      _exitSound;

        [Header("Heal Feedback")]
        [SerializeField] private Color _healColor     = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private float _colorDuration = 0.5f;
        [SerializeField] private float _popScale      = 1.3f;
        [SerializeField] private float _popDuration   = 0.2f;

        [Header("Slide Animation")]
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private float _slideOffsetX = 1200f;
        [SerializeField] private Ease _slideInEase = Ease.OutCubic;
        [SerializeField] private Ease _slideOutEase = Ease.InCubic;

        public event Action OnExited;

        private CancellationTokenSource _animCts;
        private RestContentSO _currentContent;
        private int  _pendingHealAmount;
        private bool _healed;
        private Color   _hpTextDefaultColor;
        private Vector3 _hpTextBaseScale;
        private MotionHandle _hpColorMotion;
        private MotionHandle _hpScaleMotion;

        private void Awake()
        {
            _exitButton.onClick.AddListener(() =>
            {
                _soundChannel?.RaiseEvent(new PlaySoundEvent(_exitSound, Vector3.zero));
                OnExited?.Invoke();
            });
            _closeButton?.onClick.AddListener(() =>
            {
                _soundChannel?.RaiseEvent(new PlaySoundEvent(_closeSound, Vector3.zero));
                CloseInteractAsync().Forget();
            });
            if (_interactable != null)
                _interactable.OnClicked += () => OpenInteractAsync().Forget();

            _healButton?.onClick.AddListener(OnHealClicked);

            if (_hpText != null)
            {
                _hpTextDefaultColor = _hpText.color;
                _hpTextBaseScale    = _hpText.transform.localScale;
            }

            SetPanelHidden();
            SetExitPanel(false);
            SetAreaCamera(false);
            SetInteractCamera(false);
        }

        private void OnDestroy()
        {
            _animCts?.Cancel();
            _animCts?.Dispose();
            if (_hpColorMotion.IsActive()) _hpColorMotion.Cancel();
            if (_hpScaleMotion.IsActive()) _hpScaleMotion.Cancel();
        }

        public void EnterNode(RestContentSO content)
        {
            _currentContent = content;
            if (_titleText != null && content != null)
                _titleText.text = content.displayName;

            _healed = false;
            SetAreaCamera(true);
            SetExitPanel(true);
            _interactable?.SetInteractable(true);
        }

        public async UniTask OpenInteractAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            // 패널 열릴 때 회복량 계산 (1회)
            _pendingHealAmount = CalculateHealAmount();
            RefreshRestUI();

            SetInteractCamera(true);

            _panelGroup.alpha = 1f;
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;
            _panelRect.anchoredPosition = new Vector2(_slideOffsetX, _panelRect.anchoredPosition.y);

            await LMotion.Create(_slideOffsetX, 0f, _slideDuration)
                .WithEase(_slideInEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                .ToUniTask(ct);

            _panelGroup.interactable = true;
            _panelGroup.blocksRaycasts = true;
            _battleEventChannel?.RaiseEvent(new RestOpenedEvent());
        }

        public async UniTask CloseInteractAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;

            await LMotion.Create(0f, _slideOffsetX, _slideDuration)
                .WithEase(_slideOutEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                .ToUniTask(ct);

            SetPanelHidden();
            SetInteractCamera(false);
            _interactable?.SetInteractable(true);
            _battleEventChannel?.RaiseEvent(new RestPanelClosedEvent());
        }

        public async UniTask ExitAsync()
        {
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(
                _animCts.Token, destroyCancellationToken).Token;

            if (_panelGroup.interactable)
            {
                _panelGroup.interactable = false;
                _panelGroup.blocksRaycasts = false;

                await LMotion.Create(0f, _slideOffsetX, _slideDuration)
                    .WithEase(_slideOutEase)
                    .Bind(x => _panelRect.anchoredPosition = new Vector2(x, _panelRect.anchoredPosition.y))
                    .ToUniTask(ct);
            }

            SetPanelHidden();
            SetExitPanel(false);
            SetInteractCamera(false);
            SetAreaCamera(false);
            _interactable?.SetInteractable(false);
        }

        private void OnHealClicked()
        {
            if (_playerRunState == null || _healed) return;

            int oldHp = _playerRunState.CurrentHp;
            _playerRunState.Heal(_pendingHealAmount);
            int newHp = _playerRunState.CurrentHp;

            _healed = true;
            _soundChannel?.RaiseEvent(new PlaySoundEvent(_healSound, Vector3.zero));
            _battleEventChannel?.RaiseEvent(new RestHealedEvent());
            _battleEventChannel?.RaiseEvent(
                new PlayerHpChangedEvent(oldHp, newHp, _playerRunState.MaxHp, isHeal: true));

            RefreshRestUI();
            PlayHpTextFeedback();
        }

        private void PlayHpTextFeedback()
        {
            if (_hpText == null) return;

            if (_hpColorMotion.IsActive()) _hpColorMotion.Cancel();
            _hpText.color = _healColor;
            _hpColorMotion = LMotion.Create(0f, 1f, _colorDuration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(t => { if (_hpText != null) _hpText.color = Color.Lerp(_healColor, _hpTextDefaultColor, t); });

            if (_hpScaleMotion.IsActive()) _hpScaleMotion.Cancel();
            _hpText.transform.localScale = _hpTextBaseScale * _popScale;
            _hpScaleMotion = LMotion.Create(_popScale, 1f, _popDuration)
                .WithEase(Ease.OutBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (_hpText != null) _hpText.transform.localScale = _hpTextBaseScale * s; });
        }

        private int CalculateHealAmount()
        {
            if (_restConfig == null || _playerRunState == null) return 0;
            float percent = UnityEngine.Random.Range(_restConfig.minHealPercent, _restConfig.maxHealPercent);
            return Mathf.Max(1, Mathf.RoundToInt(_playerRunState.MaxHp * percent));
        }

        private void RefreshRestUI()
        {
            if (_playerRunState == null) return;

            bool isMaxHp = _playerRunState.CurrentHp >= _playerRunState.MaxHp;

            if (_hpText != null)
                _hpText.text = $"{_playerRunState.CurrentHp}/{_playerRunState.MaxHp}";

            if (_healAmountText != null)
                _healAmountText.gameObject.SetActive(!isMaxHp && !_healed);

            if (!isMaxHp && !_healed && _healAmountText != null)
                _healAmountText.text = $"+{_pendingHealAmount} HP";

            if (_healButton != null)
                _healButton.interactable = !isMaxHp && !_healed;

            if (_healButtonText != null)
                _healButtonText.text = isMaxHp ? "HP 최대" : "휴식";
        }

        private void SetExitPanel(bool active)
        {
            if (_exitPanelGroup == null) return;
            _exitPanelGroup.alpha          = active ? 1f : 0f;
            _exitPanelGroup.interactable   = active;
            _exitPanelGroup.blocksRaycasts = active;
        }

        private void SetAreaCamera(bool active)
        {
            if (_areaCamera != null) _areaCamera.Priority = active ? 20 : -5;
        }

        private void SetInteractCamera(bool active)
        {
            if (_interactCamera != null) _interactCamera.Priority = active ? 25 : -5;
        }

        private void SetPanelHidden()
        {
            _panelGroup.alpha = 0f;
            _panelGroup.interactable = false;
            _panelGroup.blocksRaycasts = false;
            if (_panelRect != null)
                _panelRect.anchoredPosition = new Vector2(_slideOffsetX, _panelRect.anchoredPosition.y);
        }
    }
}
