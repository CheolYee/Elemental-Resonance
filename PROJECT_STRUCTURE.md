# Elemental Resonance — 프로젝트 구조 문서

> 마지막 업데이트: Phase 1 완료 + EnumSystem + 코드 지침 확정

---

## 게임 개요

Unity 6 기반 2.5D 턴제 카드 전투 게임.

```
플레이어 턴 시작
→ 카드 드로우 → 코스트 지급
→ 카드를 드래그해 행동 큐에 예약
→ 실행 버튼 클릭 → 예약된 행동을 순서대로 실행
→ 적 턴 실행 → 다시 플레이어 턴
```

---

## Assembly Definition 구조

### 어셈블리 목록

| 이름 | 경로 | 설명 |
|---|---|---|
| `ER.Input` | `Assets/00. Work/_Resources/` | Controls.cs (InputActions 생성 파일) |
| `ER.Modules` | `.../02. Scripts/Modules/` | IModule, ModuleOwner 인터페이스 레이어 |
| `ER.Systems` | `.../02. Scripts/Systems/` | DB, 애니메이션, 세이브, 입력 시스템 |
| `ER.Agents` | `.../02. Scripts/Agents/` | Agent, FSM, StatSystem (Editor 폴더 제외) |
| `ER.Agents.Editor` | `.../02. Scripts/Agents/FSM/Editor/` | 커스텀 Inspector (Editor 전용) |
| `ER.Systems.Editor` | `.../02. Scripts/Systems/EnumSystem/Editor/` | EnumCodeGenerator 등 Systems 에디터 도구 |
| `ER.Battle` | `.../02. Scripts/Battle/` | Phase 1 전투 데이터 SO |
| `ER.Combat` | `Assets/02. Scripts/` | 구 CombatSystem 스크립트 |

### 의존 관계 그래프

```
Unity Engine / Packages
    │
    ├── Unity.InputSystem ──────────────────┐
    ├── Gamelib.EventSystem (autoRef)        │
    └── Gamelib.SoundSystem (autoRef)        │
                                             │
ER.Input ←──────────────────────────────────┘
    │
ER.Modules          ER.Systems ←── ER.Input
    │                   │              + SoundSystem
    │                   │              + Unity.InputSystem
    └────────┬──────────┘
             │
         ER.Agents ←── EventSystem
             │
         ER.Agents.Editor ←── ER.Systems.Editor (Editor only)

ER.Systems.Editor ←── ER.Systems (Editor only)

ER.Combat ←── ER.Modules

ER.Battle  (의존 없음, Phase 1)
```

### 어셈블리 GUID 참조표

| 어셈블리 | GUID |
|---|---|
| ER.Input | `164e9fafb914a15499aec59f2355d045` |
| ER.Modules | `b4588862469b00b498d7ace2ac598898` |
| ER.Systems | `4cc9dd216fd475949aa0cfb56e8c8f8f` |
| ER.Agents | `2080abb65eb3b6348b5c23dc77654090` |
| ER.Systems.Editor | `538eac7639be5ec45afade049fe53337` |
| Gamelib.EventSystem | `0789bd4e3095c264a81ab741a817a621` |
| Gamelib.SoundSystem | `f8563110fcb49be4eac2d408b27c2523` |
| Unity.InputSystem | `75469ad4d38634e559750d17036d5f7c` |

> 새 어셈블리 추가 시: asmdef 생성 → Unity 리프레시 → `.meta` 파일에서 GUID 확인 → `references` 필드에 `"GUID:xxx"` 형식으로 추가.  
> 모든 ER.\* 어셈블리는 `autoReferenced: false`.

---

## 스크립트 폴더 구조

