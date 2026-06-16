using Battle.Effects;
using Battle.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Presentation
{
    public sealed class SkillEffectExecutionService
    {
        private readonly EventChannelSO _battleEventChannel;

        public SkillEffectExecutionService(EventChannelSO battleEventChannel)
        {
            _battleEventChannel = battleEventChannel;
        }

        public void Execute(SkillPresentationPlaybackContext context, SkillKeyframeData keyframe)
        {
            if (context?.UsageData?.CardInstance?.data == null || keyframe == null) return;

            string cardName = context.UsageData.CardInstance.data.cardName;
            context.UsageData.CardInstance.data.EnsureEffectSlotIds();

            if (string.IsNullOrEmpty(keyframe.effectSlotId))
            {
                Debug.LogWarning($"[SkillPresentation] 카드 '{cardName}' EffectKeyframe({keyframe.timeSeconds:0.###}s)에 effectSlotId가 비어 있습니다.");
                return;
            }

            CardEffectSlot slot = ResolveSlot(context, keyframe.effectSlotId);
            if (slot?.effect == null)
            {
                Debug.LogWarning($"[SkillPresentation] 카드 '{cardName}'에서 effectSlotId '{keyframe.effectSlotId}'를 찾지 못했습니다.");
                return;
            }

            int finalValue = CardEffectValueCalculator.Calculate(slot.effect.BaseValue, keyframe.valueMultiplier);

            // CostGainEffect의 실제 값은 BattleActionExecutor가 카드 적재 시점에 이미 적용했다.
            // 여기서는 그 변화를 화면에 보여주는 시점(연출 타이밍)만 알린다.
            if (slot.effect is CostGainEffect)
            {
                _battleEventChannel?.RaiseEvent(new CostGainRevealedEvent(finalValue));
                return;
            }

            GameObject source = context.Caster?.gameObject;
            GameObject target = ResolveEffectTarget(context, slot.effect)?.gameObject;
            slot.effect.Apply(source, target, finalValue);
        }

        private CardEffectSlot ResolveSlot(SkillPresentationPlaybackContext context, string effectSlotId)
        {
            var slots = context?.UsageData?.CardInstance?.data?.effectSlots;
            if (slots == null || string.IsNullOrEmpty(effectSlotId)) return null;

            for (int i = 0; i < slots.Count; i++)
            {
                CardEffectSlot slot = slots[i];
                if (slot != null && slot.effectSlotId == effectSlotId) return slot;
            }

            return null;
        }

        private static _00._Work._Resources._02._Scripts.Agents.Agent ResolveEffectTarget(
            SkillPresentationPlaybackContext context,
            CardEffect effect)
        {
            if (context == null || effect == null) return null;

            return effect switch
            {
                DamageEffect => context.Target,
                BlockEffect  => context.Caster,
                _            => context.Target
            };
        }
    }
}
