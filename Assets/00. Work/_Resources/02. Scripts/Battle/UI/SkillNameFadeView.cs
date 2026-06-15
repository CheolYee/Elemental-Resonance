using System.Threading;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class SkillNameFadeView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade Timing")]
        [SerializeField] private float fadeInDuration  = 0.3f;
        [SerializeField] private float holdDuration    = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private CancellationTokenSource _fadeCts;

        private void Awake()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<SkillQueueChangedEvent>(OnQueueChanged);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<SkillQueueChangedEvent>(OnQueueChanged);
            CancelFade();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void OnQueueChanged(SkillQueueChangedEvent evt)
        {
            if (evt.Current == null) return;
            ShowSkillName(evt.Current.data.cardName).Forget();
        }

        private async UniTaskVoid ShowSkillName(string name)
        {
            CancelFade();
            _fadeCts = new CancellationTokenSource();
            var token = _fadeCts.Token;

            skillNameText.text = name;

            await LMotion.Create(canvasGroup.alpha, 1f, fadeInDuration)
                .Bind(a => canvasGroup.alpha = a)
                .ToUniTask(token);

            await UniTask.Delay(System.TimeSpan.FromSeconds(holdDuration), cancellationToken: token);

            await LMotion.Create(canvasGroup.alpha, 0f, fadeOutDuration)
                .Bind(a => canvasGroup.alpha = a)
                .ToUniTask(token);
        }

        private void CancelFade()
        {
            if (_fadeCts == null) return;
            _fadeCts.Cancel();
            _fadeCts.Dispose();
            _fadeCts = null;
        }
    }
}
