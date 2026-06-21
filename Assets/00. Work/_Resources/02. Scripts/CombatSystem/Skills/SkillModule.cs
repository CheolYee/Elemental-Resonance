using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using Battle.Presentation;
using Cysharp.Threading.Tasks;
using Reflex.Attributes;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public class SkillModule : MonoBehaviour, IModule, ISkillModule
    {
        [SerializeField] private int skillStateIndex;
        [SerializeField] private int idleStateIndex;

        [Inject] private ISkillPresentationPlayer _skillPresentationPlayer;

        public ModuleOwner Owner { get; private set; }
        public AnimParamSO CurrentEntryAnimParam { get; private set; }

        public event Action OnCurrentSkillEnd;

        public void Initialize(ModuleOwner owner) => Owner = owner;

        public async UniTask UseSkillAsync(SkillUsageData data, GameObject target, CancellationToken ct = default, IReadOnlyList<Agent> allTargets = null, Func<Agent> randomTargetResolver = null, SkillFizzleToken fizzleToken = null)
        {
            if (Owner is not Agent agent || data == null) return;

            bool isCardUsage = data.CardInstance != null;

            SkillPresentationTimeline resolvedTimeline = ResolvePlayableTimeline(data);

            if (data.PresentationData != null && resolvedTimeline == null)
            {
                string name = isCardUsage ? (data.CardInstance?.data?.cardName ?? "Unknown") : "Enemy";
                Debug.LogWarning($"[SkillPresentation] '{name}'에 재생 가능한 Timeline이 없습니다. 코스트와 카드 소비만 처리합니다.");
                if (isCardUsage)
                {
                    CurrentEntryAnimParam = null;
                    OnCurrentSkillEnd?.Invoke();
                    return;
                }
            }

            // 적: 효과를 프레젠테이션 시작 전에 즉시 적용
            if (!isCardUsage)
                ApplyImmediateEnemyEffects(agent, target, data);

            bool shouldPlayPresentation = resolvedTimeline != null && _skillPresentationPlayer != null;
            bool shouldWaitForSkillState = shouldPlayPresentation
                && resolvedTimeline.animationTrack?.keyframes?.Count > 0;

            // AnimationKeyframe이 애니메이션을 담당하므로 SkillState 자체는 애니메이션을 재생하지 않는다
            CurrentEntryAnimParam = null;

            UniTask stateTask = shouldWaitForSkillState
                ? WaitForSkillStateAsync(agent, ct)
                : UniTask.CompletedTask;

            UniTask presentationTask = UniTask.CompletedTask;
            if (shouldPlayPresentation)
            {
                Agent primaryTarget = target != null ? target.GetComponentInParent<Agent>() : null;
                IReadOnlyList<Agent> targets = allTargets ?? (primaryTarget != null
                    ? (IReadOnlyList<Agent>)new List<Agent> { primaryTarget }
                    : new List<Agent>());
                var playbackContext = new SkillPresentationPlaybackContext(
                    data.PresentationData,
                    data.Grade,
                    data,
                    data.CardInstance,
                    agent,
                    primaryTarget,
                    ct,
                    targets: targets,
                    randomTargetResolver: randomTargetResolver,
                    fizzleToken: fizzleToken);
                presentationTask = _skillPresentationPlayer.PlayAsync(playbackContext);
            }

            await UniTask.WhenAll(stateTask, presentationTask);

            if (shouldWaitForSkillState)
                agent.StateMachine.ChangeState(idleStateIndex);

            CurrentEntryAnimParam = null;
            OnCurrentSkillEnd?.Invoke();
        }

        public void StopSkillIfNotFinished() { }

        private async UniTask WaitForSkillStateAsync(Agent agent, CancellationToken ct)
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
        }

        private static SkillPresentationTimeline ResolvePlayableTimeline(SkillUsageData data)
        {
            if (data?.PresentationData == null) return null;
            return data.PresentationData.TryGetPlayableTimeline(out SkillPresentationTimeline timeline)
                ? timeline
                : null;
        }

        private static void ApplyImmediateEnemyEffects(Agent agent, GameObject target, SkillUsageData data)
        {
            if (agent == null || data?.Effects == null) return;

            foreach (var effect in data.Effects)
            {
                if (effect == null) continue;
                int repeatCount = effect.isRepeat ? Mathf.Max(1, data.RepeatCount) : 1;
                for (int i = 0; i < repeatCount; i++)
                    effect.Apply(agent.gameObject, target, effect.BaseValue);
            }
        }
    }
}
