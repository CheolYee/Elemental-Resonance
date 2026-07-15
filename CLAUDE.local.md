# CLAUDE.local.md

## Project Context

Unity 6 기반 3D 턴제 카드 전투 게임.

```text
플레이어 턴 시작 → 카드 드로우 → 코스트 지급
→ 카드를 유효 대상에게 드래그 & 드롭 → 즉시 코스트 차감 + 스킬 발동
→ 스킬 애니메이션 완료 후 손패 입력 재활성화
→ "턴 종료" 버튼 클릭
→ 적 턴 실행 (배열 순서대로 순차 공격)
→ 다시 플레이어 턴 (코스트 최대값으로 충전)
```

**행동 큐는 존재하지 않는다.** 카드는 드롭 즉시 발동된다.

---

## Working Rules

- Superpowers 계열 스킬은 사용하지 않는다.
- 테스트 코드는 명시 요청이 있을 때만 추가한다.
- 구현 전 반드시 `grill-me` 스킬로 구조를 확정한다.
- 모든 비동기 처리는 UniTask. 트위닝은 LitMotion. DI는 Reflex.
- 동적 스폰 오브젝트는 `GameObjectInjector.InjectRecursive(go, _container)` 로 DI 주입.

---

## Architecture Decisions

### Card System
- `HandLayoutController`: 레이아웃·드래그·슬라이드·인터렉터블 전담
- `HandDealController`: 카드 딜/버리기 전담
- `DeckController`: DrawPile/Hand/DiscardPile/GravePile 4개 더미 런타임 관리
- `CardDisposePolicy.Grave` → GravePile, 나머지 → DiscardPile

### Battle System
- `BattleActionExecutor`: 코스트 차감 + 스킬 실행 + `SkillExecutionStartEvent/EndEvent` 발행
- `BattleTurnController`: 전체 턴 사이클 오케스트레이션
- `BattleResultController`: 전투 종료 판정 단일 책임
- 코스트 회복: `currentCost = Mathf.Max(currentCost, baseCost)` (턴 시작 시)

### Skill Presentation System
- `SkillPresentationDataSO`: Source of Truth. 등급별 Timeline 보유.
- `SkillPresentationTimeline`: Row별 키프레임 리스트. `GetEffectiveDuration()` = EndMarker 키프레임 시각 우선, 없으면 마지막 keyframe + 0.5s.
- `SkillModule.UseSkillAsync()`: AnimationKeyframe 유무로 SkillState 진입 분기. Timeline과 SkillState 모두 완료되어야 종료.
- 카드에 playable Timeline이 없으면 warning만, 코스트·카드 소비는 정상 처리.
- `SkillPresentationPlayer` → `SkillPresentationSampler` → `SkillPresentationKeyframeExecutor` 순으로 실행.
- `EffectKeyframe`: effectSlotId 기반. `CardEffectValueCalculator.Calculate(base, multiplier)`.
- VFX: `SkillVfxContainer` 풀링. `VfxDefinitionSO.key`로 child 활성화. `lifeTime > 0`이면 사용, `<= 0`이면 `GetMaxDuration()` fallback.
- `VfxDefinitionSO`: `key(SkillVfxKey)` + `prefab(GameObject)` + `defaultLifeTime`. `SkillVfxObjectData.vfxDefinition`으로 참조. 나중에 3D 프리뷰 렌더링에 `prefab` 사용 예정.
- Camera: `CinemachineCamera.transform` 직접 조작. 스킬 시작 전 `basePosition/baseRotation/baseFov` 캡처(Priority 올리기 전) → `Priority = 20` → 모든 카메라 키프레임 처리 후 `GetEffectiveDuration()` 시각까지 대기 → finally에서 Transform 복원 + `Priority = 0`. CinemachinePositionComposer/RotationComposer 제거됨. Focus 로직 제거됨.

### Agent System
- `TargetingModule`은 Agent **자식 오브젝트**에 부착
- `EnemyDataSO.attackCard`: 적이 `CardDataSO`를 직접 참조. `SkillUsageData.FromEnemyData()`로 CardInstance 생성.
- 적 스폰: `Instantiate()` + `GameObjectInjector.InjectRecursive(go, _container)`.

### Death System
- `AbstractEnemy.OnDeathStarted`: HP 0 즉시 발행 → BattleResultController 조기 감지
- `AbstractEnemy.OnDeathAnimationComplete`: 애니메이션 완료 후 발행 → DissolveModule 시작
- 적 제거: DEATH 애니메이션 → Dissolve (LitMotion) → Destroy

