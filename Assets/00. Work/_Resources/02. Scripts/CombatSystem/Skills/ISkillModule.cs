using System;
using System.Collections.Generic;
using System.Threading;
using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Scripts.CombatSystem.Skills
{
    public interface ISkillModule
    {
        ModuleOwner Owner { get; }
        event Action OnCurrentSkillEnd;
        UniTask UseSkillAsync(SkillUsageData data, GameObject target, CancellationToken ct = default, IReadOnlyList<Agent> allTargets = null);
        void StopSkillIfNotFinished();
    }
}
