# 카드 버리기 & 드로우 이펙트 시스템

작성일: 2026-06-18
상태: Phase 3 구현 완료 — 테스트 대기 중

---

## 확정된 설계

| 항목 | 결정 |
|------|------|
| 비동기 처리 | `IAsyncCardEffect` 인터페이스 신규 추가. `SkillPresentationKeyframeExecutor`에서 체크 후 await. 기존 Effect 코드 무변경 |
| 버리기 UI | 오버레이 패널: 손패 카드 → 중앙 선택 영역 LitMotion 이동, N장 확정 버튼 |
| 선택 해제 | 중앙 선택 카드 클릭 → 손패로 복귀 + 손패 재정렬, LitMotion 이동 |
| 확정 버튼 활성 조건 | 선택된 카드 수 == N 일 때만 인터렉션 가능 |
| N장 초과 선택 | 이미 N장이면 추가 선택 차단 |
| 손패 < N장 | UI 표시, 가능한 만큼 선택 후 확정 가능 |
| DiscardEffect | 단일 클래스. `discardCount: int`, `drawAfterDiscard: bool`, `drawCount: int` |
| DrawEffect 분리 | `DrawFromPileEffect` (더미 드로우) + `CreateCardEffect` (SO 생성) 별도 클래스 |
| DrawFromPile 소스 | `sourcePile: PileType` (DrawPile / DiscardPile / GravePile 단일 선택) |
| DrawFromPile 필터 | `DrawFilterType`: `Any` / `ByElement` |
| 필터 미충족 | 스킬 전체 취소(Fizzle): 타임라인 중단 + 카드 버림패 강제 이동 + 코스트 복구 |
| CreateCardEffect | `CardDataSO` 참조 → 이번 스테이지 한정 새 `CardInstance` → 손패 추가 |

---

## 아키텍처 결정

### IAsyncCardEffect

EventChannel은 `SkillEffectExecutionService`가 이미 `_battleEventChannel`로 보유 → `ExecuteAsync()` 호출 시 파라미터로 전달 (Q1: C 확정).

```csharp
public interface IAsyncCardEffect
{
    UniTask<bool> ApplyAsync(
        SkillPresentationPlaybackContext context,
        int finalValue,
        CancellationToken ct,
        Gamelib.EventSystem.EventChannelSO eventChannel);
    // bool: true = 정상 완료, false = Fizzle 요청
}
```

`SkillEffectExecutionService.ExecuteAsync()`에서:
```csharp
if (slot.effect is IAsyncCardEffect asyncEffect)
{
    int finalValue = CardEffectValueCalculator.Calculate(slot.effect.BaseValue, keyframe.valueMultiplier);
    return await asyncEffect.ApplyAsync(context, finalValue, context.CancellationToken, _battleEventChannel);
}
```

### SkillFizzleToken

```csharp
public class SkillFizzleToken
{
    public bool IsFizzled { get; private set; }
    public void RequestFizzle() => IsFizzled = true;
}
```

- `SkillPresentationPlaybackContext`에 `FizzleToken` 프로퍼티 추가
- `BattleActionExecutor`가 `UseSkillAsync()` 호출 전 `SkillFizzleToken` 생성 → 호출 후 `IsFizzled` 체크
- Fizzle이면: `costModel.currentCost += card.data.cost` 코스트 복구, `deckController.ForceDiscard(card)` 버림패 이동

### DiscardEffect 선택 UI (CardDiscardSelectPanel)

- `EventChannelSO`로 `DiscardSelectRequestEvent` 발행 → 패널 활성화
- 손패의 `CardView`들을 참조해 선택 영역으로 LitMotion 이동
- 선택 완료 시 `UniTaskCompletionSource<List<CardInstance>>`로 결과 반환
- 선택 확정 후 `DeckController`에서 선택된 카드 버림, `drawAfterDiscard`이면 추가 드로우 처리

### DrawFromPileEffect Fizzle 흐름

```
DrawFromPileEffect.ApplyAsync()
→ DeckController에서 필터 조건 카드 탐색
→ 없으면 return false (Fizzle)
→ 있으면 드로우 실행 후 return true
```

---

## 변경 파일 목록

### Phase 1 — IAsyncCardEffect + SkillFizzleToken

