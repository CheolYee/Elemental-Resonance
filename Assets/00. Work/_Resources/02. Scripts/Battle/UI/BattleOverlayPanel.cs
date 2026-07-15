using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Motion;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class BattleOverlayPanel : MonoBehaviour
    {
        [SerializeField] private UIMotionPlayer backgroundPlayer;
        [SerializeField] private UIMotionPlayer primaryPlayer;
        [SerializeField] private UIMotionPlayer secondaryPlayer;
        [SerializeField] private TMP_Text primaryText;
        [SerializeField] private TMP_Text secondaryText;

        private readonly Queue<AnnouncementRequest> _queue = new();
        private bool _isProcessing;

        private void Awake()
        {
            primaryText.rectTransform.localPosition = new Vector3(0f, 0f, 0f);
            secondaryText.rectTransform.localPosition = new Vector3(0f, 0f, 0f);
        }

        public void Enqueue(AnnouncementRequest request)
        {
            _queue.Enqueue(request);
            if (!_isProcessing)
                ProcessQueueAsync(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid ProcessQueueAsync(CancellationToken ct)
        {
            _isProcessing = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var request = _queue.Dequeue();
                    await PlayAnnouncementAsync(request, ct);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async UniTask PlayAnnouncementAsync(AnnouncementRequest request, CancellationToken ct)
        {
            primaryText.text = request.PrimaryText;

            bool hasSecondary = !string.IsNullOrEmpty(request.SecondaryText)
                                && !string.IsNullOrEmpty(request.SecondaryShow);
            if (hasSecondary)
                secondaryText.text = request.SecondaryText;

            request.OnShow?.Invoke();

            // 배경은 독립 실행 — 자체 시퀀스에 Show+Hold+Hide 포함, 텍스트와 동시 시작
            if (!string.IsNullOrEmpty(request.BackgroundSequence))
                backgroundPlayer.Play(request.BackgroundSequence, externalCt: ct);

            // 텍스트 In
            if (hasSecondary)
                await UniTask.WhenAll(
                    primaryPlayer.PlayAsync(request.PrimaryShow, ct),
                    secondaryPlayer.PlayAsync(request.SecondaryShow, ct));
            else
                await primaryPlayer.PlayAsync(request.PrimaryShow, ct);

            // Hold
            if (request.HoldDuration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(request.HoldDuration), cancellationToken: ct);

            // 텍스트 Out
            if (hasSecondary)
                await UniTask.WhenAll(
                    primaryPlayer.PlayAsync(request.PrimaryHide, ct),
                    secondaryPlayer.PlayAsync(request.SecondaryHide, ct));
            else
                await primaryPlayer.PlayAsync(request.PrimaryHide, ct);

            request.OnComplete?.Invoke();
        }
    }
}
