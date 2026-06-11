using System.Collections.Generic;
using Battle.Data;
using Battle.Enums;
using Battle.Events;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using LitMotion;
using UnityEngine;

namespace Battle.UI
{
    public class CardPileDetailPanel : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private DeckController deckController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Transform gridContent;
        [SerializeField] private PileCardItem cardItemPrefab;

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private Ease fadeEase = Ease.OutCubic;

        private readonly List<PileCardItem> _activeItems = new();
        private MotionHandle _fadeHandle;

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.AddListener<PileDetailPanelOpenedEvent>(OnPanelOpened);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleSessionStartEvent>(OnSessionStart);
            battleEventChannel.RemoveListener<PileDetailPanelOpenedEvent>(OnPanelOpened);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnSessionStart(BattleSessionStartEvent _) => ForceClose();

        private void OnPanelOpened(PileDetailPanelOpenedEvent evt)
        {
            PopulateGrid(evt.Target);
            FadeInAsync().Forget();
        }

        public void OnCloseButtonClicked() => FadeOutAsync().Forget();

        private void OnBattleEnded(BattleVictoryEvent _) => ForceClose();
        private void OnBattleEnded(BattleDefeatEvent _) => ForceClose();

        private async UniTaskVoid FadeInAsync()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (panelCanvasGroup == null) return;

            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.alpha = 0f;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(0f, 1f, fadeDuration)
                .WithEase(fadeEase)
                .Bind(a => panelCanvasGroup.alpha = a);
            await _fadeHandle.ToUniTask(cancellationToken: destroyCancellationToken);

            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        private async UniTaskVoid FadeOutAsync()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;

                if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
                _fadeHandle = LMotion.Create(panelCanvasGroup.alpha, 0f, fadeDuration)
                    .WithEase(fadeEase)
                    .Bind(a => panelCanvasGroup.alpha = a);
                await _fadeHandle.ToUniTask(cancellationToken: destroyCancellationToken);
            }

            ClearGrid();
            if (panelRoot != null) panelRoot.SetActive(false);
            battleEventChannel.RaiseEvent(new PileDetailPanelClosedEvent());
        }

        private void ForceClose()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            ClearGrid();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        private void PopulateGrid(PileDisplayTarget target)
        {
            ClearGrid();

            switch (target)
            {
                case PileDisplayTarget.Draw:
                    SpawnItems(deckController.DrawPileCards);
                    break;
                case PileDisplayTarget.Hand:
                    SpawnItems(deckController.HandCards);
                    break;
                case PileDisplayTarget.Discard:
                    SpawnItems(deckController.DiscardCards);
                    break;
                case PileDisplayTarget.Grave:
                    SpawnItems(deckController.GraveCards);
                    break;
                case PileDisplayTarget.CurrentDeck:
                    SpawnItems(deckController.CurrentDeckCards);
                    break;
            }
        }

        private void SpawnItems(IReadOnlyList<CardInstance> cards)
        {
            foreach (var card in cards)
            {
                var item = Instantiate(cardItemPrefab, gridContent);
                item.Setup(card);
                _activeItems.Add(item);
            }
        }

        private void SpawnItems(IReadOnlyList<CardDataSO> cards)
        {
            foreach (var data in cards)
            {
                var item = Instantiate(cardItemPrefab, gridContent);
                item.Setup(data);
                _activeItems.Add(item);
            }
        }

        private void ClearGrid()
        {
            foreach (var item in _activeItems)
                if (item != null) Destroy(item.gameObject);
            _activeItems.Clear();
        }
    }
}
