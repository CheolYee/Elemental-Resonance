# Slay the Spire 식 맵 이동/선택 UI 설계

작성일: 2026-06-10  
상태: Phase 7 완료 / Phase 8 진행 중 (코드 완료, 수동 검증 대기)

---

## 확정된 핵심 설계 결정 요약

- 맵은 상위 흐름. `BattleStageSO`는 전투 전용 데이터 유지, 맵 시스템이 바깥에서 제어.
- 첫 구현은 같은 씬 안에서 맵 UI 오버레이 방식으로 진행. 씬 이동 없음.
- 맵은 UI. `Hidden` / `InspectOnly` / `SelectionPending` / `TransitionInProgress` 상태 머신.
- 노드 타입: `Start`, `Battle`, `Elite`, `Rest`, `Shop`. 데이터 모델은 처음부터 확장형.
- `MapNodeDefinition`: 단일 직렬화 클래스 + 타입별 nullable 참조 (`stageRef`, `restContent`, `shopContent`).
- 노드 연결: 각 노드가 `List<string> nextNodeIds` 직접 보유. 방향은 `floor N → N+1` 단방향.
- 런타임 상태: `RunMapState` (graphId, currentNodeId, pendingNodeId, visitedNodeIds, resolvedNodeIds).
- `CurrentNodeId`는 컨텍스트 진입 완료 후 갱신. 그 전은 `PendingNodeId`.
- `Start` 노드: 런 시작 시 즉시 `Visited + Resolved`. `floorIndex = 0`.
- `SelectionPending` 맵은 유효한 노드 선택 전 닫기 불가. `InspectOnly`는 닫기 가능.
- 노드 시각 우선순위: `TransitionSelected > Current > Selectable > Visited > Locked`.
- `Rest` / `Shop`은 시네머신 카메라 전환 + UI 패널. `나가기` 버튼 하나만.
- `1 run = 1 MapGraphSO`. 층 수는 노드 `floorIndex` 최대값으로 유도.
- 저장/이어하기, 랜덤 생성, 보상 시스템은 이번 범위 제외.

---

## CLAUDE CODE 구현 계획

### 작업 원칙

- Phase 단위로 끊어서 구현. 매 Phase 종료 시 컴파일 에러 0 + 수동 플레이 검증 1회.
- `Rest` / `Shop`은 **카메라 전환 + `나가기` 버튼**까지만.
- 구조 확정 완료. 구현 중 추가 요구가 없는 한 새 아키텍처 열지 않는다.

### 폴더 구조 (생성 완료)

```
Assets/00. Work/_Resources/02. Scripts/Battle/Map/
  Data/     — MapGraphSO, MapNodeDefinition, RestContentSO, ShopContentSO
  Enums/    — MapNodeType, MapOverlayState
  Events/   — Map 전용 이벤트
  Runtime/  — RunMapState, MapRouteRuleService
  UI/       — MapOverlayController, MapFlowController, MapScreenPresenter, MapNodeView, MapLineView, MapTransitionController, MapNodeResolutionService, RestPanelController, ShopPanelController, EnvironmentController
  Editor/   — MapGraphEditorWindow
```

---

### ✅ Phase 1 — 코어 데이터 모델과 순수 상태 로직 (완료)

- `MapNodeType`, `MapOverlayState` enum
- `MapNodeDefinition`, `MapGraphSO` (GetNode/GetStartNode/GetMaxFloorIndex)
- `RestContentSO`, `ShopContentSO` (displayName + descriptionText)
- `RunMapState`, `MapRouteRuleService` (GetInitialSelectableNodeIds/GetSelectableNodeIds/IsRunComplete)
- Map 전용 이벤트 6종 (MapOverlayOpened/Closed, MapNodeSelected/Visited/Resolved, RunCleared)

---

### ✅ Phase 2 — 전투 재진입 가능 구조 리팩터링 (완료)

**신규:**
- `BattleSessionStartEvent` (`Battle.Events`)

