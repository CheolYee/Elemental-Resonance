# 절차적 맵 그래프 생성 시스템 ✅ 완료

**목적**: 매 런마다 맵 노드를 동적으로 생성하여 반복 플레이 가치를 높인다.  
기존 에디터 수동 구성 방식(`MapGraphSO` 에셋 직접 편집)을 대체.

---

## 확정된 설계 결정사항

### 층 구조
| 층(floor) | 내용 |
|-----------|------|
| 0 | Start 노드 1개 고정 |
| 1 ~ 2 | Battle 노드만 (5 또는 7개) |
| 3 ~ 11 | Battle + Elite/Rest/Shop 혼재 가능 (5 또는 7개) |
| 12 | Boss 노드 1개 고정 |

### 층당 노드 수
- **5 또는 7개** (홀수만 — 중앙 정렬 보장)
- 랜덤으로 결정, floor 0과 floor 12는 예외(1개 고정)

### xOffset 배치 (0.12 간격, 중앙 기준)
- 5개: -0.24, -0.12, 0.00, 0.12, 0.24
- 7개: -0.36, -0.24, -0.12, 0.00, 0.12, 0.24, 0.36
- `MapScreenPresenter.GetNodePosition`의 `xOffset * mapWidth` 그대로 사용

### 노드 타입 분포 (floor 3 이상)
- 층당 Elite 최대 1개, Rest 최대 1개, Shop 최대 1개
- 나머지는 Battle로 채움
- 한 층에 여러 특수 타입 공존 가능 (단 각 최대 1개)

### 런 전체 최소 보장 개수
| 타입 | 최소 보장 |
|------|---------|
| Elite | 3개 |
| Rest | 2개 |
| Shop | 2개 |

### Battle 스테이지 난이도 구간
| 구간 | floor 범위 | 풀 |
|------|-----------|-----|
| 저층 | 1 ~ 4 | lowStagePool |
| 중층 | 5 ~ 8 | midStagePool |
| 고층 | 9 ~ 11 | highStagePool |

### 노드 연결 규칙
- **Start 노드(floor 0)**: floor 1의 모든 노드에 연결 (max-outgoing 규칙 예외)
- **일반 노드**: outgoing 최대 2개
- **선 교차 금지**: `(a-k)*(b-l) < 0`이면 교차 → 연결 불허
- **고립 노드 방지**: floor N+1의 모든 노드는 incoming 1개 이상 보장

### 저장/복원
- `RunSaveData`, `RunMapState` 모두 `int seed` 필드 보유
- 런 시작 시 `UnityEngine.Random.Range(0, int.MaxValue)`로 시드 생성
- 복원 시 동일 시드로 `Generate(seed)` 재호출 → 동일 그래프 재현

### 튜토리얼 우회
- `MapFlowController.OverrideMapGraph(graph)` 호출 시 `_skipGenerator = true` 세팅
- `InitializeRun()`에서 `_skipGenerator`가 true면 생성기 건너뜀 → 고정 튜토리얼 맵 사용

---

## 완료된 구현 요약

### Phase 1 — MapGraphGeneratorSO + Generate 알고리즘 ✅
- `MapGraphGeneratorSO.cs` 신규 작성 (`Battle/Map/Data/`)
  - `lowStagePool` / `midStagePool` / `highStagePool` / `eliteStagePool` / `bossStage`
  - `defaultRestContent` / `defaultShopContent`
  - `MapGraphSO Generate(int seed)`: 노드 수 결정 → 보장 배치 → 확률 배치 → xOffset → stageRef → 연결
  - 연결 알고리즘: 비례 매핑 → 고립 노드 수정 → 선택적 2번째 연결
  - `[ContextMenu("Test Generate")]` 검증 도구 포함
- `RunSaveData.cs`에 `public int seed;` 추가

### Phase 2 — MapFlowController 통합 + 저장/복원 ✅
- `MapFlowController`에 `_generator`, `_generatedGraph`, `_skipGenerator` 필드 추가
- `InitializeRun()`: `_generator != null && !_skipGenerator`이면 seed 생성 → `Generate(seed)` → `_mapGraph` 교체
- `RestoreMapState()`: `data.seed`로 그래프 재생성 후 기존 상태 복원
- `OnDestroy()`: `_generatedGraph` 메모리 해제
- `OverrideMapGraph()`: `_skipGenerator = true` 세팅으로 튜토리얼 우회
- `SaveController.Save()`: `seed` 직렬화
- `RunMapState.cs`에 `public int seed;` 추가

### Phase 3 — 수동 검증 ✅
- Elite ≥ 3, Rest ≥ 2, Shop ≥ 2 생성 확인
- 고립 노드 없음, 선 교차 없음 육안 확인
- 저장 → 복원 동일 맵 구조 확인
- 튜토리얼 진입 시 고정 맵 사용 확인
- 층별 난이도 풀 분리 확인 (저/중/고층 스테이지)

---

## 생성된 스테이지 에셋

| 경로 | 내용 |
|------|------|
| `Assets/05. SO/Battle/Stages/Normal/Stage_Low_01~04` | 저층 Battle (goldBonus 15) |
| `Assets/05. SO/Battle/Stages/Normal/Stage_Mid_01~03` | 중층 Battle (goldBonus 30) |
| `Assets/05. SO/Battle/Stages/Normal/Stage_High_01~03` | 고층 Battle (goldBonus 60) |
| `Assets/05. SO/Battle/Stages/Stage_Elite_Duo` | 최상위 엘리트 2인 (goldBonus 75) |
