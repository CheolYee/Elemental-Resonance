# 스킬 연출 시스템 개편

작성일: 2026-06-15  
상태: Phase 1~4 전체 완료

---

## 배경 및 목표

피드백: 연출이 너무 길고 UI가 닫혀서 연속 드로우 속도감이 저하됨.

**목표:**
- 기본 카드는 연출 1초 이내, UI 유지 → 연속 드로우 가능
- 강력한 스킬은 `HideBattleUI` 키프레임으로 UI를 직접 제어 → 연출 집중도 극대화
- 카드 실행 큐 도입 → 연출 중 드롭한 카드를 순서대로 자동 실행

---

## 확정 설계

### 큐 방식 (BattleActionExecutor)
- 카드 드롭 → 코스트 즉시 차감 → 큐에 추가
- 큐 루프: 앞 연출 완료 → 다음 카드 자동 실행
- 손패 잠금은 유지 (타겟팅 애니메이션 충돌 방지)

### SkillExecutionStartEvent/EndEvent 역할 축소
- 기존: HandLayout + BattleDockSlide + CostDisplay + TurnEnd + CardPileButton 전부 잠금
- 변경: **HandLayout 잠금/해제만**
- BattleDockSlide, CostDisplay, TurnEnd, CardPileButton은 SkillExecution 이벤트에서 분리

### UI 슬라이드 아웃 → HideBattleUI/ShowBattleUI 키프레임 제어
- `HideBattleUI` 키프레임: BattleDockSlide + CostDisplay + TurnEnd + CardPileButton 슬라이드 아웃 + HandLayout 추가 잠금
- `ShowBattleUI` 키프레임: 전부 복구
- 강력한 스킬 타임라인에만 이 키프레임 삽입

### SFX 데이터 모델
- `SkillKeyframeData.sfxId` (string) → `SfxSounds` (enum)으로 교체
- 에디터 Inspector: `SfxSounds` 드롭다운
- `SfxSounds` 항목이 0개면 드롭다운 대신 "효과음이 존재하지 않습니다" 라벨

### 기본 카드 연출 가이드라인
- 카메라 키프레임 없음
- VFX 최소화
- 타임라인 총 길이 1초 이내 권장

---

## 신규 추가 요소

| 요소 | 설명 |
|------|------|
| `BattleUIHiddenEvent` | HideBattleUI 키프레임 실행 시 발행 |
| `BattleUIShownEvent` | ShowBattleUI 키프레임 실행 시 발행 |
| `IBattleUIController` | Executor → UI 이벤트 발행 인터페이스 |
| `BattleUIController` | IBattleUIController 구현체, battleEventChannel 소유 |

---

## ~~Phase 1~~ — SFX 데이터 모델 변경 + 에디터 드롭다운 (완료)

**변경 파일:**
- `SkillTimelineKeyframes.cs`: `sfxId` (string) → `sfxSound` (SfxSounds enum)
- `SkillPresentationEditorWindow.Inspector.cs`: SfxId 인스펙터 → SfxSounds 드롭다운 (항목 없으면 라벨)

**완료 기준:**
- Inspector에서 SfxSounds 드롭다운으로 SFX 선택 가능
- SfxSounds enum이 비어있으면 "효과음이 존재하지 않습니다" 라벨 표시
- 기존 타임라인 데이터 string → enum 마이그레이션 (기존 sfxId string이 있으면 None으로 초기화)

---

## ~~Phase 2~~ — UiAction / SFX 키프레임 런타임 실행 (완료)

**신규 파일:**
- `Battle/Events/BattleUIHiddenEvent.cs`
- `Battle/Events/BattleUIShownEvent.cs`
- `Battle/Presentation/IBattleUIController.cs`
- `Battle/UI/BattleUIController.cs` (MonoBehaviour, `[SerializeField] battleEventChannel`, `[SerializeField] soundChannel`)

**변경 파일:**
- `SkillPresentationKeyframeExecutor.cs`: `UiAction` + `SfxSounds` 처리 추가 (생성자에 `IBattleUIController` 주입)
- `BattleSceneInstaller.cs`: `IBattleUIController` 등록

**완료 기준:**
- HideBattleUI 키프레임 실행 시 BattleUIHiddenEvent 발행 확인
- ShowBattleUI 키프레임 실행 시 BattleUIShownEvent 발행 확인
- SfxSounds 키프레임 실행 시 PlaySoundEvent 발행 확인

---

## ~~Phase 3~~ — SkillExecutionEvent 역할 축소 + UI 이벤트 구독 재편 (완료)

**변경 파일:**
- `BattleDockSlide.cs`: SkillExecutionStart/End 제거 → BattleUIHidden/ShownEvent 구독
- `CostDisplayPanel.cs`: SkillExecutionStart/End 제거 → BattleUIHidden/ShownEvent 구독
- `TurnEndButton.cs`: SkillExecutionStart/End 제거 → BattleUIHidden/ShownEvent 구독
- `CardPileButton.cs`: SkillExecutionStart/End 제거 → BattleUIHidden/ShownEvent 구독
- `HandLayoutController.cs`: SkillExecutionStart/End 유지 (손패 잠금 책임 유일)

**완료 기준:**
- 기본 카드(HideBattleUI 키프레임 없음) 사용 시 BattleDockSlide/CostDisplay/TurnEnd가 슬라이드 아웃되지 않음
- HideBattleUI 키프레임이 있는 타임라인 실행 시 해당 시점에 UI 슬라이드 아웃
- ShowBattleUI 키프레임 실행 시 UI 복구

---

## ~~Phase 4~~ — BattleActionExecutor 큐 방식 전환 (완료)

**변경 파일:**
- `BattleActionExecutor.cs`: `_isExecuting` 제거 → `Queue<CardDroppedOnTargetEvent>` 추가, 큐 루프 구현

**큐 동작:**
```
OnCardDropped:
  costModel.currentCost -= card.cost  // 코스트 즉시 차감
  RaiseEvent(CostChangedEvent)
  Enqueue(evt)
  if (!_isRunning) StartQueue()

StartQueue():
  _isRunning = true
  while (queue.Count > 0):
    evt = Dequeue()
    RaiseEvent(SkillExecutionStartEvent)
    await UseSkillAsync(...)
    deckController.UseCard(...)
    RaiseEvent(SkillExecutionEndEvent)
  _isRunning = false
```

**완료 기준:**
- 연출 중 카드 드롭 시 코스트 즉시 차감 후 큐 대기
- 앞 연출 완료 후 자동으로 다음 카드 연출 시작
- 배틀 종료(BattleVictory/Defeat) 시 큐 클리어

---

## 주의사항

- `BattleUIHiddenEvent`/`BattleUIShownEvent`는 `SkillExecutionEnd` 이후에도 UI가 Show 상태여야 하므로, ShowBattleUI 키프레임 없이 연출이 끝나면 자동 복구되지 않음 → 강력한 스킬 타임라인에 반드시 ShowBattleUI 키프레임 포함할 것
- 큐 방식에서 배틀 승리 판정은 각 스킬 효과 적용 직후 일어나므로 큐 중간에 배틀이 끝날 수 있음 → `_battleEnded` 체크를 큐 루프 내에서도 수행
