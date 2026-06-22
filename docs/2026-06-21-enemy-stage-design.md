# 적 & 스테이지 설계 (2026-06-21)

상태: **확정**

---

## 1. 몬스터 로스터

| 분류 | 에셋 | 기본 HP | 스킬 수 |
|------|------|---------|---------|
| 일반몹 1 | Treant Minion Evergreen | 30 | 2 |
| 일반몹 2 | Imp Devil | 40 | 2 |
| 일반몹 3 | Cyclops Bat Wizard | 50 | 2 |
| 일반몹 4 | Dragon Blizzard | 60 | 2 |
| 엘리트 1 | Cyclops Bat Mage | 130 | 2 |
| 엘리트 2 | Dragon Water | 170 | 2 |
| 보스 | Treant Forest Evergreen | 200 | 2 |

> 스킬 2개 = 타임라인 에디터에서 각 1개씩 (공격 패턴 A/B 랜덤 선택)

---

## 2. HP 스케일링

**공식:** `effectiveHP = round(baseHP × (1.0 + 0.12 × floorIndex))`

| Floor | 배수 | Treant Minion | Dragon Blizzard | 엘리트 | 보스 |
|-------|------|--------------|-----------------|--------|------|
| 1 | ×1.12 | 34 | 67 | — | — |
| 4 | ×1.48 | 44 | 89 | 192 (Bat Mage) | — |
| 7 | ×1.84 | — | 110 | 313 (Dragon Water) | — |
| 10 | ×2.20 | — | — | — | 440 (Boss) |

**구현:** `StageBootstrapper.SpawnWave()` 내부에서 스폰 직후
```csharp
float multiplier = 1f + 0.12f * (playerRunState?.CurrentFloorIndex ?? 0);
int scaledHp = Mathf.RoundToInt(entry.enemyData.maxHp * multiplier);
enemy.Health.InitializeHp(scaledHp, scaledHp);
```

---

## 3. 맵 구조 (10층)

```
Floor 0:  START
Floor 1:  Battle A / Battle B
Floor 2:  Battle  (합류)
Floor 3:  Battle / Rest
Floor 4:  Elite(Bat Mage) / Shop
Floor 5:  Battle A / Battle B
Floor 6:  Battle / Rest
Floor 7:  Elite (Dragon Water)
Floor 8:  Shop / Battle
Floor 9:  Battle (합류)
Floor 10: BOSS (Treant Forest Evergreen)
```

---

## 4. 스테이지 Wave 구성 (BattleStageSO)

| 에셋명 | Wave | 등장 적 | goldBonus |
|--------|------|---------|-----------|
| Stage_F1A | 1 | Treant Minion × 2 | 0 |
| Stage_F1B | 1 | Treant Minion × 2 | 0 |
| Stage_F2 | 1 | Treant Minion + Imp Devil | 0 |
| Stage_F3 | 1 | Imp Devil × 2 | 0 |
| Stage_F4_Elite | 1 | Cyclops Bat Mage | **45** |
| Stage_F5A | 1 | Imp Devil + Bat Wizard | 0 |
| Stage_F5B | 1 | Bat Wizard × 2 | 0 |
| Stage_F6 | 1 | Bat Wizard + Imp Devil | 0 |
| Stage_F7_Elite | 1 | Dragon Water | **45** |
| Stage_F8 | 1 | Dragon Blizzard + Bat Wizard | 0 |
| Stage_F9 | 1 | Dragon Blizzard + Imp Devil | 0 |
| Stage_F10_Boss | 1 | Treant Forest Evergreen | **90** |

---

## 5. 골드 시스템 개편

### RewardConfigSO 기본값

| 필드 | 기존 | 변경 |
|------|------|------|
| baseGold | 50 | 25 |
| perFloorBonus | 10 | 6 |
| randomVariance | 15 | 8 |

### goldBonus (BattleStageSO 필드)

| 노드 타입 | goldBonus | Floor 4 예시 | Floor 9 예시 |
|-----------|-----------|-------------|-------------|
| 일반 전투 | 0 | 41~57g | 71~87g |
| 엘리트 | 45 | 86~102g | 116~132g |
| 보스 | 90 | — | 161g (Floor 10) |

### 계산식

```csharp
public int CalculateGold(int floorIndex, int goldBonus = 0)
{
    int variance = randomVariance > 0
        ? Random.Range(-randomVariance, randomVariance + 1) : 0;
    return Mathf.Max(0, baseGold + floorIndex * perFloorBonus + goldBonus + variance);
}
```

---

## 6. 상점 가격 조정 (ShopConfigSO Inspector)

| 항목 | 기존 | 변경 |
|------|------|------|
| Normal 카드 | 50 | 40 |
| Rare 카드 | 100 | 80 |
| Epic 카드 | 180 | 150 |
| Legendary 카드 | 300 | 240 |
| Reroll | 50 | 35 |
| Remove Card | 75 | 60 |

> ShopConfigSO는 코드 기본값 없음 — Inspector에서 직접 수정

---

## 7. 한 런 예상 경제 흐름

- 일반 전투 7회 × 평균 50g = 350g
- 엘리트 전투 2회 × 평균 110g = 220g
- 보스 1회 × 160g = 160g
- **총 수입 ~730g**

상점 방문 2회 × (Normal 40g × 2장 + Reroll 35g × 1회) = ~195g 지출  
**결과:** 한 런에 Normal 카드 4~6장 추가 가능
