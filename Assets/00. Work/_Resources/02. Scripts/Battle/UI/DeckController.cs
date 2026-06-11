using System.Collections.Generic;
using Battle.Data;
using Battle.Enums;
using Battle.Events;
using Battle.Instances;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class DeckController : MonoBehaviour
    {
        [SerializeField] private PlayerDeckProviderSO deckProvider;
        [SerializeField] private EventChannelSO battleEventChannel;

        private readonly List<CardInstance> _drawPile = new();
        private readonly List<CardInstance> _hand = new();
        private readonly List<CardInstance> _discardPile = new();
        private readonly List<CardInstance> _gravePile = new();
        private int _currentDeckCount;

        public int DrawCount    => _drawPile.Count;
        public int HandCount    => _hand.Count;
        public int DiscardCount => _discardPile.Count;
        public int GraveCount   => _gravePile.Count;

        public IReadOnlyList<CardInstance> DrawPileCards => _drawPile;
        public IReadOnlyList<CardInstance> HandCards     => _hand;
        public IReadOnlyList<CardInstance> DiscardCards  => _discardPile;
        public IReadOnlyList<CardInstance> GraveCards    => _gravePile;
        public IReadOnlyList<CardDataSO>   CurrentDeckCards => deckProvider.GetDeck();

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnBattleEnded(BattleVictoryEvent _) => Cleanup();
        private void OnBattleEnded(BattleDefeatEvent _)  => Cleanup();

        public void Initialize()
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _gravePile.Clear();

            var deck = deckProvider.GetDeck();
            _currentDeckCount = deck.Count;
            foreach (var data in deck)
            {
                var instance = new CardInstance(data) { currentPile = PileType.DrawPile };
                _drawPile.Add(instance);
            }

            Shuffle(_drawPile);
            NotifyPileChanged();
        }

        public List<CardInstance> DrawCards(int count)
        {
            var drawn = new List<CardInstance>();

            int fromDraw = Mathf.Min(count, _drawPile.Count);
            for (int i = 0; i < fromDraw; i++)
            {
                var card = _drawPile[^1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                card.currentPile = PileType.Hand;
                _hand.Add(card);
                drawn.Add(card);
            }

            int remaining = count - fromDraw;
            if (remaining > 0 && _discardPile.Count > 0)
            {
                int refillCount = _discardPile.Count;
                foreach (var card in _discardPile)
                {
                    card.currentPile = PileType.DrawPile;
                    _drawPile.Add(card);
                }
                _discardPile.Clear();
                Shuffle(_drawPile);
                battleEventChannel.RaiseEvent(new DrawPileRefillEvent(refillCount));

                int fromRefill = Mathf.Min(remaining, _drawPile.Count);
                for (int i = 0; i < fromRefill; i++)
                {
                    var card = _drawPile[^1];
                    _drawPile.RemoveAt(_drawPile.Count - 1);
                    card.currentPile = PileType.Hand;
                    _hand.Add(card);
                    drawn.Add(card);
                }
            }

            NotifyPileChanged();
            return drawn;
        }

        // 카드 사용 시 호출 — disposePolicy 기준으로 DiscardPile 또는 GravePile로 이동
        public void UseCard(CardInstance card)
        {
            _hand.Remove(card);
            if (card.data.disposePolicy == CardDisposePolicy.Grave)
                MoveToGrave(card);
            else
                MoveToDiscard(card);
        }

        // 턴 종료 시 손패 전체 버리기 — C-11: disposePolicy 기준으로 분기
        public void DiscardAllHand()
        {
            foreach (var card in _hand)
            {
                if (card.data.disposePolicy == CardDisposePolicy.Grave)
                {
                    card.currentPile = PileType.GravePile;
                    _gravePile.Add(card);
                }
                else
                {
                    card.currentPile = PileType.DiscardPile;
                    _discardPile.Add(card);
                }
            }
            _hand.Clear();
            NotifyPileChanged();
        }

        // C-12: Stage 종료 시 런타임 더미 폐기
        private void Cleanup()
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _gravePile.Clear();
        }

        private void MoveToDiscard(CardInstance card)
        {
            card.currentPile = PileType.DiscardPile;
            _discardPile.Add(card);
            NotifyPileChanged();
        }

        private void MoveToGrave(CardInstance card)
        {
            card.currentPile = PileType.GravePile;
            _gravePile.Add(card);
            NotifyPileChanged();
        }

        private void NotifyPileChanged()
        {
            battleEventChannel.RaiseEvent(new PileCountChangedEvent(
                _drawPile.Count, _hand.Count, _discardPile.Count, _gravePile.Count, _currentDeckCount));
        }

        // C-13: 더미 무결성 검증 (Editor 전용)
        [ContextMenu("Validate Piles")]
        public void ValidatePiles()
        {
            var seen = new HashSet<string>();
            bool hasDuplicate = false;

            void Check(IEnumerable<CardInstance> pile, string pileName)
            {
                foreach (var card in pile)
                {
                    if (!seen.Add(card.instanceId))
                    {
                        Debug.LogWarning($"[DeckController] 중복 카드 감지 in {pileName}: {card.data.cardName} ({card.instanceId})");
                        hasDuplicate = true;
                    }
                }
            }

            Check(_drawPile, "DrawPile");
            Check(_hand, "Hand");
            Check(_discardPile, "DiscardPile");
            Check(_gravePile, "GravePile");

            int total = _drawPile.Count + _hand.Count + _discardPile.Count + _gravePile.Count;
            if (!hasDuplicate)
                Debug.Log($"[DeckController] 검증 통과 — Draw:{_drawPile.Count} Hand:{_hand.Count} Discard:{_discardPile.Count} Grave:{_gravePile.Count} Total:{total}");
        }

        private static void Shuffle(List<CardInstance> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
