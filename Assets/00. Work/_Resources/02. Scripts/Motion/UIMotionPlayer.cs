using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using DelayType = Cysharp.Threading.Tasks.DelayType;

namespace _00._Work.CheolYee._02._Scripts.Motion
{
    // ── 키프레임: 특정 시점의 UI 상태 ───────────────

    [Serializable]
    public class UIMotionKeyframe
    {
        [Tooltip("이 키프레임의 시각 (초)")]
        public float time;

        [Range(0f, 1f)]
        public float alpha = 1f;

        public float scale = 1f;

        [Tooltip("원래 위치 기준 X 오프셋")]
        public float positionX;

        [Tooltip("원래 위치 기준 Y 오프셋")]
        public float positionY;

        public float rotation;

        [Tooltip("이전 키프레임 → 이 키프레임 전환에 적용되는 Ease")]
        public Ease ease = Ease.OutCubic;

        public UIMotionKeyframe() { }

        public UIMotionKeyframe(float time, float alpha, float scale,
            float posX, float posY, float rotation, Ease ease)
        {
            this.time = time;
            this.alpha = alpha;
            this.scale = scale;
            positionX = posX;
            positionY = posY;
            this.rotation = rotation;
            this.ease = ease;
        }

        public UIMotionKeyframe Clone()
        {
            return new UIMotionKeyframe(time, alpha, scale, positionX, positionY, rotation, ease);
        }
    }

    // ── 시퀀스: 키프레임의 타임라인 ──────────────────

    [Serializable]
    public class UIMotionSequence
    {
        public string label = "New Motion";
        public List<UIMotionKeyframe> keyframes = new();

