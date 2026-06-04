# CLAUDE.local.md

## Project Context

Unity 6 기반 3D 턴제 카드 전투 게임을 개발한다.

현재 전투의 큰 흐름은 다음과 같다.

```text
플레이어 턴 시작
→ 카드 드로우
→ 코스트 지급
→ 카드를 유효 대상에게 드래그 & 드롭
→ 즉시 코스트 차감 + 스킬 발동
→ 스킬 애니메이션 완료 후 손패 입력 재활성화
→ 코스트 허용 시 추가 카드 사용 가능
→ "턴 종료" 버튼 클릭
→ 적 턴 실행 (배열 순서대로 순차 공격)
→ 다시 플레이어 턴 (코스트 최대값으로 충전)
```

**행동 큐는 존재하지 않는다.** 카드는 드롭 즉시 발동된다.

현재 작업 목표는 **Stage Framework**이다. 현재 진행 단계: **Phase S-5. Player Deck Provider**

---

## Completed Foundation

### Phase 2. Battle Actor Foundation — 완료

구현 완료:

* `IBattleActor`
* `HealthModule`
* `Agent`
* `AbstractEnemy`
* `BaseEnemy`

확정 원칙:

* `StatModule`은 기초 수치 저장을 담당한다.
* `HealthModule`은 HP, Block, IsDead 등 전투 상태를 담당한다.
* `Agent`가 `IBattleActor`를 구현하고 `HealthModule`에 위임한다.
* 외부에서는 `agent.TakeDamage()` 같은 단일 진입점으로 접근한다.

---

### Phase 3. AgentRenderer & Skill System — 완료

구현 완료:

* `AgentRenderer` — `[RequireComponent(typeof(Animator))]`, `CrossFadeInFixedTime` 기반
* `AgentTrigger` — `AnimationEndTrigger()` / `DamageCastTrigger()` Animation Event 수신
* `AgentState` (추상) — `OnStateCompleted` 이벤트, `CompleteState()` 보호 메서드
* `IdleState`, `HitState`, `DeathState`, `SkillState`, `TargetingIdleState`
* `SkillModule` — `UseSkillAsync(SkillUsageData, GameObject, CancellationToken)` UniTask 반복 발동
* `SkillDataSO`, `SkillUsageData`
* `StateListSO`, `StateSO`, `StateListSOEditor`

확정 원칙:

* Player와 Enemy 모두 동일한 `SkillModule + SkillState` 구조를 사용한다.
* 애니메이터 전이는 화살표 기반이 아니라 코드 기반 `CrossFadeInFixedTime`으로 처리한다.
* 애니메이션 완료는 `AnimationEndTrigger → AgentTrigger.OnAnimationEnd → FSM State.CompleteState()` 순서로 전달된다.
* 각 애니메이션 클립 마지막 프레임에 Animation Event `AnimationEndTrigger` 설정 필수.
* `SkillModule.UseSkillAsync`는 반복 발동 루프를 처리한다 — `for (int i = 0; i < repeatCount; i++)` → 효과 적용 → 애니메이션 대기.
* 피격 시 `Agent.HandleHitEvent()` override → HIT 상태 전환 → `OnStateCompleted` → IDLE 복귀.

---

### Phase 4. Card Data & Skill Bridge — 완료

구현 완료 / 확정 구조:

| 파일 | 역할 |
|---|---|
| `Battle/Data/CardDataSO.cs` | 카드 데이터 SO — `skillData: SkillDataSO` 포함 |
| `Battle/Effects/CardEffectSO.cs` | 추상 효과 SO — `isRepeat: bool`, `Apply(source, target)` 추상 메서드 |
| `Battle/Effects/DamageEffectSO.cs` | `Apply` → `GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>()` |
| `Battle/Effects/BlockEffectSO.cs` | `Apply` → `GetComponent<Agent>() ?? GetComponentInParent<Agent>()` |
| `Battle/Enums/CardGrade.cs` | `Normal=0, Rare=1, Epic=2, Legendary=3` |
| `Battle/Instances/CardInstance.cs` | `CardDataSO data` + `CardGrade grade` (기본값 Normal) |
| `Battle/Data/EnemyDataSO.cs` | `attackSkill: SkillDataSO` + `attackEffects: List<CardEffectSO>` |
| `CombatSystem/Skills/SkillUsageData.cs` | Player·Enemy 공통 실행 데이터 — `FromCard()` / `FromEnemyData()` 팩토리 |

