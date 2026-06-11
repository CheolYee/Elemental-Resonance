using Gamelib.EventSystem;

namespace Battle.Events
{
    public class WaveStartEvent : GameEvent
    {
        public int WaveIndex;
        public int TotalWaves;

        public WaveStartEvent(int waveIndex, int totalWaves)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
        }
    }
}