### DI (Reflex)
- 씬 진입점: `ContainerScope` + `BattleSceneInstaller : IInstaller`
- SO 에셋은 DI 범위 밖 — `[SerializeField]` 유지

### DeckBuilding System (임시 구현 — 폴리싱 필요)
- `CardDatabaseSO`: 전체 카드 목록 + `maxDeckSize` 보유
- `DeckBuilderController`: 상단 카드 그리드 + 하단 덱 슬롯 + 시작 버튼
- `DeckBuilderCardItemView`: 아트워크 + 이름 + 코스트 + 설명, 클릭 이벤트
- `TempStartCardSO.SetDeck()`: 덱 빌더 → Main 씬 덱 전달 경로
- **폴리싱 시 개선 필요:** 카드 상세 팝업, 카드 필터/정렬, 덱 저장/불러오기, 전용 카드 UI 디자인, 동일 카드 장 수 표시, 덱 최소 장 수 제한

### Canvas 계층
- `BattleCanvas` (SS-Camera): TargetingOverlay만
- `OverLayCanvas` (SS-Overlay, Sort Order 10): 카드 UI 전체

---

## Stage Framework

```text
Stage = Wave 여러 개. Wave = 최대 3명. 마지막 Wave 완료 = Stage Clear. 플레이어 HP 0 = Stage Fail.
StageDataSO는 덱을 알면 안 된다 — 적 Wave 정보만 보유.
```

- `StageBootstrapper`: 스폰 오케스트레이터. Wave마다 `WaveStartEvent` 발행.
- `BattleResultController`: `WaveClearEvent`만 발행. 마지막 Wave 판단은 StageBootstrapper.
- `_waveClearPending` 플래그: `WaveClearEvent` 시 설정, `CardDrawEndEvent` 시 초기화.

---

## 이후 개발 순서

```text
1. ✅ Skill Presentation System (SP-1 ~ SP-13 + 버그 수정 완료)
2. ✅ 에디터 타임라인 구조 개편 (Bug 4)
3. ✅ 이징 연결선 표시 (Bug 5)
4. ✅ 런타임 검증 (RT-1)
5. ✅ EndMarker + 카메라 Priority 수정 (Bug 6)
6. ✅ 맵 UI Phase 5 — 노드 선택 → 스테이지 진입 연출
7. ✅ 덱 빌딩 시스템 (임시) — DeckBuilder 씬, CardDatabaseSO
8. ✅ 맵 UI Phase 6~7 — Rest/Shop 루프, MapGraphEditorWindow
9. ✅ 3D 타임라인 프리뷰 RT-2 (Phase 1~5 전체 완료) — 환경 맵+조명, 애니메이터, VFX, 카메라, 캐스터 이동
10. ✅ 스킬 연출 개편 — docs/2026-06-15-skill-presentation-refactor.md (Phase 1~4 완료)
11. ✅ 슬로우 모션 타겟팅 + VFX/캐스터 이동 — docs/2026-06-15-slowmo-targeting-vfx-caster.md (Phase 1~6 완료)
12. ✅ 맵 UI Phase 8 — 씬 연결 + 수동 검증 완료 (docs/2026-06-10-sts-map-ui-design.md)
13. ✅ 배틀 UI 리뉴얼 — HP바·방어도·데미지 텍스트·적 인텐트 + 날아다니는 카드 트레일 연출 (docs/2026-06-15-battle-ui-renewal.md, docs/2026-06-16-flying-card-trail-effect.md)
14. ✅ Fusion / Grade / Resonance System — 카드 합성 + 합성 단계 + 연출, 원소 속성에 따른 카드 색상 분기 (docs/2026-06-16-fusion-grade-element-system.md)
15. ✅ Reward System — 보상 패널 UI, 카드 선택 → FlyingCard → currentPile 연동 (docs/2026-06-17-reward-gold-system.md Phase 1~3 완료)
16. ✅ Gold System — TopBar 골드 표시, GoldChangedEvent, LMotion 카운팅 연출
17. ✅ Shop / Rest 실제 기능 — UI 열기/닫기 + 상점 구매/버리기/리롤 + 휴식 HP 회복 (docs/2026-06-17-shop-rest-ui.md)
18. ✅ Scene Transition System
19. ✅ 튜토리얼 시스템 — 하이라이트+블로킹 방식, TutorialStepSO+EventChannel 완료조건, 10단계 (docs/2026-06-29-tutorial-system.md)
20. ✅ 절차적 맵 생성 — 시드 기반 동적 노드 생성, 저/중/고층 스테이지 풀 분리, 튜토리얼 우회 (docs/2026-07-05-procedural-map-generation.md)
21. ✅ 시드 선택 UI — 새 게임 팝업(일반/시드 플레이), PlayerPrefs 씬 간 전달, 설정창 시드 표시+복사
```

