# 슬로우 모션 타겟팅 + VFX/캐스터 시스템

작성일: 2026-06-15
상태: 전 페이즈 완료

---

## 배경 및 목표

- 스킬 연출 도중에도 손패를 보이게 유지하여 연속 드로우 가능
- 타겟팅 진입 시 타임라인 애니메이션을 유지하면서 Time.timeScale 감소 → 슬로우 모션으로 타겟 선택
- VFX 파티클 시뮬레이션 속도·라이프타임 배율을 인스펙터에서 조절 가능
- 캐스터를 타임라인에서 이동시키는 키프레임 지원

---

## 확정 설계

### 슬로우 모션 타겟팅

| 항목 | 결정 |
|------|------|
| 손패 잠금 | SkillExecution 이벤트에서 HandLayoutController 완전 분리. 손패는 CardDraw / BattleUIHidden·Shown / WaveClear로만 제어 |
| FSM 보호 | Player.cs에 `_isSkillPresenting` 플래그. OnTargetingStart/End에서 플래그가 true이면 FSM 변경 스킵 |
| 슬로우 모션 | BattleSlowMotionController (신규). `_isSkillPresenting && _isTargeting` 둘 다 true일 때 Time.timeScale = slowMotionScale |
| 슬로우 배율 | 0.2f (Inspector SerializeField) |
| 복원 시점 | CardTargetingEndEvent or SkillExecutionEndEvent 중 하나라도 false가 되면 즉시 Time.timeScale = 1f |
| 큐 간 딜레이 | BattleActionExecutor 큐 루프에서 카드 사이 UniTask.Delay(0.1s) 추가 |

### VFX 속도/라이프타임

| 항목 | 결정 |
|------|------|
| 필드 위치 | `SkillVfxObjectData`에 `float simulationSpeed = 1f`, `float startLifetimeMultiplier = 1f` 고정 필드 추가 |
| 런타임 적용 | Play() 시 ParticleSystem.MainModule에 `simulationSpeed`, `startLifetime *= multiplier` 적용 |
| 에디터 표시 | VFX 오브젝트 인스펙터 ScrollView에 두 필드 추가 |

### 캐스터 이동

| 항목 | 결정 |
|------|------|
| 키프레임 종류 | `SkillKeyframeProperty.CasterPosition`, `CasterRotation` 추가 |
| 오브젝트 종류 | `SkillObjectKind.Caster` 추가 |
| 이동 방식 | basePosition + additive delta 오프셋 (카메라 방식과 동일) |
| 복원 | 스킬 종료 후 SkillCasterExecutionService finally에서 원래 위치·회전으로 복원 |
| 에디터 프리뷰 | SkillPreviewScene.SampleCaster(t) — RT-2 Phase 4a/4b 카메라 패턴 동일 적용 |
| Timeline Row | CasterPosition 1개 row (CasterRotation은 동일 오브젝트 row에 포함) |

---

## ~~Phase 1~~ — HandLayoutController SkillExecution 분리 (완료)

**변경 파일:**
- `HandLayoutController.cs`: SkillExecutionStart/End 구독 제거, `_waveClearPending` 필드 제거

**완료 기준:**
- 스킬 연출 중 손패가 슬라이드 아웃되지 않음
- 스킬 연출 중 카드 드래그 가능 (큐에 추가됨)
- WaveClear / CardDraw / BattleUIHidden 기존 동작 유지

---

## ~~Phase 2~~ — Player.cs FSM 보호 (완료)

**변경 파일:**
- `Player.cs`: `_isSkillPresenting` 플래그, SkillExecutionStart/End 구독, OnTargetingStart/End 가드

**완료 기준:**
- 스킬 연출 중 카드 드래그 시 플레이어 FSM이 TARGETING으로 전환되지 않음
- 스킬 연출 중 드롭 취소 시 FSM이 IDLE로 강제 전환되지 않음
- 스킬 연출 없을 때 타겟팅은 기존과 동일하게 TARGETING → IDLE 전환

---

