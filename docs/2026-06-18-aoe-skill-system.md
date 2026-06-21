# 범위 공격(AoE) 스킬 시스템 설계

작성일: 2026-06-18
상태: Phase 1~3 완료

---

## 확정된 설계

| 항목 | 결정 |
|------|------|
| AoE 타입 | `CardTargetType.AllEnemies = 3` (이번 구현) |
| 드롭 방식 | 적 위에 드롭 (SingleEnemy와 동일), 모든 적 전체 하이라이트 |
| 프레젠테이션 | Timeline 1회 실행, EffectKeyframe → 모든 적에게 순차 적용 |
| VFX `Target` 타입 | 적 수만큼 VFX 스폰 (각 적 위치에 개별 스폰) |
| VFX `None` 타입 (신규) | 고정 위치에 1개 스폰, 타임라인 키프레임으로 위치 조절 |
| `SkillPresentationPlaybackContext` | `Targets (IReadOnlyList<Agent>)` 추가, `Target` (primary) 유지 |
| `SkillEffectExecutionService` | `context.Targets` 루프로 모든 타겟에 이펙트 적용 |

---

## 변경 파일 목록

### Phase 1 — CardTargetType + Targeting UI + BattleActionExecutor

| 파일 | 변경 내용 |
|------|-----------|
| `Battle/Enums/CardTargetType.cs` | `AllEnemies = 3` 추가 |
| `Battle/UI/BattleTargetingController.cs` | AllEnemies case → 모든 적 TargetingModule 유효 표시 |
| `Battle/UI/BattleActionExecutor.cs` | AllEnemies 카드 감지 → RuntimeEnemyRegistrySO에서 전체 적 수집 → UseSkillAsync에 allTargets 전달 |
| `CombatSystem/Skills/SkillModule.cs` | `UseSkillAsync` 에 `IReadOnlyList<Agent> allTargets = null` optional 파라미터 추가 |

### Phase 2 — Presentation Context + Effect 적용

| 파일 | 변경 내용 |
|------|-----------|
| `Battle/Presentation/SkillPresentationPlaybackContext.cs` | `IReadOnlyList<Agent> Targets` 프로퍼티 추가, 생성자 확장 |
| `Battle/Presentation/SkillEffectExecutionService.cs` | `context.Targets` 루프로 모든 타겟에 이펙트 적용 |

### Phase 3 — VFX None 타입 + 다중 타겟 VFX

| 파일 | 변경 내용 |
|------|-----------|
| `Battle/Presentation/SkillTimelineKeyframes.cs` | `SkillVfxSpawnTarget.None` 추가 |
| `Battle/Presentation/SkillVfxExecutionService.cs` | `Target` 타입 → context.Targets 순회 VFX; `None` 타입 → 단일 고정 위치 VFX |

---

## 아키텍처 상세

### BattleActionExecutor 흐름 (AllEnemies)

```
CardDroppedOnTargetEvent 수신
→ card.data.targetType == AllEnemies
→ _enemyRegistry.Enemies → List<Agent> allTargets 수집
→ UseSkillAsync(data, targetGo, ct, allTargets)
```

### SkillModule.UseSkillAsync 확장

```csharp
public async UniTask UseSkillAsync(
    SkillUsageData data,
    GameObject target,
    CancellationToken ct = default,
    IReadOnlyList<Agent> allTargets = null)
```
- `allTargets == null` → 기존 단일 타겟 동작 (Targets = [targetAgent])
- `allTargets != null` → context.Targets = allTargets, context.Target = primary

### SkillVfxExecutionService 확장 (Phase 3)

- `SpawnTarget.Target` → context.Targets 루프, 각 agent마다 VFX 스폰
- `SpawnTarget.None` → 단일 VFX at Vector3.zero + spawnPositionOffset (타임라인 키프레임으로 이동)
- `SpawnTarget.Caster`, `BetweenCasterAndTarget` → 기존 동작 유지

---

## 페이즈별 구현 계획

### ✅ Phase 1 — CardTargetType + Targeting UI + BattleActionExecutor (완료)
- AllEnemies 타겟팅 시 모든 적 하이라이트 + 오버레이 앞 렌더링 (`BattleCameraController` AllEnemies 케이스 추가)
- 호버 시 전체 적 아웃라인 굵어짐 (`BattleTargetingController` AllEnemies 분기 처리)
- 드롭 시 카드 소비 정상 동작
- 주요 버그: `BattleActionExecutor.enemyRegistry` Inspector 미연결 → 수동 할당으로 해결

### ✅ Phase 2 — Presentation Context + Effect 적용 (완료)
- `SkillEffectExecutionService.Execute()`: BlockEffect는 Caster에만, 나머지는 `context.Targets` 전체 루프
- AllEnemies 카드 드롭 → 전체 적에게 데미지 적용 확인

### ✅ Phase 3 — VFX None 타입 + 다중 타겟 VFX (완료)
- `SkillVfxSpawnTarget.None` 추가 (`SkillTimelineKeyframes.cs`)
- `SkillVfxExecutionService`: `Target` → context.Targets 루프 다중 스폰; `None` → `_layout.noneVfxSpawnPosition` 단일 스폰
- `SkillPreviewLayoutSO.noneVfxSpawnPosition` 필드 추가
- `BattleSceneInstaller`: `SkillVfxExecutionService` 팩토리에 `skillPreviewLayout` 주입
- 에디터 프리뷰(`SkillPreviewScene.ResolveSpawnPosition`): `None` 케이스 → `layout.noneVfxSpawnPosition` 반환 (casterPos fallback 버그 수정)
- Inspector 연결 필요: `BattleSceneInstaller`의 `Skill Preview Layout` 슬롯에 `SkillPreviewLayoutSO` 에셋 할당
