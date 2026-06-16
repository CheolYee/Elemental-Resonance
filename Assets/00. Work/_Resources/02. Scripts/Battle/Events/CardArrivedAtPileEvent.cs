using Battle.Enums;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Events
{
    public class CardArrivedAtPileEvent : GameEvent
    {
        public PileDisplayTarget Target { get; }
        public Vector3 WorldPosition { get; }

        public CardArrivedAtPileEvent(PileDisplayTarget target, Vector3 worldPosition)
        {
            Target = target;
            WorldPosition = worldPosition;
        }
    }
}
