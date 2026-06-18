# 상점(Shop) / 휴식(Rest) UI 설계

작성일: 2026-06-17
상태: 설계 확정 — grill-me 완료

---

## 확정된 설계

### 공통 구조

| 항목 | 결정 |
|------|------|
| 진입 방식 | 3D 오브젝트(NPC/비석) 클릭 → Cinemachine Priority 전환 + UI 슬라이드 인 |
| 3D 오브젝트 호버 | 기존 적 아웃라인 시스템과 동일 로직 |
| 나가기 버튼 | UGUI Screen Space 우하단 고정, 항상 표시 |
| 패널 열기 모션 | 우→좌 슬라이드 인 (LitMotion) |
| 패널 닫기 모션 | 좌→우 슬라이드 아웃 (LitMotion) + Cinemachine Priority 복원 |
| RewardCardPoolService | MapScene Installer에 추가 등록 |

---

### HP 지속성 시스템

| 항목 | 결정 |
|------|------|
| HP 저장 위치 | `PlayerRunStateSO`에 `currentHp` / `maxHp` 필드 추가 |
| 배틀 시작 시 | `StageBootstrapper`가 `PlayerRunStateSO` → `HealthModule` 초기화 |
| 배틀 종료 시 | `BattleResultController`가 `HealthModule` → `PlayerRunStateSO` 저장 |

---

### 상점(Shop)

**3D 오브젝트**: NPC (클릭 → 카메라 전환 + 상점 UI 슬라이드 인)

#### 패널 구조
```
[ 상점 패널 — 우→좌 슬라이드 인 ]
  ├─ 상단: 제목 "상점" + 현재 보유 골드
  ├─ 중앙: 카드 슬롯 N장 (ShopConfigSO.cardSlotCount)
  │     ├─ 각 슬롯: RewardCardView + 가격 텍스트 + 구매 버튼
  │     ├─ 호버 시 스케일 업 (RewardCardView 기존 hover 로직 재사용)
  │     └─ 구매 완료: "구매됨" dim 처리 + 버튼 비활성화
  ├─ 하단 좌측: "카드 버리기" 버튼 (removeCardCost 표시)
  ├─ 하단 중앙: "리롤" 버튼 (rerollCost 표시)
  └─ (나가기 버튼은 패널 외부 UGUI 우하단 고정)
```

#### 카드 풀
- `RewardCardPoolService.DrawCards()` — HashSet 기반 중복 제거 추가
- floorIndex: `PlayerRunStateSO.CurrentFloorIndex`

#### 구매 흐름
1. `PlayerRunStateSO.CanAfford(price)` 확인
2. 부족 시: 가격 텍스트 빨간색 + 버튼 비활성화 + **TopBar 골드 빨간 흔들림** (LitMotion)
3. 성공 시: `SpendGold()` → `AddCard()` → 슬롯 "구매됨" 처리

#### 카드 버리기 흐름
1. "카드 버리기" 버튼 클릭 → 가로 스크롤 팝업 표시 (`RewardCardView` 재사용)
2. CurrentPile 카드 목록 표시
3. 카드 선택 → 중앙으로 lerp 이동 연출 → `RemoveCard()` + 골드 차감
4. `CurrentPile.Count == 0` 시 버리기 버튼 비활성화

#### 리롤 흐름
- `ShopConfigSO.rerollCost` 차감 → `DrawCards()` 재호출 → 슬롯 전체 갱신

---

### 휴식(Rest)

**3D 오브젝트**: 비석 (클릭 → 카메라 전환 + 휴식 UI 슬라이드 인)

#### 패널 구조
```
[ 휴식 패널 — 우→좌 슬라이드 인 ]
  ├─ 상단: 제목 "휴식"
  ├─ 중앙: 현재HP / 최대HP 텍스트
  │        +N HP 회복 예정 텍스트 (진입 시 랜덤 계산)
  ├─ 버튼: "회복" 버튼
  │     ├─ 클릭 시: Heal → 버튼 비활성화 + TopBar 체력 텍스트 초록 깜박임 (LitMotion)
  │     └─ HP 최대 시: 비활성화 + 텍스트 "HP 최대"로 변경
  └─ (나가기 버튼은 패널 외부 UGUI 우하단 고정)
```

#### 회복량 계산
- `RestConfigSO.minHealPercent` ~ `maxHealPercent` 사이 `Random.Range`
- 진입 시 1회 계산 후 고정 표시

#### RestConfigSO
```csharp
[CreateAssetMenu(menuName = "Battle/Rest Config")]
public class RestConfigSO : ScriptableObject
{
    public float minHealPercent = 0.2f;  // 최대 HP의 20%
    public float maxHealPercent = 0.4f;  // 최대 HP의 40%
}
```

---

## 페이즈별 구현 계획

### ✅ Phase 1 — HP 지속성 + 배틀 동기화 (완료)
- `HealthModule`: `InitializeHp(currentHp, maxHp)` + `Heal(amount)` 추가
- `PlayerRunStateSO`: `_currentHp`/`_maxHp` 런타임 필드, `SetHp()`, `Heal()`, `IsHpInitialized`, `Reset()` HP 초기화 포함
- `StageBootstrapper`: `playerRunState` SerializeField 추가, `InitializePlayerHp()` — 첫 배틀은 Reinitialize→SO 저장, 이후는 SO→InitializeHp
- `BattleResultController`: `playerRunState` SerializeField + `[Inject] Player` 추가, 승리 시 HealthModule→SO 저장