| 파일 | 변경 |
|------|------|
| `Battle/Effects/IAsyncCardEffect.cs` | 신규: 인터페이스 정의 |
| `Battle/Presentation/SkillFizzleToken.cs` | 신규: Fizzle 요청 토큰 |
| `Battle/Presentation/SkillPresentationPlaybackContext.cs` | `FizzleToken` 프로퍼티 추가 |
| `Battle/Presentation/SkillPresentationKeyframeExecutor.cs` | `IAsyncCardEffect` 분기 추가 |
| `Battle/Presentation/SkillPresentationPlayer.cs` | keyframe 루프에서 `FizzleToken.IsFizzled` 체크 → 중단 |
| `CombatSystem/Skills/SkillModule.cs` | `SkillFizzleToken` 파라미터 추가 |
| `Battle/UI/BattleActionExecutor.cs` | Fizzle 체크 후 코스트 복구 + 강제 버림 처리 |
| `Battle/UI/DeckController.cs` | `ForceDiscard(CardInstance)` 메서드 추가 |

### Phase 2 — DiscardEffect + 선택 오버레이 UI

| 파일 | 변경 |
|------|------|
| `Battle/Effects/DiscardEffect.cs` | 신규: `IAsyncCardEffect` 구현, `discardCount`, `drawAfterDiscard`, `drawCount` |
| `Battle/Events/DiscardSelectRequestEvent.cs` | 신규: 선택 UI 요청 이벤트 |
| `Battle/UI/CardDiscardSelectPanel.cs` | 신규: 오버레이 선택 패널 MonoBehaviour |

### Phase 3 — RandomDrawEffect + CreateCardEffect (통합)

| 파일 | 변경 |
|------|------|
| `Battle/Events/SkillDrawCardsRequestEvent.cs` | PreDrawnCards 필드 추가 |
| `Battle/Events/RandomDrawRequestEvent.cs` | 신규 |
| `Battle/Events/CreateTempCardsRequestEvent.cs` | 신규 |
| `Battle/UI/DeckController.cs` | Start() 버그수정, 이벤트 리스너, DrawFromSpecificPile |
| `Battle/UI/HandDealController.cs` | DealSkillCardsAsync 분기 |
| `Battle/Effects/RandomDrawEffect.cs` | 신규 |
| `Battle/Effects/CreateCardEffect.cs` | 신규 |

---

## 페이즈별 구현 계획

### ✅ Phase 1 — IAsyncCardEffect + SkillFizzleToken 인프라 (완료)

**목표**: 비동기 이펙트 실행 + Fizzle 신호 전달 경로 구축

**구현 항목**:
1. `IAsyncCardEffect` 인터페이스 (`UniTask<bool> ApplyAsync(...)`)
2. `SkillFizzleToken` 클래스
3. `SkillPresentationPlaybackContext`에 `FizzleToken` 추가 + `WithTimeline()` 전파
4. `SkillPresentationKeyframeExecutor.ExecuteAsync()` 분기:
   - `IAsyncCardEffect`이면 `await ApplyAsync()`, false이면 `FizzleToken.RequestFizzle()`
5. `SkillPresentationPlayer` keyframe 루프에서 `FizzleToken.IsFizzled` 체크 → `break`
6. `SkillModule.UseSkillAsync()`에 `SkillFizzleToken` 파라미터 추가
7. `BattleActionExecutor.RunQueueAsync()`:
   - `SkillFizzleToken` 생성 → `UseSkillAsync` 전달
   - 완료 후 `IsFizzled`이면 코스트 복구 + `deckController.ForceDiscard(card)`
8. `DeckController.ForceDiscard()`: 손패에서 제거 → DiscardPile 강제 이동

**검증**: Fizzle 전용 더미 Effect(`AlwaysFizzleEffect : IAsyncCardEffect`)를 임시 등록해 Fizzle 흐름(타임라인 중단 → 코스트 복구 → 카드 버림패) 동작 확인

---

### ✅ Phase 2 — DiscardEffect + 선택 오버레이 UI (완료)

**목표**: 손패에서 N장 선택해 버리는 인터렉티브 UI 구현

---

#### Phase 2 grill-me 결정 완료

| 항목 | 결정 |
|------|------|
| Q1 | `SkillEffectExecutionService`가 `ApplyAsync`에 `eventChannel` 파라미터로 전달 |
| Q2 | Detach 패턴 — `HandLayoutController.DetachCard(instance)` / `ReattachCard(view)` API 추가 |
| Q3 | DiscardEffect가 TCS로 선택 결과만 수신, 후처리(버리기/드로우)는 DiscardEffect 담당 |
| Q4 | DiscardEffect가 `DiscardCardsEvent` → (선택적) `SkillDrawCardsRequestEvent` 순으로 이벤트 발행 |
| Q5 | EventChannel로 `DiscardCardsEvent` 발행 → DeckController가 수신해 ForceDiscard 처리 |

