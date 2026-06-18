# 카드 합성 / 등급 / 원소 속성 시스템

작성일: 2026-06-16
상태: Phase 5 완료 — Phase 6 대기 중
제약: 4일 내 제출 (합성 + 상점 + 보상 + 골드 + 휴식 전부 포함)

---

## 확정된 설계

### 원소 (ElementType) — 7종, 전부 동등(재료+결과 겸용)

기존 스킬 연출 에셋이 이미 이 7개 테마로 존재(화염/번개는 실제 카드까지 존재, 나머지는 추후 제작):

- 화염 (Fire)
- 번개 (Lightning)
- 자연 (Nature)
- 빛 (Light)
- 얼음 (Ice)
- 어둠 (Dark)
- 아케인 (Arcane)

7개 모두 기본 카드 원소이면서 동시에 합성 재료/결과로 쓰일 수 있다. (4원소설 폐기)

### 합성 레시피 — 데이터 테이블 방식, 5개만 확정 (21개 전체 조합 중 일부만 사용)

| 재료1 | 재료2 | 결과 원소 |
|---|---|---|
| 화염 | 얼음 | 증기 |
| 화염 | 번개 | 폭풍 |
| 빛 | 어둠 | 황혼 |
| 자연 | 어둠 | 맹독 |
| 빛 | 아케인 | 신성 |

- 레시피에 없는 조합은 합성 불가 (테이블 미정의 = 비활성).
- 동일 원소 2장(예: 화염+화염) 합성 불가.
- 결과 원소(증기/폭풍/황혼/맹독/신성)끼리 추가로 합성하는 2차 합성은 불가 — 결과물은 최종 산출물로만 취급.
- 추후 콘텐츠 추가 시 테이블에 항목만 추가하면 확장 가능 (코드 변경 없음 목표).

### 등급 — 기존 4단계(CardGrade) 그대로 사용, enum 변경 없음

- **카드가 등급을 진화하지 않는다.** 대신 원소마다 Normal/Rare/Epic/Legendary 등급의 카드가 각각 별도의 `CardDataSO` 에셋으로 존재.
- 연속 강화(5단계 체인) 설계는 폐기. 합성은 **1회성 가챠 추첨**으로 끝난다 (실패 개념 없음 — 항상 결과 카드 1장 획득).

### 결과 카드 결정 — 가중치 가챠 + 재료 등급 반영

- 결과 원소가 정해지면, 그 원소의 등급별 카드 풀(Normal~Legendary, 존재하는 것만) 중 가중치 추첨으로 1장 선택.
- 가중치는 **재료 2장 중 더 높은 등급(MAX)** 을 기준으로 아래 표를 사용:

| 재료 기준 등급 | Normal | Rare | Epic | Legendary |
|---|---|---|---|---|
| Normal | 50% | 30% | 15% | 5% |
| Rare | 30% | 40% | 20% | 10% |
| Epic | 15% | 25% | 40% | 20% |
| Legendary | 5% | 15% | 30% | 50% |

- 재료 카드 자체의 등급은 합성 가능 여부에 영향 없음 (어떤 등급이든 재료로 사용 가능, 결과 확률에만 영향).

### 합성 재료 소모

- 정확히 2장 소모 (레시피의 두 원소 각 1장씩).
- 재료로 소모된 카드는 Discard/Grave로 가지 않고 그냥 소멸 (일반적인 "카드 사용"이 아니므로 disposePolicy 미적용).

### 합성 장소/타이밍 — 스테이지 내, 휴식 노드 아님

- 합성은 **스테이지(전투) 진행 중**에만 가능, 플레이어 턴 언제든 (횟수 제한 없음).
- 휴식(Rest) 노드에서는 처리하지 않음.
- 합성으로 얻은 카드는 **해당 스테이지 한정**으로 존재 — 영구 덱에 편입되지 않고, 스테이지 클리어 시 소멸.

### 합성 UI — 드래그 드롭 (기존 카드 플레이 방식 재사용)

1. 손패의 카드 A를 드래그하면, 손패 내 합성 가능한(레시피가 성립하는) 다른 카드들이 하이라이트.
2. 하이라이트된 카드 B 위에 드롭 → A, B 소모 → 가챠 추첨 → 결과 카드 1장이 즉시 손패에 추가.
3. 합성은 코스트를 소비하지 않음.
4. 합성 연출이 끝나야 손패 입력 재활성화 (기존 스킬 실행 패턴과 동일).

### 합성 연출 — 합성 패널 + 등급 공개 애니메이션

**트리거 ~ 패널 진입**:
1. 손패에서 카드 A를 카드 B 위에 드롭 → 두 카드가 `CardFlyAnimator`로 DiscardPile 방향으로 날아가며 사라짐.
2. `DeckController`에서 두 재료 카드를 DiscardPile로 이동.
3. **합성 패널** (OverLayCanvas 내 전용 Panel GameObject)이 등장.

