# 튜토리얼 시스템 설계

작성일: 2026-06-29
상태: 설계 확정 — grill-me 완료

---

## 확정된 설계 요약

| 항목 | 결정 |
|------|------|
| 실행 위치 | Main 씬 내 오버레이 (별도 씬 없음) |
| 진입 트리거 | 타이틀 "튜토리얼" 버튼 + 첫 실행 감지 권장 패널 |
| 커버 범위 | 전투 핵심 5가지 + 맵 흐름 간단 안내 |
| 진행 방식 | 하이라이트 + 말풍선 블로킹 (실제 조작 강제) |
| 데이터 구조 | `TutorialStepSO` + `TutorialSequenceSO` + EventChannel 완료 조건 |
| 하이라이트 | 반투명 오버레이 패널 + 발광 테두리 프레임 |
| 말풍선 위치 | 자동 배치(A) + SO 오프셋 오버라이드(B) 혼합 |
| 스킵 | 구현 (눈에 잘 안 띄는 구석에 작게 배치) |
| 배틀 세팅 | 전용 덱 SO + 전용 맵 SO (사용자가 별도 준비) |
| 진행 상태 | `PlayerPrefs`로 완료 여부만 저장 |
| 확장성 | 새 이벤트 타입 추가 시 SO 설정만 변경, 코드 수정 불필요 |

---

## 튜토리얼 단계 순서 (10단계)

### Phase A — 맵 오버레이 상태 (전투 진입 전)

| 단계 | 하이라이트 대상 | 설명 내용 | 완료 조건 |
|------|----------------|-----------|-----------|
| 1 | 맵 노드 아이콘 전체 | "이 지도를 따라 여행합니다. 각 노드는 전투⚔️ / 상점🛒 / 휴식💤 중 하나입니다." | 클릭으로 넘김 |
| 2 | 첫 번째 전투 노드 | "이 전투 노드를 선택해서 첫 번째 전투를 시작하세요!" | `MapNodeSelectedEvent` 발행 |

### Phase B — 전투 씬 (배틀 진행)

| 단계 | 하이라이트 대상 | 설명 내용 | 완료 조건 |
|------|----------------|-----------|-----------|
| 3 | 적 HP바 + 인텐트 패널 | "적의 HP와 이번 턴 행동 의도를 확인하세요." | 클릭으로 넘김 |
| 4 | 코스트 UI 패널 | "코스트는 매 턴 시작 시 충전됩니다. 카드를 사용하면 차감됩니다." | 클릭으로 넘김 |
| 5 | DrawPile 숫자 버튼 | "카드 더미입니다. 소진되면 버림 더미를 셔플해 자동으로 보충합니다." | 클릭으로 넘김 |
| 6 | 손패 카드 한 장 + 적 | "카드를 적에게 드래그해서 사용하세요!" | `CardDroppedOnTargetEvent` 발행 |
| 7 | 손패 카드 두 장 | "같은 원소끼리 드래그하면 합성됩니다! 카드를 다른 카드 위로 올려보세요." | `CardFusionRequestedEvent` 발행 |
| 8 | 턴 종료 버튼 | "턴 종료 버튼을 눌러 적 턴으로 넘어가세요." | `PlayerTurnEndRequestEvent` 발행 |
| 9 | 전체 화면 (입력 잠금) | "적이 행동 중입니다..." | `PlayerTurnStartEvent` 발행 (적 턴 완료) |
| 10 | 보상 패널 | "전투 승리! 카드 보상을 선택하세요." | `RewardPanelClosedEvent` 발행 |

---

## 아키텍처

### 클래스 구조

```
TutorialStepSO          — 단계 1개 정의 (텍스트, 하이라이트 타겟, 말풍선 설정, 완료 조건 이벤트)
TutorialSequenceSO      — TutorialStepSO 리스트 (순서 정의)
TutorialController      — 시퀀스 실행, EventChannel 구독/해제, 단계 전환 오케스트레이션
TutorialOverlayView     — 반투명 패널 + 발광 테두리 프레임 + 말풍선 UI 제어
TutorialHighlightFrame  — 대상 RectTransform 위치/크기를 읽어 발광 테두리를 배치
TutorialTooltipView     — 말풍선 텍스트 + 자동 배치 + SO 오프셋 오버라이드
```

