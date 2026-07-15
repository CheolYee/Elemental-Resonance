using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Battle.Events;
using Battle.UI;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.Tutorial
{
    // TutorialCanvas 루트에 부착. 시퀀스를 순서대로 실행하고 단계별 완료 조건을 처리.
    // completionEventTypeName은 Assembly-CSharp 내 전체 타입명 사용.
    // 예: "Battle.Events.CardDroppedOnTargetEvent"
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] private TutorialSequenceSO sequence;
        [SerializeField] private EventChannelSO eventChannel;
        [SerializeField] private TutorialOverlayView overlayView;
        [SerializeField] private TutorialHighlightFrame highlightFrame;
        [SerializeField] private TutorialTooltipView tooltipView;
        [SerializeField] private Button skipButton;
        [SerializeField] private string titleSceneName = "Title";

        private const string KeyTutorialCompleted = "tutorial_completed";

        private bool _isRunning;

        public static bool IsActive { get; private set; }
        private CancellationTokenSource _skipCts;
        private UniTaskCompletionSource _stepTcs;
        private Delegate _currentEventDelegate;
        private TutorialStepSO _currentStep;
        // 합성 완료 시 합성 카드의 RectTransform을 캐싱 — FindTargetRect("FusionCard") 전용
        private RectTransform _fusionCardRect;

        private void Awake()
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnEnable()  => eventChannel.AddListener<FusionCompletedEvent>(OnFusionCompleted);
        private void OnDisable() => eventChannel.RemoveListener<FusionCompletedEvent>(OnFusionCompleted);

        private void OnFusionCompleted(FusionCompletedEvent evt) => _fusionCardRect = evt.CardRect;

        private void OnDestroy()
        {
            skipButton.onClick.RemoveListener(OnSkipClicked);
            _skipCts?.Dispose();
        }

        public void StartTutorial()
        {
            if (_isRunning) return;
            gameObject.SetActive(true); // 하위 컴포넌트 Awake 먼저 실행
            _skipCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunSequenceAsync(_skipCts.Token).Forget();
        }

        private async UniTaskVoid RunSequenceAsync(CancellationToken ct)
        {
            _isRunning = true;
            IsActive   = true;
            try
            {
                foreach (var step in sequence.steps)
                {
                    ct.ThrowIfCancellationRequested();
                    await RunStepAsync(step, ct);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                IsActive   = false;
                if (!destroyCancellationToken.IsCancellationRequested)
                    FinalizeTutorial();
            }
        }

        private async UniTask RunStepAsync(TutorialStepSO step, CancellationToken ct)
        {
            _currentStep = step;
            _stepTcs     = new UniTaskCompletionSource();

            var targetRect = step.targetId == "FusionCard" && _fusionCardRect != null
                ? _fusionCardRect
                : FindTargetRect(step.targetId, step.targetIndex);
            Action onClicked = step.completionType == TutorialCompletionType.ClickToContinue
                ? () => _stepTcs.TrySetResult()
                : null;

            try
            {
                if (step.completionType == TutorialCompletionType.WaitForEvent)
                    SubscribeCompletionEvent(step);

                highlightFrame.SetTarget(targetRect);
                if (!string.IsNullOrEmpty(step.tooltipText))
                    tooltipView.Show(step, targetRect);
                await overlayView.ShowAsync(step.blockAllInput, onClicked, ct);
                await _stepTcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                // 취소/완료 모두 구독 해제
                UnsubscribeCompletionEvent();
                _currentStep = null;
            }

            // 정상 완료 경로만 도달 — 단계 전환 연출
            tooltipView.Hide();
            highlightFrame.SetTarget(null);
            await overlayView.HideAsync(destroyCancellationToken);
        }

        private void OnSkipClicked()
        {
            if (!_isRunning) return;
            // 즉시 시각 정리 후 루프 취소
            overlayView.HideImmediate();
            tooltipView.Hide();
            highlightFrame.SetTarget(null);
            _skipCts?.Cancel();
        }

        private void FinalizeTutorial()
        {
            overlayView.HideImmediate();
            tooltipView.Hide();
            highlightFrame.SetTarget(null);
            PlayerPrefs.SetInt(KeyTutorialCompleted, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);
            FadeManager.Instance?.GoToScene(titleSceneName);
        }

        // WaitForEvent 완료 신호 — Expression으로 생성한 Action<T>에서 호출됨
        private void OnWaitEventFired() => _stepTcs?.TrySetResult();

        private void SubscribeCompletionEvent(TutorialStepSO step)
        {
            if (string.IsNullOrEmpty(step.completionEventTypeName)) return;

            var eventType = Type.GetType(step.completionEventTypeName);
            if (eventType == null)
            {
                Debug.LogWarning($"[Tutorial] 이벤트 타입을 찾을 수 없음: {step.completionEventTypeName}");
                return;
            }

            // Action<T> 델리게이트를 런타임에 생성 — e => OnWaitEventFired()
            var param  = Expression.Parameter(eventType, "e");
            var self   = Expression.Constant(this);
            var method = typeof(TutorialController).GetMethod(
                nameof(OnWaitEventFired), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                var call   = Expression.Call(self, method);
                var lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(eventType), call, param);
                _currentEventDelegate = lambda.Compile();
            }

            typeof(EventChannelSO)
                .GetMethod("AddListener")
                ?.MakeGenericMethod(eventType)
                .Invoke(eventChannel, new object[] { _currentEventDelegate });
        }

        private void UnsubscribeCompletionEvent()
        {
            if (_currentEventDelegate == null || _currentStep == null) return;
            if (string.IsNullOrEmpty(_currentStep.completionEventTypeName)) return;

            Type eventType = Type.GetType(_currentStep.completionEventTypeName);
            if (eventType == null) return;

            typeof(EventChannelSO)
                .GetMethod("RemoveListener")
                ?.MakeGenericMethod(eventType)
                .Invoke(eventChannel, new object[] { _currentEventDelegate });

            _currentEventDelegate = null;
        }

        private static RectTransform FindTargetRect(string id, int index = 0)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var all     = FindObjectsByType<TutorialTarget>(FindObjectsSortMode.None);
            var matches = new List<TutorialTarget>();
            foreach (var t in all)
                if (t.targetId == id && t.gameObject.activeInHierarchy) matches.Add(t);

            if (matches.Count == 0)
            {
                Debug.LogWarning($"[Tutorial] targetId '{id}'인 TutorialTarget을 찾을 수 없습니다.");
                return null;
            }

            // 부모 내 sibling 순서 기준 정렬 → UI 레이아웃의 시각적 순서와 일치
            matches.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            // 음수 index: Python 스타일 역산 (-1 = 마지막)
            int safeIndex = index < 0
                ? Mathf.Clamp(matches.Count + index, 0, matches.Count - 1)
                : Mathf.Clamp(index, 0, matches.Count - 1);
            return matches[safeIndex].GetComponent<RectTransform>();
        }

        public static bool IsTutorialCompleted()
            => PlayerPrefs.GetInt(KeyTutorialCompleted, 0) == 1;
    }
}
