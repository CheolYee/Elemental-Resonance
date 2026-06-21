using System.Collections.Generic;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class CardDiscardSelectPanel : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private HandLayoutController handLayoutController;
        [SerializeField] private CardFlyAnimator cardFlyAnimator;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform selectedArea;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text instructionText;

        [Header("Floating Card Layout")]
        [SerializeField] private float floatingCardSpacing = 150f;
        [SerializeField] private float floatingHeightOffset = 20f;
        [SerializeField] private float floatingRotationPerCard = 5f;

        [Header("Selected Card Layout")]
        [SerializeField] private float selectedCardSpacing = 160f;

        [Header("Animation")]
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float cardShrinkDuration = 0.2f;
        [SerializeField] private Ease cardShrinkEase = Ease.InBack;

        private int _discardCount;
        private UniTaskCompletionSource<List<CardInstance>> _tcs;

        // 패널 루트 하위 — 손패 영역에 떠 있는 카드
        private readonly List<CardView> _floatingViews = new();
        // selectedArea 하위 — 선택된 카드
        private readonly List<CardView> _selectedViews = new();

        private float _floatingBaseY; // 손패 영역 Y (패널 루트 좌표계)

        private readonly Dictionary<CardView, CardSelectClickBridge> _bridges = new();

        private void Awake()
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()  => battleEventChannel.AddListener<DiscardSelectRequestEvent>(OnRequest);
        private void OnDisable() => battleEventChannel.RemoveListener<DiscardSelectRequestEvent>(OnRequest);

        private void OnRequest(DiscardSelectRequestEvent evt)
        {
            _discardCount = evt.DiscardCount;
            _tcs = evt.Tcs;
            OpenAsync().Forget();
        }

        private async UniTaskVoid OpenAsync()
        {
            // 손패가 discardCount보다 적으면 Fizzle (null 신호)
            if (handLayoutController.HandCards.Count < _discardCount)
            {
                var tcs = _tcs;
                _tcs = null;
                tcs.TrySetResult(null);
                return;
            }

            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.alpha = 0f;

            await LMotion.Create(0f, 1f, fadeDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => panelCanvasGroup.alpha = a)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            // 손패 카드 전체 Detach — 패널 루트 하위, 위치 유지
            var handCards = new List<CardView>(handLayoutController.HandCards);
            foreach (var view in handCards)
            {
                var detached = handLayoutController.DetachCard(view.CardInstance, transform, skipRefresh: true);
                if (detached == null) continue;
                detached.SetInteractable(false);
                _floatingViews.Add(detached);
                AttachBridge(detached, () => OnFloatingClicked(detached));
            }

            // 손패 Y 기준 저장 (재정렬 시 사용)
            if (_floatingViews.Count > 0)
            {
                float sumY = 0f;
                foreach (var v in _floatingViews)
                    sumY += ((RectTransform)v.transform).anchoredPosition.y;
                _floatingBaseY = sumY / _floatingViews.Count;
            }

            LayoutFloatingCards();
            UpdateInstructionText();
            UpdateConfirmButton();
            panelCanvasGroup.interactable = true;
        }

        private void OnFloatingClicked(CardView view)
        {
            if (!CanSelectMore()) return;
            _floatingViews.Remove(view);
            RemoveBridge(view);

            view.transform.SetParent(selectedArea, true);
            _selectedViews.Add(view);
            AttachBridge(view, () => OnSelectedClicked(view));

            LayoutFloatingCards();
            LayoutSelectedCards();
            UpdateConfirmButton();
        }

        private void OnSelectedClicked(CardView view)
        {
            _selectedViews.Remove(view);
            RemoveBridge(view);

            // 패널 루트로 복귀 — HandLayoutController가 아닌 패널 하위에 유지
            view.transform.SetParent(transform, true);
            _floatingViews.Add(view);
            AttachBridge(view, () => OnFloatingClicked(view));

            LayoutFloatingCards();
            LayoutSelectedCards();
            UpdateConfirmButton();
        }

        public void OnConfirmClicked() => CloseAsync().Forget();

        private async UniTaskVoid CloseAsync()
        {
            panelCanvasGroup.interactable = false;

            // 선택 카드 중앙 스크린 좌표 기록 (수축 전)
            Vector2 centerScreen = Vector2.zero;
            if (_selectedViews.Count > 0)
            {
                foreach (var view in _selectedViews)
                    centerScreen += (Vector2)view.transform.position;
                centerScreen /= _selectedViews.Count;
            }

            // 선택 카드 스케일 0으로 동시 수축
            if (_selectedViews.Count > 0)
            {
                var shrinkTasks = new UniTask[_selectedViews.Count];
                for (int i = 0; i < _selectedViews.Count; i++)
                {
                    var v = _selectedViews[i];
                    shrinkTasks[i] = LMotion.Create(Vector3.one, Vector3.zero, cardShrinkDuration)
                        .WithEase(cardShrinkEase)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .Bind(s => { if (v != null) v.transform.localScale = s; })
                        .ToUniTask(cancellationToken: destroyCancellationToken);
                }
                await UniTask.WhenAll(shrinkTasks);

                // FlyingCard 발사 (패널 페이드와 병렬)
                if (cardFlyAnimator != null)
                    cardFlyAnimator.FlyToDiscardAsync(centerScreen).Forget();
            }

            // 선택 카드 풀 반환
            var selected = new List<CardInstance>();
            foreach (var view in _selectedViews)
            {
                selected.Add(view.CardInstance);
                RemoveBridge(view);
                handLayoutController.ReturnViewToPool(view);
            }
            _selectedViews.Clear();

            // Floating 카드 손패 복귀
            foreach (var view in _floatingViews)
            {
                RemoveBridge(view);
                handLayoutController.ReattachCard(view);
                view.SetInteractable(true);
            }
            _floatingViews.Clear();

            await LMotion.Create(1f, 0f, fadeDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => panelCanvasGroup.alpha = a)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            panelCanvasGroup.blocksRaycasts = false;

            _tcs.TrySetResult(selected);
            _tcs = null;
        }

        private bool CanSelectMore()
        {
            int needed = Mathf.Min(_discardCount, _floatingViews.Count + _selectedViews.Count);
            return _selectedViews.Count < needed;
        }

        private void UpdateConfirmButton()
        {
            int total = _floatingViews.Count + _selectedViews.Count;
            int needed = Mathf.Min(_discardCount, total);
            confirmButton.interactable = needed > 0 && _selectedViews.Count >= needed;
        }

        private void UpdateInstructionText()
        {
            int total = _floatingViews.Count + _selectedViews.Count;
            int needed = Mathf.Min(_discardCount, total);
            instructionText.text = $"버릴 카드 {needed}장을 선택하세요";
        }

        // 손패 영역 팬 레이아웃 — _floatingBaseY 기준으로 패널 루트 좌표계에서 정렬
        private void LayoutFloatingCards()
        {
            int count = _floatingViews.Count;
            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var targetPos = new Vector2(t * floatingCardSpacing, _floatingBaseY - t * t * floatingHeightOffset);
                float targetRotZ = -t * floatingRotationPerCard;
                _floatingViews[i].TweenToLayout(targetPos, targetRotZ, moveDuration, moveEase);
            }
        }

        private void LayoutSelectedCards()
        {
            int count = _selectedViews.Count;
            for (int i = 0; i < count; i++)
            {
                float t = i - (count - 1) / 2f;
                var target = new Vector2(t * selectedCardSpacing, 0f);
                _selectedViews[i].TweenToLayout(target, 0f, moveDuration, moveEase);
            }
        }

        private void AttachBridge(CardView view, System.Action onClick)
        {
            if (_bridges.TryGetValue(view, out var existing))
            {
                Destroy(existing);
                _bridges.Remove(view);
            }
            var bridge = view.gameObject.AddComponent<CardSelectClickBridge>();
            bridge.Clicked += onClick;
            _bridges[view] = bridge;
        }

        private void RemoveBridge(CardView view)
        {
            if (!_bridges.TryGetValue(view, out var bridge)) return;
            if (bridge != null) Destroy(bridge);
            _bridges.Remove(view);
        }

        private void OnDestroy()
        {
            foreach (var bridge in _bridges.Values)
                if (bridge != null) Destroy(bridge);
            _bridges.Clear();
        }
    }
}