**합성 패널 내부 연출**:
- 패널 중앙에 카드 실루엣/베이스 UI가 있고, 뒤쪽에 빛(Glow Image) 존재.
- 빛이 **Normal(회색)부터 시작**, 결과 등급에 도달할 때까지 단계적으로 색 전환:
  - 각 단계: 빛 색 전환 → 카드 바운스 팝 (LitMotion 스케일)
  - 예) Epic 결과: 회색→팝, 파랑→팝, **보라→팝(최종 공개)**
- 마지막 등급에서 번쩍임(알파 플래시) 후 카드 전면 공개.

**결과 카드 추가**:
- `HandLayoutController.AddCard()` + `DeckController.AddFusionCard()`로 손패에 추가.
- 합성 카드는 `deckProvider.GetDeck()`에 없으므로 다음 스테이지 시작 시 자연 소멸.

**스킬 사용 시 연출**:
- 합성 결과 카드를 실제 사용할 때, 해당 카드의 SkillPresentationData timeline에 두 원소 파티클을 동시 재생하는 키프레임을 제작 (기존 Skill Presentation Editor 사용).
- 결과 원소 전용 신규 VFX는 만들지 않음 — 재료 원소 파티클 재사용.
- Skill Presentation Editor로 스킬(연출) 1개당 10~30분 소요 — 시간 내 제작 가능.

### SkillPresentationDataSO 단순화 (이 변경에 종속)

- 카드 하나당 등급별로 다른 연출이 필요 없어짐 (등급마다 별도 CardDataSO이므로 카드 자체가 고정 등급).
- `normalTimeline/rareTimeline/epicTimeline/legendaryTimeline` 4필드 → `timeline` 단일 필드로 통합.
- 에디터의 등급 탭 버튼(Normal/Rare/Epic/Legendary) 제거.
- 영향 받는 호출부: `SkillModule.cs`, `SkillPresentationPlayer.cs`, `BattleActionExecutor.cs`.

### 카드 색상 분기

- **카드 프레임(`CardViewImage`)** = 원소 색상 (7색):

| 원소 | 색상 |
|---|---|
| 화염 | #E74C3C |
| 번개 | #F1C40F |
| 자연 | #2ECC71 |
| 빛 | #E8C547 |
| 얼음 | #5DD9F2 |
| 어둠 | #4A235A |
| 아케인 | #8E44AD |

- **라벨(`Label` 오브젝트, CardViewImage와 별개)** = 등급 색상 (4색, WoW류 컨벤션):

| 등급 | 색상 |
|---|---|
| Normal | #D5D8DC |
| Rare | #3498DB |
| Epic | #9B59B6 |
| Legendary | #F39C12 |

### CardDataSO 확장

- `element: ElementType` 필드 추가.
- `grade: CardGrade` 필드 추가 — 카드 에셋 자체가 고정된 (원소, 등급) 조합을 나타내므로 SO 단계에서 필요 (가챠 풀 조회 시 `element`+`grade`로 카드 검색).

---

## 페이즈별 구현 계획

