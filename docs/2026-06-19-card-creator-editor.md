# Card Creator Editor

## 목적

CardDataSO를 빠르게 생성하고 카드 데이터베이스에 등록하는 EditorWindow.
매번 에셋 생성 → ID 설정 → 프레젠테이션 에셋 별도 생성 → DB 등록 순서를 거치는 불편함을 해소한다.

---

## 확정된 설계

| 항목 | 결정 |
|------|------|
| 형태 | `Window/Battle/Card Creator` 독립 EditorWindow |
| 레이아웃 | 2분할: 왼쪽 카드 목록(280px) / 오른쪽 편집 패널 |
| 카드 탐색 | 평면 목록 + ElementType 드롭다운 필터 + 이름 검색창 |
| 카드 에셋 경로 | `Assets/05. SO/Cards/PlayerCards/{elementType}/` |
| 프레젠테이션 경로 | `Assets/05. SO/Battle/SkillPresentations/{elementType}/` |
| 폴더 자동 생성 | 없으면 AssetDatabase.CreateFolder |
| cardId | 파일명과 동일 (자동 설정) |
| 파일명 형식 | `{ElementType}_{index:D2}_{이름}` (예: `Fire_01_Strike`) |
| index 계산 | 해당 elementType 폴더 내 기존 카드 수 + 1 |
| SkillPresentationDataSO | 항상 자동 생성 + 자동 연결 |
| effectSlots | SerializedObject 기반 Unity 기본 PropertyField |
| artwork | ObjectField + 128×128 미리보기 + 중복 사용 카드 경고 |
| 썸네일 (목록) | 32×32 AssetPreview + 카드 이름 |
| DB 등록 | 생성 시 체크박스 + 선택 카드 사후 토글 |
| DB 연결 방식 | 상단 ObjectField + EditorPrefs 경로 기억 |
| 복제 | 있음: 기존 카드 필드 복사 → 이름만 새로 입력 후 생성 |
| 저장 | 자동 (SetDirty + SaveAssets) |
| 삭제 | 없음 (Project 창 사용) |

---

## 파일 위치

```
Assets/00. Work/_Resources/02. Scripts/Battle/Data/Editor/CardCreatorWindow.cs
```

---

## Phase 1: 기본 EditorWindow 구조 + 2분할 레이아웃

### 목표
`Window/Battle/Card Creator` 메뉴로 창이 열리고, 상단 DB 슬롯과 2분할 레이아웃이 완성된다.

### 구현 항목

1. `CardCreatorWindow : EditorWindow` 클래스 생성 (`#if UNITY_EDITOR` 불필요 — Editor 폴더 내)
   - `[MenuItem("Window/Battle/Card Creator")]` 등록
   - `minSize = new Vector2(800, 600)`

2. **상단 DB 슬롯** (높이 약 44px):
   - `CardDatabaseSO` ObjectField: 변경 시 GUID를 `EditorPrefs.SetString("CardCreator_CardDbGuid", ...)` 저장
   - `TempStartCardSO` ObjectField: 동일 방식으로 `"CardCreator_StartDeckGuid"` 저장
   - `OnEnable` 시 EditorPrefs에서 GUID 로드 → `AssetDatabase.GUIDToAssetPath` → `AssetDatabase.LoadAssetAtPath` 로 복원

3. **2분할 레이아웃**:
   - `GUILayout.BeginHorizontal()`
   - 왼쪽: `GUILayout.BeginVertical(GUILayout.Width(280))`
   - 구분선: `Rect divider = GUILayoutUtility.GetRect(1, position.height); EditorGUI.DrawRect(divider, Color.gray * 0.5f)`
   - 오른쪽: `GUILayout.BeginVertical()`
   - 각 패널 내부는 빈 상태 (`GUILayout.FlexibleSpace()` 로 채움)

