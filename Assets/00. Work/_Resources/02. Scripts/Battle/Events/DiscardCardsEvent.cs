using System.Collections.Generic;
using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class DiscardCardsEvent : GameEvent
    {
        public IReadOnlyList<CardInstance> Cards { get; }

        public DiscardCardsEvent(IReadOnlyList<CardInstance> cards)
        {
            Cards = cards;
        }
    }
}
