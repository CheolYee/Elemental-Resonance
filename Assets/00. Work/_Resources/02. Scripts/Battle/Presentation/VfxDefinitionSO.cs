using System;
using UnityEngine;

namespace Battle.Presentation
{
    [CreateAssetMenu(fileName = "VfxDefinition", menuName = "Battle/VFX Definition")]
    public class VfxDefinitionSO : ScriptableObject
    {
        public SkillVfxKey key;
        public GameObject  prefab;
        public float       defaultLifeTime = 1f;

        private void OnValidate()
        {
            if (prefab != null && prefab.TryGetComponent<ParticleSystem>(out var particleSystem))
            {
                defaultLifeTime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            }
        }
    }
}
