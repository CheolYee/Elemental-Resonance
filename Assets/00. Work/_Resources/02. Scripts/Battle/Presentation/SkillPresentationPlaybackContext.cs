using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using Cysharp.Threading.Tasks;
using _02._Scripts.CombatSystem.Skills;
using Battle.Enums;
using Battle.Instances;

namespace Battle.Presentation
{
    public sealed class SkillPresentationPlaybackContext
    {
        public SkillPresentationDataSO PresentationData { get; }
        public CardGrade Grade { get; }
        public SkillUsageData UsageData { get; }
        public CardInstance CardInstance { get; }
        public Agent Caster { get; }
        public Agent Target { get; }
        public IReadOnlyList<Agent> Targets { get; }
        public CancellationToken CancellationToken { get; }
        public SkillPresentationTimeline Timeline { get; }
        public Func<Agent> RandomTargetResolver { get; }
        public SkillFizzleToken FizzleToken { get; }

        private Agent[] _preResolvedHitTargets = Array.Empty<Agent>();
        private int _damageHitIndex;

        private UniTaskCompletionSource _pauseGate;

        public bool IsPaused => _pauseGate != null;

        public void Pause()
        {
            if (_pauseGate == null)
                _pauseGate = new UniTaskCompletionSource();
        }

        public void Resume()
        {
            var gate = _pauseGate;
            _pauseGate = null;
            gate?.TrySetResult();
        }

        public async UniTask WaitForResumeAsync(CancellationToken ct)
        {
            var gate = _pauseGate;
            if (gate == null) return;
            await gate.Task.AttachExternalCancellation(ct);
        }

        public void SetPreResolvedHitTargets(Agent[] targets)
        {
            _preResolvedHitTargets = targets ?? Array.Empty<Agent>();
            _damageHitIndex = 0;
        }

        public Agent ConsumeDamageHitTarget()
        {
            if (_damageHitIndex < _preResolvedHitTargets.Length)
                return _preResolvedHitTargets[_damageHitIndex++];
            return RandomTargetResolver?.Invoke();
        }

        public Agent GetPreResolvedHitTarget(int index)
        {
            if (index >= 0 && index < _preResolvedHitTargets.Length)
                return _preResolvedHitTargets[index];
            return RandomTargetResolver?.Invoke();
        }

        public SkillPresentationPlaybackContext(
            SkillPresentationDataSO presentationData,
            CardGrade grade,
            SkillUsageData usageData,
            CardInstance cardInstance,
            Agent caster,
            Agent target,
            CancellationToken cancellationToken,
            SkillPresentationTimeline timeline = null,
            IReadOnlyList<Agent> targets = null,
            Func<Agent> randomTargetResolver = null,
            SkillFizzleToken fizzleToken = null)
        {
            PresentationData = presentationData;
            Grade = grade;
            UsageData = usageData;
            CardInstance = cardInstance;
            Caster = caster;
            Target = target;
            Targets = targets ?? (target != null ? new List<Agent> { target } : new List<Agent>());
            CancellationToken = cancellationToken;
            Timeline = timeline;
            RandomTargetResolver = randomTargetResolver;
            FizzleToken = fizzleToken ?? new SkillFizzleToken();
        }

        public SkillPresentationPlaybackContext WithTimeline(SkillPresentationTimeline timeline)
            => new(
                PresentationData,
                Grade,
                UsageData,
                CardInstance,
                Caster,
                Target,
                CancellationToken,
                timeline,
                Targets,
                RandomTargetResolver,
                FizzleToken);
    }
}