        public float TotalDuration
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < keyframes.Count; i++)
                {
                    if (keyframes[i].time > max)
                        max = keyframes[i].time;
                }
                return max;
            }
        }
    }

    // ── 플레이어 컴포넌트 ────────────────────────────

    /// <summary>
    /// 키프레임 기반 범용 UI 모션 플레이어.
    ///
    /// ─ 간단 사용법 ─────────────────────────────────
    ///
    ///   // 그냥 재생
    ///   motionPlayer.Play("Show");
    ///
    ///   // 완료 후 콜백
    ///   motionPlayer.Play("Show", onComplete: () => SetInteractable(true));
    ///
    ///   // 완료·취소 상관없이 항상 실행 (finally처럼)
    ///   motionPlayer.Play("Hide", onFinish: () => { IsOpen = false; });
    ///
    ///   // 외부에서 강제 취소가 필요한 경우 (고급)
    ///   motionPlayer.Play("Show", externalCt: _destroyCts.Token);
    ///
    /// ───────────────────────────────────────────────
    /// Play()를 호출하면 이전에 실행 중인 모션은 자동으로 취소됩니다.
    /// </summary>
    public sealed class UIMotionPlayer : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform motionTarget;

        [Header("Sequences")]
        [SerializeField] private List<UIMotionSequence> sequences = new();

        private readonly List<MotionHandle> _activeHandles = new();
        private Vector2 _baseAnchoredPos;
        private bool _baseCaptured;

        // 내부에서 관리하는 CancellationTokenSource
        // Play()를 호출할 때마다 이전 것을 취소하고 새로 만듭니다
        private CancellationTokenSource _playCts;

        public bool IsAnimating { get; private set; }
        public int SequenceCount => sequences.Count;

        // ── Unity ──────────────────────────────────

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (motionTarget == null)
                motionTarget = transform as RectTransform;

            CaptureBase();
        }

        private void OnDestroy()
        {
            CancelAll();
            _playCts?.Cancel();
            _playCts?.Dispose();
        }

        // ── 간단 API (권장) ────────────────────────

        /// <summary>
        /// 모션을 한 줄로 실행합니다. 이전 모션은 자동으로 취소됩니다.
        /// </summary>
        /// <param name="label">시퀀스 이름</param>
        /// <param name="onComplete">모션이 정상 완료됐을 때 실행할 콜백 (선택)</param>
        /// <param name="onFinish">완료·취소 상관없이 항상 실행할 콜백 (선택, finally처럼 동작)</param>
        /// <param name="externalCt">오브젝트 파괴 등 외부 취소가 필요할 때 사용 (선택)</param>
        public void Play(string label,
            Action onComplete = null,
            Action onFinish = null,
            CancellationToken externalCt = default)
        {
            PlayFireAndForget(label, onComplete, onFinish, externalCt).Forget();
        }

        private async UniTaskVoid PlayFireAndForget(
            string label,
            Action onComplete,
            Action onFinish,
            CancellationToken externalCt)
        {
            // 이전 모션 CTS를 취소하고 새로 생성
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = externalCt.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(externalCt)
                : new CancellationTokenSource();

            try
            {
                await PlayAsync(label, _playCts.Token);
                onComplete?.Invoke();
            }
            catch (OperationCanceledException) { }
            finally
            {
                onFinish?.Invoke();
            }
        }

        // ── 고급 API (UniTask를 직접 다루는 경우) ──

        /// <summary>시퀀스 이름으로 모션 재생 (await 가능)</summary>
        public async UniTask PlayAsync(string label, CancellationToken ct = default)
        {
            UIMotionSequence seq = FindSequence(label);
            if (seq == null)
            {
                Debug.LogWarning($"[UIMotionPlayer] Sequence '{label}' not found on {gameObject.name}");
                return;
            }
            await PlayInternalAsync(seq, ct);
        }

        /// <summary>시퀀스 인덱스로 모션 재생 (await 가능)</summary>
        public async UniTask PlayAsync(int index, CancellationToken ct = default)
        {
            if (index < 0 || index >= sequences.Count)
            {
                Debug.LogWarning($"[UIMotionPlayer] Sequence index {index} out of range on {gameObject.name}");
                return;
            }
            await PlayInternalAsync(sequences[index], ct);
        }

        /// <summary>모션 없이 시퀀스의 최종 키프레임 상태를 즉시 적용</summary>
        public void ApplyState(string label)
        {
            UIMotionSequence seq = FindSequence(label);
            if (seq != null) ApplyFinalState(seq);
        }

        /// <summary>모션 없이 시퀀스의 최종 키프레임 상태를 즉시 적용</summary>
        public void ApplyState(int index)
        {
            if (index >= 0 && index < sequences.Count)
                ApplyFinalState(sequences[index]);
        }

        /// <summary>진행 중인 모든 모션 즉시 취소</summary>
        public void CancelAll()
        {
            for (int i = 0; i < _activeHandles.Count; i++)
            {
                if (_activeHandles[i].IsActive())
                    _activeHandles[i].Cancel();
            }
            _activeHandles.Clear();
            IsAnimating = false;
        }

        // ── Core: 키프레임 → LitMotion 변환 ────────

        private async UniTask PlayInternalAsync(UIMotionSequence seq, CancellationToken ct)
        {
            CancelAll();
            CaptureBase();
            IsAnimating = true;

            try
            {
                if (seq.keyframes == null || seq.keyframes.Count == 0)
                    return;

                // 시간순 정렬된 복사본
                List<UIMotionKeyframe> sorted = new List<UIMotionKeyframe>(seq.keyframes);
                sorted.Sort((a, b) => a.time.CompareTo(b.time));

                // 첫 키프레임 즉시 적용
                ApplyKeyframe(sorted[0]);

                if (sorted.Count < 2)
                    return;

                // 키프레임 구간별 트윈을 동시에 스케줄링 (delay로 시작 시점 제어)
                List<UniTask> tasks = new List<UniTask>();

                for (int i = 1; i < sorted.Count; i++)
                {
                    UIMotionKeyframe from = sorted[i - 1];
                    UIMotionKeyframe to = sorted[i];

                    float segDelay = from.time;
                    float segDuration = to.time - from.time;

                    if (segDuration <= 0f)
                        continue;

                    tasks.Add(TweenSegmentAsync(from, to, segDelay, segDuration, ct));
                }

                if (tasks.Count > 0)
                    await UniTask.WhenAll(tasks);
            }
            finally
            {
                ApplyFinalState(seq);
                IsAnimating = false;
            }
        }

        private async UniTask TweenSegmentAsync(
            UIMotionKeyframe from, UIMotionKeyframe to,
            float delay, float duration, CancellationToken ct)
        {
            if (delay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delay),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update, ct);
            }

            Ease ease = to.ease;

            TweenIfChanged(from.alpha, to.alpha, duration, ease,
                v => { if (canvasGroup != null) canvasGroup.alpha = v; });

            TweenIfChanged(from.scale, to.scale, duration, ease,
                v => { if (motionTarget != null) motionTarget.localScale = Vector3.one * v; });

            TweenIfChanged(from.positionX, to.positionX, duration, ease,
                v =>
                {
                    if (motionTarget == null) return;
                    Vector2 p = motionTarget.anchoredPosition;
                    p.x = _baseAnchoredPos.x + v;
                    motionTarget.anchoredPosition = p;
                });

            TweenIfChanged(from.positionY, to.positionY, duration, ease,
                v =>
                {
                    if (motionTarget == null) return;
                    Vector2 p = motionTarget.anchoredPosition;
                    p.y = _baseAnchoredPos.y + v;
                    motionTarget.anchoredPosition = p;
                });

            TweenIfChanged(from.rotation, to.rotation, duration, ease,
                v => { if (motionTarget != null) motionTarget.localEulerAngles = new Vector3(0, 0, v); });

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update, ct);
        }

        private void TweenIfChanged(float fromVal, float toVal, float duration, Ease ease, Action<float> apply)
        {
            if (Mathf.Approximately(fromVal, toVal))
                return;

            MotionHandle handle = LMotion.Create(fromVal, toVal, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(apply);

            _activeHandles.Add(handle);
        }

        // ── State Apply ────────────────────────────

        private void ApplyKeyframe(UIMotionKeyframe kf)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = kf.alpha;

            if (motionTarget != null)
            {
                motionTarget.localScale = Vector3.one * kf.scale;

                Vector2 pos = _baseAnchoredPos;
                pos.x += kf.positionX;
                pos.y += kf.positionY;
                motionTarget.anchoredPosition = pos;

                motionTarget.localEulerAngles = new Vector3(0, 0, kf.rotation);
            }
        }

        private void ApplyFinalState(UIMotionSequence seq)
        {
            CancelAll();
            if (seq.keyframes == null || seq.keyframes.Count == 0)
                return;

            CaptureBase();

            UIMotionKeyframe last = seq.keyframes[0];
            for (int i = 1; i < seq.keyframes.Count; i++)
            {
                if (seq.keyframes[i].time >= last.time)
                    last = seq.keyframes[i];
            }
            ApplyKeyframe(last);
        }

        private void CaptureBase()
        {
            if (_baseCaptured || motionTarget == null)
                return;
            _baseAnchoredPos = motionTarget.anchoredPosition;
            _baseCaptured = true;
        }

        private UIMotionSequence FindSequence(string label)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                if (string.Equals(sequences[i].label, label, StringComparison.Ordinal))
                    return sequences[i];
            }
            return null;
        }
    }
}