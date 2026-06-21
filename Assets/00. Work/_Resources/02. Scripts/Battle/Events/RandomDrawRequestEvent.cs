using System.Collections.Generic;
using Battle.Enums;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class RandomDrawRequestEvent : GameEvent
    {
        public PileType SourcePile { get; }
        public int DrawCount { get; }
        public UniTaskCompletionSource<List<CardInstance>> Tcs { get; }

        public RandomDrawRequestEvent(PileType sourcePile, int drawCount, UniTaskCompletionSource<List<CardInstance>> tcs)
        {
            SourcePile = sourcePile;
            DrawCount = drawCount;
            Tcs = tcs;
        }
    }
}
