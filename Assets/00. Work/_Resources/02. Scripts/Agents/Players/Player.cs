using Gamelib.EventSystem;
using Systems;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Players
{
    public class Player : Agent
    {
        [Header("Player values")] 
        [field: SerializeField] public EventChannelSO PlayerEventChannel { get; private set; }
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
    }
}