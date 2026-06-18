# 범위 공격(AoE) 스킬 시스템 설계

작성일: 2026-06-18
상태: grill-me 완료 — 구현 진행 중

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

### Phase 1 — CardTargetType + Targeting UI + BattleActionExecutor
- 검증: 타겟팅 시 적 전체 하이라이트, 드롭 시 카드 소비

### Phase 2 — SkillPresentationPlaybackContext + Effect 적용
- 검증: AllEnemies 카드 드롭 → 전체 적에게 데미지 적용

### Phase 3 — VFX None 타입 + 다중 타겟 VFX
- 검증: Target 타입 VFX → 각 적 위치에 스폰; None 타입 → 고정 위치 스폰