---

## 완료된 Skill Presentation 페이즈 요약

- **SP-1**: CardEffect 추상 클래스, CardEffectSlot(GUID), CardEffectValueCalculator
- **SP-2**: TargetType.None 카드 드롭 지원, CardView 조건 플래그 분리
- **SP-3**: baseCost 필드명 변경, 코스트 회복 규칙 적용
- **SP-4~5**: SkillPresentationDataSO, SkillPresentationTimeline, 키프레임 데이터 모델
- **SP-6**: EditorWindow partial class 7개 파일 구조
- **SP-7**: Ruler + 7 Row 타임라인 UI
- **SP-8**: 키프레임 추가/삭제/정렬/복붙/다중선택/드래그/스냅/Undo
- **SP-9**: UIToolkit 기반 Keyframe Inspector, EffectSlot 드롭다운
- **SP-10**: Playhead / Playback
- **SP-11**: SkillPresentationPlayer, Sampler, Runtime 실행 구조
- **SP-12**: SkillModule Integration (AnimationKeyframe 유무 분기)
- **SP-13**: EffectKeyframe Runtime 실행, SkillEffectExecutionService
- **Bug Fix**: 적 CardDataSO 기반 스킬, DI 동적 주입, VFX SetActive+lifeTime, Cinemachine Priority

---

## ✅ Bug 4 — 에디터 타임라인 구조 개편 (완료)

- Phase 1~4: 데이터 모델 교체, 선택 상태, ObjectListPanel, Timeline Row 재구성
- Phase 5: 4분할 레이아웃 (RenderingView / TimelinePanel / HierarchyPanel / InspectorPanel), 리사이즈 핸들
- Phase 6: VFX 오브젝트 인스펙터 (VfxDefinitionSO 도입, ScrollView, 2단 구조)
  - `VfxDefinitionSO`: key + prefab(3D 프리뷰용) + defaultLifeTime
  - `SkillVfxObjectData.vfxDefinition` 으로 참조, vfxDefinition 변경 시 lifeTime 자동 세팅

---

## ✅ Bug 5 — 이징 연결선 표시 (완료)

- CamPosition/Rotation/Zoom, VfxPosition/Rotation/Scale row에 인접 키프레임 쌍 사이 이징 연결선 렌더링
- Hold=점선(회색), Linear=실선(밝은 회색), EaseIn*=파랑, EaseOut*=주황, EaseInOut*=보라
- 선 클릭 시 인스펙터에 "Transition" 드롭다운(Hold + 전체 LitMotion.Ease 목록) 표시
- 키프레임 드래그 중 이징 선 실시간 갱신 (`_easingLineContainers` Dictionary 패턴)
- RowHeight=28f, lineH=4f, fontSize=9, 라벨 색상 흰색

---

## ✅ RT-1 — 런타임 검증 (완료)

- Animation/VFX/Camera/Effect/isHold/easing 전 항목 플레이 모드 검증 완료
- VFX Active 키프레임: `vfxActiveAction`(Play/Stop) 필드 추가, 인스펙터 표시, 런타임 `AnimateActiveAsync` 처리
- VFX Active 없으면 기존 `lifeTime` fallback 유지
- 카메라/VFX 이동: base 기준 누적 오프셋(`baseOffset + keyframeDelta`) 방식으로 통일
- `SkillVfxContainer`: `GetComponentsInChildren` 전환으로 복수 ParticleSystem 완전 Stop
- ※ CinemachineComposer는 이후 Camera 리팩토링으로 제거됨 (RT-2 Phase 1 이전 작업)

---

## ✅ Bug 6 — EndMarker + 카메라 Priority 수정 (완료)

- `SkillObjectKind.EndMarker` + `SkillKeyframeProperty.TimelineEndTime` 추가
- `SkillPresentationTimeline.endMarkerKeyframe`: nullable 필드. `GetEffectiveDuration()`이 EndMarker 시각을 우선 반환
- 에디터: EndMarker 오브젝트 추가 시 자동으로 1개 키프레임 생성. 드래그/삭제/인스펙터 모두 기존 키프레임 인프라 재사용
- **카메라 Priority 조기 복원 버그**: `SkillCameraExecutionService.PlayAsync`가 카메라 키프레임 완료 즉시 `Priority = 0` 복원하던 문제 수정
  - `effectiveDuration` 파라미터 추가 → 마지막 카메라 키프레임 이후 잔여 시간만큼 대기 후 복원

