# Main Scene UI 복구 계획

작성일: 2026-06-11  
상태: 복구 진행 중

---

## 현재 씬 구조 (확인 완료)

```
Canvas
├── BattleCanvas         (Screen Space - Camera, Sort=0)
│   └── [BattleCanvas 자식 1개 - TargetingOverlay 포함 추정]
└── OverLayCanvas        (Screen Space - Overlay, Sort=10)
    ├── InsufficientCostMessage
    ├── AnnouncementPanel        ← BattleOverlayPanel 컴포넌트
    ├── Battles                  (VerticalLayoutGroup)
    │   ├── MiddleBar
    │   │   └── CardDescriptionPanel
    │   └── HandArea             ← HandLayoutController 컴포넌트
    │       ├── CostDisplayContainer
    │       │   └── CostDisplayPannel  ← CostDisplayPanel 컴포넌트
    │       │       └── [CostText TMP?]
    │       ├── HandCardContainer
    │       │   ├── [카드 컨테이너]
    │       │   └── [풀 루트]
    │       └── TurnEndPannel
    │           └── EndBtnContainer
    │               └── [TurnEndButton 컴포넌트]
    ├── CardPilePanel            ← CardPileDetailPanel 컴포넌트
    ├── MapPannel                ← MapScreenPresenter + MapOverlayController
    ├── TopBar
    │   └── BackGround           (HorizontalLayoutGroup)
    │       ├── RightContainer   (HorizontalLayoutGroup)
    │       │   ├── PlayerHealthBar
    │       │   │   ├── HeartImage  (Image)
    │       │   │   └── PlayerHealthText  ← TextMeshProUGUI ✅ 존재
    │       │   └── WaveBar      ← WaveIndicatorView 컴포넌트
    │       │       └── [WaveText TMP + 아이콘?]
    │       └── LeftContainer    ← ⚠️ 자식 없음 (삭제됨)
    └── FadeCanvas               (자체 Canvas 컴포넌트)
        └── FadeImage            ← Image + CanvasGroup ✅ 존재

Player GO
└── PlayerHealthBarView         ← PlayerHealthBarView 컴포넌트 (자식 없음, hpText=null)

Battlemanagers (자식 11개)
└── [BattleTurnController, BattleActionExecutor, BattleResultController,
    StageBootstrapper, DeckController, HandDealController,
    BattleAnnouncementController, BattleCameraController,
    BattleTargetingController, CardFlyAnimator, WaveIndicatorView? ...]
```

---

## 완전히 없는 GameObjects

| 오브젝트 | 스크립트 | 우선순위 | 위치 제안 |
|---|---|---|---|
| DrawPile 버튼 | CardPileButton (pileTarget=Draw) | ★★★ | LeftContainer 안 |
| DiscardPile 버튼 | CardPileButton (pileTarget=Discard) | ★★★ | LeftContainer 안 |
| DrawPile 카운트 | CardPileCountView | ★★★ | DrawPile 버튼 위/아래 |
| DiscardPile 카운트 | CardPileCountView | ★★★ | DiscardPile 버튼 위/아래 |
| RestPanel GO | RestPanelController | ★★ | OverLayCanvas 직속 |
| ShopPanel GO | ShopPanelController | ★★ | OverLayCanvas 직속 |

---

## 연결 끊긴 SerializeField

### Phase 1 — Inspector 연결만으로 해결 (빠름)

| 컴포넌트 GO | 필드명 | 연결 대상 |
|---|---|---|
| `Player/PlayerHealthBarView` | `hpText` | `TopBar/.../PlayerHealthBar/PlayerHealthText` (TMP_Text) |
| `TopBar/.../WaveBar` (WaveIndicatorView) | `waveText` | WaveBar 자식 TMP_Text 확인 후 연결 |
| `TopBar/.../WaveBar` (WaveIndicatorView) | `canvasGroup` | WaveBar 또는 상위 오브젝트의 CanvasGroup |
| `MapController` (MapFlowController) | `_fadeCanvasGroup` | `FadeCanvas/FadeImage` (CanvasGroup) |
| `MapController` (MapFlowController) | `_restPanel` | RestPanelController GO (Phase 3에서 생성) |
| `MapController` (MapFlowController) | `_shopPanel` | ShopPanelController GO (Phase 3에서 생성) |

### 추가 확인 필요 (플레이해보며 에러 확인)

| 컴포넌트 | 의심 필드 |
|---|---|
| CostDisplayPanel | `costText` (TMP_Text), `slideTarget` (RectTransform) |
| TurnEndButton | `slideTarget` (RectTransform) |
| HandLayoutController | `cardContainer`, `poolRoot` (Transform), `blockedDropAreas` |
| HandDealController | `handLayoutController`, `cardFlyAnimator` 연결 여부 |
| BattleActionExecutor | `deckController` |
| BattleAnnouncementController | `overlayPanel` (BattleOverlayPanel) |
| CardFlyAnimator | `pileAnchorView` |
| BattleTargetingController | `battleCamera` (Camera) |

