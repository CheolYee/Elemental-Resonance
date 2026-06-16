# 날아다니는 카드 연출 개편 — 추적형 이동·궤적 다양화·트레일 VFX

작성일: 2026-06-16
상태: ✅ Phase 1~4 전체 완료

---

## 목표

사용된 카드/리필 카드가 더미로 날아가는 연출(`CardFlyAnimator` + `FlyingCardView`)을 슬레이 더 스파이어 수준의 마법 에너지 트레일 연출로 개편한다.

---

## 현재 코드베이스 분석

### CardFlyAnimator.cs
- 호출 시점에 도착지(`pileAnchorView.GetScreenPosition(target)`)를 **한 번만 캡처**해 베지어 제어점 계산 → UI 슬라이드 중 도착지가 움직이면 엉뚱한 위치로 날아감
- 아치 높이(`arcHeightPixels`)가 고정값 → 모든 카드가 동일한 궤적
- 회전: 베지어 곡선의 수학적 접선을 매 프레임 그대로 적용 → 위치에 적용된 `flyEase`(OutQuad)의 불균등한 t 흐름 때문에 회전 변화가 한 구간에 몰려 부자연스러움
- `FlyingCardView`: `SpriteRenderer` 기반 월드 스페이스 오브젝트 (UGUI 아님). `TrailRenderer` + `ParticleSystem` 1개만 보유, 둘 다 기본 설정 수준

### PileAnchorView.cs
- `GetScreenPosition(target)`은 RectTransform의 **실시간 위치**를 반환 — 매 프레임 재조회하면 추적형으로 즉시 전환 가능

### 호출 지점 3곳
- `OnCardDropped`: 카드 사용 → Discard/Grave
- `OnDrawPileRefill`: Discard → Draw 보충
- `FlyAllToDiscard`: 턴 종료 손패 전체 → Discard

---

## 확정된 설계

| 항목 | 결정 |
|---|---|
| 도착지 추적 | 매 프레임 `PileAnchorView.GetScreenPosition(target)` 재조회 |
| 궤적 다양화 | 비행마다 아치 높이/좌우 편향/duration 랜덤 변주 |
| 회전 | 접선 각도를 목표값으로 두고 `Time.deltaTime` 기반 별도 보간(MoveTowardsAngle)으로 스무딩 — 위치 이징과 분리 |
| 구조 분리 | `CardFlyTrailEffect` 컴포넌트로 트레일/파티클 책임 분리, `FlyingCardView`는 이동 상태만 유지 |
| 트위닝 | LitMotion 유지 (DOTween 사용 안 함 — 프로젝트 규칙) |

---

## 구현 페이즈

### ✅ Phase 1 — 이동 로직 개선 (완료)
- `CardFlyAnimator.FlyCardAsync`: 목적지를 `PileDisplayTarget` enum으로 받아 매 프레임 재조회 (추적형)
- 아치 높이: 고정값 대신 이동 거리에 비례(`arcHeightRatio`, 기본 0.5) + 최소 높이 보장(`minArcHeightPixels`, 기본 80px) — 짧은 거리도 충분히 솟아오름
- 좌우 편향(제어점 가로 오프셋), `flyDuration` 랜덤 변주로 매번 다른 궤적
- 회전: 목표 접선 각도 계산은 유지하되, `Mathf.MoveTowardsAngle`로 실시간 기준 부드럽게 추적 (위치 이징과 분리)

### ✅ Phase 2 — CardFlyTrailEffect 컴포넌트 분리 + 메인 트레일 재설정 (완료)
- `CardFlyTrailEffect` 신규: TrailRenderer/ParticleSystem 참조 보유, `FlyingCardView`는 SuppressForTeleport/ResumeAfterTeleport/ResetEffect 호출만 위임
- TrailRenderer: Alignment=View, Texture Mode=Stretch, Time=0.2s, MinVertexDistance=0.05 — 기존 설정이 이미 스펙과 일치해 유지
- Width Curve: 0(0.12) → 0.5(0.06) → 1(0) 3키 추가 (기존 2키 직선 감소 → 중간 단계 보강)
- Color Gradient: 밝은 노란연두(0, alpha1) → 선명한 초록(0.5, alpha0.6) → 어두운 초록(1, alpha0)
- Material(FlyingCardTrail.mat): 이미 Additive 블렌드(`_Blend=2`)였음 — `_BaseColor`/`_Color`가 주황색으로 그라디언트를 덮고 있던 것을 흰색으로 수정해 그라디언트 색이 그대로 표현되도록 함
- Bloom: 프로젝트 Global Volume에 이미 활성화되어 있어 추가 설정 불필요

