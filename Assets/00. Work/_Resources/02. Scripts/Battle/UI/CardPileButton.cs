using System.Threading;
using Battle.Enums;
using Battle.Events;
using Battle.Map.Enums;
using Battle.Map.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class CardPileButton : MonoBehaviour
    {
        [SerializeField] private EventChannelSO       battleEventChannel;
        [SerializeField] private MapOverlayController mapOverlayController;
        [SerializeField] private PileDisplayTarget    pileTarget;

        [Header("Feedback")]
        [SerializeField] private float clickWobbleAngle = 15f;
        [SerializeField] private float clickWobbleDuration = 0.25f;
        [SerializeField] private float arrivalPopScale = 1.12f;
        [SerializeField] private float arrivalPopDuration = 0.18f;

        private Button _button;
        private bool   _battleEnded;
        private bool   _isMapOpen;
        private CancellationTokenSource _feedbackCts;

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.AddListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.AddListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.AddListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.AddListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<CardArrivedAtPileEvent>(OnCardArrived);
            if (mapOverlayController != null)
                mapOverlayController.OnStateChanged += OnMapStateChanged;
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PileDetailPanelClosedEvent>(OnPanelClosed);
            battleEventChannel.RemoveListener<BattleUIHiddenEvent>(OnBattleUIHidden);
            battleEventChannel.RemoveListener<BattleUIShownEvent>(OnBattleUIShown);
            battleEventChannel.RemoveListener<CardDrawStartEvent>(OnCardDrawStart);
            battleEventChannel.RemoveListener<CardDrawEndEvent>(OnCardDrawEnd);
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<CardArrivedAtPileEvent>(OnCardArrived);
            if (mapOverlayController != null)
                mapOverlayController.OnStateChanged -= OnMapStateChanged;
            _feedbackCts?.Cancel();
        }

        private void OnClick()
        {
            _button.interactable = false;
            battleEventChannel.RaiseEvent(new PileDetailPanelOpenedEvent(pileTarget));
            PlayClickWobbleAsync().Forget();
        }

        private void OnCardArrived(CardArrivedAtPileEvent evt)
        {
            if (evt.Target != pileTarget) return;
            PlayArrivalPopAsync().Forget();
        }

        // 클릭 시 Z축으로 살짝 비틀렸다가 원위치로 돌아온다.
        private async UniTaskVoid PlayClickWobbleAsync()
        {
            _feedbackCts?.Cancel();
            _feedbackCts = new CancellationTokenSource();
            var ct = _feedbackCts.Token;

            transform.localRotation = Quaternion.identity;
            float half = clickWobbleDuration * 0.5f;

            await LMotion.Create(0f, -clickWobbleAngle, half)
                .WithEase(Ease.OutCubic)
                .Bind(z => transform.localRotation = Quaternion.Euler(0f, 0f, z))
                .ToUniTask(cancellationToken: ct);

            await LMotion.Create(-clickWobbleAngle, 0f, half)
                .WithEase(Ease.OutCubic)
                .Bind(z => transform.localRotation = Quaternion.Euler(0f, 0f, z))
                .ToUniTask(cancellationToken: ct);
        }

        // 카드 도착 시 약하게 스케일 팝.
        private async UniTaskVoid PlayArrivalPopAsync()
        {
            _feedbackCts?.Cancel();
            _feedbackCts = new CancellationTokenSource();
            var ct = _feedbackCts.Token;

            transform.localScale = Vector3.one;

            await LMotion.Create(1f, arrivalPopScale, arrivalPopDuration * 0.4f)
                .WithEase(Ease.OutBack)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);

            await LMotion.Create(arrivalPopScale, 1f, arrivalPopDuration * 0.6f)
                .WithEase(Ease.InOutSine)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);
        }

        private void OnMapStateChanged(MapOverlayState state)
        {
            _isMapOpen = state != MapOverlayState.Hidden;
            if (_isMapOpen) _button.interactable = false;
            else if (!_battleEnded) _button.interactable = true;
        }

        private void OnSessionStart(BattleSessionStartEvent _) { _battleEnded = false; _isMapOpen = false; }
        private void OnPanelClosed(PileDetailPanelClosedEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnBattleUIHidden(BattleUIHiddenEvent _) => _button.interactable = false;
        private void OnBattleUIShown(BattleUIShownEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnCardDrawStart(CardDrawStartEvent _) => _button.interactable = false;
        private void OnCardDrawEnd(CardDrawEndEvent _) { if (!_battleEnded && !_isMapOpen) _button.interactable = true; }
        private void OnWaveClear(WaveClearEvent _) => _button.interactable = false;
        private void OnBattleEnded(BattleVictoryEvent _) { _battleEnded = true; _button.interactable = false; }
        private void OnBattleEnded(BattleDefeatEvent _) { _battleEnded = true; _button.interactable = false; }
    }
}
