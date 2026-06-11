using UnityEditor;

namespace Battle.Presentation.Editor
{
    public sealed partial class SkillPresentationEditorWindow
    {
        internal void Play()
        {
            if (_target == null) return;
            var tl = GetEditableTimeline();
            if (tl == null) return;

            float duration = tl.GetEffectiveDuration();
            if (_currentTime >= duration - 0.001f)
                _currentTime = 0f;

            _playbackStartTime  = _currentTime;
            _playbackStartedAt  = EditorApplication.timeSinceStartup;
            _isPlaying          = true;
            UpdateTimeLabel();
        }

        internal void Pause()
        {
            _isPlaying = false;
        }

        internal void Stop()
        {
            _isPlaying   = false;
            _currentTime = 0f;
            UpdateTimeLabel();
            RefreshTimeline();
        }

        private void UpdatePlayback()
        {
            SyncTargetFromReferenceCardIfNeeded();
            if (!_isPlaying || _target == null) return;

            float elapsed = (float)(EditorApplication.timeSinceStartup - _playbackStartedAt);
            _currentTime = _playbackStartTime + elapsed;

            var tl       = GetEditableTimeline();
            float dur    = tl?.GetEffectiveDuration() ?? 1f;
            if (_currentTime >= dur)
            {
                _currentTime = dur;
                _isPlaying   = false;
            }

            UpdateTimeLabel();
            RefreshTimeline();
            Repaint();
        }

        private void UpdateTimeLabel()
        {
            if (_timeLabel != null)
                _timeLabel.text = $"{_currentTime:0.00}s";
        }
    }
}