```
Assets/
├── 00. Work/
│   └── _Resources/
│       ├── Controls.inputactions          ← 입력 액션 정의
│       ├── Controls.cs                    ← 자동 생성 (ER.Input)
│       └── 02. Scripts/
│           ├── Modules/                   ← ER.Modules
│           │   ├── IModule.cs
│           │   ├── IAfterInitModule.cs
│           │   └── ModuleOwner.cs
│           │
│           ├── Systems/                   ← ER.Systems
│           │   ├── Database/
│           │   │   ├── IndexedAsset.cs    ← SO 인덱스 기반 에셋 베이스
│           │   │   └── AbstractDataTableSO.cs
│           │   ├── AnimationSystems/
│           │   │   ├── AnimParamSO.cs
│           │   │   ├── IAnimatorTrigger.cs
│           │   │   └── IRenderer.cs
│           │   ├── CoreSystem/
│           │   │   ├── SaveIdData.cs
│           │   │   └── SaveIdDataTableSo.cs
│           │   ├── SaveSystem/
│           │   │   ├── IDataSaver.cs
│           │   │   ├── SaveManager.cs
│           │   │   └── Savers/JsonSaver.cs
│           │   ├── EnumSystem/            ← 범용 enum 생성기
│           │   │   ├── EnumListSO.cs     ← enum 정의 SO (enumName, namespace, valueNames[])
│           │   │   └── Editor/           ← ER.Systems.Editor
│           │   │       ├── EnumCodeGenerator.cs  ← static 파일 생성 유틸리티
│           │   │       └── EnumListSOEditor.cs   ← 인스펙터 UI
│           │   ├── PlayerInputSO.cs
│           │   ├── UIInputSo.cs
│           │   ├── AppResolutionStartup.cs
│           │   └── CodeFormat.cs
│           │
│           ├── Agents/                    ← ER.Agents
│           │   ├── Agent.cs              ← abstract, extends ModuleOwner
│           │   ├── AgentRenderer.cs      ← IModule + IRenderer (Animator 래핑)
│           │   ├── AgentSensor.cs
│           │   ├── ActionDataModule.cs
│           │   ├── Players/
│           │   │   └── Player.cs         ← Agent 구체 구현
│           │   ├── StatSystem/
│           │   │   ├── StatSO.cs         ← 클론 가능 SO, modifier 이벤트 지원
│           │   │   ├── StatModule.cs     ← IModule, StatSO 딕셔너리 관리
│           │   │   ├── StatOverride.cs
│           │   │   └── IStatModule.cs
│           │   └── FSM/
│           │       ├── AgentState.cs     ← abstract, IRenderer 통해 애니메이션
│           │       ├── StateMachine.cs   ← 리플렉션으로 AgentState 인스턴스화
│           │       ├── StateSO.cs        ← 상태 1개 정의 (className + animParam)
│           │       ├── StateListSO.cs    ← 상태 목록 SO
│           │       └── Editor/           ← ER.Agents.Editor
│           │           ├── StateSOEditor.cs
│           │           ├── StateListSOEditor.cs
│           │           ├── StateSO View.uxml
│           │           └── StateListSO View.uxml
│           │
│           └── Battle/                   ← ER.Battle (Phase 1~)
│               ├── Enums/                ← EnumListSO가 생성하는 C# 파일
│               │   ├── BattlePhase.cs    ← 생성됨 (SO: Battle/Enums/BattlePhase.asset)
│               │   └── CardTargetType.cs ← 생성됨 (SO: Battle/Enums/CardTargetType.asset)
│               ├── Data/
│               │   ├── BattleStageSO.cs
│               │   ├── EnemyDataSO.cs
│               │   ├── EnemySpawnEntry.cs  ← [Serializable] 인라인
│               │   └── CardDataSO.cs
│               ├── Effects/
│               │   ├── CardEffectSO.cs     ← abstract SO
│               │   ├── DamageEffectSO.cs
│               │   └── BlockEffectSO.cs
│               └── Instances/
│                   └── CardInstance.cs     ← [Serializable], Guid로 instanceId
│
├── 02. Scripts/                           ← ER.Combat (구 스크립트)
│   ├── Agents/
│   │   ├── AgentSensor.cs
│   │   └── AgentTrigger.cs
│   └── CombatSystem/
│       ├── IDamageable.cs
│       ├── HealthModule.cs
│       ├── AbstractDamageCaster.cs
│       ├── DamageData.cs
│       ├── DamageCasters/
│       │   └── RayDamageCaster.cs
│       └── Skills/
│           ├── ISkill.cs
│           ├── ISkillModule.cs
│           └── SkillDataSO.cs
│
├── 00. Work/
│   └── _Resources/
│       └── Battle/
│           └── Enums/                    ← EnumListSO 에셋 (SO 소유자)
│               ├── BattlePhase.asset
│               └── CardTargetType.asset
│
└── Gamelib/                              ← 사내 공통 패키지
    ├── EventSystem/  (EventSystem.asmdef, autoReferenced)
    ├── ObjectPool/
    └── SoundSystem/  (SoundSystem.asmdef, autoReferenced)
```

---

## 핵심 아키텍처 패턴

### 1. Module 패턴

`ModuleOwner`(MonoBehaviour)가 자식 오브젝트에서 `IModule` 구현체를 자동 수집·초기화.

```csharp
// ModuleOwner.Awake()
moduleDict = GetComponentsInChildren<IModule>()
    .ToDictionary(m => m.GetType());

// 다른 모듈에서 참조
T module = owner.GetModule<T>();  // 타입 또는 인터페이스로 조회
```

- `IAfterInitModule.AfterInit()` — 모든 모듈 Initialize 완료 후 호출
- Agent → ModuleOwner → IModule들 (AgentRenderer, StatModule, ActionDataModule 등)

### 2. ScriptableObject 데이터 패턴

- `IndexedAsset` — `int AssetIndex`를 가진 SO 베이스 (런타임 딕셔너리 키로 사용)
- `AbstractDataTableSO` — `IndexedAsset[]` 목록을 담는 테이블 SO
- `StatSO` — `ICloneable`, 전투마다 `Instantiate()`로 복제해서 사용

### 3. FSM 패턴

SO 데이터로 상태 목록을 정의, 리플렉션으로 `AgentState` 서브클래스 인스턴스화.

