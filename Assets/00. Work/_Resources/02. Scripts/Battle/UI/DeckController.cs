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
        [SerializeField] private PlayerRunStateSO     playerRunState;
        [SerializeField] private EventChannelSO       battleEventChannel;

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
        public IReadOnlyList<CardDataSO> CurrentDeckCards
        {
            get
            {
                var list = new List<CardDataSO>(deckProvider.GetDeck());
                if (playerRunState != null)
                    list.AddRange(playerRunState.CurrentPile);
                return list;
            }
        }

        private void Start() => RefreshCurrentDeckCount();

        private void OnEnable()
        {
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.AddListener<DiscardCardsEvent>(OnDiscardCards);
            battleEventChannel.AddListener<RandomDrawRequestEvent>(OnRandomDrawRequest);
            battleEventChannel.AddListener<CreateTempCardsRequestEvent>(OnCreateTempCardsRequest);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<DiscardCardsEvent>(OnDiscardCards);
            battleEventChannel.RemoveListener<RandomDrawRequestEvent>(OnRandomDrawRequest);
            battleEventChannel.RemoveListener<CreateTempCardsRequestEvent>(OnCreateTempCardsRequest);
        }

        private void OnBattleEnded(BattleVictoryEvent _) => Cleanup();
        private void OnBattleEnded(BattleDefeatEvent _)  => Cleanup();

        private void OnDiscardCards(DiscardCardsEvent evt)
        {
            if (evt.Cards == null) return;
            foreach (var card in evt.Cards)
                ForceDiscard(card);
        }

        private void OnRandomDrawRequest(RandomDrawRequestEvent evt)
        {
            var drawn = evt.SourcePile == PileType.DrawPile
                ? DrawCards(evt.DrawCount)
                : DrawFromSpecificPile(evt.SourcePile, evt.DrawCount);
            evt.Tcs.TrySetResult(drawn);
        }

        private void OnCreateTempCardsRequest(CreateTempCardsRequestEvent evt)
        {
            var created = new List<CardInstance>();
            for (int i = 0; i < evt.Count; i++)
            {
                var card = new CardInstance(evt.CardData);
                AddFusionCard(card);
                created.Add(card);
            }
            evt.Tcs.TrySetResult(created);
        }

        private List<CardInstance> DrawFromSpecificPile(PileType sourcePile, int count)
        {
            var source = sourcePile switch
            {
                PileType.DiscardPile => _discardPile,
                PileType.GravePile   => _gravePile,
                _                    => null
            };

            var drawn = new List<CardInstance>();
            if (source == null || source.Count == 0) return drawn;

            int take = Mathf.Min(count, source.Count);
            for (int i = 0; i < take; i++)
            {
                int idx = UnityEngine.Random.Range(0, source.Count);
                var card = source[idx];
                source.RemoveAt(idx);
                card.currentPile = PileType.Hand;
                _hand.Add(card);
                drawn.Add(card);
            }

            NotifyPileChanged();
            return drawn;
        }

        public void Initialize()
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _gravePile.Clear();

            var deck = deckProvider.GetDeck();
            foreach (var data in deck)
            {
                var instance = new CardInstance(data) { currentPile = PileType.DrawPile };
                _drawPile.Add(instance);
            }

            // 런 중 획득 카드(CurrentPile) 추가
            if (playerRunState != null)
            {
                foreach (var data in playerRunState.CurrentPile)
                {
                    var instance = new CardInstance(data) { currentPile = PileType.DrawPile };
                    _drawPile.Add(instance);
                }
            }

            _currentDeckCount = _drawPile.Count;

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

        // 보상으로 획득한 카드를 현재 배틀 DrawPile에 즉시 추가
        public void AddRewardCard(CardDataSO data)
        {
            var instance = new CardInstance(data) { currentPile = PileType.DrawPile };
            _drawPile.Add(instance);
            _currentDeckCount++;
            NotifyPileChanged();
        }

        // 상점 버리기 등 배틀 외에서 전체 덱(시작덱 + CurrentPile)에서 카드 1장 제거
        public bool RemoveCardFromDeck(CardDataSO card)
        {
            if (playerRunState != null && playerRunState.RemoveCard(card))
            {
                RefreshCurrentDeckCount();
                return true;
            }
            if (deckProvider != null && deckProvider.RemoveCard(card))
            {
                RefreshCurrentDeckCount();
                return true;
            }
            return false;
        }

        // 배틀 외(상점 등)에서 SO 기반으로 덱 카운트를 재계산해 UI 갱신
        public void RefreshCurrentDeckCount()
        {
            int baseCount        = deckProvider   != null ? deckProvider.GetDeck().Count            : 0;
            int currentPileCount = playerRunState  != null ? playerRunState.CurrentPile.Count        : 0;
            _currentDeckCount = baseCount + currentPileCount;
            NotifyPileChanged();
        }

        // 합성 결과 카드를 손패에 직접 추가 — 영구 덱에 편입되지 않으므로 다음 스테이지 Initialize() 시 자연 소멸
        public void AddFusionCard(CardInstance card)
        {
            card.currentPile = PileType.Hand;
            _hand.Add(card);
            NotifyPileChanged();
        }

        // 합성 재료 카드 소모 — disposePolicy 무관하게 항상 GravePile로 이동
        public void ConsumeFusionMaterial(CardInstance card)
        {
            _hand.Remove(card);
            card.currentPile = PileType.GravePile;
            _gravePile.Add(card);
            NotifyPileChanged();
        }

        // Fizzle 발생 시 호출 — disposePolicy 무관하게 항상 DiscardPile로 강제 이동
        public void ForceDiscard(CardInstance card)
        {
            _hand.Remove(card);
            MoveToDiscard(card);
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

        // 턴 종료 시 손패 전체 버리기 — 미사용 카드는 disposePolicy와 무관하게 항상 DiscardPile로 순환
        public void DiscardAllHand()
        {
            foreach (var card in _hand)
            {
                card.currentPile = PileType.DiscardPile;
                _discardPile.Add(card);
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
            RefreshCurrentDeckCount();
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
