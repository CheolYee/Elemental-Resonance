using System;
using System.Collections.Generic;
using Battle.Enums;
using UnityEngine;

namespace Battle.Fusion
{
    [Serializable]
    public struct FusionRecipeEntry
    {
        public ElementType material1;
        public ElementType material2;
        public ElementType result;
    }

    [CreateAssetMenu(menuName = "Battle/Fusion Recipe Table")]
    public class FusionRecipeTableSO : ScriptableObject
    {
        public List<FusionRecipeEntry> recipes = new();
    }
}
