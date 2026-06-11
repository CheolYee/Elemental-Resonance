using UnityEngine;

namespace Battle.Effects
{
    public static class CardEffectValueCalculator
    {
        public static int Calculate(int baseValue, float multiplier = 1f)
            => Mathf.RoundToInt(baseValue * multiplier);
    }
}