확정 원칙:

* **등급 = 발동 횟수**: Normal 1회, Rare 2회, Epic 3회, Legendary 4회.
* `CardEffectSO.isRepeat = true` → 발동 횟수만큼 반복 / `false` → 1회만 실행.
* `CardDataSO.skillData`는 `SkillDataSO` 직접 참조 — 인덱스 간접 참조 없음.
* `TargetingModule`은 Agent **자식 오브젝트**에 부착 — 효과 적용 시 반드시 `GetComponentInParent`로 루트 Agent를 탐색해야 함.
* 효과 적용 타이밍: 현재는 애니메이션 시작 직전에 즉시 적용 — 나중에 스킬 연출 에디터에서 `DamageCastTrigger` 기반으로 교체 예정.

---

### Phase 5. Immediate Execution & Turn Cycle — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Battle/UI/BattleActionExecutor.cs` | `CardDroppedOnTargetEvent` 구독 → 코스트 차감 → `SkillExecutionStartEvent` → `SkillModule.UseSkillAsync` → `SkillExecutionEndEvent` |
| `Battle/UI/BattleTurnController.cs` | `PlayerTurnEndRequestEvent` 구독 → Enemy 순차 공격 → 코스트 충전 → `PlayerTurnStartEvent` |
| `Battle/UI/TurnEndButton.cs` | OnClick → `PlayerTurnEndRequestEvent` 발행 |
| `Battle/Events/SkillExecutionStartEvent.cs` | 스킬 실행 시작 — HandLayoutController 잠금 트리거 |
| `Battle/Events/SkillExecutionEndEvent.cs` | 스킬 실행 완료 — HandLayoutController 잠금 해제 트리거 |
| `Battle/Events/PlayerTurnEndRequestEvent.cs` | "턴 종료" 버튼 이벤트 |
| `Battle/Events/PlayerTurnStartEvent.cs` | 플레이어 턴 시작 이벤트 |

수정된 파일:

| 파일 | 변경 내용 |
|---|---|
| `Battle/UI/HandLayoutController.cs` | `ActionQueueRegisteredEvent` → `CardDroppedOnTargetEvent` 구독으로 교체, `SkillExecutionStartEvent/EndEvent` 구독 → `SetAllCardsInteractable()` |
| `Agents/Players/Player.cs` | `HandleHitEvent()` override — HIT 상태 전환 후 `OnStateCompleted` → IDLE 복귀 |
| `Agents/Enemies/AbstractEnemy.cs` | `HandleHitEvent()` override — 동일 패턴, EnemyState.IDLE(=2) 복귀 |

확정 원칙:

* `BattleActionExecutor`가 코스트 차감·스킬 실행·UI 잠금을 담당한다.
* `BattleTurnController`가 전체 턴 사이클을 오케스트레이션한다 — Enemy 배열 인덱스 0→1→2 순서.
* `SkillExecutionStartEvent`/`SkillExecutionEndEvent`는 플레이어 스킬 실행과 적 턴 모두에서 손패 잠금용으로 재활용된다.
* `HandLayoutController`는 `CardDroppedOnTargetEvent`를 구독해 카드 제거 — 드래그 중이던 `_draggedCard`도 처리.

---

