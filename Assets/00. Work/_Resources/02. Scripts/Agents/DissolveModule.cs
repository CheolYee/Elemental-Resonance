using System;
using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public class DissolveModule : MonoBehaviour, IModule
    {
        [SerializeField] private float dissolveDuration = 1.2f;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int DissolveYMinId   = Shader.PropertyToID("_DissolveYMin");
        private static readonly int DissolveYMaxId   = Shader.PropertyToID("_DissolveYMax");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        public void Initialize(ModuleOwner owner)
        {
            _renderers = owner.GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            SetHeightBounds();
        }

        private void SetHeightBounds()
        {
            if (_renderers.Length == 0) return;

            var bounds = _renderers[0].bounds;
            for (int i = 1; i < _renderers.Length; i++)
                bounds.Encapsulate(_renderers[i].bounds);

            _mpb.SetFloat(DissolveYMinId, bounds.min.y);
            _mpb.SetFloat(DissolveYMaxId, bounds.max.y);
            foreach (var r in _renderers)
                r.SetPropertyBlock(_mpb);
        }

        public void StartDissolve(Action onComplete = null)
        {
            DissolveAsync(onComplete).Forget();
        }

        private async UniTaskVoid DissolveAsync(Action onComplete)
        {
            await LMotion.Create(0f, 1f, dissolveDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(v =>
                {
                    _mpb.SetFloat(DissolveAmountId, v);
                    foreach (var r in _renderers)
                        r.SetPropertyBlock(_mpb);
                })
                .ToUniTask();
            onComplete?.Invoke();
        }
    }
}
