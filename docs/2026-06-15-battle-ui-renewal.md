# 배틀 UI 리뉴얼 — HP바·방어도·데미지 텍스트·적 인텐트

작성일: 2026-06-15
상태: ✅ 전체 완료 (2026-06-16)

---

## 목표

1. **HP바 리뉴얼** — 플레이어·적 모두 Slider 기반으로 통일, 방어도(Block) 수치 표시 추가 ✅
2. **데미지 텍스트** — 피격 시 실제 데미지 수치가 화면에 팝업 ✅
3. **적 인텐트 UI** — 적에 호버 시 다음 턴 행동을 카드 형태 패널로 표시 ✅
4. **적 락온 UI** — 적 호버 시 4-코너 브라켓이 모이는 락온 연출 ✅ (범위공격 대비 다중 타겟 지원 구조)

---

## 완료 보고서

### Phase 1 — HealthModule 이벤트 확장
- `OnDamageTaken(damageToHp, damageToBlock)`, `OnBlockChanged(currentBlock)` 이벤트 추가
- `TakeDamage`에서 Block 흡수량/HP 피해량 분리 계산 후 이벤트 발행
- `BattleTurnController`: 플레이어 턴 시작 시 플레이어 Block + Cost(baseCost) 초기화, 적 턴 시작 시 모든 적 Block 초기화

### Phase 2+3 — HP바 리뉴얼
- `AgentHealthBarView` 베이스 클래스 신규 (red/gray Slider + BlockSlider(HP 미러) + ShieldGroup + TMPEffect 언더레이 색 전환)
- `EnemyHealthBarView`가 `AgentHealthBarView` 상속으로 교체, nameText/CanvasGroup 페이드인은 자식 클래스에서 유지
- 플레이어 인게임 HP바는 `AgentHealthBarView`를 그대로 사용

### Phase 4 — 데미지 텍스트
- `DamageTextView`(IPoolable, Screen Space UI) + `DamageTextSpawner`(DI 등록, PoolManagerSO 풀링) + `DamageTextTrigger`(IModule, OnDamageTaken 구독)
- 3D 월드 스페이스 대신 Screen Space Overlay로 전환 — 카메라 거리에 따른 크기 왜곡 방지

### Phase 5 — 적 인텐트 UI
- `EnemyHoverController`: `BattleTargetingController`와 동일한 Physics.Raycast 패턴으로 적 호버 감지, `TargetableRegisteredEvent` 목록 기반 collider 매칭
- `EnemyIntentPanel`: `CardDescriptionPanel` 패턴 재사용, attackCard의 artwork/이름/설명 + 사용 적 이름 표시, 우측 상단 고정

### 추가 — 적 락온 UI (Phase 5 확장)
- `EnemyLockOnView`: 4-코너 ㄱ자 브라켓, Renderer Bounds → 스크린 좌표 투영, spread-in 컨버지 애니메이션 + 상시 추적
- `EnemyLockOnController`: 고정 풀(3개), `EnemyHoverEvent`/`EnemyHoverExitEvent` 구독 — 적 리스트를 받는 범용 구조로 설계해 향후 범위공격 다중 타겟에 그대로 재사용 가능

### 버그 수정 (구현 중 발견)
- `EnemyIntentPanel`: 페이드 아웃 중 재호버 시 `_isOpen` 갱신 누락으로 패널이 영구히 사라지지 않던 버그 수정
- `BattleActionExecutor`/`SkillEffectExecutionService`: 스킬 큐 도입 후 `CostGainEffect`가 연출 키프레임 시점에만 적용되어 조건부 카드가 큐 대기 중 중복 사용 가능했던 버그 — 카드 적재 시점에 즉시 해석하도록 수정
- `CostDisplayPanel`: 위 수정으로 생긴 "코스트 숫자가 연출보다 먼저 바뀌는" 어색함을 `_displayCost`(화면 표시용 그림자 값) 분리로 해결. `CostGainRevealedEvent`를 연출 키프레임 시점에 발행해 화면 숫자만 그 때 따라가도록 함
- `DeckController.DiscardAllHand()`: 미사용 무덤(Grave) 카드가 턴 종료 시 영구히 GravePile로 가버려 순환 불가능했던 버그 — disposePolicy는 `UseCard()`(실제 사용 시)에만 적용, 미사용 카드는 항상 DiscardPile로 이동하도록 수정

---

## 확정된 아키텍처 (참고용 요약)

```
HealthModule
├── OnHpChanged(currentHp, maxHp)
├── OnDamageTaken(damageToHp, damageToBlock)
└── OnBlockChanged(currentBlock)

AgentHealthBarView (베이스) → EnemyHealthBarView(상속)
DamageTextView/Spawner/Trigger (Screen Space, PoolManagerSO)
EnemyHoverController → EnemyIntentPanel + EnemyLockOnController(EnemyLockOnView 풀)

BattleActionExecutor (스킬 큐)
├── 카드 적재 시 cost 지불 + CostGainEffect 즉시 해석(ResolveCostGain)
└── SkillEffectExecutionService가 키프레임 시점에 CostGainRevealedEvent만 발행(표시용)

DeckController
├── UseCard(): disposePolicy 기준 Discard/Grave 분기
└── DiscardAllHand(): 미사용 카드는 항상 DiscardPile
```