### TutorialStepSO 필드

```csharp
[CreateAssetMenu(menuName = "Tutorial/Step")]
public class TutorialStepSO : ScriptableObject
{
    [Header("콘텐츠")]
    public string tooltipText;               // 말풍선 텍스트
    public string highlightTargetTag;        // 하이라이트할 GameObject 태그

    [Header("말풍선 위치")]
    public TooltipAnchor preferredAnchor;    // 자동 배치 선호 방향 (Top/Bottom/Left/Right)
    public Vector2 anchorOffset;             // SO 오프셋 오버라이드 (0,0이면 자동만 사용)

    [Header("완료 조건")]
    public TutorialCompletionType completionType; // ClickToContinue | WaitForEvent
    public string completionEventTypeName;   // 완료 조건 이벤트 타입 전체 이름 (예: "Battle.Events.CardDroppedOnTargetEvent")

    [Header("입력 제어")]
    public bool blockAllInput;               // true면 하이라이트 대상 외 모든 입력 차단
}
```

### 완료 조건 이벤트 처리

`TutorialController`는 `TutorialStepSO.completionEventTypeName`을 `Type.GetType()`으로 리졸브해서 `EventChannelSO`에 리플렉션으로 구독합니다. 새 이벤트 타입이 추가돼도 SO 필드값만 바꾸면 코드 수정 없이 완료 조건으로 활용할 수 있습니다.

### Canvas 계층

```
TutorialOverlayCanvas   (Sort Order 99)  — 반투명 검정 패널
TutorialHighlightCanvas (Sort Order 100) — 발광 테두리 프레임 + 말풍선 + 스킵 버튼
```

하이라이트 대상 오브젝트는 원래 캔버스에 그대로 두고, `RectTransform` 월드 좌표를 읽어 `TutorialHighlightCanvas`의 테두리 프레임 위치/크기만 맞춥니다. 오브젝트 이동 없음.

### 진행 상태 저장

```csharp
const string KEY_TUTORIAL_COMPLETED = "tutorial_completed";
PlayerPrefs.SetInt(KEY_TUTORIAL_COMPLETED, 1);  // 완료 시
PlayerPrefs.GetInt(KEY_TUTORIAL_COMPLETED, 0);  // 0이면 미완료
```

### 첫 실행 감지 패널

타이틀 씬에서 `PlayerPrefs.GetInt(KEY_TUTORIAL_COMPLETED, 0) == 0`이면 "처음 하시는 것 같습니다. 튜토리얼을 진행하는 것을 권장합니다." 패널 표시. "예" 클릭 시 튜토리얼 진입, "괜찮습니다" 클릭 시 일반 게임 진입.

### 튜토리얼 진입 흐름

```
타이틀 "튜토리얼" 버튼 클릭
  → TutorialDeckSO를 PlayerDeckProviderSO에 강제 주입
  → TutorialMapGraphSO를 MapGraphSO로 강제 주입
  → DeckBuilding 씬 스킵
  → Main 씬 로드
  → TutorialController.StartTutorial() 호출
  → 단계 1부터 시작
```

---

## 구현 페이즈

### Phase 1 — 튜토리얼 데이터 구조 + 오버레이 기반 UI

