using System;
using UnityEngine;

namespace Battle.Effects
{
    [Serializable]
    public class CardEffectSlot
    {
        [HideInInspector] public string effectSlotId;
        [SerializeReference] public CardEffect effect;
    }
}
