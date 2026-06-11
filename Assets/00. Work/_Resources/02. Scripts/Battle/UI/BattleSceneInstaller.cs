using _00._Work._Resources._02._Scripts.Agents.Players;
using Battle.Data;
using Battle.Presentation;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using Reflex.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Battle.UI
{
    public class BattleSceneInstaller : MonoBehaviour, IInstaller
    {
        [SerializeField] private Player               player;
        [SerializeField] private BattleCostModelSO    costModel;
        [SerializeField] private EventChannelSO       battleEventChannel;
        [SerializeField] private PoolManagerSo        vfxPoolManager;
        [SerializeField] private PoolItemSo           vfxContainerItem;
        [SerializeField] private CinemachineCamera    skillPresentationCamera;
        [SerializeField] private CinemachineImpulseSource skillCameraImpulseSource;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(player);

            builder.RegisterFactory(
                _ => new SkillEffectExecutionService(costModel, battleEventChannel),
                typeof(SkillEffectExecutionService),
                new[] { typeof(SkillEffectExecutionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new SkillVfxExecutionService(vfxPoolManager, vfxContainerItem),
                typeof(SkillVfxExecutionService),
                new[] { typeof(SkillVfxExecutionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new SkillCameraExecutionService(skillPresentationCamera, skillCameraImpulseSource),
                typeof(SkillCameraExecutionService),
                new[] { typeof(SkillCameraExecutionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new SkillPresentationSampler(),
                typeof(SkillPresentationSampler),
                new[] { typeof(SkillPresentationSampler), typeof(ISkillPresentationSampler) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                container => new SkillPresentationKeyframeExecutor(
                    container.Resolve<SkillEffectExecutionService>()),
                typeof(SkillPresentationKeyframeExecutor),
                new[] { typeof(SkillPresentationKeyframeExecutor), typeof(ISkillPresentationKeyframeExecutor) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                container => new SkillPresentationPlayer(
                    container.Resolve<ISkillPresentationSampler>(),
                    container.Resolve<ISkillPresentationKeyframeExecutor>(),
                    container.Resolve<SkillCameraExecutionService>(),
                    container.Resolve<SkillVfxExecutionService>()),
                typeof(SkillPresentationPlayer),
                new[] { typeof(SkillPresentationPlayer), typeof(ISkillPresentationPlayer) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