**신규 이벤트 클래스**:
- `DiscardSelectRequestEvent(int discardCount, UniTaskCompletionSource<List<CardInstance>> tcs)` — 패널 활성화 요청
- `DiscardCardsEvent(List<CardInstance> cards)` — DeckController가 수신해 버리기 처리
- `SkillDrawCardsRequestEvent(int drawCount, UniTaskCompletionSource tcs)` — HandDealController가 수신해 드로우 연출

---

**구현 항목**:

`DiscardEffect`:
- `discardCount: int`, `drawAfterDiscard: bool`, `drawCount: int`
- `ApplyAsync`: `DiscardSelectRequestEvent` 발행(TCS 포함) → await TCS → `DiscardCardsEvent` 발행 → drawAfterDiscard면 `SkillDrawCardsRequestEvent` 발행 + await TCS

`CardDiscardSelectPanel` (OverlayCanvas 위 전용 패널):
- `DiscardSelectRequestEvent` 수신 시 활성화
- 손패 모든 CardView를 `HandLayoutController.DetachCard()`로 분리 → 패널 "미선택 영역"으로 LitMotion 이동
- 카드 클릭 → "선택 영역"으로 이동 / 재클릭 → 미선택 영역 복귀
- N장 초과 선택 차단, 손패 < N장이면 가능한 만큼 선택 후 확정 가능
- 확정 버튼: 선택 수 == discardCount(또는 손패 전체 < discardCount면 손패 전체) 일 때 Interactable
- 확정 → TCS.SetResult(selectedCards) → 미선택 카드는 `HandLayoutController.ReattachCard(view)`로 복귀

`HandLayoutController` 추가 API:
- `DetachCard(CardInstance instance, Transform newParent = null) → CardView`
- `ReattachCard(CardView view)`

`DeckController`:
- `DiscardCardsEvent` 리스너 추가 → 각 카드에 `ForceDiscard()` 호출

`HandDealController`:
- `SkillDrawCardsRequestEvent` 리스너 추가 → `DealSkillCardsAsync(count, tcs)` 실행 (stagger 딜 + TCS 완료)

**검증**: DiscardEffect 카드 사용 → 오버레이 패널 활성 → 카드 선택/해제 애니메이션 → 확정 → 카드 버림패 이동 확인

---

### ✅ Phase 3 — RandomDrawEffect + CreateCardEffect (완료)

**목표**: 더미에서 랜덤 드로우 + CardDataSO로 임시 카드 생성

#### grill-me 확정 결정

| 항목 | 결정 |
|------|------|
| 이벤트 전달 | `SkillDrawCardsRequestEvent`에 `PreDrawnCards` 필드 추가. null이면 기존 DrawCards(), 있으면 AddCard 애니메이션만 |
| DrawPile 리셔플 | sourcePile = DrawPile이면 기존 `DrawCards()` 그대로 (리셔플 포함). 그 외 더미는 `DrawFromSpecificPile()` (리셔플 없음) |
| drawCount | 두 이펙트 모두 N장 조절 가능 |
| 0장 결과 | Fizzle 없음, return true |
| 생성 카드 영구성 | 스테이지 한정 임시 — AddFusionCard() 패턴 재사용 |

#### 버그 수정

- `DeckController.Start()` → `RefreshCurrentDeckCount()` 추가 → currentPile 카운트 텍스트 초기값 수정

#### 구현 완료 파일

| 파일 | 변경 |
|------|------|
| `Battle/Events/SkillDrawCardsRequestEvent.cs` | PreDrawnCards 필드 + 생성자 오버로드 추가 |
| `Battle/Events/RandomDrawRequestEvent.cs` | 신규 |
| `Battle/Events/CreateTempCardsRequestEvent.cs` | 신규 |
| `Battle/UI/DeckController.cs` | Start() 버그수정, 이벤트 리스너 2개, DrawFromSpecificPile(private) |
| `Battle/UI/HandDealController.cs` | DealSkillCardsAsync PreDrawnCards 분기 |
| `Battle/Effects/RandomDrawEffect.cs` | 신규: sourcePile, drawCount, IAsyncCardEffect |
| `Battle/Effects/CreateCardEffect.cs` | 신규: cardDataSO, createCount, IAsyncCardEffect |

---

## 완료 후 보고서 작성 예정
