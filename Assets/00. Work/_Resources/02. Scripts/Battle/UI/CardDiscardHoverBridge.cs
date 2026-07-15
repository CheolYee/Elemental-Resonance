using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Battle.UI
{
    public class CardDiscardHoverBridge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private EventChannelSO _soundChannel;
        private SfxSounds      _hoverSound;
        private float          _hoverScale = 1.1f;
        private float          _duration   = 0.15f;
        private MotionHandle   _handle;

        public void Init(EventChannelSO channel, SfxSounds sound, float hoverScale = 1.1f, float duration = 0.15f)
        {
            _soundChannel = channel;
            _hoverSound   = sound;
            _hoverScale   = hoverScale;
            _duration     = duration;
        }

        public void ResetScale()
        {
            if (_handle.IsActive()) _handle.Cancel();
            transform.localScale = Vector3.one;
        }

        private void OnDisable() => ResetScale();

        public void OnPointerEnter(PointerEventData _)
        {
            _soundChannel?.RaiseEvent(new PlaySoundEvent(_hoverSound, Vector3.zero));
            TweenScale(_hoverScale);
        }

        public void OnPointerExit(PointerEventData _) => TweenScale(1f);

        private void TweenScale(float target)
        {
            if (_handle.IsActive()) _handle.Cancel();
            float current = transform.localScale.x;
            _handle = LMotion.Create(current, target, _duration)
                .WithEase(Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (this != null) transform.localScale = new Vector3(s, s, 1f); });
        }
    }
}
