using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class ActionDataModule : MonoBehaviour, IModule
    {
        public Vector3 LastHitPoint { get; set; }
        public Vector3 LastHitNormal { get; set; }
        public ModuleOwner Attacker { get; set; }
        
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
    }
}