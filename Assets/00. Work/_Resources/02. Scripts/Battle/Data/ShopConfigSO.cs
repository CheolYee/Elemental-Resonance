using Battle.Enums;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "Battle/Shop Config")]
    public class ShopConfigSO : ScriptableObject
    {
        [Header("Card Slots")]
        public int cardSlotCount = 4;

        [Header("Card Prices by Grade")]
        public int normalCardPrice    = 50;
        public int rareCardPrice      = 100;
        public int epicCardPrice      = 180;
        public int legendaryCardPrice = 300;

        [Header("Service Prices")]
        public int   rerollCost             = 50;
        public int   removeCardCost         = 75;
        public float rerollCostMultiplier     = 1.5f;
        public float removeCardCostMultiplier = 1.5f;

        public int GetCardPrice(CardGrade grade) => grade switch
        {
            CardGrade.Normal    => normalCardPrice,
            CardGrade.Rare      => rareCardPrice,
            CardGrade.Epic      => epicCardPrice,
            CardGrade.Legendary => legendaryCardPrice,
            _                   => normalCardPrice
        };
    }
}
