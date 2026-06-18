using UnityEngine;
using UnityEditor;
using Battle.Data;
using Battle.Enums;
using Battle.Effects;
using System.Collections.Generic;

public static class FusionCardCreator
{
    [MenuItem("Tools/Create Fusion Result Cards")]
    public static void CreateAll()
    {
        const string basePath = "Assets/05. SO/Cards/PlayerCards";

        var elements = new (ElementType type, string en, string ko)[]
        {
            (ElementType.Steam,    "Steam",    "증기"),
            (ElementType.Storm,    "Storm",    "폭풍"),
            (ElementType.Twilight, "Twilight", "황혼"),
            (ElementType.Poison,   "Poison",   "맹독"),
            (ElementType.Holy,     "Holy",     "신성"),
        };

        var grades = new (CardGrade grade, string en, string ko, int dmg)[]
        {
            (CardGrade.Normal,    "Normal",    "강타", 10),
            (CardGrade.Rare,      "Rare",      "파동", 15),
            (CardGrade.Epic,      "Epic",      "폭격", 22),
            (CardGrade.Legendary, "Legendary", "심판", 30),
        };

        int created = 0;

        foreach (var e in elements)
        {
            var folder = $"{basePath}/{e.en}";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(basePath, e.en);

            foreach (var g in grades)
            {
                var path = $"{folder}/{e.en}_{g.en}.asset";
                if (AssetDatabase.LoadAssetAtPath<CardDataSO>(path) != null) continue;

                var so           = ScriptableObject.CreateInstance<CardDataSO>();
                so.cardId        = $"{e.en.ToLower()}_{g.en.ToLower()}_01";
                so.cardName      = $"{e.ko} {g.ko}";
                so.cost          = 1;
                so.description   = $"[{g.en}] {e.ko} 속성 기본 공격 카드 (데미지 {g.dmg})";
                so.targetType    = CardTargetType.SingleEnemy;
                so.disposePolicy = CardDisposePolicy.Discard;
                so.elementType   = e.type;
                so.grade         = g.grade;
                so.cardType      = CardType.Attack;
                so.effectSlots   = new List<CardEffectSlot>
                {
                    new CardEffectSlot { effect = new DamageEffect { damage = g.dmg } }
                };
                so.EnsureEffectSlotIds();

                AssetDatabase.CreateAsset(so, path);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FusionCardCreator] 완료: {created}개 CardDataSO 생성");
    }
}
