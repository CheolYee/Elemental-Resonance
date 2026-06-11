# Claude Code Stage Authoring Walkthrough

이 문서는 `StoryPreviewWindow` 기반 스테이지 어써링 키프레임 에디터를 다른 코딩 에이전트가 빠르게 이해하도록 쓰여 있다.

목표는 세 가지다.

1. 오브젝트 선택이 어디서 시작되고 어떤 상태가 바뀌는지 이해한다.
2. 타임라인 row 가 언제 생기고 어떤 데이터에 연결되는지 이해한다.
3. 키프레임 값을 어디서 수정하고, 그 값이 어떤 저장 함수로 내려가는지 이해한다.

같이 넣은 스크립트 `scripts/Show-StageAuthoringFlow.ps1` 를 먼저 실행하면 현재 번들 안에서 핵심 메서드의 파일/라인 위치를 바로 볼 수 있다.

실행 예시:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Selection
powershell -ExecutionPolicy Bypass -File .\scripts\Show-StageAuthoringFlow.ps1 -Section Timeline
```

## 1. 가장 먼저 잡아야 할 정신 모델

이 에디터는 세 덩어리로 움직인다.

```text
StoryPreviewWindow
├─ 왼쪽 위: Stage Preview Viewport
├─ 왼쪽 아래: Timeline Panel
└─ 오른쪽: Object List + Inspector
```

실제로는 아래처럼 조립된다.

- 상단 바: `source/Editor/StoryPreviewWindow.Layout.cs`의 `BuildTopBar()`
- 메인 왼쪽 패널: `BuildLeftPanel()`
- 스테이지 월드: `BuildStoryRenderingRoot()`
- 타임라인 패널: `BuildTimelinePanel()`
- 오른쪽 오브젝트 목록/인스펙터: `source/Editor/StoryPreviewWindow.Inspector.cs`의 `BuildActorInspector()`

핵심은 `_selectionKind` 이다.

- 선택 종류는 `StoryPreviewWindow.cs` 안의 `StageSelectionKind`
- 값은 `Actor`, `Background`, `Camera`, `Sound`, `None`
- 타임라인과 인스펙터는 거의 전부 `_selectionKind` 를 보고 다시 그려진다

즉, 이 에디터는 "현재 선택된 오브젝트 타입" 하나를 중심으로 전체 UI 가 갈아끼워지는 구조다.

## 2. 현재 라인과 source of truth

이 에디터는 별도 preview 전용 asset 을 쓰지 않는다. 실제 authoring 데이터가 곧 preview source of truth 다.

주요 데이터 경로는 아래다.

- 현재 선택된 라인: `StoryPreviewWindow._currentLine`
- 현재 라인 안의 스테이지 모듈: `StoryStageLayoutModuleSO`
- 현재 라인의 절대 스냅샷 상태
  - 액터: `StoryActorStateData`
  - 배경: `StoryBackgroundStateData`
  - 카메라 기본값: `StoryCameraTrackData.defaultState`
- 현재 라인의 intra-line 트랙 상태
  - 액터 트랙: `StoryActorTrackData`
  - 배경 트랙: `StoryBackgroundTrackData`
  - 카메라 트랙: `StoryCameraTrackData.keyframes`
  - 사운드 트랙: `StorySoundTrackData`

현재 라인의 스테이지 모듈을 찾는 함수는 아래다.

- `source/Editor/StoryPreviewWindow.Inspector.cs`
- `FindCurrentStageLayout()`
- `GetOrCreateCurrentStageLayout(...)`

이 함수들이 중요하다. 모든 편집이 결국 현재 라인의 `StoryStageLayoutModuleSO` 를 읽고, 없으면 만들고, 거기에 다시 쓴다.

## 3. 라인을 선택했을 때 무슨 일이 일어나는가

라인 선택의 시작점은 보통 그래프 에디터다.

1. `source/Editor/StoryGraphEditorWindow.cs`
2. 노드 선택이 바뀌면 `OnCanvasSelectionChanged(...)`
3. 여기서 `StoryPreviewWindow.NotifyLineSelected(previewLine)` 호출
4. `source/Editor/StoryPreviewWindow.cs` 의 `NotifyLineSelected(...)`
5. 열린 프리뷰 창에 `OnExternalLineSelected(line)` 전달
6. line 이 있으면 `ShowLineSnapshot(line)` 호출
7. `source/Editor/StoryPreviewWindow.Playback.cs` 의 `ShowLineSnapshot(...)`
8. 여기서 `_currentLine = line`
9. `BuildStageStateAt(line)` 로 누적 스테이지 상태 재구성
10. `RebuildActorLayer()`, `RefreshActorInspector()`, `RefreshDialogue()`, `RefreshChoices()`

여기서 실제 누적 상태를 만드는 함수가 중요하다.

- `BuildStageStateAt(...)`
- `ApplyStageModulesToState(...)`

이 로직은 에피소드 entry line 부터 현재 line 까지 따라오면서 각 line 의 `StoryStageLayoutModuleSO` 를 적용해 `_stageState` 와 `_bgState` 를 만든다.

즉, 타임라인은 "현재 line 의 track" 만 편집하지만, preview stage 자체는 "현재 line 에 도달했을 때의 누적 결과" 를 먼저 만든 뒤 그 위에 key sampling 을 덮는 구조다.

## 4. 오브젝트 하이러키는 실제로 어디에 있는가

이 에디터에는 Unity Hierarchy 같은 별도 TreeView 가 없다.

오브젝트 하이러키 역할은 오른쪽 패널의 `Stage Actors` 섹션이 맡는다.

생성 위치:

- `source/Editor/StoryPreviewWindow.Inspector.cs`
- `BuildActorInspector()`
- `_actorListRoot`

목록을 채우는 함수:

- `RefreshActorList()`

이 함수가 아래 순서로 row 를 만든다.

1. `BuildBackgroundRow()`
2. `BuildCameraRow()`
3. `BuildSoundRow()`
4. `_stageState` 를 순회하며 각 actor row 생성

즉, "하이러키 선택" 이라는 개념은 사실 아래 네 타입 row 중 하나를 고르는 것과 같다.

- Background
- Camera
- Sound
- Actor 인스턴스들

## 5. 오브젝트를 선택하면 어떤 함수가 실행되는가

### 5-1. Actor 선택

선택 진입점은 두 군데다.

- 오른쪽 목록 row 클릭: `RefreshActorList()` 내부 `row.RegisterCallback(...)`
- 스테이지 뷰 액터 클릭: `source/Editor/StoryPreviewWindow.Actors.cs` 의 `RegisterActorInteraction(...)`

둘 다 결국 `SelectActor(actorKey)` 로 수렴한다.

`SelectActor(...)` 가 하는 일:

1. `_selectionKind = StageSelectionKind.Actor`
2. `_selectedActorKey = actorInstanceKey`
3. 필요 시 기존 timeline selection clear
4. `RefreshActorList()`
5. `HighlightSelectedActor()`
6. `UpdateCameraGizmoVisual()`
7. `RefreshActorInspector()`
8. `RefreshAuthoringControls()`
9. `RefreshTimelinePanel()`

즉, Actor 선택 후 타임라인과 인스펙터는 모두 actor 문맥으로 재구성된다.

### 5-2. Background 선택

진입점:

- 오른쪽 목록: `BuildBackgroundRow()`
- 스테이지 뷰 배경 클릭/드래그: `source/Editor/StoryPreviewWindow.Background.cs` 의 `RegisterBackgroundInteraction(...)`

수렴점:

- `SelectBackground()`

이 함수는 actor 선택과 동일하게 `_selectionKind = Background` 로 바꾸고, 이후 `RefreshTimelinePanel()` 과 `RefreshActorInspector()` 를 호출한다.

### 5-3. Camera 선택

진입점:

- 오른쪽 목록: `BuildCameraRow()`
- 스테이지 뷰 카메라 gizmo 클릭: `source/Editor/StoryPreviewWindow.Actors.cs` 의 `HandleCameraGizmoPointerDown(...)`

수렴점:

- `SelectCamera()`

카메라는 약간 특이하다.

- 첫 클릭은 주로 "선택" 역할
- 이미 카메라가 선택된 상태에서 다시 drag 하면 gizmo drag 가 시작된다

즉, 클릭 한 번에 선택과 드래그를 동시에 바로 시작하지 않도록 분리돼 있다.

### 5-4. Sound 선택

진입점:

- 오른쪽 목록: `source/Editor/StoryPreviewWindow.Sound.cs` 의 `BuildSoundRow()`

수렴점:

- `SelectSound()`

사운드는 stage viewport 오브젝트가 아니라 timeline/inspector 중심 타입이다. 그래서 선택 진입점도 목록 row 쪽이 핵심이다.

## 6. 선택 후 타임라인에 무엇이 표시되는가

타임라인 전체 재구성 진입점은 항상 같다.

- `source/Editor/StoryPreviewWindow.Timeline.cs`
- `RefreshTimelinePanel()`

이 함수는 `_selectionKind` 를 보고 분기한다.

- Actor 선택이면 `BuildActorTimelineRows()`
- Background 선택이면 `BuildBackgroundTimelineRows()`
- Camera 선택이면 `BuildCameraTimelineRows()`
- Sound 선택이면 `BuildSoundTimelineRows()`

그리고 현재 selection 에서 참조할 실제 key 리스트는 `GetCurrentTimelineKeyframes()` 가 결정한다.

### 6-1. Actor 선택 시 표시 row

파일:

- `source/Editor/StoryPreviewWindow.Timeline.cs`
- `BuildActorTimelineRows()`

표시 기준:

- 현재 actor 의 `StoryActorTrackData.keyframes`
- property 존재 여부는 `HasProperty(...)` 로 판정

보이는 row:

- `Position`
- `Scale`
- `Expression`

즉, actor track 안에 해당 property 의 key 가 하나라도 있어야 row 가 보인다.

### 6-2. Background 선택 시 표시 row

파일:

- `BuildBackgroundTimelineRows()`

보이는 row:

- `Cut`
- `Position`
- `Scale`

전제:

- 현재 line 에 background state 가 있어야 함
- `StoryBackgroundTrackData.keyframes` 안에 각 property key 가 있어야 row 가 보임

### 6-3. Camera 선택 시 표시 row

파일:

- `BuildCameraTimelineRows()`

보이는 row:

- `Target`
- `Offset`
- `Zoom`

전제:

- `StoryCameraTrackData.keyframes` 안에 해당 property key 가 있어야 함

### 6-4. Sound 선택 시 표시 row

파일:

- `source/Editor/StoryPreviewWindow.Sound.cs`
- `BuildSoundTimelineRows()`

보이는 row:

- `BGM`
- `SFX`

Actor/Background/Camera 와 다르게 사운드는 `StoryActorKeyframeData` 를 직접 저장하지 않는다.

- 실제 저장은 `StoryBgmKeyframeData`
- 실제 저장은 `StorySfxKeyframeData`
- timeline selection 용으로만 proxy `StoryActorKeyframeData` 를 만들어 쓴다

즉, sound timeline 은 "저장 모델" 과 "timeline selection 모델" 이 분리돼 있다.

이게 사운드 포팅에서 가장 놓치기 쉬운 포인트다.

## 7. 프로퍼티 row 는 어떻게 생성되는가

### 7-1. Add Property 버튼에서 시작

타임라인 툴바는 `BuildTimelinePanel()` 에서 만들어진다.

여기서 중요한 버튼:

- `Add Property`
- `Add Key`
- `Remove Key`

`Add Property` 버튼은 `ShowAddPropertyMenu()` 를 연다.

이 함수는 `_selectionKind` 기준으로 메뉴 구성을 바꾼다.

- Actor: `Position`, `Scale`, `Expression`
- Background: `BackgroundCut`, `BackgroundPosition`, `BackgroundScale`
- Camera: `CameraTarget`, `CameraOffset`, `CameraZoom`
- Sound: `SoundBgm`, `SoundSfx`

여기서 중요한 규칙:

- 이미 존재하는 property row 는 `AddPropertyMenuItem(...)` 에서 disabled 된다
- row 는 "빈 row 생성" 개념이 아니라 "첫 key 생성" 개념으로 만들어진다

즉, property row 는 keyframe 데이터가 생긴 결과로 나타난다.

### 7-2. AddPropertyKey -> AddTimelineKeyAtPlayhead

메뉴에서 property 를 고르면 아래 순서로 간다.

1. `AddPropertyKey(property)`
2. `_selectedTimelineProperty = property`
3. `AddTimelineKeyAtPlayhead()`

`AddTimelineKeyAtPlayhead()` 는 selection type 에 따라 다른 저장 함수를 부른다.

- Actor: `AddOrUpdateKey(...)`
- Background: `AddOrUpdateBackgroundKey(...)`
- Camera: `AddOrUpdateCameraKey(...)`
- Sound: `AddSoundKeyAtPlayhead(...)`

이 시점에 "현재 playhead 시간" 에 첫 key 가 생긴다.

그리고 다음 `RefreshTimelinePanel()` 에서 `HasProperty(...) == true` 가 되므로 row 가 보이게 된다.

핵심은 이거다.

- row 는 UI-only 엔티티가 아니다
- row 는 underlying keyframe list 에 어떤 property 가 존재하느냐의 시각화 결과다

## 8. row 위 빈 공간을 눌렀을 때 무슨 일이 일어나는가

각 row 의 오른쪽 lane 은 `CreateTimelineLane(...)` 에서 만든다.

여기서 입력 동작이 갈린다.

### 8-1. 우클릭

우클릭이면:

1. `_selectedTimelineProperty = property`
2. playhead 를 해당 x 위치 시간으로 이동
3. `ShowRowKeyContextMenu(property, time)` 호출
4. 사운드면 `ShowSoundRowKeyContextMenu(...)`

여기서 메뉴 항목은 보통 `Add/Update ... Key @ time` 이다.

즉, "특정 row 의 특정 시간에 key 하나를 찍는" 가장 직접적인 경로다.

### 8-2. 좌클릭

좌클릭이면:

- key 선택 박스 드래그 시작
- 또는 playhead drag 시작

즉, lane 자체는 "박스 선택 / playhead 이동 / 우클릭 생성" 이 세 역할을 맡는다.

## 9. 키프레임은 어떻게 선택되는가

### 9-1. 일반 key 선택

일반 key marker 는 `AddActorKeyMarker(...)` 에서 만든다.

선택 시 일어나는 일:

1. `SetInteractionContext(InteractionContext.Timeline)`
2. 타임라인 focus
3. 단일 선택이면 `SelectSingleTimelineKey(...)`
4. Ctrl/Cmd 면 `ToggleTimelineKeySelection(...)`
5. playhead 를 key 시간으로 이동
6. `ApplyTimelinePlayheadSample()`
7. `RefreshActorInspector()`
8. `RefreshTimelinePanel()`

즉, key 선택은 timeline selection state 와 inspector contents 를 동시에 바꾼다.

### 9-2. 사운드 key 선택

사운드는 `HandleSoundKeyPointerDown(...)` 에서 처리한다.

일반 key 와 거의 같은 흐름이지만 추가로:

- `_selectedSoundRowKind`
- `_selectedBgmKey`
- `_selectedSfxKey`

를 유지한다.

그리고 proxy key selection 을 실제 `StoryBgmKeyframeData` / `StorySfxKeyframeData` 로 역매핑하는 함수가 있다.

- `GetOrCreateSoundKeyProxy(...)`
- `ResolveBgmKeyFromProxy(...)`
- `ResolveSfxKeyFromProxy(...)`
- `UpdateSelectedSoundKeysFromTimelineSelection()`

사운드를 포팅할 때는 이 proxy layer 를 이해하지 못하면 selection 과 inspector 편집이 어긋난다.

## 10. 인스펙터는 어떻게 바뀌는가

오른쪽 상세 인스펙터 재구성의 중심은 `RefreshActorInspector()` 다.

이 함수는 selection 에 따라 완전히 다른 UI 를 그린다.

- `Background` 선택이면 `BuildBackgroundInspector()`
- `Camera` 선택이면 `BuildCameraInspector()`
- `Sound` 선택이면 `BuildSoundInspector()`
- `Actor` 선택이면 actor snapshot 필드 + selected key inspector + selected segment inspector

즉, 이 패널은 "오브젝트 인스펙터" 와 "선택된 키프레임 인스펙터" 를 같은 영역에 합쳐 놓은 구조다.

### 10-1. Actor 선택 시 인스펙터

기본 snapshot 필드:

- Character
- Stage Position
- Scale Mult
- Visible
- Focused
- Sort Order

그 아래 timeline selection 상태에 따라 추가 패널이 붙는다.

- 단일 key 선택: `BuildSelectedTimelineKeyInspector()`
- 다중 key 선택: `BuildSelectedTimelineGroupInspector()`
- segment 선택: `BuildSelectedTimelineSegmentInspector()`

### 10-2. Selected Key Inspector 가 실제로 수정하는 값

`BuildSelectedTimelineKeyInspector()` 는 key property 별로 다른 필드를 그린다.

Actor key:

- `Position` -> `key.stageLocalPosition`
- `Scale` -> `key.scale`
- `Expression` -> `key.expression`

Background key:

- `BackgroundCut` -> `key.background`, `key.backgroundKey`
- `BackgroundPosition` -> `key.stageLocalPosition`
- `BackgroundScale` -> `key.scale`

Camera key:

- `CameraTarget` -> `key.cameraTargetActorKey`, `key.cameraFollowMode`, `key.cameraSnapshotNormalizedPosition`
- `CameraOffset` -> `key.cameraStageLocalPosition`
- `CameraZoom` -> `key.cameraZoom`

실제 저장 호출은 대부분 `SaveCurrentTimelineKeyframes(...)` 안에서 이뤄진다.

중요한 공통 후처리:

- `ApplyTimelinePlayheadSample()`
- `RefreshTimelinePanel()`
- 필요 시 `RefreshActorInspector()`

즉, 인스펙터에서 값을 바꾸면 preview stage 와 timeline marker 모두 즉시 갱신된다.

### 10-3. Segment Inspector

segment bar 선택은 `HandleSegmentPointerDown(...)` 에서 시작한다.

여기서:

- `SelectTimelineSegment(...)`
- 우클릭이면 `ShowEasingMenu(...)`
- 좌클릭이면 `BuildSelectedTimelineSegmentInspector()`

segment inspector 에서는 사실상 easing 을 편집한다.

- field: `EnumField("Easing", toKey.easing)`

즉, "키 자체 값" 과 "키 사이 구간의 easing" 이 분리돼 있다.

### 10-4. Sound Inspector

사운드는 `BuildSoundInspector()` 로 들어간다.

단일 BGM key 선택 시:

- `BuildSelectedBgmKeyInspector(...)`
- 편집 가능 값:
  - Time
  - Operation
  - BGM enum
  - Transition

단일 SFX key 선택 시:

- `BuildSelectedSfxKeyInspector(...)`
- 편집 가능 값:
  - Time
  - SFX enum

사운드는 일반 key inspector 와 다르게 `SaveSoundTrackToCurrent(...)` 로 저장된다.

## 11. 키 시간 drag 는 어떻게 동작하는가

key 를 드래그하면 시작점은 marker 쪽이다.

- 일반 key: `AddActorKeyMarker(...)`
- 사운드 key: `HandleSoundKeyPointerDown(...)`

실제 이동은 `OnTimelinePointerMove(...)` 가 맡는다.

### 11-1. 단일 key drag

1. key marker 클릭
2. `_isTimelineKeyDragging = true`
3. `_draggingTimelineKeyIndex`, `_draggingTimelineKeyStartTime` 저장
4. pointer move 중 `SetTimelineKeyTime(...)`
5. playhead 갱신
6. `ApplyTimelinePlayheadSample()`
7. `RefreshTimelinePanel()`

### 11-2. 다중 key drag

1. 여러 key 선택
2. drag 시작 시 `BeginTimelineGroupKeyDrag(...)`
3. move 중 `MoveTimelineGroupKeys(...)`
4. 각 key 에 deltaTime 적용
5. collision 검사 후 timeSeconds / normalizedTime 갱신

### 11-3. 충돌 방지와 스냅

관련 함수:

- `ApplySnap(...)`
- `HasTimelineKeyCollision(...)`

즉, 이 타임라인은 자유 드래그지만 다음 두 규칙을 유지한다.

- snap 이 켜져 있으면 시간은 스냅 간격에 맞춤
- 같은 row/property/time 충돌은 막음

## 12. 키프레임 값 수정이 실제 asset 저장으로 내려가는 함수

이 부분이 다른 프로젝트 포팅에서 가장 중요하다.

각 타입별 저장 함수:

- Actor track: `SaveActorTrackToCurrent(...)`
- Background track: `SaveBackgroundTrackToCurrent(...)`
- Camera track: `SaveCameraTrackToCurrent(...)`
- Sound track: `SaveSoundTrackToCurrent(...)`

이 함수들이 공통으로 하는 일:

1. 현재 line 의 `StoryStageLayoutModuleSO` 확보
2. Undo 기록
3. 해당 트랙 보장
4. setter 실행
5. null 제거
6. 시간 순 정렬
7. selection index 복구
8. `MarkLayoutDirty(...)`

즉, "인스펙터에서 field 하나 바꿈" 이나 "키 하나 드래그" 같은 동작도 결국 전부 이 저장 함수 계열을 거친다.

다른 프로젝트로 옮길 때도 반드시 이 계층을 유지하는 것이 좋다.

- UI callback
- typed save function
- normalize/sort/selection repair
- dirty mark

직접 리스트를 만지는 식으로 흩어지면 곧바로 버그가 난다.

## 13. Stage viewport 에서 직접 조작할 때 key 가 생기는 방식

이 에디터는 timeline 만으로 편집하지 않는다. stage viewport 조작도 timeline 과 연결된다.

### 13-1. Actor drag

핵심 함수:

- `source/Editor/StoryPreviewWindow.Actors.cs`
- `RegisterActorInteraction(...)`

pointer up 시 분기:

- 선택된 Position key 가 있으면 `TryApplySelectedTimelineKeyFromState(...)`
- Record 모드면 `RecordActorKeyframeFromState(...)`
- 둘 다 아니면 snapshot 상태 저장 `SaveActorStateToCurrent(...)`

즉, actor drag 는 상황에 따라 세 가지 의미를 가진다.

1. 현재 key 값 수정
2. 현재 playhead 에 record key 생성/갱신
3. line snapshot 자체 수정

### 13-2. Background drag

핵심 함수:

- `RegisterBackgroundInteraction(...)`

pointer up 시 분기:

- Record 모드면 `AddOrUpdateBackgroundKey(...)`
- 선택된 BackgroundPosition key 가 있으면 `TryApplySelectedTimelineKeyFromBackground(...)`
- 아니면 `SaveBackgroundStateToCurrent(...)`

### 13-3. Camera gizmo drag

핵심 함수:

- `HandleCameraGizmoPointerDown(...)`
- `EndCameraGizmoDrag(...)`

pointer up 시 분기:

- Record 모드면 `AddOrUpdateCameraKey(..., CameraOffset, ...)`
- 선택된 CameraOffset key 가 있으면 `TryApplySelectedTimelineKeyFromCamera(...)`
- 아니면 `SaveCameraStateToCurrent(...)`

즉, viewport 조작과 timeline editing 이 완전히 분리돼 있지 않다. 같은 액션이 selection state 와 record state 에 따라 "snapshot 편집" 또는 "keyframe 편집" 으로 바뀐다.

이 점이 3D preview 포팅에서 가장 먼저 복제해야 하는 UX 다.

## 14. Record 모드는 정확히 무엇인가

핵심 함수:

- `ToggleTimelineRecord()`

record 가능한 selection:

- Actor
- Background
- Camera

record 불가:

- Sound

record 를 켜면 선택 대상을 잠그고, viewport 조작이 "현재 playhead 의 key 생성/수정" 으로 바뀐다.

즉, record 모드는 Maya/Sequencer 류와 비슷하게 "scene manipulation -> key write" 브리지 역할을 한다.

## 15. 다른 프로젝트의 3D 스킬 연출 에디터로 옮길 때 유지해야 할 골격

비슷한 구조에서 기능만 다른 편집기를 만들고 싶다면 아래는 그대로 가져가는 편이 좋다.

### 유지할 것

- `_selectionKind` 중심 UI 분기
- 현재 선택 타입별 `GetCurrentTimelineKeyframes()`
- `RefreshTimelinePanel()` -> `Build*TimelineRows()` 구조
- `RefreshActorInspector()` 같은 selection-driven inspector rebuild 구조
- `Save*TrackToCurrent()` 같은 typed save gateway
- viewport 조작이 snapshot/edit/record 로 갈리는 분기

### 바꿔야 할 것

- `StoryActorKeyframeProperty` 를 스킬 연출용 property enum 으로 교체
- 2D stage position / scale 계산
- `StoryTransitionSampler` 의 sampling 수학
- 2D sprite 배치 (`Actors.cs`, `Background.cs`)
- camera gizmo 시각화

### 3D 포팅에서 특히 추천하는 방식

- "오브젝트 선택 / 타임라인 / 인스펙터" 구조는 유지
- preview renderer 만 3D scene graph 로 교체
- key 저장 모델은 2D/3D 와 무관한 plain data 로 유지
- sampler 는 pure function 으로 유지

즉, 렌더링 계층만 바꾸고 편집 계층은 유지해야 한다.

## 16. Claude Code 에게 직접 주면 좋은 읽기 순서

아래 순서대로 읽으라고 하면 가장 빠르다.

1. `source/Editor/StoryPreviewWindow.cs`
2. `source/Editor/StoryPreviewWindow.Layout.cs`
3. `source/Editor/StoryPreviewWindow.Inspector.cs`
4. `source/Editor/StoryPreviewWindow.Timeline.cs`
5. `source/Editor/StoryPreviewWindow.Sound.cs`
6. `source/Editor/StoryPreviewWindow.Actors.cs`
7. `source/Editor/StoryPreviewWindow.Background.cs`
8. `source/Editor/StoryPreviewWindow.Playback.cs`
9. `source/Data/Modules/StoryStageLayoutModuleSO.cs`
10. `source/Data/Modules/StoryActorKeyframeData.cs`
11. `source/Shared/StoryTransitionSampler.cs`

그리고 아래 문장을 같이 주면 이해가 빠르다.

```text
이 에디터는 selection-kind 기반 UI 이다.
타임라인 row 는 빈 UI 가 아니라 underlying keyframe property 존재 여부의 시각화다.
viewport 조작은 snapshot 수정, selected key 수정, record key 생성 중 하나로 동작한다.
사운드는 실제 저장 타입과 timeline selection 타입이 다르므로 proxy key 계층을 먼저 이해해야 한다.
```

## 17. 마지막으로 요약

이 에디터를 한 문장으로 요약하면 이렇다.

`현재 line 의 StageLayoutModuleSO를 source of truth로 삼고, selection kind 하나를 축으로 object list / timeline / inspector / viewport 조작을 모두 다시 그리는 custom keyframe editor`

다른 프로젝트에서 비슷한 구조를 만들 때는 개별 기능보다 아래 순서를 먼저 복제하면 된다.

1. 현재 선택 타입 결정
2. 선택 타입에 맞는 track/key list resolve
3. 그 key list 로 timeline row build
4. key 선택 상태로 inspector build
5. inspector/viewport 입력을 typed save function 으로 내려쓰기
