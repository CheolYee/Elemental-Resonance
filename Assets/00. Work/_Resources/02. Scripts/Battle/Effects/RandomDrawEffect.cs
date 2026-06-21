using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Enums;
using Battle.Events;
using Battle.Instances;
using Battle.Presentation;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class RandomDrawEffect : CardEffect, IAsyncCardEffect
    {
        public PileType sourcePile = PileType.DrawPile;
        public int drawCount = 1;

        public override int BaseValue => drawCount;

        public override void Apply(GameObject source, GameObject target, int finalValue) { }

        public async UniTask<bool> ApplyAsync(
            SkillPresentationPlaybackContext context,
            int finalValue,
            CancellationToken ct,
            EventChannelSO eventChannel)
        {
            int count = finalValue > 0 ? finalValue : drawCount;

            var drawTcs = new UniTaskCompletionSource<List<CardInstance>>();
            eventChannel.RaiseEvent(new RandomDrawRequestEvent(sourcePile, count, drawTcs));

            List<CardInstance> drawn;
            try
            {
                drawn = await drawTcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (drawn == null || drawn.Count == 0)
                return true;

            var animTcs = new UniTaskCompletionSource();
            eventChannel.RaiseEvent(new SkillDrawCardsRequestEvent(drawn, animTcs));
            try
            {
                await animTcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}
