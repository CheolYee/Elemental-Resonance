using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class TargetingOverlay : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private float targetAlpha = 0.5f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private Ease fadeEase = Ease.OutCubic;

        private MotionHandle _fadeHandle;

        private void Awake() => overlayCanvasGroup.alpha = 0f;

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.AddListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardTargetingStartEvent>(OnTargetingStart);
            battleEventChannel.RemoveListener<CardTargetingEndEvent>(OnTargetingEnd);
        }

        private void OnTargetingStart(CardTargetingStartEvent _) => FadeTo(targetAlpha);
        private void OnTargetingEnd(CardTargetingEndEvent _) => FadeTo(0f);

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(overlayCanvasGroup.alpha, target, fadeDuration)
                .WithEase(fadeEase)
                .Bind(a => overlayCanvasGroup.alpha = a);
        }
    }
}
