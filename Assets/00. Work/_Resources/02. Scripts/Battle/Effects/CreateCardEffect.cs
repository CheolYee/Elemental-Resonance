using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Data;
using Battle.Events;
using Battle.Instances;
using Battle.Presentation;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class CreateCardEffect : CardEffect, IAsyncCardEffect
    {
        public CardDataSO cardDataSO;
        public int createCount = 1;

        public override int BaseValue => createCount;

        public override void Apply(GameObject source, GameObject target, int finalValue) { }

        public async UniTask<bool> ApplyAsync(
            SkillPresentationPlaybackContext context,
            int finalValue,
            CancellationToken ct,
            EventChannelSO eventChannel)
        {
            if (cardDataSO == null)
                return true;

            int count = finalValue > 0 ? finalValue : createCount;

            var createTcs = new UniTaskCompletionSource<List<CardInstance>>();
            eventChannel.RaiseEvent(new CreateTempCardsRequestEvent(cardDataSO, count, createTcs));

            List<CardInstance> created;
            try
            {
                created = await createTcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (created == null || created.Count == 0)
                return true;

            var animTcs = new UniTaskCompletionSource();
            eventChannel.RaiseEvent(new SkillDrawCardsRequestEvent(created, animTcs));
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
