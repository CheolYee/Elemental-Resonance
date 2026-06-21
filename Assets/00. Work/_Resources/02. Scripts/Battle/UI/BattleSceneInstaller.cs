using _00._Work._Resources._02._Scripts.Agents.Players;
using Battle.Fusion;
using Battle.Presentation;
using Battle.Services;
using DeckBuilding;
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
        [SerializeField] private EventChannelSO       battleEventChannel;
        [SerializeField] private PoolManagerSo        vfxPoolManager;
        [SerializeField] private PoolItemSo           vfxContainerItem;
        [SerializeField] private CinemachineCamera    skillPresentationCamera;
        [SerializeField] private CinemachineImpulseSource skillCameraImpulseSource;
        [SerializeField] private BattleUIController   battleUIController;
        [SerializeField] private DamageTextSpawner    damageTextSpawner;
        [SerializeField] private CardDatabaseSO       cardDatabase;
        [SerializeField] private FusionRecipeTableSO  fusionRecipeTable;
        [SerializeField] private GradeWeightTableSO   gradeWeightTable;
        [SerializeField] private RewardCardPoolService rewardCardPoolService;
        [SerializeField] private SkillPreviewLayoutSO  skillPreviewLayout;

        public void InstallBindings(ContainerBuilder builder)
        {
            builder.RegisterValue(player);
            builder.RegisterValue(damageTextSpawner);
            builder.RegisterValue(rewardCardPoolService);

            builder.RegisterFactory(
                _ => (IBattleUIController)battleUIController,
                typeof(BattleUIController),
                new[] { typeof(BattleUIController), typeof(IBattleUIController) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new SkillEffectExecutionService(battleEventChannel),
                typeof(SkillEffectExecutionService),
                new[] { typeof(SkillEffectExecutionService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new SkillVfxExecutionService(vfxPoolManager, vfxContainerItem, skillPreviewLayout),
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
                _ => new SkillCasterExecutionService(),
                typeof(SkillCasterExecutionService),
                new[] { typeof(SkillCasterExecutionService) },
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
                    container.Resolve<SkillEffectExecutionService>(),
                    container.Resolve<IBattleUIController>()),
                typeof(SkillPresentationKeyframeExecutor),
                new[] { typeof(SkillPresentationKeyframeExecutor), typeof(ISkillPresentationKeyframeExecutor) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new FusionRecipeService(fusionRecipeTable),
                typeof(FusionRecipeService),
                new[] { typeof(FusionRecipeService) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                _ => new FusionResultPicker(cardDatabase, gradeWeightTable),
                typeof(FusionResultPicker),
                new[] { typeof(FusionResultPicker) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);

            builder.RegisterFactory(
                container => new SkillPresentationPlayer(
                    container.Resolve<ISkillPresentationSampler>(),
                    container.Resolve<ISkillPresentationKeyframeExecutor>(),
                    container.Resolve<SkillCameraExecutionService>(),
                    container.Resolve<SkillCasterExecutionService>(),
                    container.Resolve<SkillVfxExecutionService>()),
                typeof(SkillPresentationPlayer),
                new[] { typeof(SkillPresentationPlayer), typeof(ISkillPresentationPlayer) },
                Reflex.Enums.Lifetime.Singleton,
                Reflex.Enums.Resolution.Lazy);
        }
    }
}