### ✅ Phase 3 — 보조 파티클(스파크/빛조각) 설정 (완료)
- `manage_vfx`(unity-mcp) `particle_set_shape`/`particle_set_color_over_lifetime` 액션으로 Shape(Cone, angle15, radius0.02), Color Over Lifetime(노란연두→초록→어두운초록 alpha0), Noise(strength0.2), VelocityOverLifetime(Local Y -0.3) 일괄 적용 — 중첩 모듈은 `manage_components`로는 설정 불가능했으나 `manage_vfx`로 해결
- Renderer: StretchedBillboard, lengthScale=2.5, velocityScale=0.4 — 빠른 입자는 빛 조각처럼, 느린 입자는 점처럼 보이는 혼합 효과
- startColor 노란연두, gravityModifier -0.05로 완화

### 버그 수정 (Phase 3 검증 중 발견)
- **파티클 미방출 버그**: `CardFlyTrailEffect.ResetEffect()`가 풀에서 꺼낼 때마다 파티클을 `Stop()`시키는데 이후 `Play()`를 호출하는 곳이 없어 항상 정지 상태로 날아감 — `ResumeAfterTeleport()`에서 `trailParticle.Play()` 호출 추가
- **카메라 흔들림/줌 시 트레일 튐 버그 (1차 시도 — CameraSnapshot, 폐기)**: `Camera.main.ScreenToWorldPoint()`를 매 프레임 호출하는데, 스킬 연출 중 Cinemachine이 흔들림/줌을 입히면 그 노이즈가 궤적에 섞임. 비행 시작 시점 카메라 상태를 스냅샷으로 고정하는 방식을 시도했으나, 카드 사용 연출의 **줌**(지속적 FOV 변화)에는 스냅샷이 점점 어긋나 "더미까지 못 가는" 새 버그를 유발 — 흔들림(고주파 노이즈)과 줌(저주파 의도된 변화)은 상충되는 요구라 스냅샷 하나로 둘 다 해결 불가능
- **최종 해결 — 전용 카메라(PileCam) 사용**: 씬에 이미 `PileCam`(Overlay 카메라, cullingMask="FlyingCard" 레이어 전용)이 존재했으나 `TargetCameraSync`로 메인 카메라의 흔들림/줌까지 그대로 미러링하고 있었음. `TargetCameraSync` 제거 → PileCam이 완전히 고정됨. `CardFlyAnimator`가 `Camera.main` 대신 `pileCamera`(PileCam) 참조로 매 프레임 직접 `ScreenToWorldPoint` 호출 (스냅샷 불필요, PileCam 자체가 흔들리지 않으므로). 메인 카메라 cullingMask(255)는 이미 "FlyingCard" 레이어(8)를 제외하고 있어 중복 렌더링 없음

### ✅ Phase 3 폴리싱 — 트레일/파티클 최종 다듬기 (완료)

**파티클 (도깨비불 → 고운 점으로 재조정)**
- VFX Graph 전환 검토: `com.unity.visualeffectgraph` 17.3.0 설치 확인됨. unity-mcp Skill에 그래프 노드 자동 생성 기능은 없어(`vfx_*` 액션은 기존 .vfx 에셋의 노출 파라미터 제어용) 자동화로는 한계 — 사용자가 직접 VFX Graph 에디터에서 그래프를 만들 경우를 대비해 도깨비불(Turbulence + 깜빡이는 Size/Color) 노드 구성을 안내했으나, 최종적으로는 기존 ParticleSystem을 계속 사용하기로 결정
- `Render Mode`: StretchedBillboard → **Billboard** — 선으로 늘어져 보이던 문제 해결, 동그란 점으로 렌더링
- `Start Size`: 1.0(의도치 않게 커짐) → **0.025** 고정값으로 축소
- `Emission Rate`: 25 → **60** (입자 개수 증가)
- `Velocity over Lifetime` Y: -0.3 → **-0.7** (뒤쪽으로 흩어지는 강도 증가)
- `Noise Strength`: 0.2 → **0.45** (불규칙한 흔들림 증가)
- 참고: `manage_vfx`(unity-mcp)의 `particle_set_main` 액션으로 MinMaxCurve 범위를 직접 지정하면 무시되고 단일값(1.0)으로 덮어써지는 버그성 동작 발견 — 범위가 필요 없는 단순 값은 `manage_components`의 flat shortcut(단일 스칼라)으로 설정하는 게 안전

