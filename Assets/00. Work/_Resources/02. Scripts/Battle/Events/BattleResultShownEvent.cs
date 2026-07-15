using Gamelib.EventSystem;

namespace Battle.Events
{
    public class BattleResultShownEvent : GameEvent
    {
        public bool IsVictory { get; }
        public BattleResultShownEvent(bool isVictory) => IsVictory = isVictory;
    }
}
