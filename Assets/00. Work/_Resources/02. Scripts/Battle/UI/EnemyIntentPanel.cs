using System;
using System.Threading;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class EnemyIntentPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Settings")]
        [SerializeField] private float hoverDelay = 0.2f;
        [SerializeField] private float fadeDuration = 0.15f;

        private bool _isOpen;
        private MotionHandle _fadeHandle;
        private CancellationTokenSource _delayCts;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<EnemyHoverEvent>(OnEnemyHover);
            battleEventChannel.AddListener<EnemyHoverExitEvent>(OnEnemyHoverExit);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<EnemyHoverEvent>(OnEnemyHover);
            battleEventChannel.RemoveListener<EnemyHoverExitEvent>(OnEnemyHoverExit);
            _delayCts?.Cancel();
        }

        private void OnEnemyHover(EnemyHoverEvent evt)
        {
            _delayCts?.Cancel();
            _delayCts = new CancellationTokenSource();

            if (canvasGroup.alpha > 0f)
            {
                UpdateData(evt);
                FadeTo(1f);
            }
            else
            {
                OpenWithDelayAsync(evt, _delayCts.Token).Forget();
            }
        }

        private void OnEnemyHoverExit(EnemyHoverExitEvent _)
        {
            _delayCts?.Cancel();
            if (_isOpen) ClosePanel();
        }

        private async UniTaskVoid OpenWithDelayAsync(EnemyHoverEvent evt, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(hoverDelay), cancellationToken: ct);
            UpdateData(evt);
            FadeTo(1f);
            _isOpen = true;
        }

        private void ClosePanel()
        {
            _isOpen = false;
            FadeTo(0f);
        }

        private void UpdateData(EnemyHoverEvent evt)
        {
            var card = evt.Enemy.EnemyData.attackCard;
            if (card == null) return;

            nameText.text = card.cardName;
            descriptionText.text = card.description;
            if (artworkImage != null) artworkImage.sprite = card.artwork;
        }

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, target, fadeDuration)
                .Bind(a => canvasGroup.alpha = a);
        }
    }
}
