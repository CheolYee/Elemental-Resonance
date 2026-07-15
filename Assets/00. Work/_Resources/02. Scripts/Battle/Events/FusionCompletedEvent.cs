using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Events
{
    public class FusionCompletedEvent : GameEvent
    {
        public RectTransform CardRect { get; }
        public FusionCompletedEvent(RectTransform cardRect) => CardRect = cardRect;
    }
}
