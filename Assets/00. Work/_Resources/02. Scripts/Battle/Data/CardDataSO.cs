using System.Collections.Generic;
using Battle.Effects;
using Battle.Enums;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "New Card Data", menuName = "Battle/Card Data", order = 2)]
    public class CardDataSO : ScriptableObject
    {
        public string cardId;
        public string cardName;
        public int cost;
        public CardTargetType targetType;
        public List<CardEffectSO> effects;
    }
}