**`StageBootstrapper` 변경:**
- `Start()` 자동 실행 제거 → `public async UniTask BeginStage(BattleStageSO stage)` 명시 API
- 진입 시 이전 적 즉시 `Destroy` + `enemyRegistry.Clear()`
- `BattleSessionStartEvent` 발행 → `deckController.Initialize()` → 웨이브 스폰 순서
- `[ContextMenu("Debug: Begin Stage")]` 디버그 진입점

**`Player` 변경:**
- `Start()` → `StateMachine.ChangeState(IDLE)` 로만 변경 (씬 로드 시 등장 애니 없음)
- `BeginEntryAsync()` 신규 추가 — 매 호출마다 `_entryCompletion` 재생성 + ENTRY 상태 전환
- `StageBootstrapper.BeginStage()`가 `_player.BeginEntryAsync()`로 플레이어 등장 제어

**`BattleSessionStartEvent` 구독 추가 → 플래그 리셋:**
- `BattleResultController`: `_battleEnded`, `_isExecuting`, `_pendingWaveClear`, `_livingEnemyCount`
- `BattleTurnController`: `_battleEnded`
- `BattleActionExecutor`: `_battleEnded`, `_isExecuting`
- `TurnEndButton` / `CostDisplayPanel` / `HandLayoutController`: `_battleEnded`, `_waveClearPending`
- `HandDealController`: `_battleEnded`
- `CardPileButton`: `_battleEnded` + `interactable` 복원
- `BattleAnnouncementController`: `_battleEnded`, `_turnCount`
- `CardPileDetailPanel`: `ForceClose()` 호출

---

### ✅ Phase 3 — 맵 오버레이 셸과 모달 입력 차단 (완료)

**목표:** 맵 UI를 화면 최상단 모달 오버레이로 띄우고, `InspectOnly` / `SelectionPending` 상태 전환을 만든다.

**작업:**

- `MapOverlayController` 구현
  - `MapOverlayState` (Hidden / InspectOnly / SelectionPending / TransitionInProgress) 상태 머신
  - `OpenInspect()` / `OpenForSelection()` / `Close()` 공개 API
  - `InspectOnly`에서만 Close 허용. `SelectionPending`에서는 Close 버튼 비활성화.
- `OverLayCanvas`(Sort Order 10) 위에 맵 전용 Canvas 루트 추가 (Sort Order 20)
  - 전체 화면 `Image` (alpha 0.85 정도) + `GraphicRaycaster` → 하위 UI 입력 완전 차단
  - `ScrollRect` (세로 스크롤 전용, horizontal=false)
- TopBar 맵 버튼 (기존 전투 UI TopBar에 추가)
  - `battleEventChannel`의 `BattleVictoryEvent` → `MapOverlayController.OpenForSelection()` 호출 전까지 비활성
  - `SelectionPending`, `TransitionInProgress` 중 비활성화
  - 클릭 시 `MapOverlayController.OpenInspect()`
- `MapFlowController` 골격 구현 (MonoBehaviour, `[SerializeField] MapGraphSO`)
  - `InitializeRun()` — `RunMapState` 초기화, `Start` 노드를 Visited+Resolved 처리
  - `BattleVictoryEvent` 구독 → `MapOverlayController.OpenForSelection()` 호출
  - `BattleDefeatEvent` 구독 → 런 종료 처리 (맵 열지 않음)

**완료 기준:**
- 전투 중 TopBar 버튼으로 조회용 맵을 열고 닫을 수 있다.
- 맵이 열린 동안 아래 UI 클릭/드래그가 완전히 차단된다.
- 승리 후 맵이 `SelectionPending`으로 자동 열리며 닫기 불가가 적용된다.
- 패배 시 맵이 열리지 않는다.

**구현 요약:**
- `MapOverlayController`: Hidden/InspectOnly/SelectionPending/TransitionInProgress 상태 머신, LitMotion 페이드 0.15s
- `MapFlowController`: RunMapState 소유, BattleVictoryEvent → 플래그, BattleResultShownEvent → 0.5s 딜레이 후 OpenForSelection
- `MapTopBarButton`: 상태 기반 interactable, Hidden↔InspectOnly 토글
- `AnnouncementRequest.OnComplete` 콜백 + `BattleResultShownEvent` 추가 — 연출 완료 후 맵 오픈 타이밍 제어
- Canvas 계층: MapCanvas(20) + TopBarCanvas(30) 분리