---

## 복구 페이즈

### Phase 1 — PlayerHealthBarView hpText 연결 ← 현재 여기

**목표**: 플레이어 HP가 화면에 표시됨

1. `Player/PlayerHealthBarView` 오브젝트 선택
2. Inspector → `hpText` 슬롯에 `TopBar/BackGround/RightContainer/PlayerHealthBar/PlayerHealthText` 드래그

**검증**: 플레이 진입 시 HP가 "100/100" 형태로 표시됨

---

### Phase 2 — WaveIndicatorView waveText 연결

**목표**: 웨이브 시작 시 "1/3 웨이브" 텍스트 팝업 표시

1. `TopBar/.../WaveBar` 선택 → Inspector에서 WaveIndicatorView 확인
2. `waveText` 슬롯에 WaveBar 자식 TMP_Text 연결
3. `canvasGroup` 슬롯에 CanvasGroup 연결 (WaveBar에 없으면 추가)
4. `battleEventChannel` 슬롯에 BattleEventChannel SO 연결 (빠져있으면)

**검증**: 스테이지 진입 시 웨이브 팝업 표시됨

---

### Phase 3 — FadeCanvas + MapFlowController 연결

**목표**: 지도 노드 선택 시 페이드 인/아웃 작동

1. `MapController` 선택 → MapFlowController Inspector
2. `_fadeCanvasGroup` → `FadeCanvas/FadeImage` CanvasGroup 연결
3. FadeCanvas의 자체 Canvas Sort Order가 40인지 확인 (현재 OverLayCanvas=10 안에 있어서 overrideSorting 필요할 수 있음)

**검증**: 지도에서 노드 클릭 시 화면이 페이드 됨

---

### Phase 4 — CardPileButton + CardPileCountView 생성

**목표**: DrawPile / DiscardPile 버튼 + 장 수 표시

위치: `TopBar/BackGround/LeftContainer` 안에 생성

**DrawPile 버튼:**
```
GameObject: DrawPileButton
  Components:
    - RectTransform
    - Image (버튼 배경)
    - Button → OnClick 없음 (CardPileButton이 처리)
    - CardPileButton
        battleEventChannel = BattleEventChannel SO
        pileTarget = Draw
  자식:
    - TMP_Text "DrawCount" (ex: "20")
```

**DiscardPile 버튼:**
```
GameObject: DiscardPileButton
  Components: (동일)
    - CardPileButton
        pileTarget = Discard
```

**CardPileCountView** (카운트 자동 갱신이 필요하면 별도 컴포넌트):
- 현재 코드에 CardPileCountView 스크립트가 없음 → 버튼 내 TMP_Text를 DeckController 이벤트로 갱신하거나 직접 연결 방식 확인 필요

> ⚠️ `CardPileCountView` 스크립트가 씬에 없어서 find_gameobjects 결과 0. 스크립트 경로 확인 후 작업 필요.

**검증**: 화면 하단 좌측에 드로우/버린 카드 장 수 표시, 클릭 시 CardPilePanel 열림

---

### Phase 5 — Rest/Shop Panel 생성 (Phase 6 맵 기능)

**목표**: 지도 Rest/Shop 노드 선택 시 패널 열림, 나가기 클릭 시 지도로 복귀

**RestPanel:**
```
GameObject: RestPanel (OverLayCanvas 직속)
  Components:
    - RectTransform (전체화면)
    - RestPanelController
        _camera = RestCam (이미 씬에 있음)
        _panelRoot = 자신 or 자식 패널 루트
        _titleText = TMP_Text
        _descText = TMP_Text
        _exitButton = Button
```

**ShopPanel:** (동일 구조, ShopPanelController)

**MapFlowController 연결:**
- `_restPanel` → RestPanel GO
- `_shopPanel` → ShopPanel GO

**검증**: 지도에서 Rest 노드 클릭 → RestPanel 열림, "나가기" 클릭 → 페이드 후 지도 재개

---

### Phase 6 — 전투 UI 전체 검증

플레이 모드에서 콘솔 에러 없이 아래가 작동하면 완료:

- [ ] HP 표시 (플레이어)
- [ ] 코스트 표시 + 슬라이드 인/아웃
- [ ] 턴 종료 버튼 슬라이드 인/아웃
- [ ] 웨이브 팝업
- [ ] 카드 드래그 → 스킬 발동
- [ ] 드로우/버린 더미 버튼 + 카운트
- [ ] 페이드 전환 (지도↔전투)

---

## CardPileCountView 스크립트 위치 확인 필요

`find_gameobjects by_component CardPileCountView` → 결과 0  
스크립트 파일 경로: `Assets/00. Work/_Resources/02. Scripts/Battle/UI/CardPileCountView.cs`  
씬에 GO가 없으므로 Phase 4에서 새로 생성 필요. 스크립트 내 SerializeField 먼저 확인할 것.
