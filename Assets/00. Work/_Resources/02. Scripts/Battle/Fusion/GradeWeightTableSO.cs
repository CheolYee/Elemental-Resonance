using Battle.Enums;
using UnityEngine;

namespace Battle.Fusion
{
    [CreateAssetMenu(menuName = "Battle/Grade Weight Table")]
    public class GradeWeightTableSO : ScriptableObject
    {
        public int[] normalWeights    = { 50, 30, 15,  5 };
        public int[] rareWeights      = { 30, 40, 20, 10 };
        public int[] epicWeights      = { 15, 25, 40, 20 };
        public int[] legendaryWeights = {  5, 15, 30, 50 };

        public int[] GetWeights(CardGrade grade) => grade switch
        {
            CardGrade.Normal    => normalWeights,
            CardGrade.Rare      => rareWeights,
            CardGrade.Epic      => epicWeights,
            CardGrade.Legendary => legendaryWeights,
            _                   => normalWeights
        };
    }
}
