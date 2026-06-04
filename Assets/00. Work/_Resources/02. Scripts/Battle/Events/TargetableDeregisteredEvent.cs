using Battle.Modules;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class TargetableDeregisteredEvent : GameEvent
    {
        public TargetingModule Module { get; }
        public TargetableDeregisteredEvent(TargetingModule module) { Module = module; }
    }
}