1. **✅ Phase 1 — 데이터 모델 (완료)**: `ElementType` enum 추가, `CardDataSO`에 `elementType`/`grade` 필드 추가, `SkillPresentationDataSO`를 `normalTimeline/rareTimeline/epicTimeline/legendaryTimeline` 4필드에서 `timeline` 단일 필드로 단순화 (`[FormerlySerializedAs("normalTimeline")]`로 기존 연출 데이터 보존). 호출부 3곳(`SkillModule.cs`, `SkillPresentationPlayer.cs`, `BattleActionExecutor.cs`) 수정, 에디터 등급 탭 버튼(`_selectedGrade`/`_gradeButtons`/`SelectGrade`/`RefreshGradeButtons`) 제거. Unity 재컴파일 확인 — 에러/경고 0건.
2. **✅ Phase 2 — 합성 로직 (완료)**: `FusionRecipeTableSO`(레시피 리스트), `GradeWeightTableSO`(4×4 가중치), `FusionRecipeService`(조합 조회), `FusionResultPicker`(가챠 추첨). `DeckController.AddFusionCard/ConsumeFusionMaterial` 추가. `BattleSceneInstaller`에 DI 등록 + SerializeField 3개 추가.
3. **✅ Phase 3 — 카드 색상 분기 (완료)**: `CardType` enum(Attack/Support) + `CardDataSO.cardType` 추가. `CardColorUtility` 공유 유틸리티 생성 — ElementColors(8), GradeColors(4), GetTypeName, Apply, ApplyTextEffects, Reset, ResetTextEffects. 적용 대상 5개: `CardView`, `CardDescriptionPanel`, `PileCardItem`, `EnemyIntentPanel`, `DeckBuilderCardItemView`. 각 뷰에 frameImage/labelImage/typeText/nameTextEffect/typeTextEffect SerializeField 추가. TMPEffect로 nameText·typeText의 아웃라인+쉐도우 색상을 등급 색상으로 동적 변경.
4. **✅ Phase 4 — 드래그 드롭 합성 UI (완료)**: `CardFusionRequestedEvent(draggedCard, targetCard)` 신규. `CardColorUtility.GetElementColor()` 추가. `CardView`에 `fusionGlowImage`/`fusionDimOverlay` SerializeField + `SetFusionState(eligible, glowColor)`/`ClearFusionState()` 추가. `HandLayoutController`에 `[Inject] FusionRecipeService` + `UpdateFusionHighlights`/`ClearFusionHighlights` 추가 — 드래그 시작 시 합성 가능 카드 Glow, 불가 카드 딤 오버레이. `CardDragHandler.HandleEndDrag` — 손패 안 드롭 시 `RaycastAll`로 대상 CardView 감지 → `CardFusionRequestedEvent` 발행. Inspector: CardView 프리팹에 FusionGlow/FusionDimOverlay Image 추가 필요.
5. **✅ Phase 5 — 합성 실행 + 연출 (완료)**:
   - `FusionExecutionController`: `CardFusionRequestedEvent` 구독 → 레시피 검증 → `TryDetachFusionMaterials`(뷰 분리) → `PlayFusionDepartureAsync`(팝→수축, 병렬) → `ReturnViewToPool` → `FlyToGraveAsync`(병렬) → `FusionPanel.ShowAsync` → `AddCard`
   - `FusionPanel`: CanvasGroup 기반 show/hide(SetActive 없음), 등급 공개 루프(`CancellationTokenSource` 스킵), 카드 공개 시 흰색 플래시 오버레이(in은 await, out은 `.Forget()`), `_resultCardOriginalScale` 기준 OutBack 스케일 reveal, UniTaskCompletionSource 클릭 닫기, GlowImage 스케일 동기화
   - `CardView`: `SetFusionHoverState`(CalculateSnapToBottomY + 회전 리셋 + sortingOrder=3), `PlayFusionDepartureAsync`(팝→수축), `RefreshUsability` null 가드 추가
   - `HandLayoutController`: `TryDetachFusionMaterials`(worldPositionStays 분리), `ReturnViewToPool`, `SetFusionHoverTarget`(합성 가능 카드만 호버), `AddCard`에 `SetHandLayoutController` 주입 추가
   - `CardDragHandler`: OnDrag 중 `SetFusionHoverTarget` 감지, `SetHandLayoutController` 세터 추가
   - `CardFlyAnimator`: `FlyToGraveAsync(Vector2)` 추가
6. **✅ Phase 6 — 스테이지 한정 카드 라이프사이클 (구현 불필요)**:
   - 합성 카드는 `AddFusionCard()`로 런타임 더미에만 추가됨 (`deckProvider.GetDeck()` 미포함).
   - `BattleVictoryEvent` → `Cleanup()` → 모든 런타임 더미 초기화. 다음 스테이지 `Initialize()`는 영구 덱만 참조.
   - 기존 아키텍처가 이미 완전히 격리를 보장하므로 추가 코드 없음.
7. **✅ Phase 7 — 콘텐츠 제작 (완료)**:
   - `ElementType` enum에 합성 결과 원소 5개 추가: Steam=8, Storm=9, Twilight=10, Poison=11, Holy=12
   - `CardColorUtility.ElementColors`에 5개 색상 추가 (원소 테마별 색조)
   - `FusionResultPicker.GetEffectiveWeights(maxGrade, resultElement)` 추가 — 등급별 유효 가중치 배열 반환
   - `FusionProbabilityTooltip` 신규 컴포넌트 — 드래그 호버 시 카드 B 상단에 Normal/Rare/Epic/Legendary % 표시, 등급 색상 적용
   - `HandLayoutController.SetFusionHoverTarget()` 연동 — TryGetResult로 resultElement 추출, maxGrade 계산 후 Show/Hide
   - `CardDataSO` 에셋 20개 생성: 5원소 × 4등급, 속성별 폴더 구조(`PlayerCards/{Steam,Storm,Twilight,Poison,Holy}/`)
   - `FusionCardCreator` Editor 전용 툴 스크립트로 에셋 일괄 생성
   - 연출 에셋은 유저가 Skill Presentation Editor에서 직접 제작

한 페이즈 구현 + 검증 완료 후 다음 페이즈로 진행. **다음 시작: Phase 7.**