### 검증
- `Window/Battle/Card Creator` 메뉴로 창이 열린다
- 상단에 CardDatabaseSO, TempStartCardSO ObjectField가 보인다
- 창을 닫고 다시 열면 DB 에셋이 자동 복원된다
- 창 크기 변경 시 레이아웃이 유지된다

---

## Phase 2: 왼쪽 카드 목록 패널

### 목표
PlayerCards 폴더의 CardDataSO를 읽어 목록으로 표시하고 필터링 및 선택이 동작한다.

### 구현 항목

1. **카드 목록 로드**:
   ```
   AssetDatabase.FindAssets("t:CardDataSO", new[]{"Assets/05. SO/Cards/PlayerCards"})
   ```
   결과를 `_allCards: List<CardDataSO>` 에 저장.
   `OnEnable` + 왼쪽 패널 상단 "새로고침" 버튼으로 갱신.

2. **필터 UI** (왼쪽 패널 내부 상단):
   - `_filterElement: ElementType` 드롭다운 (None = 전체)
   - `_searchText: string` 검색창 (`EditorGUILayout.TextField`)
   - 두 조건 AND로 `_filteredCards: List<CardDataSO>` 계산

3. **카드 목록 ScrollView**:
   - `_scrollPos` 로 ScrollView 관리
   - 각 항목: 높이 36px
     - 32×32 썸네일: `AssetPreview.GetAssetPreview(card.artwork)` (null이면 기본 아이콘)
     - 카드 이름 텍스트
   - 클릭 시:
     - `_selectedCard = card`
     - `_selectedSerializedObject = new SerializedObject(card)`
     - `_isCreatingNew = false`
   - 선택된 항목: `EditorGUI.DrawRect` 배경 강조 (밝은 파랑)

4. **목록 하단 버튼**:
   - "새 카드" 버튼: `_isCreatingNew = true`, 임시 필드 초기화
   - "복제" 버튼: `_selectedCard != null` 일 때만 활성화 (Phase 5에서 동작 구현)

### 검증
- PlayerCards 폴더의 CardDataSO 목록이 표시된다
- ElementType 필터로 해당 속성 카드만 보인다
- 이름 검색이 동작한다
- 카드 선택 시 해당 항목 배경이 강조된다
- 썸네일이 표시된다 (artwork가 null인 경우 기본 아이콘)

---

## Phase 3: 오른쪽 편집 패널 + 카드 생성

### 목표
새 카드를 생성하거나 기존 카드를 편집할 수 있다.
생성 시 CardDataSO + SkillPresentationDataSO가 자동으로 만들어진다.

### 구현 항목

#### 3-1. 패널 모드 분기
- `_isCreatingNew: bool`
  - `true`: 신규 생성 폼 표시
  - `false && _selectedCard != null`: 기존 카드 편집 표시
  - `false && _selectedCard == null`: "왼쪽에서 카드를 선택하거나 새 카드를 만드세요" 안내 텍스트

#### 3-2. 신규 생성 폼 (임시 변수 관리)
```
_newElementType: ElementType
_newCardName: string           (이름 부분만 타이핑)
_newCost: int
_newTargetType: CardTargetType
_newDisposePolicy: CardDisposePolicy
_newGrade: CardGrade
_newCardType: CardType
_newDescription: string        (TextArea, 3줄)
_newArtwork: Sprite
_addToCardDb: bool             (체크박스)
_addToStartDeck: bool          (체크박스)
_isDuplicateMode: bool         (Phase 5 복제 모드 플래그)
```

- **파일명 미리보기** (읽기 전용 레이블):
  `{ElementType}_{index:D2}_{_newCardName}`
  - index: `Assets/05. SO/Cards/PlayerCards/{elementType}/` 내 기존 에셋 수 + 1
  - `_newCardName` 이 비어 있으면 `"..."` 표시

- **artwork 필드**:
  - ObjectField
  - Sprite 연결 시 128×128 미리보기 (`GUILayout.Label(texture, GUILayout.Width(128), GUILayout.Height(128))`)
  - 중복 검사: `_allCards` 중 동일 Sprite를 사용하는 카드가 있으면
    `EditorGUILayout.HelpBox("이미 {카드이름}에서 사용 중인 아이콘입니다.", MessageType.Warning)` 표시

