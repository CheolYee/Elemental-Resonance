using System;
using System.Threading;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class InsufficientCostMessage : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private float displayDuration = 0.8f;

        private MotionHandle _fadeHandle;
        private CancellationTokenSource _cts;

        private void Awake() => canvasGroup.alpha = 0f;

        private void OnEnable() => battleEventChannel.AddListener<InsufficientCostEvent>(OnInsufficientCost);

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<InsufficientCostEvent>(OnInsufficientCost);
            _cts?.Cancel();
        }

        private void OnInsufficientCost(InsufficientCostEvent evt)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            ShowAndHideAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid ShowAndHideAsync(CancellationToken ct)
        {
            FadeTo(1f);
            await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), cancellationToken: ct);
            FadeTo(0f);
        }

        private void FadeTo(float targetAlpha)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, targetAlpha, fadeDuration)
                .Bind(a => canvasGroup.alpha = a);
        }
    }
}
