using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Data;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class CardRewardPanel : MonoBehaviour
    {
        [SerializeField] private RewardCardView[] cardViews = new RewardCardView[3];
        [SerializeField] private CanvasGroup      panelGroup;

        [SerializeField] private float cardAppearDuration = 0.25f;
        [SerializeField] private float cardStagger        = 0.08f;
        [SerializeField] private float hideDuration       = 0.2f;
        [SerializeField] private float dimAlpha           = 0.3f;
        [SerializeField] private float dimDuration        = 0.2f;

        public Action<CardDataSO, Vector2> OnCardSelected;

        private CardDataSO[] _cards;
        private bool _selected;

        private void Awake()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha          = 0f;
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable   = false;
            }
        }

        public void Show(List<CardDataSO> cards)
        {
            _selected = false;
            _cards    = new CardDataSO[cardViews.Length];

            for (int i = 0; i < cardViews.Length; i++)
            {
                if (cardViews[i] == null) continue;

                var group = cardViews[i].GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1f;
                cardViews[i].transform.localScale = Vector3.one;

                if (i < cards.Count && cards[i] != null)
                {
                    _cards[i] = cards[i];
                    cardViews[i].gameObject.SetActive(true);
                    cardViews[i].Setup(cards[i]);
                    cardViews[i].SetInteractable(true);

                    int captured = i;
                    cardViews[i].OnClicked = () => HandleSelect(captured);
                }
                else
                {
                    cardViews[i].gameObject.SetActive(false);
                }
            }

            if (panelGroup != null)
            {
                panelGroup.alpha          = 1f;
                panelGroup.blocksRaycasts = true;
                panelGroup.interactable   = true;
            }

            AppearAsync().Forget();
        }

        private async UniTaskVoid AppearAsync()
        {
            var ct = destroyCancellationToken;
            var tasks = new List<UniTask>();

            for (int i = 0; i < cardViews.Length; i++)
            {
                if (cardViews[i] == null || !cardViews[i].gameObject.activeSelf) continue;
                tasks.Add(AppearCardAsync(cardViews[i], i * cardStagger, ct));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask AppearCardAsync(RewardCardView view, float delay, CancellationToken ct)
        {
            view.transform.localScale = Vector3.one * 0.8f;
            var group = view.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;

            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

            await UniTask.WhenAll(
                LMotion.Create(0.8f, 1f, cardAppearDuration)
                    .WithEase(Ease.OutBack)
                    .Bind(s => { if (view != null) view.transform.localScale = Vector3.one * s; })
                    .ToUniTask(ct),
                group != null
                    ? LMotion.Create(0f, 1f, cardAppearDuration * 0.8f)
                        .Bind(a => { if (group != null) group.alpha = a; })
                        .ToUniTask(ct)
                    : UniTask.CompletedTask);
        }

        private void HandleSelect(int index)
        {
            if (_selected) return;
            _selected = true;
            SelectAsync(index).Forget();
        }

        private async UniTaskVoid SelectAsync(int selectedIndex)
        {
            var ct = destroyCancellationToken;

            var dimTasks = new List<UniTask>();
            for (int i = 0; i < cardViews.Length; i++)
            {
                if (cardViews[i] == null || i == selectedIndex) continue;
                cardViews[i].SetInteractable(false);
                var group = cardViews[i].GetComponent<CanvasGroup>();
                if (group != null)
                {
                    float from = group.alpha;
                    dimTasks.Add(LMotion.Create(from, dimAlpha, dimDuration)
                        .Bind(a => group.alpha = a)
                        .ToUniTask(ct));
                }
            }
            await UniTask.WhenAll(dimTasks);

            Vector2 cardScreenPos = cardViews[selectedIndex].transform.position;
            await cardViews[selectedIndex].PlaySelectAsync(ct);

            OnCardSelected?.Invoke(_cards[selectedIndex], cardScreenPos);

            if (panelGroup != null)
                await LMotion.Create(1f, 0f, hideDuration)
                    .Bind(a => panelGroup.alpha = a)
                    .ToUniTask(ct);

            if (panelGroup != null)
            {
                panelGroup.blocksRaycasts = false;
                panelGroup.interactable   = false;
            }
        }
    }
}
