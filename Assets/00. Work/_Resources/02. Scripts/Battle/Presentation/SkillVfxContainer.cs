using System;
using System.Collections.Generic;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace Battle.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkillVfxContainer : PoolableMono
    {
        [SerializeField] private List<VfxEntry> entries = new();

        public override void ResetItem()
        {
            foreach (var e in entries)
            {
                if (e.vfx == null) continue;
                StopParticleSystems(e.vfx);
                e.vfx.SetActive(false);
            }
        }

        public void Play(SkillVfxKey key, float simulationSpeed = 1f, float startLifetimeMultiplier = 1f)
        {
            foreach (var e in entries)
            {
                if (e.key != key || e.vfx == null) continue;
                e.vfx.SetActive(true);
                foreach (var ps in e.vfx.GetComponentsInChildren<ParticleSystem>(includeInactive: true))
                {
                    var main = ps.main;
                    main.simulationSpeed           = simulationSpeed;
                    main.startLifetimeMultiplier   = startLifetimeMultiplier;
                    ps.Play();
                }
            }
        }

        public float GetMaxDuration(SkillVfxKey key)
        {
            foreach (var e in entries)
            {
                if (e.key != key || e.vfx == null) continue;
                var ps = e.vfx.GetComponentInChildren<ParticleSystem>(includeInactive: true);
                if (ps == null) return 1f;
                var main = ps.main;
                return main.duration + main.startLifetime.constantMax;
            }
            return 1f;
        }

        public void Stop()
        {
            foreach (var e in entries)
            {
                if (e.vfx == null) continue;
                StopParticleSystems(e.vfx);
                e.vfx.SetActive(false);
            }
        }

        public void PauseParticles()
        {
            foreach (var e in entries)
            {
                if (e.vfx == null || !e.vfx.activeSelf) continue;
                foreach (var ps in e.vfx.GetComponentsInChildren<ParticleSystem>(false))
                    if (ps.isPlaying) ps.Pause(true);
            }
        }

        public void ResumeParticles()
        {
            foreach (var e in entries)
            {
                if (e.vfx == null || !e.vfx.activeSelf) continue;
                foreach (var ps in e.vfx.GetComponentsInChildren<ParticleSystem>(false))
                    if (ps.isPaused) ps.Play(true);
            }
        }

        private static void StopParticleSystems(GameObject vfx)
        {
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(includeInactive: false))
                ps.Stop(withChildren: false, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        [Serializable]
        public class VfxEntry
        {
            public SkillVfxKey key;
            public GameObject  vfx;
        }
    }
}