**Hovl Studio 에셋 트러블슈팅**
- 사용자가 상용 VFX 에셋(`Hovl Studio/AAA Projectiles Vol 2`)의 `Projectile 1`을 `FlyingCardView`에 추가했으나 렌더링되지 않는 문제 발생
- 원인 1: `Projectile 1`이 Default 레이어(0)에 있어 PileCam(FlyingCard 레이어 8 전용)이 렌더링하지 못함 → FlyingCard 레이어로 변경
- 원인 2: 메인 파티클 머티리얼 슬롯이 **원본 에셋 프리팹 자체**에서부터 null (에셋 결함, 씬 문제 아님) — Demo 씬에서도 참조를 찾지 못해 사용자가 직접 머티리얼을 골라 채우기로 함
- 최종적으로 메인 파티클은 사용하지 않고, **트레일 머티리얼(`Trail21cg.mat`, Hovl Studio)만 우리 트레일에 적용**하는 방향으로 정리됨

**메인 트레일 (Trail21cg.mat 적용 후 재조정)**
- Color Gradient를 HDR 밝기로 상향 (예: R1.6 G3.2 B0.9) — `_Emission`배율(4)을 가진 Add 셰이더와 곱해져 Bloom이 강하게 반응
- `_Usecenterglow` 활성화 시도 → `_Mask` 텍스처가 비어있어(null) 트레일 전체가 사라지는 버그 발생, 다시 0으로 복구
- `time`: 0.2 → 0.5초 (길이 증가)
- `widthMultiplier`: 1 → 3 (너비 증가, 곡선 비율 유지)
- `minVertexDistance`: 0.05 → 0.001 (카드와 트레일 사이 빈 공간 감소 시도, 부분적 효과)
- `Texture Mode`: Stretch → **Tile**, `_Usedepth`: 1 → 0 — 빈 공간의 실제 원인이었던 머티리얼 텍스처 스트레칭 문제 해결
- `Texture Scale.x`: 1 → 0.25 — Tile 전환으로 반복 횟수가 늘어 짧아 보이던 글로우 조각을 다시 길게 조정

### ✅ Phase 4 — 도착 연출 + 파일버튼 피드백 (완료)

**확정된 설계 (grill-me)**
- `CardArrivedAtPileEvent(PileDisplayTarget, Vector3)` 신규 — 도착 연출과 파일버튼 반응이 공유하는 단일 신호
- 도착 시: 카드 스프라이트 숨김(흡수됨) + 지속 파티클 신규 방출만 중지(기존 입자는 자연 소멸) + ArrivalBurst 1회 + 트레일은 그대로 두고 0.2초 후 풀 반환(잔광)
- 파일버튼: 클릭 시 Z축 비틀림(0°→-15°→0°, 슬더스 스타일), 카드 도착 시 약한 스케일 팝(1.0→1.12→1.0)

**구현**
- `FlyingCardView.PlayArrivalEffect()`: SpriteRenderer 비활성화 + `CardFlyTrailEffect.PlayArrival()` 위임, `ResetItem()`에서 복원
- `CardFlyTrailEffect.PlayArrival()`: `trailParticle.Stop(StopEmitting)`(클리어 안 함) + `arrivalBurst.Play()/Emit(18)`
- `CardFlyAnimator.FlyCardAsync`: 비행 완료 → 이벤트 발행 → `PlayArrivalEffect()` → `trailLingerDuration`(0.2s) 대기 → `_pool.Push()`
- `FlyingCardView` 프리팹에 `ArrivalBurst` 자식(ParticleSystem) 신규 추가 — Sphere Shape, startColor 밝은 초록, startSpeed 2.5, lifetime 0.25s, FlyingCardTrail.mat 재사용
- `CardPileButton`: `CardArrivedAtPileEvent` 구독(pileTarget 일치 시), 클릭/도착 각각 LitMotion 비동기 시퀀스(`await ... ToUniTask()`)로 처리 — `WithOnComplete` API는 검증 안 돼 기존 프로젝트의 순차 await 패턴으로 작성

**도구 한계 노트**
- `manage_vfx`의 `particle_set_emission`(burst) 및 `particle_set_color_over_lifetime`이 `ArrivalBurst`에서는 적용되지 않는 경우가 있었음 — 버스트는 `ParticleSystem.Emit()` 코드 호출로 대체, Color Over Lifetime은 Inspector에서 수동 보강 필요