#### 3-3. 생성 버튼 로직 (`CreateCard()`)
```
1. 유효성: _newCardName.Trim() 비어있으면 DisplayDialog 경고 후 중단
2. 카드 폴더 경로: $"Assets/05. SO/Cards/PlayerCards/{_newElementType}"
   - AssetDatabase.IsValidFolder 로 확인, 없으면 AssetDatabase.CreateFolder
3. index 계산: AssetDatabase.FindAssets("t:CardDataSO", new[]{cardFolder}).Length + 1
4. 파일명: $"{_newElementType}_{index:D2}_{_newCardName.Trim()}"
5. CardDataSO 생성:
   - ScriptableObject.CreateInstance<CardDataSO>()
   - 모든 임시 필드 세팅
   - card.cardId = fileName
   - card.cardName = _newCardName (또는 별도 입력값)
   - AssetDatabase.CreateAsset(card, $"{cardFolder}/{fileName}.asset")
6. SkillPresentationDataSO 생성:
   - presFolder: $"Assets/05. SO/Battle/SkillPresentations/{_newElementType}"
   - 폴더 없으면 CreateFolder
   - ScriptableObject.CreateInstance<SkillPresentationDataSO>()
   - AssetDatabase.CreateAsset(pres, $"{presFolder}/{fileName}_Presentation.asset")
7. card.presentationData = pres
   EditorUtility.SetDirty(card)
8. DB 등록:
   - _addToCardDb && _cardDb != null: _cardDb.allCards.Add(card); SetDirty(_cardDb)
   - _addToStartDeck && _startDeckSO != null: _startDeckSO.GetDeck().Add(card); SetDirty(_startDeckSO)
9. AssetDatabase.SaveAssets()
10. 목록 갱신 (LoadAllCards 재호출)
11. _selectedCard = card, _selectedSerializedObject = new SerializedObject(card), _isCreatingNew = false
```

#### 3-4. 기존 카드 편집 (SerializedObject 기반)
오른쪽 패널 내부 OnGUI:
```csharp
_selectedSerializedObject.Update();

// cardName, cost, targetType, disposePolicy, elementType, grade, cardType, description
// 각각 EditorGUILayout.PropertyField(_selectedSerializedObject.FindProperty("fieldName"))

// artwork: ObjectField + 미리보기 + 중복 경고 (신규 생성 폼과 동일 로직)

// effectSlots
var effectSlotsProp = _selectedSerializedObject.FindProperty("effectSlots");
EditorGUILayout.PropertyField(effectSlotsProp, new GUIContent("Effect Slots"), true);

if (_selectedSerializedObject.ApplyModifiedProperties())
{
    EditorUtility.SetDirty(_selectedCard);
    AssetDatabase.SaveAssets();
}
```

### 검증
- "새 카드" 클릭 후 ElementType 선택 시 파일명 미리보기가 실시간 갱신된다
- 이름 입력 없이 생성 버튼 클릭 시 경고 다이얼로그가 뜬다
- 생성 후 `Assets/05. SO/Cards/PlayerCards/{elementType}/` 에 CardDataSO 파일이 생긴다
- `Assets/05. SO/Battle/SkillPresentations/{elementType}/` 에 Presentation 파일이 생긴다
- 생성된 카드의 `presentationData` 가 자동 연결되어 있다
- 생성된 카드의 `cardId` 가 파일명과 일치한다
- DB 체크박스에 따라 등록 여부가 반영된다
- 기존 카드 선택 시 수정이 즉시 에셋에 저장된다
- 중복 artwork 경고가 표시된다

---

## Phase 4: DB 등록 관리

### 목표
오른쪽 패널 하단에서 현재 선택된 카드의 DB 등록 상태를 보고 토글할 수 있다.

### 구현 항목

