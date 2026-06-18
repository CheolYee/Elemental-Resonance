# 보상 시스템 / 골드 시스템

작성일: 2026-06-17
상태: Phase 3 완료

---

## 확정된 설계

### 보상 종류
- **골드** + **카드 3장 중 1장 선택** (스킵 없음)
- 아이템/유물 없음. 상점에서는 카드 구매 + 카드 버리기 + 리롤

### 카드 보상 풀 규칙
- **합성 결과 원소 제외**: `ElementType.Steam / Storm / Twilight / Poison / Holy` → 보상/상점 풀에서 영구 제외
  - 이 카드들은 합성 또는 향후 별도 경로로만 획득 가능
- **등급 가중치**: `MapNodeDefinition.floorIndex` 기반. `RewardConfigSO`에 층수 구간별 가중치 테이블 설정

| floorIndex | Normal | Rare | Epic | Legendary |
|---|---|---|---|---|
| 0~2 | 60% | 30% | 8% | 2% |
| 3~5 | 40% | 40% | 15% | 5% |
| 6~8 | 20% | 35% | 30% | 15% |
| 9+ | 10% | 25% | 35% | 30% |

### 골드 공식
```
gold = baseGold + floorIndex × perFloorBonus + Random(-randomVariance, +randomVariance)
gold = Mathf.Max(0, gold)
```
- 파라미터는 `RewardConfigSO`에서 설정 (baseGold=50, perFloorBonus=10, randomVariance=15 예시)

### 런 상태 저장 — `PlayerRunStateSO`
- `int gold` — 현재 보유 골드
- `List<CardDataSO> currentPile` — 런 중 획득한 카드 목록
- RuntimeSO: `[CreateAssetMenu]`, Inspector로 주입 (DI 범위 외)
- 런 시작 시 `Reset()` 호출로 초기화

### CurrentPile → 덱 연동
- `DeckController.Initialize()` 시 `deckProvider.GetDeck()` + `playerRunState.currentPile` 합산
- 합성 카드(AddFusionCard)는 기존 방식 유지 — currentPile과 무관

### 보상 패널 흐름
```
BattleVictoryEvent
  → BattleAnnouncementController: "STAGE CLEAR" 연출
  → OnComplete: BattleResultShownEvent 발행
  → RewardPanelController: 보상 패널 등장 (골드 행 → 카드 선택 행)
  → 카드 선택 시 FlyingCard 연출 → currentPile 추가
  → 닫기 버튼 → RewardPanelClosedEvent 발행
  → MapFlowController: 맵 오픈
```

- `MapFlowController`는 기존 `BattleResultShownEvent` 대신 `RewardPanelClosedEvent` 구독으로 변경

### 보상 패널 UI 구조
- ScrollRect 기반 패널 — 항목이 위에서 아래로 쌓임
- **골드 행**: 골드 아이콘 + 획득량 텍스트, 클릭 시 즉시 획득 (LMotion 카운팅 연출)
- **카드 선택 행**: 3장의 CardView 나열, 호버 시 4개 모서리 브라켓 UI 등장, 클릭 시 획득
- Z순서: Top바 최상위 → 맵/Pile 패널 → 보상 패널 (Sort Order 기준)
- 닫기: 카드 선택 완료 후 "닫기" 버튼 활성화

### 상점(Shop) 구조 — `ShopConfigSO`
- **카드 진열 수**: `[SerializeField] int cardSlotCount = 4` (나중에 조정 가능)
- **카드 구매**: 등급별 고정 가격 (Normal=50G, Rare=100G, Epic=180G, Legendary=300G)
- **카드 버리기**: 고정 가격 (기본 75G), currentPile에서 카드 1장 영구 제거
- **리롤**: 고정 가격 (기본 50G), 카드 진열 전체 교체
- 상점 카드 풀: 보상 카드 풀과 동일 규칙 (합성 원소 제외, floorIndex 등급 가중치)
- 구매 카드 → `PlayerRunStateSO.currentPile` 추가

### 휴식(Rest) 구조
- HP `restHealPercent`% 회복 (기본 30%, `RewardConfigSO`에서 설정)
- `Player`의 현재 HP 기반으로 계산: `heal = maxHp * restHealPercent`
- 초과 회복 없음 (`Mathf.Min(currentHp + heal, maxHp)`)