---

### ✅ Phase 4 — 맵 렌더링과 상태 시각화 (완료)

**목표:** 노드/선/정보 패널을 실제로 렌더링하고 상태 시각 규칙을 붙인다.

**작업:**
- `MapScreenPresenter` — `MapGraphSO + RunMapState` 기반으로 노드·선 생성·갱신
- `MapNodeView` — 노드 아이콘 + 상태별 색상/애니메이션
  - `Locked`: 회색, 인터랙션 없음
  - `Selectable`: 원본 색, 반복 스케일 펄스 (LitMotion)
  - `Visited`: 채도 낮춤
  - `Current`: 외곽선 고정 링, 펄스 없음
  - `TransitionSelected`: 클릭 직후 원형 강조 링
  - `isSelectableVisual`과 `isInteractable` 분리 (InspectOnly에서 시각은 유지, 클릭만 차단)
- `MapLineView` — 연결선 상태별 강조 (지나온/선택가능/미도달/선택중)
- `MapInfoPanelPresenter` (우측 상단 고정)
  - hover 시 노드 타입, 상태, BattleStageSO 이름 (전투 노드), 짧은 설명 표시
  - 비호버 시 `CanvasGroup` 알파 페이드 아웃
  - 1초 안 재호버 시 fade-out 취소 후 즉시 갱신

**완료 기준:**
- 조회 모드에서 맵을 열면 Current/Visited/Selectable/Locked가 한눈에 구분된다.
- hover 정보 패널이 우측 상단에서 안정적으로 갱신된다.

**구현 요약:**
- `MapNodeVisualState` enum 추가 (Locked/Selectable/Visited/Current/TransitionSelected)
- `MapNodeView`: 단일 프리팹 + 상태별 색상/펄스링/고정링, LitMotion 펄스 애니메이션, IPointerEnterHandler/ExitHandler
- `MapLineView`: MPUIKit MPImage Rectangle 회전 방식, 3종 LineState 색상
- `MapInfoPanelPresenter`: hover 시 노드 정보 표시, 1초 딜레이 fade-out (CancellationTokenSource 패턴)
- `MapScreenPresenter`: floorSpacing/nodePadding 기반 레이아웃, Content 크기 자동 설정, Content/노드 anchor 하단(0.5,0) 고정으로 Start가 맵 최하단 배치
- `MapGraphSO`: `[ContextMenu] Auto Generate Node IDs` 추가, nodeId 인스펙터 노출
- `MapFlowController.OpenInspect()` 공개 API로 분리, `MapTopBarButton`이 `MapFlowController` 경유하도록 수정

**Phase 4 추가 개선 (2026-06-14):**
- `MapInfoPanelPresenter` 제거 — hover 정보 패널 불필요 판단으로 삭제
- `MapNodeView` 노드 시각 전면 개편:
  - 펄스 애니메이션 제거. Selectable = 원본 알파 0.6, Locked = 알파 0.25, Visited/Current = 알파 1.0
  - 호버(Selectable만): 스케일 1→1.18 + 알파 0.6→1.0 LitMotion 보간. 호버 종료 시 복귀
  - TransitionSelected: `_pulseRingImage` fillAmount 0→1 + 스케일 1→1.15 (LitMotion 0.35s 원형 드로잉)
  - Visited/Current: `_currentRingImage` 정적 표시
  - `MapScreenPresenter`에 노드 타입별 Color 필드 추가 (`_battleColor` 등), `Setup()`에 색상 전달
- `MapLineView` 대시 방식으로 전환:
  - 단일 Image → 동적 대시 스폰 방식 (`_dashLength`, `_dashGap`, `_lineThickness`)
  - 라인 길이와 무관하게 대시 간격 항상 일정
  - 색상: Visited=알파1.0, Selectable=알파0.6, Locked=알파0.25 (흰색 통일)

---

### ✅ Phase 5 — 노드 선택 연출과 컨텍스트 전환 (완료)

