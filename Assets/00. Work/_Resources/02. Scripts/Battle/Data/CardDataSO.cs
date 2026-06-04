using System.Collections.Generic;
using _02._Scripts.CombatSystem.Skills;
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
        public Sprite artwork;
        [TextArea] public string description;
        public CardTargetType targetType;
        public List<CardEffectSO> effects;
        public SkillDataSO skillData;
    }
}