### 골드 UI
- Top바에 항상 표시 (기존 UI 존재)
- `GoldDisplayView`: `PlayerRunStateSO`의 gold 변경 이벤트 구독, LMotion으로 숫자 카운팅 애니메이션
- 이벤트: `GoldChangedEvent(int oldAmount, int newAmount)`

---

## 신규 클래스 목록

| 클래스 | 역할 |
|---|---|
| `PlayerRunStateSO` | 런 상태 (골드 + CurrentPile) |
| `RewardConfigSO` | 골드 공식 파라미터, 등급 가중치 테이블, 휴식 회복률 |
| `ShopConfigSO` | 상점 카드 수, 등급별 가격, 리롤/버리기 가격 |
| `RewardCardPoolService` | floorIndex + 합성 카드 필터 적용 후 카드 N장 추출 |
| `RewardPanelController` | 보상 패널 전체 오케스트레이션 |
| `RewardLootRowGold` | 골드 보상 행 UI |
| `RewardLootRowCard` | 카드 선택 행 UI (3장 + 모서리 브라켓) |
| `LootItemView` | 단일 아이템 Rect — 호버 시 브라켓 애니메이션 |
| `GoldDisplayView` | Top바 골드 텍스트 갱신 + LMotion |
| `GoldChangedEvent` | 골드 변경 이벤트 |
| `RewardPanelClosedEvent` | 보상 패널 닫힘 이벤트 (맵 오픈 트리거) |

### 기존 클래스 수정
| 클래스 | 변경 내용 |
|---|---|
| `DeckController` | `Initialize()` 시 `playerRunState.currentPile` 합산 |
| `MapFlowController` | `BattleResultShownEvent` → `RewardPanelClosedEvent` 구독으로 변경 |
| `ShopPanelController` | 껍데기에서 실제 구매/버리기/리롤 기능 구현 |
| `RestPanelController` | 껍데기에서 HP 회복 기능 구현 |
| `PlayerRunStateSO` 관련 | `BattleSceneInstaller`에 SO 등록 불필요 (Inspector 직접 주입) |

---

## 페이즈별 구현 계획

### ✅ Phase 1 — 데이터 모델 (완료)
- `PlayerRunStateSO`: gold + currentPile, Reset/AddGold/SpendGold/CanAfford/AddCard/RemoveCard
- `RewardConfigSO`: 골드 공식(baseGold/perFloorBonus/randomVariance) + FloorGradeWeight 테이블 + restHealPercent
- `ShopConfigSO`: cardSlotCount(직렬화) + 등급별 가격 + rerollCost/removeCardCost + GetCardPrice()
- `GoldChangedEvent`, `RewardPanelClosedEvent` 이벤트 클래스 추가
- `DeckController.Initialize()`: playerRunState.CurrentPile 카드를 DrawPile에 합산
- `MapFlowController`: BattleResultShownEvent → RewardPanelClosedEvent로 맵 오픈 트리거 교체, InitializeRun()에 playerRunState.Reset() 추가
- Inspector 연결 완료: MapFlowController._playerRunState, DeckController.playerRunState → 동일 SO 에셋

---

### ✅ Phase 2 — 골드 지급 + UI 갱신 (완료)
- `PlayerRunStateSO`: `currentFloorIndex` 필드 + `SetFloorIndex()`/`CurrentFloorIndex` 추가
- `MapFlowController`: 배틀/엘리트 노드 전환 시 `_playerRunState?.SetFloorIndex(node.floorIndex)` 호출
- `RewardCardPoolService`: Steam/Storm/Twilight/Poison/Holy 합성 원소 제외 → floorIndex 가중치로 N장 추출 (Phase 3에서 사용)
- `GoldRewardService`: `BattleResultShownEvent` 구독 → `RewardConfigSO.CalculateGold(floorIndex)` → `PlayerRunStateSO.AddGold()` → `GoldChangedEvent` 발행
- `GoldDisplayView`: `GoldChangedEvent` 구독, `Awake`에서 `GetComponentInChildren<TMP_Text>` 자동 탐색, LMotion float 카운팅(0.4초, OutCubic) → `Mathf.RoundToInt` 표시
- 씬 배치: `Battlemanagers`에 `GoldRewardService`/`RewardCardPoolService`, `CoinBar`에 `GoldDisplayView`, Inspector SO 연결 완료

---

