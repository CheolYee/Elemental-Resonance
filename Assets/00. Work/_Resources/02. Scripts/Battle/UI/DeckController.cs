using System.Collections.Generic;
using System.Linq;
using Battle.Data;
using Battle.Instances;
using UnityEngine;

namespace Battle.UI
{
    public class DeckController : MonoBehaviour
    {
        [SerializeField] private PlayerDeckProviderSO deckProvider;

        private List<CardInstance> _drawPile = new();
        private List<CardInstance> _discardPile = new();
        private List<CardInstance> _exhaustPile = new();

        private void Start()
        {
            _drawPile = deckProvider.GetDeck()
                .Select(data => new CardInstance(data))
                .ToList();
            Shuffle(_drawPile);
        }

        public CardInstance DrawCard()
        {
            if (_drawPile.Count > 0)
            {
                var card = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                return card;
            }

            if (_discardPile.Count > 0)
            {
                int index = Random.Range(0, _discardPile.Count);
                var card = _discardPile[index];
                _discardPile.RemoveAt(index);
                return card;
            }

            return null;
        }

        public void Discard(CardInstance card)
        {
            if (card != null)
                _discardPile.Add(card);
        }

        public void Exhaust(CardInstance card)
        {
            if (card != null)
                _exhaustPile.Add(card);
        }

        private static void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
