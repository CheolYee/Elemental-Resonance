# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Unconfirmed Requirement Protocol

If a requirement, behavior rule, data model, file ownership boundary, UI hierarchy, naming convention, migration direction, or runtime/editor responsibility is not clearly defined, do not guess and do not implement the uncertain part.

Instead:

1. Stop before modifying code related to the uncertain area.
2. Summarize the ambiguity briefly.
3. Ask the user focused questions with 2-3 concrete options when possible.
4. State the recommended option and why.
5. Continue implementation only after the user confirms the direction.

Assumptions must be explicitly labeled as assumptions. Do not silently turn assumptions into code, serialized data, prefab hierarchy changes, or migration logic.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

## Language Rule

All explanations, plans, summaries, implementation notes, and questions must be written in Korean.

Code identifiers, class names, method names, enum names, file names, folder names, Unity API names, and package names may remain in English.

When asking the user for confirmation, ask in Korean.

## SOLID First

Always prioritize SOLID principles when designing and implementing code.

Follow these rules:

Keep each class focused on a single responsibility.
Do not make one manager class handle unrelated systems.
Separate data, battle logic, UI logic, input logic, and presentation/animation logic.
Prefer clear dependencies over hidden global access.
Design features so they can be extended later without rewriting existing code.
Avoid over-engineering, but do not create temporary structures that block future expansion.

This is a 3-week prototype, so implementation speed matters.
However, fast implementation should not mean mixing responsibilities or creating hard-to-replace systems.

Use grill-me Before Implementation

Before implementing a new feature or changing an existing structure, use the grill-me skill to confirm the design first.

Check the following before writing code:

Which class or module should own this responsibility?
Does this feature belong to the current phase?
Does it conflict with the existing FSM or module system?
Can it later support fusion, card grades, resonance gauge, rewards, and stage expansion?
Is the implementation data-driven where appropriate?
Are ScriptableObjects used for editable game data?
Are UI, gameplay logic, and animation/presentation logic kept separate?

If the structure is unclear, do not guess.
Ask the user in Korean before implementing.

## Dependency Injection (Reflect)

의존성 주입은 반드시 Reflect DI를 사용한다.

- `new` 키워드로 서비스 의존성을 직접 생성하지 않는다.
- MonoBehaviour에서 싱글톤이나 정적 클래스로 서비스에 직접 접근하지 않는다.
- 생성자 주입 또는 Reflect의 주입 방식을 따른다.
- DI 컨테이너 바깥에서 의존성을 임의로 resolve하지 않는다.

## Async / Await (UniTask)

모든 비동기 처리는 UniTask를 사용한다. 코루틴을 신규 작성하지 않는다.

- `Coroutine` / `StartCoroutine` / `IEnumerator`를 새로 작성하지 않는다.
- `Task` / `async Task` 대신 `UniTask` / `async UniTask`를 사용한다.
- 프레임 대기: `await UniTask.Yield()` 또는 `await UniTask.NextFrame()`
- 시간 대기: `await UniTask.Delay(TimeSpan)` 또는 `await UniTask.WaitForSeconds(seconds)`
- 취소는 `CancellationToken`으로 처리하고, `UniTaskCompletionSource`로 직접 완료 신호를 보낸다.
- LitMotion 트윈 대기 시 `.ToUniTask(cancellationToken)`으로 연결한다.

## Tweening (LitMotion)

모든 트위닝과 값 보간 애니메이션은 LitMotion을 사용한다.

- DOTween, iTween 등 다른 트위닝 라이브러리를 사용하지 않는다.
- 기본 패턴: `LMotion.Create(from, to, duration).Bind(target)`
- 시퀀스가 필요하면 UniTask와 조합한다: `.ToUniTask()`
- UI 애니메이션, 카메라 이동, 수치 보간 모두 LitMotion으로 통일한다.

## Encoding
- Save Markdown, C# source, and Unity text assets as UTF-8.
- When using PowerShell to read files, prefer `Get-Content -Encoding UTF8`.
- When a tool must write text directly, specify UTF-8 explicitly and verify Korean text did not become mojibake.
- Prefer patch-based edits for shared docs and source files so Claude/Codex do not disagree on encoding.

## Event Communication (EventChannelSO)

오브젝트 간 이벤트 통신은 반드시 `EventChannelSO`를 통한 채널링 방식을 사용한다.

- C# `event Action`, `UnityEvent`로 직접 오브젝트를 참조하지 않는다.
- 이벤트 데이터는 `GameEvent`를 상속한 전용 클래스로 정의한다.
- 구독: `channel.AddListener<MyEvent>(OnMyEvent)`
- 해제: `channel.RemoveListener<MyEvent>(OnMyEvent)` — OnDisable 또는 OnDestroy에서 반드시 해제.
- 발행: `channel.RaiseEvent(new MyEvent(...))`
- `EventChannelSO` Asset은 Inspector에서 주입받는다 (DI 또는 SerializeField).

## How to work in this repo
- Read the relevant rule files before making large edits.
- Prefer small, reviewable changes over broad refactors.
- When a change affects architecture, explain the reason and the touched systems first.