### ✅ Phase 3 — 보상 패널 UI (완료)
**목표**: 슬더스 스타일 전리품 창 구현

실제 구현 내용:
- `CardRewardPanel.OnCardSelected`: `Action<CardDataSO>` → `Action<CardDataSO, Vector2>` 로 변경 — 선택 시점 카드 스크린 좌표 전달
- `RewardPanelController`: `_cardFlyAnimator`, `_deckController` 필드 추가 (Inspector 주입). 카드 선택 콜백에서 `PlayerRunStateSO.AddCard()` + `DeckController.AddRewardCard()` + `CardFlyAnimator.FlyToCurrentDeck()` 순서로 처리
- `DeckController.AddRewardCard(CardDataSO)`: DrawPile에 즉시 추가, `_currentDeckCount++`, `NotifyPileChanged()`
- `CardFlyAnimator.FlyToCurrentDeck(Vector2 fromScreen)`: `PileDisplayTarget.CurrentDeck` 대상으로 FlyingCard 3D 연출

버그 수정 (Phase 3 진행 중 발견):
- `DeckController.ConsumeFusionMaterial()`: DiscardPile → GravePile로 수정 (합성 재료는 영구 소멸)
- `HandLayoutController.ForceReturnDraggedCard()`: 턴 종료 버림(DiscardHand) 전 드래그 중 카드 강제 복귀 — 드래그 중 턴 종료 시 카드 소실 버그 수정
- `HandDealController.DiscardHand()`: `handLayoutController.ForceReturnDraggedCard()` 선행 호출 추가
- `HandLayoutController`: `drawPileRect` SerializeField + `GetDealStartPos()` — 딜 애니메이션 시작 위치를 DrawPile 버튼 실좌표로 개선
- `DeckController.Initialize()`: `_currentDeckCount = _drawPile.Count` 위치를 CurrentPile 합산 이후로 이동 (맵 이동 후 덱 카운트 감소 버그 수정)
- `DeckBuilderController.OnStartClicked()`: `_playerRunState?.Reset()` 추가 — 에디터 플레이 세션 간 CurrentPile 누적 버그 수정
- `FusionProbabilityTooltip`: 페이드인/아웃(LitMotion 0.12s), 지연 표시(0.2s), empty pool 폴백(원소 미지정 카드가 없어도 기본 가중치 표시)

Inspector 연결 필요:
- `DeckBuilderController`: `_playerRunState` → PlayerRunState SO
- `RewardPanelController`: `_cardFlyAnimator` → CardFlyAnimator, `_deckController` → DeckController
- `HandLayoutController`: `drawPileRect` → DrawPile 버튼 RectTransform

검증: 전투 승리 → STAGE CLEAR 연출 종료 → 보상 패널 등장 → 카드 선택 → FlyingCard → CurrentPile 반영 → 패널 닫기 → 맵 오픈

---

### Phase 4 — 상점 실제 기능
**목표**: Shop 노드 진입 시 카드 구매/버리기/리롤 가능

구현:
- `ShopPanelController` 실제 기능:
  - 진입 시 `RewardCardPoolService`로 카드 N장 진열
  - 카드 구매: 골드 차감 → `currentPile.AddCard()` → 해당 슬롯 "구매됨" 표시
  - 카드 버리기: 골드 차감 → `currentPile` 목록에서 카드 선택 UI → `RemoveCard()`
  - 리롤: 골드 차감 → 카드 목록 전체 재추출

검증: 상점 진입 → 카드 구매/버리기/리롤 정상 동작 + 골드 반영

---

### Phase 5 — 휴식 노드 HP 회복
**목표**: Rest 노드 진입 시 HP 회복

구현:
- `RestPanelController` 실제 기능:
  - 진입 시 회복 가능 HP 표시 (`maxHp * restHealPercent`)
  - "회복" 버튼 클릭 → `Player` HP 회복 (LMotion HP 바 연출)
  - 회복 완료 후 "나가기" 버튼만 남김

검증: 휴식 노드 진입 → HP 회복 → 나가기 → 맵 오픈

---

## 완료 기준
- 전투 승리 시 골드 + 카드 보상 정상 지급
- CurrentPile 카드가 다음 스테이지 시작 시 덱에 포함
- 상점에서 카드 구매/버리기/리롤 동작
- 휴식에서 HP 회복 동작
- Top바 골드 상시 표시 + 변경 시 LMotion 연출