```csharp
// StateSO: className (문자열) + animParam
// StateMachine 생성자에서:
Type type = Type.GetType(stateSO.className);
AgentState state = (AgentState)Activator.CreateInstance(type, agent, paramHash);
```

상태 전환: `stateMachine.ChangeState(stateIndex, transitionDuration)`

### 4. Enum 생성 시스템

`EnumListSO` SO를 에디터에서 만들고 "C# 파일 생성" 버튼으로 enum C# 파일을 출력한다.

```
EnumListSO 에셋 (Battle/Enums/*.asset)
  enumName      → "BattlePhase"
  namespaceName → "Battle.Enums"
  valueNames[]  → ["None", "BattleStart", ...]
  generatePath  → 절대 경로 (폴더 선택 버튼으로 설정)
        ↓
EnumListSOEditor "C# 파일 생성" 클릭
        ↓
EnumCodeGenerator.WriteEnumFile() → BattlePhase.cs 출력
```

- 새 enum 추가: `Assets/Create > Enum/Enum List` → 필드 입력 → 생성
- `StateListSOEditor`도 내부적으로 `EnumCodeGenerator`를 사용 (assetIndex 할당은 StateListSOEditor가 담당)

### 5. 네임스페이스 현황

| 폴더 | 네임스페이스 |
|---|---|
| Modules/ | `_00._Work._Resources._02._Scripts.Modules` |
| Systems/ | `_00._Work._Resources._02._Scripts.Systems.*` |
| Agents/ | `_00._Work._Resources._02._Scripts.Agents.*` (일부 `Agents.FSM`, `Agents.StatSystem`) |
| Battle/ | `Battle.Data`, `Battle.Effects`, `Battle.Instances`, `Battle.Enums` |
| Assets/02. Scripts/ | `CombatSystem`, `_02._Scripts.*` |

> Agents 폴더는 긴 네임스페이스와 짧은 네임스페이스가 혼재함. Battle부터 짧은 형태로 통일.

---

## Phase 1 — Data Foundation (완료)

### 구현 완료 목록

| 파일 | 타입 | 설명 |
|---|---|---|
| `BattlePhase.cs` | enum | 전투 상태 11개 |
| `CardTargetType.cs` | enum | None / SingleEnemy / SingleAlly |
| `EnemySpawnEntry.cs` | [Serializable] class | enemyData + slotIndex |
| `EnemyDataSO.cs` | ScriptableObject | enemyId, name, prefab, maxHp, attackDamage |
| `BattleStageSO.cs` | ScriptableObject | stageId, List\<EnemySpawnEntry\> (최대 3) |
| `CardDataSO.cs` | ScriptableObject | cardId, name, cost, targetType, effects |
| `CardEffectSO.cs` | abstract SO | 카드 효과 베이스 |
| `DamageEffectSO.cs` | ScriptableObject | int damage |
| `BlockEffectSO.cs` | ScriptableObject | int block |
| `CardInstance.cs` | [Serializable] class | instanceId (Guid), CardDataSO data |

### 에디터 생성 메뉴

```
Assets/Create
└── Battle/
    ├── Stage              → BattleStageSO
    ├── Enemy Data         → EnemyDataSO
    ├── Card Data          → CardDataSO
    └── Effects/
        ├── Damage         → DamageEffectSO
        └── Block          → BlockEffectSO
```

### 미구현 (Phase 2 이후)

```
BattleManager, TurnController, DeckController, ActionQueue
Card Drag Input, Card UI, Enemy Spawn
실제 피해/방어도 적용, 전투 턴 진행, 승리/패배 처리
합성·속성·등급·공명·보상 시스템
```

---

## 개발 규칙 요약

- **SOLID 우선** — 클래스 단일 책임, 데이터/전투로직/UI/연출 혼재 금지
- **구현 전 grill-me** — 새 기능 전 반드시 구조 검토
- **SO 기반 데이터** — 에디터에서 수정 가능한 데이터는 ScriptableObject로
- **Enum은 EnumListSO로 관리** — 수동 enum 파일 작성 금지, SO에서 생성
- **Phase 범위 엄수** — 현재 Phase 밖의 것은 구현하지 않음
- **인코딩** — C#/MD 파일은 UTF-8

### 라이브러리 지침

| 목적 | 사용 라이브러리 | 금지 |
|---|---|---|
| 의존성 주입 | **Reflect DI** | `new`로 서비스 직접 생성, 싱글톤 직접 접근 |
| 비동기 처리 | **UniTask** (`async UniTask`) | `Coroutine`, `IEnumerator`, `async Task` |
| 트위닝 | **LitMotion** (`LMotion.Create(...).Bind(...)`) | DOTween, iTween 등 |

UniTask + LitMotion 조합 패턴:
```csharp
// 트윈 완료까지 대기
await LMotion.Create(0f, 1f, 0.3f).Bind(target).ToUniTask(cancellationToken);

// 시간 대기
await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: ct);

// 프레임 대기
await UniTask.NextFrame(ct);
```
