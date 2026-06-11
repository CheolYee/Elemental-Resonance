using Battle.Instances;
using Battle.Modules;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Events
{
    public class CardDroppedOnTargetEvent : GameEvent
    {
        public CardInstance CardInstance { get; }
        public TargetingModule Target { get; }
        public Vector2 CardScreenPosition { get; }

        public CardDroppedOnTargetEvent(CardInstance cardInstance, TargetingModule target, Vector2 cardScreenPosition)
        {
            CardInstance = cardInstance;
            Target = target;
            CardScreenPosition = cardScreenPosition;
        }
    }
}
