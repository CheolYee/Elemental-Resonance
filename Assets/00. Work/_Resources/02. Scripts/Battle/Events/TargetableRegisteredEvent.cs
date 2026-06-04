using Battle.Modules;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class TargetableRegisteredEvent : GameEvent
    {
        public TargetingModule Module { get; }
        public TargetableRegisteredEvent(TargetingModule module) { Module = module; }
    }
}
