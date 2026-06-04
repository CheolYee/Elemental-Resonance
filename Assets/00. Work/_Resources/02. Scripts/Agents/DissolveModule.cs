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

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        public void Initialize(ModuleOwner owner)
        {
            _renderers = owner.GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void StartDissolve(Action onComplete = null)
        {
            DissolveAsync(onComplete).Forget();
        }

        private async UniTaskVoid DissolveAsync(Action onComplete)
        {
            await LMotion.Create(0f, 1f, dissolveDuration)
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
