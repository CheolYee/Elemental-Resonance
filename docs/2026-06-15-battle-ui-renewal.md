# 배틀 UI 리뉴얼 — HP바·방어도·데미지 텍스트·적 인텐트

작성일: 2026-06-15
상태: Phase 1~4 완료, Phase 5(적 인텐트 UI) 진행 중

---

## 목표

1. **HP바 리뉴얼** — 플레이어·적 모두 Slider 기반으로 통일, 방어도(Block) 수치 표시 추가 ✅
2. **데미지 텍스트** — 피격 시 실제 데미지 수치가 화면에 팝업 ✅
3. **적 인텐트 UI** — 적에 호버 시 다음 턴 행동을 카드 형태 패널로 표시

---

## 확정된 아키텍처

### 설계 결정 요약

| 항목 | 결정 |
|---|---|
| HealthModule 이벤트 | `OnDamageTaken(damageToHp, damageToBlock)` + `OnBlockChanged(currentBlock)` 별도 추가 |
| Block 시각화 | BlockSlider(HP와 동일값) + ShieldGroup(아이콘+수치) + HPText TMPEffect 언더레이 색 전환 |
| 데미지 텍스트 | Screen Space Overlay Canvas, WorldToScreenPoint 변환 |
| 데미지 텍스트 풀링 | PoolManagerSO + DI 주입(DamageTextSpawner) |
| 인텐트 데이터 | EnemyDataSO.attackCard 1개 유지 |
| 인텐트 트리거 | Physics.Raycast (EnemyHoverController 별도 클래스) |
| 인텐트 패널 위치 | 우측 상단 스크린 스페이스 고정 (CardDescriptionPanel 패턴) |
| 플레이어 HP | TopBar 텍스트 유지 + AgentHealthBarView 인게임 슬라이더 신규 |
| 적 Block | 지원 (EnemyHealthBarView도 동일 구조) |
| 데미지 텍스트 색 | HP 피해 → 빨간 숫자, 완전 방어 → 파란 "방어됨" |
| Block 초기화 | 플레이어 턴 시작 시 플레이어 Block, 적 턴 시작 시 적 Block |
| 코스트 초기화 | 플레이어 턴 시작 시 baseCost로 완전 복원 |

### 구현된 클래스

```
HealthModule
├── OnHpChanged(currentHp, maxHp)            기존 유지
├── OnDamageTaken(damageToHp, damageToBlock) 신규
└── OnBlockChanged(currentBlock)             신규

AgentHealthBarView (베이스)
├── red/gray Slider, BlockSlider(HP 미러), HPText
├── ShieldGroup (아이콘 + BlockText)
└── TMPEffect hpTextEffect: Block 유무에 따라 underlayColor 전환

EnemyHealthBarView : AgentHealthBarView
└── nameText + CanvasGroup 페이드인 (AgentTrigger)

DamageTextView : MonoBehaviour, IPoolable
├── Screen Space RectTransform UI 프리팹
└── risePixels 위로 이동 + LitMotion 페이드아웃 → Push 반환

DamageTextSpawner : MonoBehaviour (DI 등록)
├── WorldToScreenPoint 변환
├── Pop 후 uiParent(Overlay Canvas)로 즉시 reparent
└── Spawn(worldPos, damageToHp, damageToBlock)

DamageTextTrigger : MonoBehaviour, IModule
├── [Inject] DamageTextSpawner
└── HealthModule.OnDamageTaken → Spawn 호출

BattleTurnController
├── 플레이어 턴 시작: _player.ResetBlock(), costModel.currentCost = baseCost
└── 적 턴 시작: 모든 적 ResetBlock()
```

---

## ✅ Phase 1 — HealthModule 이벤트 확장 (완료)

- `OnDamageTaken`, `OnBlockChanged` 이벤트 추가
- `TakeDamage`: damageToBlock/damageToHp 분리 계산 후 이벤트 발행
- `AddBlock`, `ResetBlock`: `OnBlockChanged` 발행
- `BattleTurnController`: Block + Cost 턴 시작 시 초기화

## ✅ Phase 2+3 — HP바 리뉴얼 (완료)

- `AgentHealthBarView` 베이스 클래스 신규 작성
- `EnemyHealthBarView` : AgentHealthBarView 상속으로 교체
- 플레이어 인게임 HP바: AgentHealthBarView 직접 사용
- BlockSlider, ShieldGroup, TMPEffect 아웃라인 전환 구현

## ✅ Phase 4 — 데미지 텍스트 (완료)

- `DamageTextView`: IPoolable, Screen Space UI, LitMotion 애니메이션
- `DamageTextSpawner`: DI 등록, PoolManagerSO 풀링, uiParent reparent
- `DamageTextTrigger`: IModule, [Inject], OnDamageTaken 구독

---

## Phase 5 — 적 인텐트 UI

**목표**: 적에 마우스를 올리면 우측 상단에 attackCard를 카드 형태로 표시.

### 구현 항목

- `EnemyHoverController`: Physics.Raycast로 적 감지, EnemyHoverEvent/EnemyHoverExitEvent 발행
  - BattleTargetingController와 동일한 layerMask + camera 사용
  - 상시 작동 (타겟팅 중에도 동작)
- `EnemyIntentPanel`: CardDescriptionPanel 패턴 재사용
  - EnemyHoverEvent 구독
  - attackCard에서 artwork, cardName, description 표시 (코스트 숨김)
  - 우측 상단 고정 위치
  - hoverDelay + LitMotion 페이드인/아웃
- `EnemyHoverEvent(AbstractEnemy enemy)`: 신규 GameEvent
- `EnemyHoverExitEvent`: 신규 GameEvent
