using Battle.Enums;
using UnityEngine;

namespace Battle.UI
{
    public class PileAnchorView : MonoBehaviour
    {
        [SerializeField] private RectTransform drawPileRect;
        [SerializeField] private RectTransform discardPileRect;
        [SerializeField] private RectTransform gravePileRect;
        [SerializeField] private RectTransform currentDeckRect;

        public Vector2 GetScreenPosition(PileDisplayTarget target) => target switch
        {
            PileDisplayTarget.Draw        => drawPileRect.position,
            PileDisplayTarget.Discard     => discardPileRect.position,
            PileDisplayTarget.Grave       => gravePileRect.position,
            PileDisplayTarget.CurrentDeck => currentDeckRect.position,
            _                            => Vector2.zero
        };
    }
}
