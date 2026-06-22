using System;
using System.Threading.Tasks;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Gamelib.SoundSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour, IPoolable
    {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        
        private AudioSource _audioSource;
        private Coroutine _finishCoroutine;
        private Coroutine _fadeCoroutine;
        private bool _completionInvoked;
        private float _baseVolume;
        public GameObject GameObject => this == null ? null : gameObject;
        [field:SerializeField] public PoolItemSo PoolItem { get; set; }
        
        public event Action<SoundPlayer> OnSoundFinished;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlaySound(SoundClipSo clipData)
        {
            PrepareForPlayback(clipData);
            if (clipData.audioType == AudioTypes.SFX)
            {
                _audioSource.outputAudioMixerGroup = sfxGroup;
            }
            else if (clipData.audioType == AudioTypes.MUSIC)
            {
                _audioSource.outputAudioMixerGroup = musicGroup;
            }

            _audioSource.volume = clipData.volume;
            _audioSource.pitch = clipData.pitch;
            if (clipData.randomizePitch)
            {
                _audioSource.pitch += Random.Range(-clipData.randomPitchModifier, clipData.randomPitchModifier);
            }
            _audioSource.clip = clipData.clip;

            _audioSource.loop = clipData.loop;

            if (!clipData.loop)
            {
                float time = _audioSource.clip.length + .2f;
                _finishCoroutine = StartCoroutine(DisableSoundTimer(time));
            }
            _audioSource.Play();
        }

        public void PlaySoundWithEnvelope(SoundClipSo clipData, float fadeInDuration, float fadeOutDuration)
        {
            PrepareForPlayback(clipData);
            if (clipData.audioType == AudioTypes.SFX)
                _audioSource.outputAudioMixerGroup = sfxGroup;
            else if (clipData.audioType == AudioTypes.MUSIC)
                _audioSource.outputAudioMixerGroup = musicGroup;

            _audioSource.pitch = clipData.pitch;
            if (clipData.randomizePitch)
                _audioSource.pitch += Random.Range(-clipData.randomPitchModifier, clipData.randomPitchModifier);

            _audioSource.clip = clipData.clip;
            _audioSource.loop = clipData.loop;
            _audioSource.volume = fadeInDuration > 0f ? 0f : _baseVolume;
            _audioSource.Play();

            if (fadeInDuration > 0f)
                _fadeCoroutine = StartCoroutine(FadeVolume(0f, _baseVolume, fadeInDuration));

            if (!clipData.loop && _audioSource.clip != null)
                _finishCoroutine = StartCoroutine(DisableSoundTimerWithEnvelope(_audioSource.clip.length, fadeOutDuration));
        }

        public void ResetItem()
        {
            StopActiveCoroutines();
            _audioSource?.Stop();
        }
        
        private System.Collections.IEnumerator DisableSoundTimer(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            CompletePlayback();
        }

        private System.Collections.IEnumerator DisableSoundTimerWithEnvelope(float clipLength, float fadeOutDuration)
        {
            float safeFade = Mathf.Max(0f, fadeOutDuration);
            float waitBeforeFade = Mathf.Max(0f, clipLength - safeFade);
            if (waitBeforeFade > 0f)
                yield return new WaitForSecondsRealtime(waitBeforeFade);

            if (safeFade > 0f)
                yield return FadeVolume(Mathf.Max(0f, _audioSource.volume), 0f, safeFade);

            CompletePlayback();
        }

        private System.Collections.IEnumerator FadeVolume(float from, float to, float duration)
        {
            if (_audioSource == null)
                yield break;

            if (duration <= 0f)
            {
                _audioSource.volume = to;
                yield break;
            }

            float elapsed = 0f;
            _audioSource.volume = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _audioSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _audioSource.volume = to;
        }

        public void FadeOutAndStop(float duration)
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                CompletePlayback();
                return;
            }

            StopActiveCoroutines(stopAudio: false);
            _fadeCoroutine = StartCoroutine(FadeOutAndStopRoutine(duration));
        }

        private System.Collections.IEnumerator FadeOutAndStopRoutine(float duration)
        {
            float startVolume = _audioSource != null ? _audioSource.volume : 0f;
            if (duration > 0f)
                yield return FadeVolume(startVolume, 0f, duration);

            CompletePlayback();
        }

        public void Pause()
        {
            _audioSource.Pause();
        }

        public void Resume()
        {
            _audioSource.UnPause();
        }

        public void ForceStopSound()
        {
            StopActiveCoroutines();
            _audioSource.Stop();
        }

        private void PrepareForPlayback(SoundClipSo clipData)
        {
            StopActiveCoroutines();
            _completionInvoked = false;
            _baseVolume = clipData != null ? clipData.volume : 1f;
        }

        private void CompletePlayback()
        {
            if (_completionInvoked)
                return;

            _completionInvoked = true;
            StopActiveCoroutines(stopAudio: false);
            if (_audioSource != null)
                _audioSource.Stop();
            OnSoundFinished?.Invoke(this);
        }

        private void StopActiveCoroutines(bool stopAudio = false)
        {
            if (this == null) return;

            if (_finishCoroutine != null)
            {
                StopCoroutine(_finishCoroutine);
                _finishCoroutine = null;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (stopAudio && _audioSource != null)
                _audioSource.Stop();
        }

    }
}