오른쪽 패널 하단 "── DB 관리 ──" 구분선 이후:

1. **전체 DB (`CardDatabaseSO`)**:
   - 등록 여부: `_cardDb != null && _cardDb.allCards.Contains(_selectedCard)`
   - 등록됨 → "✓ 전체 DB 등록됨" + "해제" 버튼
   - 미등록 → "✗ 전체 DB 미등록" + "등록" 버튼
   - 버튼 클릭: Add/Remove → `EditorUtility.SetDirty(_cardDb)` → `AssetDatabase.SaveAssets()`

2. **시작 덱 (`TempStartCardSO`)**:
   - 등록 여부: `_startDeckSO != null && _startDeckSO.GetDeck().Contains(_selectedCard)`
   - 동일 패턴으로 토글
   - Add: `_startDeckSO.GetDeck().Add(card)`
   - Remove: `_startDeckSO.RemoveCard(card)`

3. DB 에셋이 연결되지 않은 경우:
   - `EditorGUILayout.HelpBox("상단에서 DB를 연결해주세요.", MessageType.Info)` 표시

### 검증
- 카드 선택 시 두 DB의 등록 여부가 정확히 표시된다
- 등록/해제 버튼 클릭 후 즉시 상태가 바뀐다
- 저장 후 에디터 재시작 시에도 등록 상태가 유지된다

---

## Phase 5: 복제 기능

### 목표
선택된 카드를 기반으로 새 에셋을 복제 생성한다.

### 구현 항목

1. **"복제" 버튼 클릭** (왼쪽 패널 하단):
   - 신규 생성 모드(`_isCreatingNew = true`)로 전환
   - 임시 필드를 원본 카드 값으로 초기화:
     ```
     _newElementType = _selectedCard.elementType
     _newCardName = ""           // 이름만 비워서 새로 입력받음
     _newCost = _selectedCard.cost
     _newTargetType = _selectedCard.targetType
     _newDisposePolicy = _selectedCard.disposePolicy
     _newGrade = _selectedCard.grade
     _newCardType = _selectedCard.cardType
     _newDescription = _selectedCard.description
     _newArtwork = _selectedCard.artwork  // 중복 경고 표시됨
     _addToCardDb = false
     _addToStartDeck = false
     _isDuplicateMode = true
     _duplicateSource = _selectedCard     // effectSlots 복사 원본 보관
     ```
   - 오른쪽 패널 상단에 `"[복제 모드] 원본: {원본카드이름}"` 안내 레이블 표시

2. **생성 버튼 클릭 시 추가 동작** (`_isDuplicateMode == true`):
   - effectSlots 복사:
     ```csharp
     string srcJson = JsonUtility.ToJson(_duplicateSource);
     var tempSrc = ScriptableObject.CreateInstance<CardDataSO>();
     JsonUtility.FromJsonOverwrite(srcJson, tempSrc);
     newCard.effectSlots = tempSrc.effectSlots;
     DestroyImmediate(tempSrc);
     ```
   - 이후 `EnsureEffectSlotIds()` 호출로 GUID 재생성
   - 나머지 생성 로직은 Phase 3와 동일

### 검증
- "복제" 버튼 클릭 시 신규 생성 모드로 전환되고 원본 필드가 채워진다
- 이름은 비어 있어 새로 입력받는다
- 생성 후 별도 에셋이 만들어진다
- effectSlots 내용이 복사되고 GUID가 새로 부여된다
- 원본 카드는 변경되지 않는다

---

## 완료 기준

모든 Phase 완료 후:
- [ ] `Window/Battle/Card Creator` 에서 창이 열린다
- [ ] CardDataSO + SkillPresentationDataSO 에셋이 한 번에 생성된다
- [ ] 파일명 = cardId, 속성별 폴더 자동 분류
- [ ] 전체 DB / 시작 덱 등록이 에디터 내에서 관리된다
- [ ] 기존 카드 선택 → 편집 → 자동 저장
- [ ] 복제 기능 동작
