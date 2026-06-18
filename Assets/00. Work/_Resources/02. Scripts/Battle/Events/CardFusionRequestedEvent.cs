using Battle.Instances;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class CardFusionRequestedEvent : GameEvent
    {
        public CardInstance DraggedCard { get; }
        public CardInstance TargetCard  { get; }

        public CardFusionRequestedEvent(CardInstance draggedCard, CardInstance targetCard)
        {
            DraggedCard = draggedCard;
            TargetCard  = targetCard;
        }
    }
}