---

## ✅ RT-2 Phase 1 — 3D 프리뷰 씬 기반 구조 (완료)

- `SkillPreviewLayoutSO`: casterPrefab/Pos, targetPrefabs[3]/Pos[3], environmentPrefab, cameraPosition, cameraRotationEuler, cameraFov
- `SkillPreviewScene`: `EditorSceneManager.NewPreviewScene()` + **`camera.scene = _scene` + `CameraType.Preview`** (URP에서 preview scene 렌더링의 핵심)
- `SkillPreviewAgentTag`: `spawnOffset`(Vector3) + `spawnRotationOffset`(Vector3 Euler) — 프리팹별 스폰 보정값
- `EditorPrefs` 영구 저장: Card SO path, Preview Layout path, Prefab Folder string (에디터 재시작 후 복원)
- 플레이 모드 진입 시 씬 소멸 / 종료 시 자동 재생성 (`playModeStateChanged` 이벤트)
- 렌더링: `camera.Render()` → RenderTexture → `IMGUIContainer.GUI.DrawTexture`

---

# 다음 페이즈

## RT-2 — 3D 타임라인 프리뷰 (Editor Scene Preview)

**목적**: Skill Presentation Editor의 RenderingView 영역에서 Playhead 시각 기준으로 캐릭터 애니메이션·VFX·카메라 연출을 에디터 타임에 프리뷰한다.

---

### 확정된 아키텍처 결정

| 항목 | 결정 |
|------|------|
| 렌더링 | `EditorSceneManager.NewPreviewScene()` + Camera → RenderTexture → `IMGUIContainer` |
| 프리뷰 범위 | VFX + Camera + Animation 전체 |
| 씬 생명주기 | `OnEnable` 생성 / `OnDisable` 소멸, 카드·등급 전환 시 오브젝트만 교체 |
| 레이아웃 데이터 | `SkillPreviewLayoutSO` (EditorWindow에서 `[SerializeField]`로 연결) |
| Animation 스크럽 | Rebind + `CrossFadeInFixedTime(hash, 0.1f)` + `animator.Update(delta)` fast-forward |
| VFX 스크럽 | `particleSystem.Simulate(t - spawnTime, withChildren:true, restart:true)` 매 프레임 |
| Camera | 단순 `Camera` 컴포넌트. `baseOffset + keyframeDelta` 방식. CamShake는 프리뷰 스킵 |
| VFX 스폰 기준 | Caster=casterPosition, Target=targetPositions[0], Between=두 위치 중간점 |

---

### SkillPreviewLayoutSO 구조

파일 위치: `Assets/00. Work/_Resources/02. Scripts/Battle/Presentation/SkillPreviewLayoutSO.cs`

```csharp
[CreateAssetMenu(menuName = "Battle/Skill Preview Layout")]
public class SkillPreviewLayoutSO : ScriptableObject
{
    public GameObject   casterPrefab;
    public GameObject[] targetPrefabs;        // 슬롯 0~2 (최대 3)
    public Vector3      casterPosition;
    public Vector3[]    targetPositions;      // 슬롯 0~2 고정
    public GameObject   environmentPrefab;   // null이면 단색 배경
    public Vector3      cameraPosition;
    public Vector3      cameraRotationEuler;
    public float        cameraFov = 60f;
}
```

---

### SkillPreviewScene 구조 (Editor-only 클래스)

파일 위치: `Assets/00. Work/_Resources/02. Scripts/Battle/Presentation/Editor/SkillPreviewScene.cs`

- `NewPreviewScene()` 래퍼. 씬 내 오브젝트 생성·소멸·갱신 전담.
- 외부에서 `Rebuild(layout, timeline, grade)` 호출 시 씬 내 오브젝트 전체 교체.
- `Sample(float t)` 호출 시 해당 시각 기준으로 Animator·VFX·Camera 갱신.
- `RenderTexture` 프로퍼티로 IMGUIContainer에 연결.

---

### RT-2 구현 페이즈

#### ~~Phase 1~~ — 완료 (RT-2 Phase 1 요약 참고)

---

#### ~~Phase 2~~ — 완료