**구현 항목:**
- `TutorialCompletionType` enum (`ClickToContinue`, `WaitForEvent`)
- `TooltipAnchor` enum (`Top`, `Bottom`, `Left`, `Right`)
- `TutorialStepSO` ScriptableObject
- `TutorialSequenceSO` ScriptableObject (steps: `List<TutorialStepSO>`)
- `TutorialOverlayView`: 반투명 패널 FadeIn/FadeOut, `CanvasGroup` 제어
- `TutorialHighlightFrame`: 대상 `RectTransform` WorldCorners 읽어 테두리 위치/크기 맞추기
- `TutorialTooltipView`: 텍스트 표시, 선호 방향 자동 배치, SO 오프셋 적용
- `TutorialCanvas` Prefab: `TutorialOverlayCanvas(99)` + `TutorialHighlightCanvas(100)` 구조

**검증:** Unity Editor에서 임의 SO를 만들어 특정 UI 위에 테두리와 말풍선이 올바르게 표시되는지 확인.

---

### Phase 2 — TutorialController + 이벤트 완료 조건

**구현 항목:**
- `TutorialController`: `TutorialSequenceSO` 받아서 단계별 진행
- 리플렉션으로 `completionEventTypeName` → `Type` 리졸브 + `EventChannelSO` 구독
- `ClickToContinue` 타입: 말풍선에 "탭하여 계속" 표시, 클릭 시 다음 단계
- `WaitForEvent` 타입: 이벤트 수신 시 자동으로 다음 단계
- 스킵 버튼: `TutorialHighlightCanvas` 우하단 고정, 클릭 시 `SkipTutorial()` → 완료 처리 + 오버레이 제거
- `PlayerPrefs` 완료 저장

**검증:** 모든 10단계를 순서대로 진행했을 때 막힘 없이 완료되고, 스킵 버튼도 정상 동작 확인.

---

### Phase 3 — 타이틀 연동 + 첫 실행 감지

**구현 항목:**
- 타이틀 씬에 "튜토리얼" 버튼 추가
- 첫 실행 감지 패널 (`TutorialRecommendPanel`): "예" / "괜찮습니다" 버튼
- 튜토리얼 진입 시 `TutorialDeckSO`, `TutorialMapGraphSO` 강제 주입 후 Main 씬 로드
- `TutorialController`가 Main 씬에서 자동 시작되는 진입점 (`ITutorialBootstrapper` 또는 `TutorialSceneInstaller`)

**검증:**
- 첫 실행 시 권장 패널 표시 → "예" → 튜토리얼 정상 시작
- 튜토리얼 완료 후 재시작 시 권장 패널 미표시
- 타이틀 "튜토리얼" 버튼 → 항상 튜토리얼 시작

---

### Phase 4 — 단계별 SO 에셋 생성 + 수동 검증

**구현 항목:**
- 10개 `TutorialStepSO` 에셋 생성 및 설정
- `TutorialSequenceSO` 에셋에 10단계 연결
- 각 하이라이트 대상 오브젝트에 태그 설정
- 튜토리얼 전용 덱 SO / 맵 SO 연결 (사용자 준비 에셋 활용)

**검증:** 처음부터 끝까지 전체 튜토리얼 플레이 완주, 각 단계 조작 강제 동작 확인, 합성 단계(7) 특별 확인.

---

## 확장 가이드

새 카드 효과(버프/디버프) 튜토리얼 단계 추가 방법:
1. 새 이벤트 클래스 생성 (예: `DebuffAppliedEvent`)
2. `TutorialStepSO` 에셋 생성
3. `completionEventTypeName` = `"Battle.Events.DebuffAppliedEvent"` 입력
4. `TutorialSequenceSO`의 steps 리스트에 추가
5. **코드 수정 없음**

---

## 주의사항

- `TutorialController`는 `EventChannelSO`에 구독한 완료 조건 리스너를 단계 전환 시 반드시 해제해야 함 (중복 구독 방지)
- 하이라이트 대상 오브젝트가 비활성 상태이거나 없는 경우 null 체크 후 해당 단계 스킵 처리
- 튜토리얼 중 전투 패배 시: 튜토리얼 취소 + 타이틀로 복귀 (완료 저장 안 함)
- 튜토리얼 전용 맵 SO는 노드가 1~2개로 고정, 첫 노드는 반드시 Battle 타입
