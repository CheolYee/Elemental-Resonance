using Battle.Enums;

namespace Battle.Fusion
{
    public class FusionRecipeService
    {
        private readonly FusionRecipeTableSO _table;

        public FusionRecipeService(FusionRecipeTableSO table) => _table = table;

        public bool TryGetResult(ElementType a, ElementType b, out ElementType result)
        {
            result = ElementType.None;
            if (a == ElementType.None || b == ElementType.None) return false;
            foreach (var recipe in _table.recipes)
            {
                if ((recipe.material1 == a && recipe.material2 == b) ||
                    (recipe.material1 == b && recipe.material2 == a))
                {
                    result = recipe.result;
                    return true;
                }
            }
            return false;
        }

        public bool CanFuse(ElementType a, ElementType b) =>
            a != ElementType.None && b != ElementType.None && TryGetResult(a, b, out _);
    }
}
