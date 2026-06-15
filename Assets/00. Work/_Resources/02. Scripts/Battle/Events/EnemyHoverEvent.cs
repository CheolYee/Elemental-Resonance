using _00._Work._Resources._02._Scripts.Agents.Enemies;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class EnemyHoverEvent : GameEvent
    {
        public AbstractEnemy Enemy { get; }

        public EnemyHoverEvent(AbstractEnemy enemy)
        {
            Enemy = enemy;
        }
    }
}
