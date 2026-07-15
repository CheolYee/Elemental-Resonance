using Battle.Enums;
using Battle.Fusion;
using Battle.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckBuilding
{
    public class FusionRecipeRowView : MonoBehaviour
    {
        [SerializeField] private Image    _material1Icon;
        [SerializeField] private TMP_Text _material1Text;
        [SerializeField] private Image    _material2Icon;
        [SerializeField] private TMP_Text _material2Text;
        [SerializeField] private Image    _resultIcon;
        [SerializeField] private TMP_Text _resultText;

        public void Setup(FusionRecipeEntry entry, ElementIconTableSO iconTable)
        {
            Apply(_material1Icon, _material1Text, entry.material1, iconTable);
            Apply(_material2Icon, _material2Text, entry.material2, iconTable);
            Apply(_resultIcon,    _resultText,    entry.result,    iconTable);
        }

        private static void Apply(Image icon, TMP_Text label, ElementType element, ElementIconTableSO table)
        {
            if (icon != null)
            {
                var sprite   = table != null ? table.GetIcon(element) : null;
                icon.sprite  = sprite;
                icon.enabled = sprite != null;
                icon.color   = CardColorUtility.GetElementColor(element);
            }
            if (label != null)
            {
                label.text  = CardColorUtility.GetElementName(element);
                label.color = CardColorUtility.GetElementColor(element);
            }
        }
    }
}