### Phase UI-1. Hand Card Layout — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Battle/Data/CardDataSO.cs` | `artwork` Sprite 필드 추가 |
| `Battle/UI/CardView.cs` | `PoolableMono` 상속, 아트워크·이름·코스트 표시, LitMotion 위치·회전 트윈 |
| `Battle/UI/HandLayoutController.cs` | 부채꼴 레이아웃 계산, `Pool` 직접 소유, UniTask 순차 등장 |

확정 원칙:

* `HandLayoutController`가 `Pool`을 직접 생성·소유한다 (`PoolManagerSo` 없이).
* Canvas 계층: `OverLayCanvas(SS-Overlay) → Battles → HandArea → CardContainer + PoolRoot`. (Phase UI-5에서 SS-Camera BattleCanvas에서 분리됨)
* `_currentRotZ` 필드로 회전값을 자체 추적 — Unity `localEulerAngles` 0~360 래핑 문제 회피.
* 카드 진입 시 `SnapRotation(목표각 + entryRotationOffset)`으로 위치 트윈만 동작.
* `dealStaggerDelay`로 카드가 한 장씩 순서대로 등장하는 연출 적용.

---

### Phase UI-2. Card Hover & Description — 완료

확정 원칙:

* 카드 프리팹에 `Canvas` 컴포넌트를 추가해 `overrideSorting`으로 호버 레이어 관리 — Sibling 순서 불변.
* 호버 시 회전을 0으로 정렬하고, `CalculateSnapToBottomY()`로 카드 밑면을 화면 바닥에 스냅.
* `EventChannelSO(BattleEventChannel)`을 통해 `CardView ↔ CardDescriptionPanel` 통신 — 직접 참조 없음.
* `canvasGroup.alpha > 0f` 조건으로 "패널이 보이는 상태"를 판단.

---

### Phase UI-3. Cost UI & Unusable Card Feedback — 완료

확정 원칙:

* `BattleCostModelSO`(SO)가 코스트 데이터 소스.
* 코스트 변경은 `CostChangedEvent` → `BattleEventChannel` 경유.
* `BattleActionExecutor`가 카드 드롭 시 `currentCost -= card.data.cost` 처리, `BattleTurnController`가 턴 시작 시 `currentCost = maxCost` 충전.

---

### Phase UI-4. Card Drag & Targeting State — 완료

확정 원칙:

* `CardDragHandler`가 드래그 로직 전담 — `CardView`는 인터페이스 위임만.
* `IEndDragHandler.OnEndDrag`으로 드래그 종료 감지.
* 드래그 중 `OnPointerEnter/Exit` 차단 — `IsDragging` 체크.

---

### Phase UI-5. Target Detection & Outline — 완료

확정 원칙:

* `TargetingModule`은 IModule 패턴으로 Agent 자식 계층에 컴포넌트로 부착.
* 외곽선은 `MaterialPropertyBlock`으로 `_OutlineWidth`/`_OutlineColor` 제어.
* Canvas 레이어 분리:
  * `BattleCanvas` (SS-Camera): `TargetingOverlay`만 포함
  * `OverLayCanvas` (SS-Overlay, Sort Order 10): 카드 UI 전체
* `BattleCameraController`: Cinemachine 가상 카메라 두 개 전환 — EaseInOut 0.4초 블렌드.
* `TargetCameraSync`: `RenderPipelineManager.beginCameraRendering` 콜백 사용 (LateUpdate 불가).

---