**목표:** `SelectionPending`에서 노드 선택 → 링 연출 → 페이드 → 다음 컨텍스트 진입 흐름 완성.

**구현 요약:**
- `MapTransitionController` / `MapNodeResolutionService` 별도 클래스 없이 `MapFlowController`에 흡수
  - `ExecuteTransitionAsync(nodeId)`: 링 대기 → FadeCanvas 페이드인 → 맵 닫기 + BeginStage → 페이드아웃
  - `TransitionInProgress` 상태 진입으로 전환 중 추가 입력 차단
- `FadeCanvas` (Sort Order 40) 전용 검정 이미지 + CanvasGroup 방식으로 페이드 처리
- `MapOverlayController.EnterTransitionInProgress()` / `CloseForTransition()` 추가
- `MapOverlayController.ApplyState()`: `TransitionInProgress`일 때 `interactable=false` 유지
- `MapScreenPresenter.Refresh()` 시그니처에 `overlayState` 추가 → Selectable 노드 interactable 자동 계산
- `MapScreenPresenter.OnNodeClicked` 이벤트 추가 → `MapFlowController`에서 구독
- `MapNodeView.MarkTransitionSelected()` / `NodeId` 프로퍼티 추가
- `_openForSelectionOnStart` 플래그: 플레이 시작 시 바로 맵 SelectionPending으로 오픈 (테스트용)
- `_ringDuration` / `_fadeInDuration` / `_fadeOutDuration` Inspector 노출 (기본값 0.35s / 0.25s / 0.25s)
- `OnBattleVictory`: `MarkResolved(currentNodeId)` + `IsRunComplete` 시 `RunClearedEvent` 발행

**완료 기준 충족:**
- 맵에서 selectable 노드 클릭 시 링→페이드→Battle/Elite 스테이지 진입 순서 정상.
- 전환 중 추가 클릭/닫기 입력 무시됨.

---

### ✅ Phase 6 — `Rest` / `Shop` 최소 루프 (완료)

**목표:** `Rest`/`Shop` 진입 후 `나가기`로 맵 복귀까지 동작.

**구현 요약:**
- `RestPanelController` / `ShopPanelController`: `GameObject.SetActive` → `CanvasGroup` 방식 전환
  - `SetPanelVisible(bool)`: alpha/interactable/blocksRaycasts 일괄 제어
  - 나중에 카메라 연출 + LitMotion 페이드 확장 가능한 구조로 설계
- `MapFlowController`에 `CinemachineBrain` 추가:
  - `WaitForCameraBlend(ct)`: Priority 변경 후 `NextFrame` + `WaitUntil(!IsBlending)` 대기
  - Rest/Shop Open 후, Close 후 각각 호출 → 카메라 블렌드 완료 후 페이드아웃 시작
  - `_cinemachineBrain` null 시 대기 없이 기존 동작 유지

**완료 기준 충족:**
- `Rest`/`Shop` 노드 진입 후 `나가기`를 누르면 맵 선택 모드로 돌아온다.
- 카메라 전환이 페이드 아웃 전에 완료되어 화면이 열릴 때 카메라가 이동 중이지 않다.

---

### ✅ Phase 7 — `MapGraphEditorWindow` authoring 도구 (완료)

**목표:** 수동 맵 제작을 빠르게 할 수 있는 에디터 창.

