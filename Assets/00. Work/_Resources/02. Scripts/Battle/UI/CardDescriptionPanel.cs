using System;
using System.Threading;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class CardDescriptionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image labelImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private ScrollRect descriptionScrollRect;
        [SerializeField] private TMPEffect nameTextEffect;
        [SerializeField] private TMPEffect typeTextEffect;

        [Header("Settings")]
        [SerializeField] private float hoverDelay = 0.3f;
        [SerializeField] private float fadeDuration = 0.15f;

        [Header("Auto Scroll")]
        [SerializeField] private float scrollDuration = 1.5f;
        [SerializeField] private float pauseAtBottom = 2f;
        [SerializeField] private float pauseAtTop = 0.5f;
        [SerializeField] private Ease scrollEase = Ease.InOutSine;

        private bool _isOpen;
        private MotionHandle _fadeHandle;
        private CancellationTokenSource _delayCts;
        private CancellationTokenSource _scrollCts;
        private CardInstance _currentHoveredInstance;

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardHoverEvent>(OnCardHover);
            battleEventChannel.AddListener<CardHoverExitEvent>(OnCardHoverExit);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardHoverEvent>(OnCardHover);
            battleEventChannel.RemoveListener<CardHoverExitEvent>(OnCardHoverExit);
            _delayCts?.Cancel();
            _scrollCts?.Cancel();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;
            _delayCts?.Cancel();
            _scrollCts?.Cancel();
            if (_isOpen) ClosePanel();
        }

        private void OnCardHover(CardHoverEvent evt)
        {
            _currentHoveredInstance = evt.CardInstance;
            _delayCts?.Cancel();
            _delayCts = new CancellationTokenSource();

            // 패널이 보이는 상태(열려 있거나 페이드 아웃 중)면 즉시 갱신
            if (canvasGroup.alpha > 0f)
            {
                _isOpen = true;
                UpdateData(evt);
                FadeTo(1f);
                RestartAutoScroll();
            }
            else
            {
                OpenWithDelayAsync(evt, _delayCts.Token).Forget();
            }
        }

        private void OnCardHoverExit(CardHoverExitEvent evt)
        {
            // 이미 다른 카드로 hover가 옮겨간 경우 무시
            if (evt.CardInstance != _currentHoveredInstance) return;

            _currentHoveredInstance = null;
            _delayCts?.Cancel();
            _scrollCts?.Cancel();
            if (_isOpen) ClosePanel();
        }

        private async UniTaskVoid OpenWithDelayAsync(CardHoverEvent evt, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(hoverDelay), cancellationToken: ct);
            UpdateData(evt);
            FadeTo(1f);
            _isOpen = true;
            RestartAutoScroll();
        }

        private void ClosePanel()
        {
            _isOpen = false;
            FadeTo(0f);
        }

        private void UpdateData(CardHoverEvent evt)
        {
            var data = evt.CardInstance.data;
            nameText.text = data.cardName;
            costText.text = data.cost.ToString();
            descriptionText.text = data.description;
            artworkImage.sprite = data.artwork;
            if (typeText != null) typeText.text = CardColorUtility.GetTypeName(data.cardType);
            CardColorUtility.Apply(frameImage, labelImage, data);
            CardColorUtility.ApplyTextEffects(nameTextEffect, typeTextEffect, data.grade);
        }

        private void FadeTo(float targetAlpha)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, targetAlpha, fadeDuration)
                .Bind(a => canvasGroup.alpha = a);
        }

        private void RestartAutoScroll()
        {
            if (descriptionScrollRect == null) return;
            _scrollCts?.Cancel();
            _scrollCts = new CancellationTokenSource();
            AutoScrollLoopAsync(_scrollCts.Token).Forget();
        }

        private async UniTaskVoid AutoScrollLoopAsync(CancellationToken ct)
        {
            // ContentSizeFitter가 레이아웃을 갱신할 때까지 한 프레임 대기
            await UniTask.NextFrame(ct);

            var content = descriptionScrollRect.content;
            var viewport = descriptionScrollRect.viewport != null
                ? descriptionScrollRect.viewport
                : (RectTransform)descriptionScrollRect.transform;

            if (content.rect.height <= viewport.rect.height) return;

            descriptionScrollRect.verticalNormalizedPosition = 1f;

            while (!ct.IsCancellationRequested)
            {
                await LMotion.Create(1f, 0f, scrollDuration)
                    .WithEase(scrollEase)
                    .Bind(v => descriptionScrollRect.verticalNormalizedPosition = v)
                    .ToUniTask(cancellationToken: ct);

                await UniTask.Delay(TimeSpan.FromSeconds(pauseAtBottom), cancellationToken: ct);

                descriptionScrollRect.verticalNormalizedPosition = 1f;

                await UniTask.Delay(TimeSpan.FromSeconds(pauseAtTop), cancellationToken: ct);
            }
        }
    }
}
