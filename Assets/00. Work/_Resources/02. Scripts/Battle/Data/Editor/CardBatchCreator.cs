using System;
using System.Collections.Generic;
using Battle.Data;
using Battle.Effects;
using Battle.Enums;
using Battle.Presentation;
using UnityEditor;
using UnityEngine;

namespace Battle.Editor
{
    /// <summary>
    /// Window/Battle/카드 일괄 생성 — CardDataSO + SkillPresentationDataSO 전체 생성.
    /// 이펙트 타임라인(EffectKeyframe)은 Skill Presentation Editor에서 별도 구성 필요.
    /// </summary>
    public static class CardBatchCreator
    {
        private const string CardRoot  = "Assets/05. SO/Cards/PlayerCards";
        private const string TempRoot  = "Assets/05. SO/Cards/TempCards";
        private const string PresRoot  = "Assets/05. SO/Battle/SkillPresentations";

        [MenuItem("Window/Battle/카드 설명 방어도로 일괄 변환")]
        public static void FixBlockToDefense()
        {
            var guids = AssetDatabase.FindAssets("t:CardDataSO");
            int fixed_ = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
                if (card == null || !card.description.Contains("블록")) continue;
                card.description = card.description
                    .Replace("블록을", "방어도를")
                    .Replace("블록", "방어도")
                    .Replace("방어도을", "방어도를");
                EditorUtility.SetDirty(card);
                fixed_++;
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("완료", $"{fixed_}장의 카드 설명에서 '블록' → '방어도' 변환 완료.", "확인");
        }

