using System.Collections.Generic;
using Battle.Data;
using Battle.Enums;
using TMProEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public static class CardColorUtility
    {
        public static readonly Dictionary<ElementType, Color> ElementColors;

        private static readonly Dictionary<ElementType, (ElementType a, ElementType b)> FusionParents = new()
        {
            { ElementType.Steam,    (ElementType.Fire,      ElementType.Ice)    },
            { ElementType.Storm,    (ElementType.Lightning,  ElementType.Nature) },
            { ElementType.Twilight, (ElementType.Dark,       ElementType.Light)  },
            { ElementType.Poison,   (ElementType.Nature,     ElementType.Dark)   },
            { ElementType.Holy,     (ElementType.Light,      ElementType.Arcane) },
        };

        static CardColorUtility()
        {
            var baseColors = new Dictionary<ElementType, Color>
            {
                { ElementType.None,      Color.white },
                { ElementType.Fire,      new Color(0.906f, 0.298f, 0.235f) },
                { ElementType.Lightning, new Color(0.100f, 0.200f, 0.820f) },
                { ElementType.Nature,    new Color(0.180f, 0.800f, 0.443f) },
                { ElementType.Light,     new Color(0.980f, 0.960f, 0.600f) },
                { ElementType.Ice,       new Color(0.365f, 0.851f, 0.949f) },
                { ElementType.Dark,      new Color(0.050f, 0.300f, 0.100f) },
                { ElementType.Arcane,    new Color(0.557f, 0.267f, 0.678f) },
            };

            ElementColors = new Dictionary<ElementType, Color>(baseColors);

            foreach (var kvp in FusionParents)
            {
                var (a, b) = kvp.Value;
                if (baseColors.TryGetValue(a, out var ca) && baseColors.TryGetValue(b, out var cb))
                    ElementColors[kvp.Key] = Color.Lerp(ca, cb, 0.5f);
            }
        }

        public static readonly Dictionary<CardGrade, Color> GradeColors = new()
        {
            { CardGrade.Normal,    new Color(0.835f, 0.847f, 0.863f) },
            { CardGrade.Rare,      new Color(0.204f, 0.596f, 0.859f) },
            { CardGrade.Epic,      new Color(0.608f, 0.349f, 0.714f) },
            { CardGrade.Legendary, new Color(0.953f, 0.612f, 0.071f) },
        };

        public static Color GetElementColor(ElementType type) =>
            ElementColors.TryGetValue(type, out var c) ? c : Color.white;

        public static string GetTypeName(CardType type) => type switch
        {
            CardType.Attack  => "공격",
            CardType.Support => "지원",
            _                => string.Empty,
        };

        public static string GetElementName(ElementType type) => type switch
        {
            ElementType.Fire      => "화염",
            ElementType.Lightning => "번개",
            ElementType.Nature    => "자연",
            ElementType.Light     => "빛",
            ElementType.Ice       => "얼음",
            ElementType.Dark      => "어둠",
            ElementType.Arcane    => "비전",
            ElementType.Steam     => "증기",
            ElementType.Storm     => "폭풍",
            ElementType.Twilight  => "황혼",
            ElementType.Poison    => "독",
            ElementType.Holy      => "신성",
            _                     => "무속성",
        };

        public static void Apply(Image frameImage, Image labelImage, CardDataSO data)
        {
            if (frameImage != null && ElementColors.TryGetValue(data.elementType, out var elemColor))
                frameImage.color = elemColor;
            if (labelImage != null && GradeColors.TryGetValue(data.grade, out var gradeColor))
                labelImage.color = gradeColor;
        }

        public static void ApplyTextEffects(TMPEffect nameEffect, TMPEffect typeEffect, CardGrade grade)
        {
            if (!GradeColors.TryGetValue(grade, out var color)) return;

            if (nameEffect != null)
            {
                nameEffect.outlineColor  = color;
                nameEffect.underlayColor = color;
                nameEffect.SetupEffect();
            }
            if (typeEffect != null)
            {
                typeEffect.outlineColor  = color;
                typeEffect.underlayColor = color;
                typeEffect.SetupEffect();
            }
        }

        public static void ApplyElementTextEffect(TMPEffect effect, ElementType element)
        {
            if (effect == null) return;
            var color = GetElementColor(element);
            effect.outlineColor  = color;
            effect.underlayColor = color;
            effect.SetupEffect();
        }

        public static void ApplyCostImage(Image costImage, ElementIconTableSO table, ElementType element)
        {
            if (costImage == null || table == null) return;
            var icon = table.GetIcon(element);
            costImage.sprite  = icon;
            costImage.enabled = icon != null;
        }

        public static void Reset(Image frameImage, Image labelImage)
        {
            if (frameImage != null) frameImage.color = Color.white;
            if (labelImage != null) labelImage.color = Color.white;
        }

        public static void ResetTextEffects(TMPEffect nameEffect, TMPEffect typeEffect)
        {
            if (nameEffect != null)
            {
                nameEffect.outlineColor  = Color.black;
                nameEffect.underlayColor = Color.black;
                nameEffect.SetupEffect();
            }
            if (typeEffect != null)
            {
                typeEffect.outlineColor  = Color.black;
                typeEffect.underlayColor = Color.black;
                typeEffect.SetupEffect();
            }
        }
    }
}