### Phase UI-9. 체력바 UI — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Agents/HealthModule.cs` | `event Action<int, int> OnHpChanged` 추가 — `TakeDamage` / `Reinitialize` 시 발행 |
| `Battle/UI/PlayerHealthBarView.cs` | `IModule`, Player에 부착, `[SerializeField] TMP_Text`로 `현재HP/최대HP` 텍스트 표시 |
| `Battle/UI/EnemyHealthBarView.cs` | `IModule`, Enemy World Space Canvas에 부착, 이름 텍스트 + 빨간/회색 슬라이더 |
| `Battle/UI/LookAtCamera.cs` | World Space Canvas를 매 LateUpdate마다 카메라 방향으로 회전 |
| `Agents/Enemies/AgentEnemyState.cs` | Enemy 전용 추상 상태 — `protected AbstractEnemy _enemy` 보유 |
| `Agents/Enemies/EnemyEntryState.cs` | 등장 애니메이션 재생 후 `AnimationEndTrigger` → `EnemyState.IDLE` 전환 |
| `Agents/Enemies/EnemyState.cs` | `ENTRY = 4` 추가 |
| `Agents/Enemies/BaseEnemy.cs` | `Start()`에서 IDLE 대신 ENTRY 상태로 시작 |

확정 원칙:

* **플레이어 체력바**: OverLayCanvas Screen Space — TMP 텍스트 `현재HP/최대HP` 형식, `PlayerHealthBarView(IModule)`이 Player 루트에 부착되어 SerializeField로 TMP_Text 참조.
* **적 체력바**: Agent 자식 World Space Canvas — 빨간 슬라이더(즉시 LitMotion 트윈) + 회색 슬라이더(딜레이 후 LitMotion 트윈) + 이름 텍스트.
* `EnemyHealthBarView`는 `owner.GetModule<AgentTrigger>().OnAnimationEnd`에 **한 번만** 구독 → ENTRY 애니메이션 종료 시 CanvasGroup alpha 0→1 페이드인 후 즉시 구독 해제.
* `AgentEnemyState`는 `Activator.CreateInstance(type, agent, paramHash)` 패턴을 유지하기 위해 생성자를 `(Agent, int)`로 받고 내부에서 `as AbstractEnemy` 캐스팅.
* Block 표시는 이번 Phase 범위 밖 — 구조는 막지 않음.

---

## Core Development Rules

### Language Rule

모든 설명, 계획, 구현 노트, 질문은 한국어로 작성한다.

단, 클래스명, 메서드명, enum명, 파일명, 폴더명, Unity API명은 영어를 유지할 수 있다.

---

### SOLID 우선

코드는 SOLID 원칙을 최대한 지켜 작성한다.

* 하나의 클래스는 하나의 책임만 가진다.
* 데이터, 전투 로직, UI, 입력, 연출 로직을 섞지 않는다.
* 하나의 Manager가 모든 기능을 직접 처리하지 않도록 한다.
* 나중에 합성, 승급, 공명, 보상 시스템을 추가할 수 있도록 확장 가능하게 만든다.
* 단, 프로토타입 개발 속도를 해칠 정도의 과도한 추상화는 피한다.

---

### 구현 전 grill-me 스킬 사용

새 기능을 구현하기 전에는 반드시 `grill-me` 스킬로 구조를 먼저 검토한다.

구조가 불명확하면 임의로 구현하지 말고 질문한다.

---

## Stage Framework

### 확정 설계 원칙

```text
Stage = 전투 1맵. Wave를 여러 개 가질 수 있다.
Wave = 최대 3명의 적 등장. 모두 사망하면 다음 Wave로 전환.
마지막 Wave 완료 = Stage Clear. 플레이어 HP 0 = Stage Fail.
```

**덱 분리 원칙:**
- `StageDataSO`는 덱을 알면 안 된다 — 적 Wave 정보만 가진다.
- 덱은 `PlayerDeckProvider`에서 가져온다 (현재는 임시 TempStartCardSO).
- 런타임 중 원본 SO 에셋을 직접 수정하지 않는다 — 런타임 복사본 사용.

**카드 더미 규칙:**
```text
Hand = 손패 / DrawPile = 가짐패 / DiscardPile = 버림패
드로우: 가짐패 우선 → 부족 시 버림패 셔플 후 합산
카드패 진입 시점: 플레이어 턴 시작 / 새 Stage / Wave 전환
코스트가 0이어도 자동 턴 종료 없음 — 턴 종료 버튼 필수
```

