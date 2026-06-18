using Gamelib.EventSystem;

namespace Battle.Events
{
    public class PlayerHpChangedEvent : GameEvent
    {
        public int  OldHp  { get; }
        public int  NewHp  { get; }
        public int  MaxHp  { get; }
        public bool IsHeal { get; }

        public PlayerHpChangedEvent(int oldHp, int newHp, int maxHp, bool isHeal)
        {
            OldHp  = oldHp;
            NewHp  = newHp;
            MaxHp  = maxHp;
            IsHeal = isHeal;
        }
    }
}
