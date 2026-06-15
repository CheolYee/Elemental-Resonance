# 스킬 큐 UI + 턴 종료 잠금 시스템

작성일: 2026-06-15
상태: 전 페이즈 완료 (씬 수동 배치 대기)

---

## 배경 및 목표

- 연속 카드 사용 시 현재/다음 스킬을 TopBar에 아트워크로 표시
- 큐 실행 중 턴 종료 버튼 잠금
- 스킬 실행 시 이름이 화면 중앙 상단에 페이드 인/아웃
- VFX 풀 NullRef 버그 수정

---

## 확정 설계

### 이벤트

| 이벤트 | 발행 시점 | 구독자 |
|--------|-----------|--------|
| `SkillQueueStartedEvent` | 빈 큐에서 첫 카드 진입 시 | TurnEndButton, SkillQueueView |
| `SkillQueueCompletedEvent` | 큐가 완전히 비워질 때 | TurnEndButton, SkillQueueView |
| `SkillQueueChangedEvent(current, pending)` | Enqueue/Dequeue마다 | SkillQueueView, SkillNameFadeView |

### 턴 종료 버튼
- `SkillQueueStartedEvent` → SlideOut + 비활성
- `SkillQueueCompletedEvent` → SlideIn + 활성 (단 _battleEnded, _waveClearPending 체크)

### 스킬 이름 페이드 (`SkillNameFadeView`)
- 위치: 화면 중앙 상단 오버레이 (OverLayCanvas)
- `SkillQueueChangedEvent` 수신 시 current 카드 이름 표시
- LitMotion: `CanvasGroup.alpha` fade in 0.3s → 유지 0.5s → fade out 0.5s

### TopBar 큐 인디케이터 (`SkillQueueView`)
- 레이아웃: `[현재 아트워크] [다음 아트워크] [+N]` 가로
- `SkillQueueStartedEvent` → 표시, `SkillQueueCompletedEvent` → 플레이스홀더로 복귀
- 빈 슬롯: `[SerializeField] Sprite placeholderSprite`
- 1장 대기: 다음 슬롯 = 플레이스홀더, +N 숨김
- 전환 애니메이션: LitMotion 왼쪽 슬라이드 + 페이드 (anchoredPosition + CanvasGroup.alpha)

### VFX NullRef 수정
- `SkillVfxExecutionService.PlayVfxAsync` finally 블록에 `if (container != null)` 가드

---

## ~~Phase 1~~ — VFX NullRef 수정 (완료)

**변경 파일:**
- `SkillVfxExecutionService.cs`: finally 블록 container null 체크

**완료 기준:**
- 씬 전환 또는 배틀 종료 후 VFX NullRef 오류 미발생

---

## ~~Phase 2~~ — 큐 이벤트 + TurnEndButton 잠금 (완료)

**신규 파일:**
- `Battle/Events/SkillQueueStartedEvent.cs`
- `Battle/Events/SkillQueueCompletedEvent.cs`
- `Battle/Events/SkillQueueChangedEvent.cs`

**변경 파일:**
- `BattleActionExecutor.cs`: QueueStarted/Completed/Changed 이벤트 발행 추가
- `TurnEndButton.cs`: SkillQueueStarted/Completed 구독 추가

**완료 기준:**
- 큐 실행 중 턴 종료 버튼 슬라이드 아웃 (클릭 불가)
- 큐 완료 후 턴 종료 버튼 복귀

---

## ~~Phase 3~~ — 스킬 이름 페이드 텍스트 UI (완료)

**신규 파일:**
- `Battle/UI/SkillNameFadeView.cs`

**씬 작업 (수동):**
- OverLayCanvas 하위에 `SkillNameFadeView` GameObject 배치 (중앙 상단)
- TMP_Text + CanvasGroup 컴포넌트 추가, battleEventChannel 연결

**완료 기준:**
- 각 카드 실행 시 카드 이름이 중앙 상단에 페이드 인 → 유지 → 페이드 아웃

---

## ~~Phase 4~~ — TopBar 큐 인디케이터 UI (완료)

**신규 파일:**
- `Battle/UI/SkillQueueView.cs`

**씬 수동 작업:**
- TopBar 하위에 `SkillQueueView` GameObject 배치
- Current 슬롯: `Image` + `CanvasGroup` + `RectTransform` → Inspector 연결
- Next 슬롯: `Image` + `CanvasGroup` + `RectTransform` → Inspector 연결
- Extra Count: `GameObject`(활성/비활성 루트) + `TMP_Text`("+N") → Inspector 연결
- `battleEventChannel`, `placeholderSprite` 연결

**완료 기준:**
- 카드 사용 시 현재/다음 아트워크 표시
- 카드 교체 시 왼쪽 슬라이드 + 페이드 전환
- 큐 완료 후 플레이스홀더로 복귀
