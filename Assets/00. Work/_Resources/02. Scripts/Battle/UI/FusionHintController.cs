using System.Collections.Generic;
using Battle.Enums;
using Battle.Events;
using Battle.Fusion;
using Gamelib.EventSystem;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class FusionHintController : MonoBehaviour
    {
        [SerializeField] private EventChannelSO       battleEventChannel;
        [SerializeField] private HandLayoutController handLayoutController;

        [Inject] private FusionRecipeService _fusionRecipeService;

        private FusionHintView                  _activeHint;
        private Battle.Instances.CardInstance   _activeHintInstance;

        private void OnEnable()
        {
            battleEventChannel.AddListener<CardHoverEvent>(OnCardHover);
            battleEventChannel.AddListener<CardHoverExitEvent>(OnCardHoverExit);
            battleEventChannel.AddListener<CardDragStartEvent>(OnCardDragStart);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<CardHoverEvent>(OnCardHover);
            battleEventChannel.RemoveListener<CardHoverExitEvent>(OnCardHoverExit);
            battleEventChannel.RemoveListener<CardDragStartEvent>(OnCardDragStart);
        }

        private void OnCardHover(CardHoverEvent evt)
        {
            HideActive();

            var hoveredElement = evt.CardInstance.data.elementType;
            var hints = new List<FusionHintData>();
            var seen  = new HashSet<ElementType>();

            foreach (var view in handLayoutController.HandCards)
            {
                if (view.CardInstance == evt.CardInstance) continue;
                var partnerElement = view.CardInstance.data.elementType;
                if (!seen.Add(partnerElement)) continue;
                if (_fusionRecipeService.TryGetResult(hoveredElement, partnerElement, out var resultElement))
                {
                    hints.Add(new FusionHintData
                    {
                        MaterialElement = partnerElement,
                        ResultElement   = resultElement,
                    });
                }
            }

            if (hints.Count == 0) return;

            var cardView = FindCardView(evt.CardInstance);
            if (cardView == null) return;

            var hintView = cardView.GetComponentInChildren<FusionHintView>();
            if (hintView == null) return;

            _activeHint         = hintView;
            _activeHintInstance = evt.CardInstance;
            hintView.Show(hints);
        }

        private void OnCardHoverExit(CardHoverExitEvent evt)
        {
            if (_activeHintInstance == evt.CardInstance)
                HideActive();
        }

        private void OnCardDragStart(CardDragStartEvent _) => HideActive();

        private void HideActive()
        {
            if (_activeHint == null) return;
            _activeHint.Hide();
            _activeHint         = null;
            _activeHintInstance = null;
        }

        private CardView FindCardView(Battle.Instances.CardInstance instance)
        {
            foreach (var view in handLayoutController.HandCards)
                if (view.CardInstance == instance) return view;
            return null;
        }
    }
}
