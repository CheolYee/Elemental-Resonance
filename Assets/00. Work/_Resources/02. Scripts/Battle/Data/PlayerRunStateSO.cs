using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "PlayerRunState", menuName = "Battle/Player Run State")]
    public class PlayerRunStateSO : ScriptableObject
    {
        [SerializeField] private int _gold;
        [SerializeField] private List<CardDataSO> _currentPile = new();

        private int _currentFloorIndex;
        private int _currentHp;
        private int _maxHp;

        public int Gold => _gold;
        public int CurrentFloorIndex => _currentFloorIndex;
        public IReadOnlyList<CardDataSO> CurrentPile => _currentPile;
        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsHpInitialized => _maxHp > 0;

        public void SetFloorIndex(int index) => _currentFloorIndex = index;

        public void SetHp(int currentHp, int maxHp)
        {
            _maxHp = maxHp;
            _currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }

        public void Heal(int amount)
        {
            if (_maxHp <= 0 || amount <= 0) return;
            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
        }

        public void Reset()
        {
            _gold = 0;
            _currentFloorIndex = 0;
            _currentPile.Clear();
            _currentHp = 0;
            _maxHp = 0;
        }

        // 반환값: 변경 전 골드 — 호출부에서 GoldChangedEvent 발행에 사용
        public int AddGold(int amount)
        {
            int old = _gold;
            _gold = Mathf.Max(0, _gold + amount);
            return old;
        }

        public int SpendGold(int cost)
        {
            int old = _gold;
            _gold = Mathf.Max(0, _gold - cost);
            return old;
        }

        public bool CanAfford(int cost) => _gold >= cost;

        public void AddCard(CardDataSO card)
        {
            if (card != null) _currentPile.Add(card);
        }

        public bool RemoveCard(CardDataSO card) => _currentPile.Remove(card);

        public void LoadFromSave(int gold, int currentHp, int maxHp, int floorIndex, List<CardDataSO> cards)
        {
            _gold              = gold;
            _currentHp         = currentHp;
            _maxHp             = maxHp;
            _currentFloorIndex = floorIndex;
            _currentPile.Clear();
            _currentPile.AddRange(cards);
        }
    }
}