---

### ✅ Phase 2 — 3D 인터랙터블 오브젝트 + 패널 슬라이드 애니메이션 (완료)
- `InteractableObject`: Physics.Raycast + Mouse.current 방식(EventSystem 아님). 기본 아웃라인 width=4, 호버 width=8, 클릭 시 자체 비활성화 후 `OnClicked` 발행
- `ShopPanelController` / `RestPanelController`: `_areaCamera`(Priority 20) + `_interactCamera`(Priority 25) 분리. 우→좌 슬라이드 인 / 좌→우 슬라이드 아웃 (LitMotion). `CloseInteractAsync` 완료 시 `SetInteractable(true)` 재활성화
- `HP 지속성`: `PlayerRunStateSO.SetHp/Heal`, `StageBootstrapper.InitializePlayerHp`, `BattleResultController` 승리 시 저장 완료

---

### ✅ Phase 3 — Shop 카드 진열 + 구매 (완료)
- `RewardCardPoolService.DrawCards()`: HashSet 인스턴스 중복 제거
- `BattleSceneInstaller`에 `RewardCardPoolService` RegisterValue 등록, `ShopPanelController`에서 `[Inject]`
- `GoldInsufficientEvent` 신규, `GoldDisplayView` 빨간 X축 흔들림 처리
- `ShopCardSlotView`: `RewardCardView` + 가격 TMP + 구매 버튼. Inspector 미리 배치(GridLayoutGroup)
- `ShopPanelController`: 진입 시 슬롯 Setup + 구매 흐름(SpendGold → AddCard → RefreshCurrentDeckCount → GoldChangedEvent → FlyToCurrentDeck)
- `PlayerRunStateSO.SpendGold`: old 반환값 방식으로 변경
- `RewardCardView._baseScale`: Awake에서 초기 스케일 저장, Setup/Hover 시 기준으로 사용
- `DeckController.CurrentDeckCards`: deckProvider + playerRunState.CurrentPile 합산 반환
- `DeckController.RefreshCurrentDeckCount()`: SO 기반 카운트 재계산 메서드 추가
- `BattleDockSlide` / `TurnEndButton` / `CostDisplayPanel`: `_inNodeContext` 플래그 추가, Shop/Rest 중 SlideIn 차단

---

### ✅ Phase 4 — 카드 버리기 + 리롤 (완료)
- `ShopConfigSO`: `rerollCostMultiplier`, `removeCardCostMultiplier` 추가 (기본 1.5배, 누적 배율)
- `ShopCardSlotView`: `IsPurchased` 프로퍼티 추가
- `PlayerDeckProviderSO`: `RemoveCard()` abstract 추가 / `TempStartCardSO` 구현
- `DeckController.RemoveCardFromDeck()`: CurrentPile → deckProvider 순 탐색 후 제거 + UI 갱신
- `CardRemovePopupController` 신규: `SetActive(true)` 이후 `BuildCardList()` 호출(레이아웃 보장), `DeckController.CurrentDeckCards`(시작덱+CurrentPile) 표시, 선택 시 `PlaySelectAsync()` → `RemoveCardFromDeck()` + `SpendGold()` + `GoldChangedEvent` → 페이드 아웃
- `ShopPanelController`: 리롤/버리기 SerializeField 추가, `_rerollCount`/`_removeCount` 세션 관리, 가격 배율 증가, 미구매 슬롯만 교체, 버리기 버튼 카드 유무 체크를 `DeckController.CurrentDeckCards.Count`로 수정

---

### ✅ Phase 5 — Rest HP 회복 (완료)
- `RestConfigSO` 신규 (`minHealPercent=0.2`, `maxHealPercent=0.4`)
- `PlayerHpChangedEvent` 신규 (`OldHp`, `NewHp`, `MaxHp`, `IsHeal`)
- `PlayerHealthBarView`: `EventChannelSO` 구독 추가, `PlayerHpChangedEvent` 수신 시 텍스트 갱신 + 초록 깜박임 + Scale Pop
- `RestPanelController`: `OpenInteractAsync()` 시 회복량 계산, 패널 내 HP 텍스트 + 회복 예정 텍스트 표시, 버튼 1회 사용 후 비활성화, HP 최대 시 "HP 최대" 텍스트, 회복 시 `PlayerHpChangedEvent` 발행 + 패널 내 HP 텍스트 초록 깜박임 + Scale Pop
- Inspector 연결: `PlayerHealthBarView`에 EventChannelSO, `RestPanelController`에 RestConfigSO/PlayerRunStateSO/EventChannelSO/버튼/텍스트 연결

---

## ✅ 전체 완료 (Phase 1~5 모두 구현 및 테스트 완료)

## 완료 기준
- 상점 진입 → 카드 4장 진열 → 구매(골드 차감 + CurrentPile 추가) → 리롤 → 카드 버리기
- 휴식 진입 → HP 회복 연출 → TopBar 갱신 → 나가기
- 골드 부족 시 가격 빨간색 + 버튼 비활성화 + TopBar 흔들림
- 다음 배틀에서 구매 카드가 덱에 포함
- 배틀 후 HP가 런 전반에 유지됨
