using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class ActionDataModule : MonoBehaviour, IModule
    {
        [field: SerializeField] public Vector2 LastHitPoint { get; set; }
        [field: SerializeField] public Vector2 LastHitDirection { get; set; }
        [field: SerializeField] public Vector2 LastHitNormal { get; set; }
        [field: SerializeField] public Vector2 LastKnockBackDirection { get; set; }
        
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
    }
}