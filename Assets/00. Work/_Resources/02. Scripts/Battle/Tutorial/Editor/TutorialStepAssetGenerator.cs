#if UNITY_EDITOR
using System.Collections.Generic;
using Battle.Tutorial;
using UnityEditor;
using UnityEngine;

namespace Battle.Tutorial.Editor
{
    // Menu: Battle/Generate Tutorial Step Assets
    // Assets/05. SO/Tutorial/Steps/ 에 10개 TutorialStepSO를 생성/덮어쓰기 후
    // Assets/05. SO/Tutorial/TutorialSequence.asset 에 steps 리스트를 연결한다.
    //
    // targetId 규칙:
    //   - 씬에 고정된 UI 오브젝트 → 해당 오브젝트에 TutorialTarget 컴포넌트를 붙이고 targetId 입력
    //   - MapNodeView 프리팹 → TutorialTarget 부착 (id 비워둠), 런타임에 MapNodeType.ToString() 자동 설정
    //   - CardView 프리팹   → TutorialTarget 부착, targetId = "HandCard" 고정
    public static class TutorialStepAssetGenerator
    {
        private const string StepsFolder  = "Assets/05. SO/Tutorial/Steps";
        private const string SequencePath = "Assets/05. SO/Tutorial/TutorialSequence.asset";

        private struct StepData
        {
            public string FileName;
            public string TooltipText;
            public string TargetId;
            public int    TargetIndex;
            public TooltipAnchor Anchor;
            public TutorialCompletionType CompletionType;
            public string EventTypeName;
            public bool BlockAllInput;
        }

        [MenuItem("Battle/Generate Tutorial Step Assets")]
        private static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(StepsFolder))
                AssetDatabase.CreateFolder("Assets/05. SO/Tutorial", "Steps");

            var definitions = BuildDefinitions();
            var stepAssets  = new List<TutorialStepSO>();

            foreach (var def in definitions)
            {
                string path = $"{StepsFolder}/{def.FileName}.asset";
                var so = AssetDatabase.LoadAssetAtPath<TutorialStepSO>(path);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<TutorialStepSO>();
                    AssetDatabase.CreateAsset(so, path);
                }

