using System.Threading;
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
    public class SkillQueueView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private Sprite placeholderSprite;

        [Header("Current Slot")]
        [SerializeField] private Image currentImage;
        [SerializeField] private CanvasGroup currentGroup;
        [SerializeField] private RectTransform currentRect;

        [Header("Next Slot")]
        [SerializeField] private Image nextImage;
        [SerializeField] private CanvasGroup nextGroup;
        [SerializeField] private RectTransform nextRect;

        [Header("Extra Count")]
        [SerializeField] private GameObject extraCountRoot;
        [SerializeField] private TMP_Text extraCountText;

        [Header("Animation")]
        [SerializeField] private float slideDistance = 30f;
        [SerializeField] private float animDuration = 0.2f;
        [SerializeField] private Ease animEase = Ease.OutCubic;

        private Vector2 _currentBasePos;
        private Vector2 _nextBasePos;
        private CancellationTokenSource _animCts;

        private void Awake()
        {
            if (currentRect != null) _currentBasePos = currentRect.anchoredPosition;
            if (nextRect != null) _nextBasePos = nextRect.anchoredPosition;
            ResetToPlaceholder();
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillQueueCompletedEvent>(OnQueueCompleted);
            battleEventChannel.AddListener<SkillQueueChangedEvent>(OnQueueChanged);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillQueueCompletedEvent>(OnQueueCompleted);
            battleEventChannel.RemoveListener<SkillQueueChangedEvent>(OnQueueChanged);
            CancelAnim();
        }

        private void OnQueueCompleted(SkillQueueCompletedEvent _) => AnimateToAsync(null, null, 0).Forget();

        private void OnQueueChanged(SkillQueueChangedEvent evt)
        {
            var current = evt.Current;
            var next = evt.Pending.Count > 0 ? evt.Pending[0] : null;
            int extra = Mathf.Max(0, evt.Pending.Count - 1);
            AnimateToAsync(current, next, extra).Forget();
        }

        private async UniTaskVoid AnimateToAsync(CardInstance current, CardInstance next, int extra)
        {
            CancelAnim();
            _animCts = new CancellationTokenSource();
            var token = CancellationTokenSource.CreateLinkedTokenSource(_animCts.Token, destroyCancellationToken).Token;

            float halfDur = animDuration * 0.5f;

            // fade out + slide left
            await UniTask.WhenAll(
                LMotion.Create(currentGroup.alpha, 0f, halfDur).WithEase(animEase)
                    .Bind(a => currentGroup.alpha = a).ToUniTask(token),
                LMotion.Create(nextGroup.alpha, 0f, halfDur).WithEase(animEase)
                    .Bind(a => nextGroup.alpha = a).ToUniTask(token),
                LMotion.Create(currentRect.anchoredPosition, _currentBasePos + new Vector2(-slideDistance, 0f), halfDur).WithEase(animEase)
                    .Bind(p => currentRect.anchoredPosition = p).ToUniTask(token),
                LMotion.Create(nextRect.anchoredPosition, _nextBasePos + new Vector2(-slideDistance, 0f), halfDur).WithEase(animEase)
                    .Bind(p => nextRect.anchoredPosition = p).ToUniTask(token)
            );

            // update content
            currentImage.sprite = current?.data.artwork ?? placeholderSprite;
            nextImage.sprite = next?.data.artwork ?? placeholderSprite;

            bool showExtra = extra > 0;
            if (extraCountRoot != null) extraCountRoot.SetActive(showExtra);
            if (showExtra && extraCountText != null) extraCountText.text = $"+{extra}";

            // snap positions back
            currentRect.anchoredPosition = _currentBasePos;
            nextRect.anchoredPosition = _nextBasePos;

            // fade in
            await UniTask.WhenAll(
                LMotion.Create(0f, 1f, halfDur).WithEase(animEase)
                    .Bind(a => currentGroup.alpha = a).ToUniTask(token),
                LMotion.Create(0f, 1f, halfDur).WithEase(animEase)
                    .Bind(a => nextGroup.alpha = a).ToUniTask(token)
            );
        }

        private void ResetToPlaceholder()
        {
            if (currentImage != null) currentImage.sprite = placeholderSprite;
            if (nextImage != null) nextImage.sprite = placeholderSprite;
            if (currentGroup != null) currentGroup.alpha = 1f;
            if (nextGroup != null) nextGroup.alpha = 1f;
            if (extraCountRoot != null) extraCountRoot.SetActive(false);
        }

        private void CancelAnim()
        {
            if (_animCts == null) return;
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }
    }
}