**Wave 전환 흐름:**
```text
마지막 적 사망 → 스킬 연출 완료 → 짧은 대기
→ 다음 Wave 있으면: 적 스폰 + 카드패 드로우 (동시)
→ 적 등장 모션 + 드로우 연출 완료 후 입력 허용
```

**적 스폰 규칙:**
- `slotIndex 0~2` 기반 배치, `localOffset`으로 미세 조정
- `isLargeEnemy = true`이면 1차 규칙상 중앙 슬롯 단독 배치

---

### Phase S-1. Battle Result & End Condition — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Battle/Events/BattleVictoryEvent.cs` | 모든 적 사망 시 발행 |
| `Battle/Events/BattleDefeatEvent.cs` | 플레이어 사망 시 발행 |
| `Battle/UI/BattleResultController.cs` | Enemy `OnDeath` 개별 구독 + `_livingEnemyCount` 추적, `_pendingVictory` / `_pendingDefeat` 플래그로 스킬 완료 후 판정 |

수정된 파일:

| 파일 | 변경 내용 |
|---|---|
| `Battle/UI/BattleTurnController.cs` | `BattleVictoryEvent` / `BattleDefeatEvent` 구독 → `_battleEnded` 플래그 → 이후 턴 진행 차단 |
| `Battle/UI/HandLayoutController.cs` | `BattleVictoryEvent` / `BattleDefeatEvent` 구독 → 카드 잠금, `SkillExecutionEndEvent`에서 `_battleEnded` 체크 추가 |

확정 원칙:

* `BattleResultController`가 전투 종료 판정 단일 책임 — `BattleTurnController`는 턴 진행만.
* `BattleResultController`의 구독 설정은 `Start()`에서 처리 — Enemy가 `Awake`에서 완전히 초기화된 뒤 구독.
* 스킬 실행 중(`_isExecuting`) 사망 발생 시 `_pending` 플래그 세팅 → `SkillExecutionEndEvent` 수신 시 처리.
* Defeat가 Victory보다 우선 (`OnSkillEnd`에서 `_pendingDefeat` 먼저 체크).
* S-4 동적 스폰 도입 시 `BattleResultController.enemies` 목록을 Registry 패턴으로 교체 예정.

---

### Phase S-2. Turn Branch Cleanup — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Battle/Events/CardDrawStartEvent.cs` | 카드 드로우 시작 이벤트 |
| `Battle/Events/CardDrawEndEvent.cs` | 카드 드로우 완료 이벤트 |

수정된 파일:

| 파일 | 변경 내용 |
|---|---|
| `Battle/UI/HandLayoutController.cs` | `DealStartingCardsAsync`에 `CardDrawStartEvent/EndEvent` 발행 추가, `SkillExecutionStart/CardDrawStart` 시 슬라이드 아웃 (아래), `End` 시 슬라이드 인 |
| `Battle/UI/TurnEndButton.cs` | `SkillExecutionStart/CardDrawStart/Victory/Defeat` 구독 → 슬라이드 아웃 (오른쪽), 종료 시 슬라이드 인, 전투 종료 후 클릭 차단 |
| `Battle/UI/CostDisplayPanel.cs` | 동일 이벤트 구독 → 슬라이드 아웃 (왼쪽) |

확정 원칙:

* 슬라이드 대상 요소는 VerticalLayoutGroup 영향을 받지 않는 **빈 부모 컨테이너**를 트윈 — `[SerializeField] RectTransform slideTarget`으로 Inspector에서 컨테이너 연결.
* `slideOutOffset`, `slideDuration`, `slideEase` 모두 Inspector 조정 가능.
* 전투 종료(`_battleEnded`) 시 슬라이드 인 차단 — 영구 아웃 상태 유지.
* `CardDrawStartEvent`는 `DealStartingCardsAsync` 시작 시 발행 — 나중에 S-6 Wave 드로우에서도 동일하게 재활용.

