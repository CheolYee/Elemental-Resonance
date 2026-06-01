# CLAUDE.local.md

## Project Context

Unity 6 기반 3D 턴제 카드 전투 게임을 개발한다.

현재 큰 전투 구조는 다음과 같다.

```text
플레이어 턴 시작
→ 카드 드로우
→ 코스트 지급
→ 카드를 드래그해 행동 큐에 예약
→ 실행 버튼 클릭
→ 예약된 행동을 순서대로 실행
→ 적 턴 실행
→ 다시 플레이어 턴
```

현재는 **1단계 전투 기본 루프**를 구현 중이며,
이 문서는 그중 **Phase 4: BattleManager & Turn Loop**을 다룬다.

---

## ✅ Phase 2: Battle Actor Foundation — 완료

### 구현 결과

| 파일 | 역할 |
|---|---|
| `Agents/IBattleActor.cs` | 공통 전투 유닛 인터페이스 |
| `Agents/HealthModule.cs` | HP·Block·IsDead 관리, StatModule에서 MaxHP 읽음 |
| `Agents/Agent.cs` | IBattleActor 구현, HealthModule에 위임 |
| `Agents/Enemies/AbstractEnemy.cs` | 모든 적의 추상 기반 클래스 |
| `Agents/Enemies/BaseEnemy.cs` | 씬에 배치되는 기본 적 유닛 |

### 확정된 설계 원칙

- StatModule = 기초 수치 저장(MaxHP, 버프/디버프 보정)
- HealthModule = 전투 상태 관리(CurrentHp, Block, IsDead)
- Agent가 IBattleActor를 구현하고 HealthModule에 위임 → 단일 진입점
- EventChannelSO 연동은 다음 Phase에서 추가
- EnemyDataSO.maxHp는 스포너용 메타데이터로 보존

---

## ✅ Phase 3: AgentRenderer & Attack Motion — 완료

### 구현 결과

| 파일 | 역할 |
|---|---|
| `Agents/AgentRenderer.cs` | IRenderer 구현, CrossFadeInFixedTime으로 코드 드리븐 애니메이션 전환 |
| `Agents/AgentTrigger.cs` | 애니메이션 이벤트 수신 모듈 (OnAnimationEnd, OnDamageCast) |
| `Agents/FSM/AgentState.cs` | FSM 상태 기반 클래스, OnStateCompleted 이벤트, CompleteState() |
| `Agents/FSM/States/IdleState.cs` | 대기 상태 |
| `Agents/FSM/States/HitState.cs` | 피격 상태 — AnimationEnd 이벤트 → CompleteState() |
| `Agents/FSM/States/DeathState.cs` | 사망 상태 — AnimationEnd 이벤트 → CompleteState() |
| `Agents/FSM/States/SkillState.cs` | 스킬 상태 — SkillModule.CurrentSkill.animParam에서 클립 해시 읽어 재생 |
| `CombatSystem/Skills/SkillModule.cs` | 스킬 오케스트레이터 — UseSkill() 호출 시 SkillState 진입, OnStateCompleted → Idle 복귀 |
| `CombatSystem/Skills/SkillDataSO.cs` | 스킬 데이터 SO (AnimParamSO animParam 추가) |
| `Agents/FSM/Editor/StateListSOEditor.cs` | enum 생성 에디터 — 숫자 포함 경로도 유효한 C# 네임스페이스로 정제 |

### 확정된 설계 원칙

- **Player·Enemy 모두 동일한 SkillModule + SkillState 사용** — 적 공격도 스킬 판정이므로 별도 EnemyAttackState 불필요
- 애니메이터 화살표 없이 코드로 상태 전환 — `PlayClip(CrossFadeInFixedTime)` 방식
- 애니메이션 완료 타이밍은 Animator 이벤트(`AnimationEndTrigger`) → AgentTrigger → FSM State 순으로 전달
- SkillModule이 스킬 사이클 전체를 오케스트레이션 (Phase 4에서 시네머신·파티클·데미지 타이밍 추가)
- StateListSO 2개 (Player용·Enemy용) → `PlayerState` enum, `EnemyState` enum 각각 생성
- FSM 공유 상태: IdleState, SkillState, HitState, DeathState (Player·Enemy 동일)

---

## Core Development Rules

### SOLID 우선

코드는 SOLID 원칙을 최대한 지켜 작성한다.

* 하나의 클래스는 하나의 책임만 가진다.
* 데이터, 전투 로직, UI, 연출 로직을 섞지 않는다.
* BattleManager가 모든 기능을 직접 처리하지 않도록 한다.
* 나중에 합성, 승급, 공명, 보상 시스템을 추가할 수 있도록 확장 가능하게 만든다.
* 단, 프로토타입 개발 속도를 해칠 정도의 과도한 추상화는 피한다.

### 구현 전 grill-me 스킬 사용

새 기능을 구현하기 전에는 반드시 `grill-me` 스킬로 구조를 먼저 검토한다.

검토해야 할 내용:

* 이 기능의 책임이 어느 클래스에 있는가?
* 지금 Phase 범위를 넘지 않는가?
* 나중에 확장될 시스템과 충돌하지 않는가?
* SO 기반 데이터 구조와 연결 가능한가?
* 임시 구현이라면 나중에 제거하기 쉬운가?

구조가 불명확하면 임의로 구현하지 말고 질문한다.

---

## Current Phase

# Phase 4. BattleManager & Turn Loop

## Goal

플레이어 턴 → 적 턴의 기본 루프를 돌릴 수 있는 전투 관리 구조를 만든다.
ActionQueue에 예약된 행동을 순서대로 실행하고, 적 AI가 SkillModule을 통해 공격하도록 한다.

**구현 전에 반드시 `grill-me` 스킬로 구조를 먼저 확인한다.**

---

## 미구현 항목

```text
BattleManager — 전투 시작/종료, 승리/패배 판정
TurnController — 플레이어 턴 / 적 턴 전환
DeckController, ActionQueue — 카드 드로우, 행동 예약·실행
카드 드래그 입력, 카드 UI
적 스폰, 적 의도 표시
적 AI 턴 — SkillModule.UseSkill() 호출로 공격
카메라 연출 (시네머신) — SkillModule Phase 4 확장
스킬 이펙트 (파티클), 데미지 캐스트 타이밍
카드 합성, 등급, 공명, 보상
EventChannelSO 전투 이벤트 연동
```