        [MenuItem("Window/Battle/카드 일괄 생성")]
        public static void CreateAllCards()
        {
            if (!EditorUtility.DisplayDialog("카드 일괄 생성",
                "모든 카드(60장 + 생성 전용 8장)를 생성합니다.\n이미 존재하는 파일은 건너뜁니다.\n계속하시겠습니까?",
                "생성", "취소"))
                return;

            AssetDatabase.StartAssetEditing();
            int created = 0;
            int skipped = 0;
            try
            {
                // ─── 1. 생성 전용 임시 카드 (먼저 생성해야 CreateCardEffect에서 참조 가능) ───
                var iceShard     = MakeTempCard("얼음 파편",      0, CardTargetType.RandomEnemy, CardDisposePolicy.Discard,
                    "얼음 파편을 날려 랜덤한 적에게 3의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(3));

                var arcBullet    = MakeTempCard("비전 탄환",      0, CardTargetType.RandomEnemy, CardDisposePolicy.Discard,
                    "비전 에너지를 압축한 탄환을 날려 랜덤한 적에게 5의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(5));

                var arcBigBullet = MakeTempCard("대비전탄",       1, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    "고밀도 마력을 압축한 거대 탄환을 발사해 적에게 15의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(15));

                var holyArrow    = MakeTempCard("신성 화살",      0, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    "신성한 빛의 화살을 날려 적에게 5의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(5));

                var holyBurst    = MakeTempCard("성스러운 폭발",  1, CardTargetType.AllEnemies,  CardDisposePolicy.Grave,
                    "신성한 빛이 폭발해 모든 적에게 18의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(18));

                var divineJudge  = MakeTempCard("신의 심판 탄",   2, CardTargetType.AllEnemies,  CardDisposePolicy.Grave,
                    "신의 심판이 내려 모든 적에게 30의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(30));

                // ─── 2. None — 기본 공용 카드 ────────────────────────────────────────────
                Make("None_01_기본 공격", "기본 공격", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.None, CardGrade.Normal, CardType.Attack,
                    "무기를 휘둘러 적에게 7의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(7));

                Make("None_02_기본 방어", "기본 방어", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.None, CardGrade.Normal, CardType.Support,
                    "몸을 보호해 7의 방어도를 얻는다.",
                    ref created, ref skipped,
                    BLK(7));

                Make("None_03_기본 드로우", "기본 드로우", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.None, CardGrade.Normal, CardType.Support,
                    "자연의 흐름에 몸을 맡겨 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    DRW(1));

                Make("None_04_기본 버리기", "기본 버리기", 0, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.None, CardGrade.Normal, CardType.Support,
                    "불필요한 카드를 버려 손패를 정리한다.",
                    ref created, ref skipped,
                    DSC(1));

                // ─── 3. Fire — 소진형 고화력 (모두 Grave) ───────────────────────────────
                Make("Fire_01_화염탄", "화염탄", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Fire, CardGrade.Normal, CardType.Attack,
                    "압축된 화염을 발사해 적에게 10의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(10));

                Make("Fire_02_화염 폭풍", "화염 폭풍", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Fire, CardGrade.Rare, CardType.Attack,
                    "맹렬한 불길을 내뿜어 적에게 22의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(22));

                Make("Fire_03_화염 연쇄", "화염 연쇄", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Fire, CardGrade.Epic, CardType.Attack,
                    "맹렬한 화염이 두 번 연속 작렬한다. 적 하나에게 14의 피해를 2회 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(14), DMG(14));

                Make("Fire_04_업화", "업화", 3, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Fire, CardGrade.Legendary, CardType.Attack,
                    "모든 것을 태워버리는 극한의 화염. 적에게 40의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DMG(40));

                // ─── 4. Ice — 방어+파편 생성 ─────────────────────────────────────────────
                Make("Ice_01_얼음 방패", "얼음 방패", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Ice, CardGrade.Normal, CardType.Support,
                    "얼음으로 몸을 감싸 8의 방어도를 얻고, 얼음 파편 1장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(8), CRT(iceShard, 1));

                Make("Ice_02_빙벽", "빙벽", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Ice, CardGrade.Rare, CardType.Support,
                    "두꺼운 얼음 장벽을 세워 18의 방어도를 얻고, 얼음 파편 2장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(18), CRT(iceShard, 2));

                Make("Ice_03_빙하 폭발", "빙하 폭발", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Ice, CardGrade.Epic, CardType.Support,
                    "빙하를 폭발시켜 12의 방어도를 얻고, 흩어진 얼음 파편 3장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(12), CRT(iceShard, 3));

                Make("Ice_04_빙하 요새", "빙하 요새", 3, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Ice, CardGrade.Legendary, CardType.Support,
                    "거대한 얼음 요새를 구축해 25의 방어도를 얻고, 얼음 파편 4장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(25), CRT(iceShard, 4));

                // ─── 5. Lightning — 분산 다타격 ──────────────────────────────────────────
                Make("Lightning_01_번개 화살", "번개 화살", 1, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Lightning, CardGrade.Normal, CardType.Attack,
                    "번개를 내리쳐 모든 적에게 4의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(4));

                Make("Lightning_02_연속 번개", "연속 번개", 2, CardTargetType.RandomEnemy, CardDisposePolicy.Discard,
                    ElementType.Lightning, CardGrade.Rare, CardType.Attack,
                    "연속적인 번개로 랜덤한 적을 3회 공격해 각각 5의 피해를 입힌다. 각 타격은 서로 다른 적을 맞힐 수 있다.",
                    ref created, ref skipped,
                    DMG(5), DMG(5), DMG(5));

                Make("Lightning_03_뇌격", "뇌격", 2, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Lightning, CardGrade.Epic, CardType.Attack,
                    "강력한 번개로 모든 적에게 8의 피해를 입힌 후, 잔류 전류로 다시 4의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(8), DMG(4));

                Make("Lightning_04_천둥 폭풍", "천둥 폭풍", 3, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Lightning, CardGrade.Legendary, CardType.Attack,
                    "폭풍 같은 번개로 모든 적에게 12의 피해를 입힌 후, 연속 방전으로 다시 8의 피해를 입힌다.",
                    ref created, ref skipped,
                    DMG(12), DMG(8));

                // ─── 6. Nature — 코스트·드로우 엔진 ─────────────────────────────────────
                Make("Nature_01_덩굴 채찍", "덩굴 채찍", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Nature, CardGrade.Normal, CardType.Attack,
                    "덩굴로 적을 후려쳐 6의 피해를 입히고 코스트를 1 회복한다.",
                    ref created, ref skipped,
                    DMG(6), CGN(1));

                Make("Nature_02_숲의 부름", "숲의 부름", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Nature, CardGrade.Rare, CardType.Support,
                    "숲의 목소리에 귀를 기울여 카드 2장을 뽑는다.",
                    ref created, ref skipped,
                    DRW(2));

                Make("Nature_03_자연의 흐름", "자연의 흐름", 0, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Nature, CardGrade.Epic, CardType.Support,
                    "자연의 흐름을 타 카드 2장을 뽑고 코스트를 1 회복한다.",
                    ref created, ref skipped,
                    DRW(2), CGN(1));

                Make("Nature_04_대순환", "대순환", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Nature, CardGrade.Legendary, CardType.Support,
                    "대자연의 순환에 몸을 맡겨 카드 3장을 뽑고 코스트를 2 회복한다.",
                    ref created, ref skipped,
                    DRW(3), CGN(2));

                // ─── 7. Light — 극강 방어벽 ──────────────────────────────────────────────
                Make("Light_01_빛의 화살", "빛의 화살", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Light, CardGrade.Normal, CardType.Attack,
                    "신성한 빛의 화살로 적에게 6의 피해를 입히고 3의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(6), BLK(3));

                Make("Light_02_성벽", "성벽", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Light, CardGrade.Rare, CardType.Support,
                    "빛의 장벽을 세워 22의 방어도를 얻는다.",
                    ref created, ref skipped,
                    BLK(22));

                Make("Light_03_신성한 보호", "신성한 보호", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Light, CardGrade.Epic, CardType.Support,
                    "신성한 가호가 내려 18의 방어도를 얻고 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    BLK(18), DRW(1));

                Make("Light_04_빛의 요새", "빛의 요새", 3, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Light, CardGrade.Legendary, CardType.Support,
                    "넘을 수 없는 빛의 요새를 세워 35의 방어도를 얻는다.",
                    ref created, ref skipped,
                    BLK(35));

                // ─── 8. Dark — 버리기+폭발 데미지 ───────────────────────────────────────
                Make("Dark_01_어둠의 기습", "어둠의 기습", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Dark, CardGrade.Normal, CardType.Attack,
                    "손패 1장을 희생해 어둠의 힘을 해방한다. 적에게 12의 피해를 입힌다.",
                    ref created, ref skipped,
                    DSC(1), DMG(12));

                Make("Dark_02_흑암의 강타", "흑암의 강타", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Dark, CardGrade.Rare, CardType.Attack,
                    "손패 1장을 희생해 흑암의 일격을 날린다. 적에게 20의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DSC(1), DMG(20));

                Make("Dark_03_어둠의 폭발", "어둠의 폭발", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Dark, CardGrade.Epic, CardType.Attack,
                    "손패 1장을 희생해 어둠의 폭발을 일으킨다. 적에게 28의 피해를 입힌다.",
                    ref created, ref skipped,
                    DSC(1), DMG(28));

                Make("Dark_04_심연", "심연", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Dark, CardGrade.Legendary, CardType.Attack,
                    "손패 2장을 희생해 심연의 힘을 해방한다. 적에게 38의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DSC(2), DMG(38));

                // ─── 9. Arcane — 코스트 조작+카드 생성 ──────────────────────────────────
                Make("Arcane_01_마력 충전", "마력 충전", 0, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Arcane, CardGrade.Normal, CardType.Support,
                    "주변의 마력을 끌어모아 코스트를 1 회복한다.",
                    ref created, ref skipped,
                    CGN(1));

                Make("Arcane_02_비전 창조", "비전 창조", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Arcane, CardGrade.Rare, CardType.Support,
                    "마법으로 비전 탄환 2발을 만들어 손패에 추가한다.",
                    ref created, ref skipped,
                    CRT(arcBullet, 2));

                Make("Arcane_03_마법 가속", "마법 가속", 0, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Arcane, CardGrade.Epic, CardType.Support,
                    "시간의 흐름을 가속해 코스트를 2 회복하고 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    CGN(2), DRW(1));

                Make("Arcane_04_비전 대폭발", "비전 대폭발", 0, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Arcane, CardGrade.Legendary, CardType.Support,
                    "모든 마력을 집중해 강력한 대비전탄 3발을 손패에 추가하고 코스트를 1 회복한다.",
                    ref created, ref skipped,
                    CRT(arcBigBullet, 3), CGN(1));

                // ─── 10. Steam — 공방 일체 (Fire + Ice) ─────────────────────────────────
                Make("Steam_01_증기 분사", "증기 분사", 2, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Steam, CardGrade.Normal, CardType.Attack,
                    "고압 증기를 분사해 모든 적에게 8의 피해를 입히고 8의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(8), BLK(8));

                Make("Steam_02_끓는 격류", "끓는 격류", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Steam, CardGrade.Rare, CardType.Attack,
                    "끓어오르는 증기의 흐름으로 적에게 14의 피해를 입히고 14의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(14), BLK(14));

                Make("Steam_03_증기 폭발", "증기 폭발", 3, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Steam, CardGrade.Epic, CardType.Attack,
                    "거대한 증기 폭발로 모든 적에게 20의 피해를 입히고 15의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(20), BLK(15));

                Make("Steam_04_대증기", "대증기", 3, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Steam, CardGrade.Legendary, CardType.Attack,
                    "최강의 증기 폭발로 모든 적에게 25의 피해를 입히고, 25의 방어도를 얻으며, 얼음 파편 2장을 손패에 추가한다.",
                    ref created, ref skipped,
                    DMG(25), BLK(25), CRT(iceShard, 2));

                // ─── 11. Storm — 연타+드로우 (Lightning + Nature) ────────────────────────
                Make("Storm_01_돌풍", "돌풍", 1, CardTargetType.RandomEnemy, CardDisposePolicy.Discard,
                    ElementType.Storm, CardGrade.Normal, CardType.Attack,
                    "돌풍을 일으켜 랜덤한 적에게 5의 피해를 입히고 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    DMG(5), DRW(1));

                Make("Storm_02_폭풍 질주", "폭풍 질주", 2, CardTargetType.RandomEnemy, CardDisposePolicy.Discard,
                    ElementType.Storm, CardGrade.Rare, CardType.Attack,
                    "빠른 폭풍으로 랜덤한 적을 2회 공격해 각 6의 피해를 입히고 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    DMG(6), DMG(6), DRW(1));

                Make("Storm_03_뇌우", "뇌우", 2, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Storm, CardGrade.Epic, CardType.Attack,
                    "번개를 동반한 폭풍으로 모든 적에게 8의 피해를 입히고 카드 2장을 뽑는다.",
                    ref created, ref skipped,
                    DMG(8), DRW(2));

                Make("Storm_04_대폭풍", "대폭풍", 3, CardTargetType.AllEnemies, CardDisposePolicy.Discard,
                    ElementType.Storm, CardGrade.Legendary, CardType.Attack,
                    "무적의 폭풍을 일으켜 모든 적에게 10의 피해를 입히고, 카드 3장을 뽑으며, 코스트를 1 회복한다.",
                    ref created, ref skipped,
                    DMG(10), DRW(3), CGN(1));

                // ─── 12. Twilight — 공방 균형 (Dark + Light) ─────────────────────────────
                Make("Twilight_01_황혼의 손길", "황혼의 손길", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Twilight, CardGrade.Normal, CardType.Attack,
                    "어둠과 빛이 교차하는 황혼의 힘으로 적에게 8의 피해를 입히고 8의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(8), BLK(8));

                Make("Twilight_02_황혼의 심판", "황혼의 심판", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Twilight, CardGrade.Rare, CardType.Attack,
                    "황혼의 힘이 폭발해 적에게 15의 피해를 입히고 15의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DMG(15), BLK(15));

                Make("Twilight_03_황혼의 폭발", "황혼의 폭발", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Twilight, CardGrade.Epic, CardType.Attack,
                    "손패 1장을 희생해 황혼의 힘을 해방한다. 적에게 18의 피해를 입히고 18의 방어도를 얻는다.",
                    ref created, ref skipped,
                    DSC(1), DMG(18), BLK(18));

                Make("Twilight_04_황혼의 지배", "황혼의 지배", 3, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Twilight, CardGrade.Legendary, CardType.Attack,
                    "황혼의 완전한 지배. 적에게 25의 피해를 입히고, 25의 방어도를 얻으며, 카드 1장을 뽑는다.",
                    ref created, ref skipped,
                    DMG(25), BLK(25), DRW(1));

                // ─── 13. Poison — 패 사이클 (Nature + Dark) ──────────────────────────────
                Make("Poison_01_독안개", "독안개", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Poison, CardGrade.Normal, CardType.Support,
                    "독안개를 피워 패의 흐름을 바꾼다. 카드 2장을 뽑고 1장을 버린다.",
                    ref created, ref skipped,
                    DRW(2), DSC(1));

                Make("Poison_02_독성 순환", "독성 순환", 1, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Poison, CardGrade.Rare, CardType.Attack,
                    "독이 퍼지며 감각이 날카로워진다. 카드 3장을 뽑고 1장을 버린 후 적에게 8의 피해를 입힌다.",
                    ref created, ref skipped,
                    DRW(3), DSC(1), DMG(8));

                Make("Poison_03_부패", "부패", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Discard,
                    ElementType.Poison, CardGrade.Epic, CardType.Attack,
                    "부패의 힘으로 패를 정제한다. 카드 3장을 뽑고 2장을 버린 후 적에게 18의 피해를 입힌다.",
                    ref created, ref skipped,
                    DRW(3), DSC(2), DMG(18));

                Make("Poison_04_절대 독", "절대 독", 2, CardTargetType.SingleEnemy, CardDisposePolicy.Grave,
                    ElementType.Poison, CardGrade.Legendary, CardType.Attack,
                    "절대적인 독의 힘이 폭발한다. 카드 4장을 뽑고 2장을 버린 후 적에게 25의 피해를 입힌다. 사용 후 소멸한다.",
                    ref created, ref skipped,
                    DRW(4), DSC(2), DMG(25));

                // ─── 14. Holy — 방어+강화 생성 (Light + Arcane) ─────────────────────────
                Make("Holy_01_신성한 빛", "신성한 빛", 1, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Holy, CardGrade.Normal, CardType.Support,
                    "신성한 빛이 내려 10의 방어도를 얻고 신성 화살 1발을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(10), CRT(holyArrow, 1));

                Make("Holy_02_천사의 가호", "천사의 가호", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Holy, CardGrade.Rare, CardType.Support,
                    "천사의 가호로 18의 방어도를 얻고 신성 화살 2발을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(18), CRT(holyArrow, 2));

                Make("Holy_03_성스러운 요새", "성스러운 요새", 2, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Holy, CardGrade.Epic, CardType.Support,
                    "성스러운 요새를 세워 20의 방어도를 얻고 강력한 성스러운 폭발 1장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(20), CRT(holyBurst, 1));

                Make("Holy_04_신의 심판", "신의 심판", 3, CardTargetType.None, CardDisposePolicy.Discard,
                    ElementType.Holy, CardGrade.Legendary, CardType.Support,
                    "신의 가호 아래 30의 방어도를 얻고 강력한 신의 심판 1장을 손패에 추가한다.",
                    ref created, ref skipped,
                    BLK(30), CRT(divineJudge, 1));
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("완료",
                $"카드 생성 완료!\n생성: {created}장\n건너뜀(기존 존재): {skipped}장", "확인");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  이펙트 슬롯 생성 헬퍼 (이름 짧게 유지)
        // ─────────────────────────────────────────────────────────────────────────
        private static CardEffectSlot DMG(int v) => Slot(new DamageEffect { damage = v });
        private static CardEffectSlot BLK(int v) => Slot(new BlockEffect  { block  = v });
        private static CardEffectSlot DRW(int v) => Slot(new RandomDrawEffect { drawCount   = v });
        private static CardEffectSlot CGN(int v) => Slot(new CostGainEffect   { costGain    = v });
        private static CardEffectSlot DSC(int v) => Slot(new DiscardEffect    { discardCount = v });
        private static CardEffectSlot CRT(CardDataSO card, int count) =>
            Slot(new CreateCardEffect { cardDataSO = card, createCount = count });

        private static CardEffectSlot Slot(CardEffect effect) => new()
        {
            effectSlotId = Guid.NewGuid().ToString(),
            effect       = effect
        };

        // ─────────────────────────────────────────────────────────────────────────
        //  카드 생성 헬퍼
        // ─────────────────────────────────────────────────────────────────────────
        private static CardDataSO MakeTempCard(
            string cardName, int cost, CardTargetType targetType, CardDisposePolicy policy,
            string description, ref int created, ref int skipped,
            params CardEffectSlot[] effects)
        {
            EnsureFolder(TempRoot);
            string path = $"{TempRoot}/{cardName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
            if (existing != null) { skipped++; return existing; }

            var card = ScriptableObject.CreateInstance<CardDataSO>();
            card.cardId        = cardName;
            card.cardName      = cardName;
            card.cost          = cost;
            card.targetType    = targetType;
            card.disposePolicy = policy;
            card.elementType   = ElementType.None;
            card.grade         = CardGrade.Normal;
            card.cardType      = CardType.Attack;
            card.description   = description;
            card.effectSlots   = new List<CardEffectSlot>(effects);
            card.EnsureEffectSlotIds();

            AssetDatabase.CreateAsset(card, path);
            created++;
            return card;
        }

        private static void Make(
            string fileName, string cardName, int cost,
            CardTargetType targetType, CardDisposePolicy disposePolicy,
            ElementType elementType, CardGrade grade, CardType cardType,
            string description, ref int created, ref int skipped,
            params CardEffectSlot[] effects)
        {
            string cardFolder = elementType == ElementType.None
                ? $"{CardRoot}/None"
                : $"{CardRoot}/{elementType}";
            EnsureFolder(cardFolder);

            string cardPath = $"{cardFolder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<CardDataSO>(cardPath) != null)
            {
                skipped++;
                return;
            }

            var card = ScriptableObject.CreateInstance<CardDataSO>();
            card.cardId        = fileName;
            card.cardName      = cardName;
            card.cost          = cost;
            card.targetType    = targetType;
            card.disposePolicy = disposePolicy;
            card.elementType   = elementType;
            card.grade         = grade;
            card.cardType      = cardType;
            card.description   = description;
            card.effectSlots   = new List<CardEffectSlot>(effects);
            card.EnsureEffectSlotIds();

            AssetDatabase.CreateAsset(card, cardPath);

            // SkillPresentationDataSO 자동 생성 (빈 타임라인 — 연출은 에디터에서 별도 구성)
            string presFolder = elementType == ElementType.None
                ? $"{PresRoot}/None"
                : $"{PresRoot}/{elementType}";
            EnsureFolder(presFolder);

            string presPath = $"{presFolder}/{fileName}_Presentation.asset";
            if (AssetDatabase.LoadAssetAtPath<SkillPresentationDataSO>(presPath) == null)
            {
                var pres = ScriptableObject.CreateInstance<SkillPresentationDataSO>();
                AssetDatabase.CreateAsset(pres, presPath);
                card.presentationData = pres;
                EditorUtility.SetDirty(card);
            }

            created++;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name   = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