- `SkillPreviewScene`: `_timeline`, `_casterAnimator`, `_lastSampledTime` 필드. `Sample(t)` → `SampleAnimators(t)`.
- `SampleAnimators`: t 이하 **마지막 AnimParam 키프레임** 탐색 → `Animator.Play(hash, 0, 0f)` + `Update(0f)` + `Update(timeIntoClip)`. 트랜지션 없는 PlayClip 구조에서 fast-forward 방식은 잘못된 상태를 유발하므로 마지막 키프레임만 사용.
- `SkillPresentationEditorWindow.Preview.cs`: `_lastSampledTime` 필드. `DrawPreview()` 에서 시각 변화 감지 시 `Sample()` 호출.

---

#### ~~Phase 3~~ — 완료

- `VfxEntry` struct: Data/Go/Particles/BasePosition/BaseRotation/BaseScale + PosKeys/RotKeys/ScaleKeys/ActiveKeys (spawn 시 정렬 캐시)
- `SampleVfx(t)`: localT 기반 active 판단, VfxActive 키프레임 우선, `ps.Simulate()`, `base + additive delta` 보간
- `InterpolateDelta`: isHold=snap, `EaseUtility.Evaluate(normalizedT, ease)`
- `ApplyVfxObjectChange` → `RebuildPreview()`, `ApplyKeyframeChange` → `_lastSampledTime=MinValue; Repaint()`
- 키프레임 추가/삭제/드래그/붙여넣기/시간변경 → `RebuildPreview()` (VfxEntry 캐시 무효화)

---

#### ~~Phase 4a~~ — Camera 키프레임 연동 (완료)

- `_layout: SkillPreviewLayoutSO` 필드 추가 (Rebuild 시 저장)
- `IsRecordingCamera` 프로퍼티 + `SetRecordingCameraTransform(pos, euler)` 공개 메서드 추가
- `SampleCamera(t)`: CamPos/Rot → `base + InterpolateDelta`, CamZoom → `InterpolateFov` (absolute FOV)
- `FilterCameraKeys()` / `InterpolateFov()` 헬퍼 추가, CamShake 스킵
- `Sample(t)` → `SampleCamera(t)` 호출 추가

---

#### ~~Phase 4b~~ — 카메라 녹화 모드 (완료)

- UIToolkit `Button`으로 REC 버튼 분리 (IMGUIContainer `PickingMode.Ignore` 제거)
- `_isCameraFree` 토글(CAM 버튼): OFF 시 키프레임 그대로, ON 시 WASD/QE/우클릭 자유 이동
- `EditorApplication.update` 항상 등록 (`OnEnable`/`OnDisable`), `OnCameraUpdate()`로 이동 처리
- Snapshot 방식: REC 종료 시 현재 Playhead 시각에 CamPosition + CamRotation delta 키프레임 생성
- `IsRecordingCamera`: 자유 이동 중 또는 REC 중 `true` → `SampleCamera(t)` 스킵

---

#### ~~Phase 5~~ — 환경 맵 + 조명 (완료)

**목표**: environmentPrefab이 지정된 경우 맵이 배경으로 렌더링됨.

**구현 항목**:
1. `SkillPreviewScene.Create()` 시:
   - Directional Light (intensity=1.0f, rotation=(50,-30,0)) 기본 생성
   - `_layout.environmentPrefab != null`이면 `SceneManager.MoveGameObjectToScene(Instantiate(environmentPrefab), _previewScene)` 호출
2. `RebuildEnvironment(layout)`: layout 변경 시 환경 오브젝트 교체
3. null이면 Camera의 `clearFlags = SolidColor`, `backgroundColor = new Color(0.15f, 0.15f, 0.17f)` 유지

**검증**: SkillPreviewLayoutSO의 environmentPrefab 슬롯에 맵 프리팹 연결 시 배경에 맵이 렌더링됨.

---

### 주의사항

- `SkillPreviewScene`은 `#if UNITY_EDITOR` 가드 필수. 런타임 빌드에 포함되면 안 됨.
- `NewPreviewScene`에서 스폰된 오브젝트는 Reflex DI 주입 불필요 (에디터 전용 시각화).
- Animator가 없는 프리팹(환경 오브젝트 등)은 null 체크로 조용히 스킵.
- VfxDefinitionSO.prefab이 null인 경우 해당 VFX 슬롯 스킵.
- RenderTexture는 `OnDisable`에서 반드시 `Release()` 후 `DestroyImmediate()` 호출.
