using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private float          hoverScale   = 1.08f;
        [SerializeField] private float          duration     = 0.15f;
        [SerializeField] private Ease           ease         = Ease.OutBack;

        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds      hoverSound;
        [SerializeField] private SfxSounds      clickSound;

        private Vector3      _baseScale;
        private MotionHandle _handle;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            soundChannel?.RaiseEvent(new PlaySoundEvent(hoverSound, Vector3.zero));
            TweenScale(_baseScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData _)
        {
            TweenScale(_baseScale);
        }

        public void OnPointerClick(PointerEventData _)
        {
            soundChannel?.RaiseEvent(new PlaySoundEvent(clickSound, Vector3.zero));
        }

        private void TweenScale(Vector3 target)
        {
            if (_handle.IsActive()) _handle.Cancel();
            _handle = LMotion.Create(transform.localScale, target, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (this != null) transform.localScale = s; });
        }
    }
}
