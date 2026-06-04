using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public class SkillModule : MonoBehaviour, IModule, ISkillModule
    {
        [SerializeField] private int skillStateIndex;
        [SerializeField] private int idleStateIndex;

        public ModuleOwner Owner { get; private set; }
        public SkillDataSO CurrentSkill { get; private set; }

        public event Action OnCurrentSkillEnd;

        public void Initialize(ModuleOwner owner) => Owner = owner;

        public async UniTask UseSkillAsync(SkillUsageData data, GameObject target, CancellationToken ct = default)
        {
            if (Owner is not Agent agent) return;

            CurrentSkill = data.SkillData;

            List<Battle.Effects.CardEffectSO> repeatEffects = data.Effects.Where(e => e.isRepeat).ToList();
            List<Battle.Effects.CardEffectSO> onceEffects = data.Effects.Where(e => !e.isRepeat).ToList();

            for (int i = 0; i < data.RepeatCount; i++)
            {
                foreach (var effect in repeatEffects)
                    effect.Apply(agent.gameObject, target);

                if (data.SkillData != null)
                {
                    agent.StateMachine.ChangeState(skillStateIndex);

                    var tcs = new UniTaskCompletionSource();
                    var state = agent.StateMachine.CurrentState;
                    void OnComplete() => tcs.TrySetResult();
                    state.OnStateCompleted += OnComplete;

                    try
                    {
                        await tcs.Task.AttachExternalCancellation(ct);
                    }
                    finally
                    {
                        state.OnStateCompleted -= OnComplete;
                    }

                    agent.StateMachine.ChangeState(idleStateIndex);
                }
            }

            foreach (var effect in onceEffects)
                effect.Apply(agent.gameObject, target);

            OnCurrentSkillEnd?.Invoke();
        }

        public void StopSkillIfNotFinished() { }
    }
}
