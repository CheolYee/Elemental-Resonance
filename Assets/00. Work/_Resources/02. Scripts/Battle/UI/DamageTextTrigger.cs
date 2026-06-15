using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using Reflex.Attributes;
using UnityEngine;

namespace Battle.UI
{
    public class DamageTextTrigger : MonoBehaviour, IModule
    {
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

        [Inject] private DamageTextSpawner _spawner;

        private HealthModule _health;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            _health.OnDamageTaken += OnDamageTaken;
        }

        private void OnDamageTaken(int damageToHp, int damageToBlock)
        {
            _spawner.Spawn(transform.position + spawnOffset, damageToHp, damageToBlock);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDamageTaken -= OnDamageTaken;
        }
    }
}
