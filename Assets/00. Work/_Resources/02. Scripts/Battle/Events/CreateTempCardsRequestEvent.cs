using System.Collections.Generic;
using Battle.Data;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CreateTempCardsRequestEvent : GameEvent
    {
        public CardDataSO CardData { get; }
        public int Count { get; }
        public UniTaskCompletionSource<List<CardInstance>> Tcs { get; }

        public CreateTempCardsRequestEvent(CardDataSO cardData, int count, UniTaskCompletionSource<List<CardInstance>> tcs)
        {
            CardData = cardData;
            Count = count;
            Tcs = tcs;
        }
    }
}