## ~~Phase 3~~ — BattleSlowMotionController 신규 (완료)

**신규 파일:**
- `Battle/UI/BattleSlowMotionController.cs`

**완료 기준:**
- 스킬 연출 중 타겟팅 진입 시 Time.timeScale = 0.2
- 타겟팅 종료(드롭 또는 취소) 시 즉시 Time.timeScale = 1f
- 스킬 연출 없는 상황에서 타겟팅 시 timeScale 변화 없음

---

## ~~Phase 4~~ — BattleActionExecutor 큐 간 딜레이 (완료)

**변경 파일:**
- `BattleActionExecutor.cs`: 큐 루프에서 카드 사이 `await UniTask.Delay(TimeSpan.FromSeconds(0.1f))` 추가

**완료 기준:**
- 연속 카드 사용 시 스킬 사이에 플레이어가 Idle 자세로 잠깐 복귀하는 모습 확인

---

## ~~Phase 5~~ — VFX 속도·라이프타임 배율 (완료)

**확정 설계:**
- `SkillVfxObjectData`에 `simulationSpeed = 1f`, `startLifetimeMultiplier = 1f` 추가
- `SkillVfxContainer.Play(key, simulationSpeed, lifetimeMultiplier)` — `ps.Play()` 전에 `main.simulationSpeed`, `main.startLifetimeMultiplier` 적용
- `SkillVfxExecutionService`에서 `vfxObj` 값 전달
- VFX Active 경로(Play키프레임) + Legacy 경로(lifeTime) 모두 적용

**변경 파일:**
- `SkillTimelineKeyframes.cs`: `SkillVfxObjectData`에 두 필드 추가
- `SkillVfxContainer.cs`: `Play()` 시그니처 변경 + ParticleSystem 세팅
- `SkillVfxExecutionService.cs`: `container.Play()` 호출 시 값 전달
- `SkillPresentationEditorWindow.Inspector.cs`: VFX 오브젝트 인스펙터에 두 FloatField 추가

**완료 기준:**
- Inspector에서 simulationSpeed / startLifetimeMultiplier 조절 가능
- 런타임 VFX 재생 시 설정값이 반영됨

---

## ~~Phase 6~~ — 캐스터 이동 키프레임 (완료)

**확정 설계:**
- `SkillKeyframeProperty.CasterPosition / CasterRotation`, `SkillObjectKind.Caster` 추가
- `SkillPresentationTimeline`에 `casterTrack: SkillSingleTrackData` 추가
- 키프레임 필드: `key.position` / `key.rotationEuler` 재사용 (casterTrack 전용 트랙이므로 VFX와 충돌 없음)
- 이동 방식: `basePosition + additive delta` (카메라 패턴 동일)
- 복원: `SkillCasterExecutionService.PlayAsync` finally 블록에서 원위치 복원
- 에디터 Row: CasterPosition + CasterRotation 2개 행, 이징 연결선 포함
- DI: `BattleSceneInstaller`에 등록, `SkillPresentationPlayer` 생성자 파라미터 추가

**변경 파일:**
- `SkillTimelineKeyframes.cs`: enum 값 추가
- `SkillPresentationTimeline.cs`: `casterTrack` 필드 추가
- `SkillCasterExecutionService.cs` (신규): Camera 패턴 동일, `context.Caster.transform` 사용
- `SkillPresentationPlayer.cs`: `RunCasterAsync` 추가, 생성자 파라미터 추가
- `BattleSceneInstaller.cs`: `SkillCasterExecutionService` 등록
- `SkillPresentationEditorWindow.*.cs`: Caster 오브젝트 추가, 2개 Row, 이징 연결선
- `SkillPreviewScene.cs`: `SampleCaster(t)` 추가

**완료 기준:**
- 에디터에서 CasterPosition / CasterRotation 키프레임 추가·편집 가능
- 런타임 스킬 재생 시 캐스터가 지정 오프셋으로 이동 후 스킬 종료 시 원위치 복원
- 에디터 프리뷰 Playhead 이동 시 캐스터 위치 반영
