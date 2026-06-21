using System;
using System.Collections.Generic;
using System.Threading;
using Battle.Events;
using Battle.Instances;
using Battle.Presentation;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class DiscardEffect : CardEffect, IAsyncCardEffect
    {
        public int discardCount = 1;

        public override int BaseValue => discardCount;

        public override void Apply(GameObject source, GameObject target, int finalValue) { }

        public async UniTask<bool> ApplyAsync(
            SkillPresentationPlaybackContext context,
            int finalValue,
            CancellationToken ct,
            EventChannelSO eventChannel)
        {
            int count = finalValue > 0 ? finalValue : discardCount;

            var tcs = new UniTaskCompletionSource<List<CardInstance>>();
            eventChannel.RaiseEvent(new DiscardSelectRequestEvent(count, tcs));

            Time.timeScale = 0f;
            List<CardInstance> selected;
            try
            {
                selected = await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                Time.timeScale = 1f;
            }

            if (selected == null)
            {
                context.FizzleToken.RequestFizzle();
                return false;
            }

            if (selected.Count == 0) return true;

            eventChannel.RaiseEvent(new DiscardCardsEvent(selected));
            return true;
        }
    }
}