**구현 요약:**
- `MapGraphEditorWindow` + partial 5개 (`Layout`, `Canvas`, `Inspector`, `Validation`, `Actions`)
- **캔버스**: IMGUI + Handles.DrawBezier, 배경색 `(0.11,0.13,0.18)`, 층 구분선 7px
- **노드**: 55×55 정사각형, 타입별 색상, 포트(상/하) 드래그로 연결선 생성
- **연결선**: 베지어 곡선, 기본 5px 흰색 / 선택 시 7px 청록색
- **입력**: 좌클릭 노드 선택·드래그, 우클릭 컨텍스트 메뉴(노드 추가/삭제), 연결선 클릭 선택
- **단축키 (IMGUI EventType.KeyDown)**: Delete/Backspace 노드·연결 삭제, Ctrl+Z/Y Undo/Redo, Ctrl+C/V 노드 복사·붙여넣기
- **스냅**: 툴바 토글 + Step FloatField (기본 off, step=0.1)
- **툴바**: MapGraphSO ObjectField, New MapGraphSO 버튼, Map Width / Floor Spacing 편집 (런타임과 동일한 스케일)
- **Inspector (UIToolkit)**: nodeType EnumField, floorIndex IntegerField, nodeId 읽기전용 Label, stageRef ObjectField (Battle/Elite만 표시), 변경 시 Undo+SetDirty+Validation 즉시 갱신
- **Validation**: Start 1개 강제, same-floor/skip-floor/역방향/끊긴 연결, 중간 층 outgoing 없음, 마지막 층 outgoing 존재, BFS 도달 불가 노드 — 오류 클릭 시 캔버스 포커스 + 빨간 테두리
- **리사이즈 핸들**: Inspector(좌우, 150~500px) / Validation(상하, 60~350px), 호버 시 파란 강조

**완료 기준 충족:**
- `MapGraphSO` 하나를 창 안에서 생성·연결·검증할 수 있다.

---

### Phase 8 — 씬 연결과 수동 검증

**목표:** `Main.unity`에서 런 1회 완주 확인.

**씬 연결 작업 (완료):**
- 맵 오버레이 Canvas 루트 씬에 배치
- TopBar 맵 버튼 → `MapFlowController` 연결 (`_openForSelectionOnStart = false`)
- Rest/Shop 카메라 연결 (`CinemachineBrain`)
- `MapFlowController`에 초기 `MapGraphSO` 연결

**버그 수정 (2026-06-15, 코드 완료):**
- `MapTopBarButton` + `CardPileButton` 상호 잠금 — Pile 패널 열림 중 Map 버튼 잠금, 맵 열림 중 Pile 버튼 잠금
- `BattleSessionStartEvent` ~ `CardDrawEndEvent` 구간 잠금 — 스테이지 시작 연출 중 두 버튼 모두 잠금
  - `MapTopBarButton`: `_battleEventChannel` 필드 추가 (Inspector 연결 필요)
  - `CardPileButton`: `mapOverlayController` 필드 추가 (Inspector 연결 필요)
- `NodeContextEnteredEvent` 신규 추가 — Rest/Shop 진입 시 `BattleDockSlide` / `CostDisplayPanel` / `TurnEndButton` SlideOut 보장

**Inspector 연결 남은 항목:**
- `MapTopBarButton` → `_battleEventChannel` 필드: BattleEventChannel SO 연결
- `CardPileButton` (3개) → `mapOverlayController` 필드: MapOverlayController 오브젝트 연결

**수동 검증 체크리스트:**
- [ ] 게임 시작 시 `Start` 노드가 현재 위치로 보인다
- [ ] 조회용 맵 버튼은 전투/휴식/상점에서 공통 노출
- [ ] 조회용 맵은 닫을 수 있다
- [ ] 승리 후 열린 맵은 닫을 수 없다
- [ ] selectable 노드만 펄스 + 선택 가능
- [ ] 클릭 시 링→페이드→컨텍스트 진입 순서 정상
- [ ] `Battle` 두 번 이상 재진입해도 전투 UI 정상
- [ ] `Rest`/`Shop` 나가기 시 맵 복귀
- [ ] `Rest`/`Shop` 진입 시 코스트·Pile·TurnEnd UI 슬라이드 아웃
- [ ] 마지막 층 완료 시 런 클리어 발생
- [ ] 패배 시 맵 복귀 없이 런 종료
- [ ] 스테이지 시작 연출 중 Map/Pile 버튼 잠금 확인

---

## 주의사항 (전 페이즈 공통)

- 오버레이 입력 차단 누수 주의
- `PendingNodeId`와 `CurrentNodeId` 갱신 타이밍 혼동 주의
- 동적 스폰 오브젝트는 `GameObjectInjector.InjectRecursive(go, _container)` DI 주입 필수
- 구현 범위 밖: 랜덤 맵 생성, 저장/이어하기, 보상 시스템, `Rest`/`Shop` 실제 기능 확장
