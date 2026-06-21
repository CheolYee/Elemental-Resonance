using System.Collections.Generic;
using Battle.Instances;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace Battle.Events
{
    public class SkillDrawCardsRequestEvent : GameEvent
    {
        public int DrawCount { get; }
        public IReadOnlyList<CardInstance> PreDrawnCards { get; }
        public UniTaskCompletionSource Tcs { get; }

        // 기존: drawCount 기반 (HandDealController가 DrawCards 호출)
        public SkillDrawCardsRequestEvent(int drawCount, UniTaskCompletionSource tcs)
        {
            DrawCount = drawCount;
            Tcs = tcs;
        }

        // 신규: 이미 Hand로 이동된 카드 목록 전달 (AddCard 애니메이션만 수행)
        public SkillDrawCardsRequestEvent(IReadOnlyList<CardInstance> preDrawnCards, UniTaskCompletionSource tcs)
        {
            PreDrawnCards = preDrawnCards;
            Tcs = tcs;
        }
    }
}
