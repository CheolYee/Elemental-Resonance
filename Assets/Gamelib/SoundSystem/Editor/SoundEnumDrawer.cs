using System;
using UnityEditor;
using UnityEngine;

namespace Gamelib.SoundSystem.Editor
{
    // SfxSounds, BgmSounds 등 SoundListSo 연동 enum용. 검색 + 재생/일시정지 + 시크바 제공.
    // 제네릭 인스턴스마다 static 재생 상태가 독립 → SfxSounds와 BgmSounds 상태 자동 분리.
    public abstract class SoundEnumDrawer<T> : EnumSearchDrawerBase<T> where T : Enum
    {
        private static bool      _isPlaying;
        private static int       _pausedSamplePos;
        private static float     _previewTime;
        private static AudioClip _currentClip;
        private static bool      _updateRegistered;

        protected override SoundListSo GetSoundList() => SoundEditorUtils.FindSoundList<T>();

        protected override string[] BuildDisplayNames(T[] values, SoundListSo list)
            => SoundEditorUtils.BuildDisplayNames(values, list);

        protected override bool HasPreview => true;

        protected override string GetPreviewLabel(int currentVal)
        {
            var clip = GetClip(currentVal, SoundEditorUtils.FindSoundList<T>());
            return _isPlaying && clip != null && clip == _currentClip ? "⏸" : "▶";
        }

        protected override void OnPreviewClicked(int intValue, SoundListSo soundList)
        {
            EnsureUpdateRegistered();
            var clip = GetClip(intValue, soundList);
            if (clip == null) return;

            bool isMine = clip == _currentClip;
            if (_isPlaying && isMine)
            {
                _pausedSamplePos = SoundEditorUtils.GetPreviewSamplePosition();
                if (_currentClip.samples > 0)
                    _previewTime = (float)_pausedSamplePos / _currentClip.samples * _currentClip.length;
                SoundEditorUtils.StopEditorClip();
                _isPlaying = false;
            }
            else
            {
                if (!isMine) { _currentClip = clip; _pausedSamplePos = 0; _previewTime = 0f; }
                StartPlay();
            }
        }

        protected override float GetExtraHeight() => EditorGUIUtility.singleLineHeight + 2f;

        protected override void DrawExtras(Rect rect, int currentIntValue, SoundListSo soundList)
        {
            EnsureUpdateRegistered();
            var   clip     = GetClip(currentIntValue, soundList);
            bool  isMine   = clip != null && clip == _currentClip;
            float duration = clip != null && clip.length > 0f ? clip.length : 1f;
            float showTime = isMine ? _previewTime : 0f;

            float indent    = EditorGUIUtility.labelWidth;
            float sliderW   = rect.width - indent - 56f;
            var   sliderRect = new Rect(rect.x + indent, rect.y, sliderW, EditorGUIUtility.singleLineHeight);
            var   timeRect   = new Rect(sliderRect.xMax + 2f, rect.y, 54f, EditorGUIUtility.singleLineHeight);

            GUI.enabled = clip != null;

            float newTime = GUI.HorizontalSlider(sliderRect, showTime, 0f, duration);
            if (clip != null && !Mathf.Approximately(newTime, showTime))
            {
                _currentClip     = clip;
                _previewTime     = newTime;
                _pausedSamplePos = Mathf.RoundToInt(newTime / duration * clip.samples);
                if (_isPlaying && isMine) StartPlay();
            }

            GUI.Label(timeRect, $"{FormatTime(showTime)}/{FormatTime(duration)}", EditorStyles.miniLabel);
            GUI.enabled = true;
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static AudioClip GetClip(int intValue, SoundListSo list)
        {
            if (list?.sounds == null || intValue < 0 || intValue >= list.sounds.Length) return null;
            return list.sounds[intValue]?.clip;
        }

        private static void StartPlay()
        {
            if (_currentClip == null) return;
            SoundEditorUtils.StopEditorClip();
            int start = Mathf.Clamp(_pausedSamplePos, 0, _currentClip.samples - 1);
            SoundEditorUtils.PlayEditorClipFromSample(_currentClip, start);
            _isPlaying = true;
        }

        private static void EnsureUpdateRegistered()
        {
            if (_updateRegistered) return;
            EditorApplication.update += OnEditorUpdate;
            _updateRegistered = true;
        }

        private static void OnEditorUpdate()
        {
            if (!_isPlaying) return;
            if (!SoundEditorUtils.IsPreviewPlaying())
            {
                _isPlaying = false; _pausedSamplePos = 0; _previewTime = 0f;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                return;
            }
            var clip = _currentClip;
            if (clip != null && clip.samples > 0)
                _previewTime = (float)SoundEditorUtils.GetPreviewSamplePosition() / clip.samples * clip.length;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static string FormatTime(float s)
        {
            int m  = (int)(s / 60);
            int sec = (int)(s % 60);
            int ms = (int)((s - Mathf.Floor(s)) * 10);
            return $"{m}:{sec:00}.{ms}";
        }
    }
}