---

### Phase S-3. StageDataSO & WaveData Definition — 완료

구현 완료:

| 파일 | 변경 내용 |
|---|---|
| `Battle/Data/BattleStageSO.cs` | `enemies` 제거 → `waves: List<WaveData>`, `clearRewardPlaceholder: string` 추가 |
| `Battle/Data/WaveData.cs` | 신규 — `enemySpawns: List<EnemySpawnEntry>` |
| `Battle/Data/EnemySpawnEntry.cs` | `localOffset: Vector3`, `isLargeEnemy: bool` 추가 |

확정 원칙:

* `BattleStageSO`는 `stageId`, `waves`, `clearRewardPlaceholder`만 보유 — 덱 참조 없음.
* `WaveData`는 별도 파일로 분리 — `EnemySpawnEntry`와 동일한 파일 분리 패턴.
* `waveIndex` 필드 없음 — 리스트 순서가 곧 인덱스.
* `stageName` 필드 없음 — stageId로 충분, 결과 UI 필요 시 추가.
* `clearRewardPlaceholder`는 `string` 타입 — 보상 시스템 확정 전 메모용.
* `isLargeEnemy = true`이면 S-4 스폰 로직에서 중앙 슬롯 단독 배치 처리.

---

### Phase S-4. Stage Bootstrap & Enemy Spawn — 완료

구현 완료:

| 파일 | 역할 |
|---|---|
| `Battle/UI/StageBootstrapper.cs` | `BattleStageSO` 기반 Wave 스폰, Player+Enemy 동시 ENTRY 대기 → `BattleReadyEvent` |
| `Battle/Data/RuntimeEnemyRegistrySO.cs` | 런타임 적 목록 — `Register/Unregister/Clear`, `IReadOnlyList<AbstractEnemy> Enemies` |
| `Battle/Events/EnemiesUpdatedEvent.cs` | 적 목록 갱신 신호 — `BattleResultController`/`BattleTurnController` 재구독 트리거 |
| `Battle/Events/BattleReadyEvent.cs` | 모든 ENTRY 완료 신호 — HandLayoutController 카드 딜 트리거 |
| `Agents/Players/PlayerEntryState.cs` | Player ENTRY 상태 — `AnimationEndTrigger` → `CompleteState()` |

수정된 파일:

| 파일 | 변경 내용 |
|---|---|
| `Agents/Players/PlayerState.cs` | `ENTRY = 5` 추가 |
| `Agents/Players/Player.cs` | `Start()`에서 ENTRY 시작, `WaitForEntryComplete(): UniTask` 노출 |
| `Agents/Enemies/AbstractEnemy.cs` | `InitializeEntry()` + lazy TCS `GetOrCreateEntryCompletion()` + `WaitForEntryComplete()` |
| `Agents/Enemies/BaseEnemy.cs` | `Start()` → `InitializeEntry()` |
| `Agents/Enemies/EnemyEntryState.cs` | `HandleAnimationEnd()` → `CompleteState()` (직접 IDLE 전환 제거) |
| `Battle/UI/BattleResultController.cs` | `[SerializeField] enemies` 제거 → `RuntimeEnemyRegistrySO` + `EnemiesUpdatedEvent` 구독 |
| `Battle/UI/BattleTurnController.cs` | 동일 패턴 |
| `Battle/UI/HandLayoutController.cs` | `Awake()`에서 즉시 슬라이드 아웃, `BattleReadyEvent` → `DealStartingCardsAsync()` |
| `Battle/UI/TurnEndButton.cs` | `Awake()`에서 즉시 슬라이드 아웃 |
| `Battle/UI/CostDisplayPanel.cs` | 동일 |

확정 원칙:

* `StageBootstrapper`가 스폰 오케스트레이터 단일 책임 — `BattleStageSO.waves[0]`부터 시작.
* 슬롯 위치는 씬의 `Transform[] slotPositions` 3개 — Inspector에서 연결.
* `isLargeEnemy = true` → 스폰 시 `slotIndex`를 1로 강제.
* `StageBootstrapper`가 스폰 후 `enemyRegistry.Register(enemy)` 직접 처리.
* `FindObjectsByType<AbstractEnemy>()` 로 씬 배치 적 + 동적 스폰 적 모두 ENTRY 대기 — 씬 전환 기간 호환.
* lazy TCS(`GetOrCreateEntryCompletion`) 패턴 — `WaitForEntryComplete()` 수집 타이밍과 `InitializeEntry()` 호출 타이밍 무관.
* `BattleReadyEvent` 수신 후 카드 딜 시작 → `CardDrawEndEvent` → UI 슬라이드 인.

---

### Phase S-5. Player Deck Provider

**Goal**: PlayerDeckProvider에서 덱 가져오기

**완료 기준:**
```text
전투 시작 시 PlayerDeckProvider에서 덱 수신.
현재는 TempStartCardSO로 임시 제공.
DrawPile 생성 가능.
런타임 중 원본 SO 에셋 미수정. Play 종료 후 에디터 상태 유지.
```

---

### Phase S-6. Wave Transition

**Goal**: Wave 클리어 후 다음 Wave 전환

**완료 기준:**
```text
Wave 전 적 사망 → Wave Clear → 스킬 연출 완료 대기.
다음 Wave 적 스폰 + 카드패 드로우 동시 진행.
가짐패 우선 드로우, 부족 시 버림패 셔플 합산.
카드패 진입 시 코스트 최대 회복.
적 등장 모션 + 드로우 연출 완료 후 입력 허용.
```

---

### Phase S-7. Stage Clear / Stage Fail Branch

**Goal**: Wave 결과를 Stage 결과로 연결

**완료 기준:**
```text
마지막 Wave 완료 → 스킬 연출 완료 대기 → Stage Clear.
플레이어 사망 → DeathState 완료 대기 → Stage Fail.
전투 UI 트위닝 아웃 가능.
Stage Clear / Fail 이후 카드 입력 + 턴 진행 중단.
```

---

### Phase S-8. Stage Result UI Placeholder

**Goal**: 임시 결과 UI 표시

**완료 기준:**
```text
Stage Clear 시 "Stage Clear" UI 표시.
Stage Fail 시 "Stage Failed" UI 표시.
결과 UI 중 카드 입력 + 턴 진행 불가.
임시 재시작 / 닫기 버튼 배치.
```

---

### Stage Framework 이후 순서

```text
1. DI Refactoring (전투 컨트롤러 Reflect DI 적용 + Player 직접 참조 제거)
2. Card Grave System
3. Skill Presentation Editor
4. Fusion / Grade / Resonance System
5. Reward System
```

**DI 리팩토링 범위 (Stage Framework 완료 후):**
- `BattleResultController`, `BattleTurnController`, `BattleActionExecutor` 등 전투 컨트롤러의 `[SerializeField] Player player` 직접 참조 → Reflect DI로 교체
- Enemy 직접 참조는 S-4에서 동적 스폰 + Registry 패턴으로 자연 해소됨
- Stage Framework 완료 전에 DI 적용 시 S-4, S-6 구조 변경 때 재수정 필요 — 의도적으로 후순위

---

## Out of Scope

Stage Framework 완료 전까지 구현하지 않는다.

```text
범위 공격 타겟팅 완성
합성 UI / 카드 합성 시스템
카드 등급 표시 UI / 카드 등급별 스킬 데이터 분기
공명 게이지 UI
스킬 연출 에디터 연동 (DamageCastTrigger 타이밍 연동 포함)
카드 보상 UI
버프/디버프 UI
스킬 실행 중 UI 슬라이드 아웃 연출
적 행동 의도 표시 UI
```

단, 위 기능이 나중에 추가될 수 있도록 구조를 막지 않는다.