                var serialized = new SerializedObject(so);
                serialized.FindProperty("tooltipText")           .stringValue    = def.TooltipText;
                serialized.FindProperty("targetId")              .stringValue    = def.TargetId;
                serialized.FindProperty("targetIndex")           .intValue       = def.TargetIndex;
                serialized.FindProperty("preferredAnchor")       .enumValueIndex = (int)def.Anchor;
                serialized.FindProperty("completionType")        .enumValueIndex = (int)def.CompletionType;
                serialized.FindProperty("completionEventTypeName").stringValue   = def.EventTypeName;
                serialized.FindProperty("blockAllInput")         .boolValue      = def.BlockAllInput;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(so);
                stepAssets.Add(so);
            }

            // TutorialSequenceSO steps 리스트 갱신
            var seq = AssetDatabase.LoadAssetAtPath<TutorialSequenceSO>(SequencePath);
            if (seq == null)
            {
                seq = ScriptableObject.CreateInstance<TutorialSequenceSO>();
                AssetDatabase.CreateAsset(seq, SequencePath);
            }

            var seqSerialized = new SerializedObject(seq);
            var stepsProp     = seqSerialized.FindProperty("steps");
            stepsProp.arraySize = stepAssets.Count;
            for (int i = 0; i < stepAssets.Count; i++)
                stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = stepAssets[i];
            seqSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(seq);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TutorialGenerator] {stepAssets.Count}개 StepSO 생성/갱신 완료. Sequence 연결 완료.");
            Selection.activeObject = seq;
        }

        private static List<StepData> BuildDefinitions() => new()
        {
            // ── Phase A: 맵 ───────────────────────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_01_MapOverview",
                TooltipText    = "이 지도를 따라 여행합니다.\n각 노드는 전투 / 상점 / 휴식 중 하나입니다.",
                TargetId       = "MapContainer",   // 맵 노드 전체 컨테이너에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_02_SelectNode",
                TooltipText    = "전투 노드를 선택해 첫 번째 전투를 시작하세요!",
                TargetId       = "Battle",         // MapNodeView 프리팹에 TutorialTarget 부착 → 런타임에 "Battle" 자동 설정
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Map.Events.MapNodeSelectedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 카드 드로우 완료 대기
            {
                FileName       = "TutorialStep_02b_BattleGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.CardDrawEndEvent",
                BlockAllInput  = false,
            },

            // ── Phase B: 전투 UI 소개 ─────────────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_03_EnemyInfo",
                TooltipText    = "적에게 마우스를 올리면 HP와 이번 턴 행동 의도를 확인할 수 있습니다.",
                TargetId       = "",               // World Space 캔버스라 중앙 표시
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_04_CostPanel",
                TooltipText    = "코스트는 매 턴 시작 시 충전됩니다.\n카드를 사용하면 차감됩니다.",
                TargetId       = "CostPanel",      // 코스트 표시 패널에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },

            // ── Phase C: 카드 더미 소개 (묶음) ───────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_05_DrawPile",
                TooltipText    = "뽑을 카드 더미입니다.\n소진되면 버린 카드 더미를 셔플해 자동으로 보충합니다.",
                TargetId       = "DrawPile",       // DrawPile 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_05b_DiscardPile",
                TooltipText    = "버린 카드 더미입니다.\n카드를 사용하거나 턴을 종료하면 이곳으로 이동합니다.",
                TargetId       = "DiscardPile",    // DiscardPile 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_05c_GravePile",
                TooltipText    = "소멸 카드 더미입니다.\n일부 카드는 사용 시 이곳으로 이동하며 셔플되지 않습니다.",
                TargetId       = "GravePile",      // GravePile 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },

            // ── Phase D: 상단 메뉴 소개 ──────────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_05d_SpeedButton",
                TooltipText    = "배속 버튼입니다.\n전투 연출 속도를 조절할 수 있습니다.",
                TargetId       = "SpeedButton",    // 배속 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_05e_DeckButton",
                TooltipText    = "보유 카드 더미(덱)입니다.\n클릭하면 현재 덱 전체를 확인할 수 있습니다.",
                TargetId       = "DeckButton",     // 덱 보기 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },

            // ── Phase E: 카드 액션 ────────────────────────────────────────────────
            new StepData   // 안내: 카드 사용 설명 읽기
            {
                FileName       = "TutorialStep_06_UseCard",
                TooltipText    = "카드를 적에게 드래그해서 사용하세요!",
                TargetId       = "HandCard",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 실제 카드 사용 대기
            {
                FileName       = "TutorialStep_06b_UseCardGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.CardDroppedOnTargetEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 합성 설명 읽기
            {
                FileName       = "TutorialStep_07_Fusion",
                TooltipText    = "원소 카드끼리 드래그하면 합성됩니다!\n카드를 다른 카드 위로 올려보세요.",
                TargetId       = "HandCard",
                TargetIndex    = 1,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 실제 합성 대기
            {
                FileName       = "TutorialStep_07b_FusionGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.CardFusionRequestedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트: 합성 애니메이션 완료 + 합성 카드 손패 진입 대기
            {
                FileName       = "TutorialStep_07c_FusionWait",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.FusionCompletedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 합성 카드 사용 지시
            {
                FileName       = "TutorialStep_07d_UseFusionCard",
                TooltipText    = "합성 카드가 생겼습니다!\n카드를 적에게 드래그해서 사용해보세요.",
                TargetId       = "FusionCard", // FusionCompletedEvent로 받은 CardRect 직접 사용
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 합성 카드 실제 사용 대기
            {
                FileName       = "TutorialStep_07e_UseFusionGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.CardDroppedOnTargetEvent",
                BlockAllInput  = false,
            },
            new StepData
            {
                FileName       = "TutorialStep_08_EndTurn",
                TooltipText    = "턴 종료 버튼을 눌러 적 턴으로 넘어가세요.",
                TargetId       = "EndTurnButton",  // 턴 종료 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.PlayerTurnEndRequestEvent",
                BlockAllInput  = false,
            },

            // ── Phase F: 적 턴 ────────────────────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_09_EnemyTurn",
                TooltipText    = "적이 행동 중입니다...",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.PlayerTurnStartEvent",
                BlockAllInput  = true,
            },

            // ── Phase G: 마지막 전투 — 승리 ──────────────────────────────────────
            new StepData   // 안내: 최후의 일격
            {
                FileName       = "TutorialStep_10_FinalBattle",
                TooltipText    = "카드를 사용해 적을 물리치고 승리하세요!",
                TargetId       = "HandCard",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 전투 승리 대기
            {
                FileName       = "TutorialStep_10b_VictoryGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.BattleVictoryEvent",
                BlockAllInput  = false,
            },

            // ── Phase H: 보상 ─────────────────────────────────────────────────────
            new StepData   // 게이트: 보상 패널 등장 애니메이션 완료 대기
            {
                FileName       = "TutorialStep_11_RewardPanelGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.RewardPanelShownEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 보상 선택
            {
                FileName       = "TutorialStep_11b_Reward",
                TooltipText    = "전투 승리!\n보상을 선택하세요.",
                TargetId       = "RewardPanel",    // 보상 패널 루트에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 보상 패널 닫힘 대기 (플레이어가 모든 보상 수령 후 나가기)
            {
                FileName       = "TutorialStep_11c_RewardExitGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.RewardPanelClosedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 맵 오버레이 등장 애니메이션 완료 대기
            {
                FileName       = "TutorialStep_11d_MapOpenGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.MapOverlayOpenedEvent",
                BlockAllInput  = false,
            },

            // ── Phase I: 상점 맵 노드 선택 ───────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_12_MapToShop",
                TooltipText    = "다음 노드를 선택해 상점에 들러보세요!",
                TargetId       = "Shop",           // MapNodeView Shop 노드에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Map.Events.MapNodeSelectedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 상점 노드 진입 완료 대기
            {
                FileName       = "TutorialStep_12b_ShopNodeGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.NodeContextEnteredEvent",
                BlockAllInput  = false,
            },

            // ── Phase J: 상점 튜토리얼 ───────────────────────────────────────────
            new StepData   // NPC 클릭 유도 → ShopOpenedEvent 대기
            {
                FileName       = "TutorialStep_13_ShopInteract",
                TooltipText    = "상점에 도착했습니다!\nNPC를 클릭해 상점을 열어보세요.",
                TargetId       = "ShopInteractable", // 상점 NPC(InteractableObject)에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.ShopOpenedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 상점 이용 방법 설명
            {
                FileName       = "TutorialStep_13b_ShopPanel",
                TooltipText    = "마음에 드는 카드를 골라보세요.\n리롤로 진열된 카드를 새로 뽑을 수 있습니다.",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 상점 패널 닫기 버튼 클릭 대기
            {
                FileName       = "TutorialStep_13b2_ShopCloseGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.ShopClosedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 나가기 버튼 설명
            {
                FileName       = "TutorialStep_13c_ShopExit",
                TooltipText    = "나가기 버튼을 눌러 다음 여정을 시작하세요.",
                TargetId       = "ShopExitButton", // 나가기 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 나가기 버튼 클릭 대기
            {
                FileName       = "TutorialStep_13c2_ShopExitGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.ShopExitedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 맵 오버레이 등장 완료 대기
            {
                FileName       = "TutorialStep_13d_MapOpenGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.MapOverlayOpenedEvent",
                BlockAllInput  = false,
            },

            // ── Phase K: 휴식 맵 노드 선택 ───────────────────────────────────────
            new StepData
            {
                FileName       = "TutorialStep_14_MapToRest",
                TooltipText    = "휴식 노드를 선택해보세요!",
                TargetId       = "Rest",           // MapNodeView Rest 노드에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Map.Events.MapNodeSelectedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 휴식 노드 진입 완료 대기
            {
                FileName       = "TutorialStep_14b_RestNodeGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.NodeContextEnteredEvent",
                BlockAllInput  = false,
            },

            // ── Phase L: 휴식 튜토리얼 ───────────────────────────────────────────
            new StepData   // NPC 클릭 유도 → RestOpenedEvent 대기
            {
                FileName       = "TutorialStep_15_RestInteract",
                TooltipText    = "휴식 장소에 도착했습니다!\nNPC를 클릭해 쉬어가세요.",
                TargetId       = "RestInteractable", // 휴식 NPC(InteractableObject)에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.RestOpenedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 회복 버튼 설명 (체력 만땅이어도 넘어갈 수 있도록 ClickToContinue)
            {
                FileName       = "TutorialStep_15b_RestHeal",
                TooltipText    = "회복 버튼으로 HP를 회복할 수 있습니다.\nHP가 부족하다면 눌러보세요!",
                TargetId       = "HealButton",     // 회복 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 안내: 닫기 버튼 설명
            {
                FileName       = "TutorialStep_15c_RestClose",
                TooltipText    = "닫기 버튼을 누르면 패널을 닫고\n다시 NPC와 대화할 수 있습니다.",
                TargetId       = "RestCloseButton", // 휴식 패널 닫기 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 실제 닫기 버튼 클릭 대기
            {
                FileName       = "TutorialStep_15c2_RestCloseGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.RestPanelClosedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 안내: 나가기 버튼 설명
            {
                FileName       = "TutorialStep_15d_RestExit",
                TooltipText    = "나가기 버튼을 눌러 다음 여정으로 이동하세요.",
                TargetId       = "RestExitButton", // 휴식 패널 나가기 버튼에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData   // 게이트: 실제 나가기 버튼 클릭 대기
            {
                FileName       = "TutorialStep_15d2_RestExitGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Top,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.RestExitedEvent",
                BlockAllInput  = false,
            },
            new StepData   // 무음 게이트 — 맵 오버레이 등장 완료 대기
            {
                FileName       = "TutorialStep_15e_MapOpenGate",
                TooltipText    = "",
                TargetId       = "",
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.WaitForEvent,
                EventTypeName  = "Battle.Events.MapOverlayOpenedEvent",
                BlockAllInput  = false,
            },

            // ── Phase M: 엘리트 / 보스 노드 설명 (플레이 없음) ───────────────────
            new StepData
            {
                FileName       = "TutorialStep_16_EliteInfo",
                TooltipText    = "엘리트 노드입니다.\n강력한 적이 등장하지만 고급 보상을 얻을 수 있습니다.",
                TargetId       = "Elite",          // MapNodeView Elite 노드에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
            new StepData
            {
                FileName       = "TutorialStep_17_BossInfo",
                TooltipText    = "보스 노드입니다.\n각 구간의 최종 관문으로 가장 강력한 적이 기다립니다.",
                TargetId       = "Boss",           // MapNodeView Boss 노드에 TutorialTarget 부착
                TargetIndex    = 0,
                Anchor         = TooltipAnchor.Bottom,
                CompletionType = TutorialCompletionType.ClickToContinue,
                EventTypeName  = "",
                BlockAllInput  = true,
            },
        };
    }
}
#endif
