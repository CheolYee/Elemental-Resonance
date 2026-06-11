using Battle.Enums;
using Battle.Events;
using Gamelib.EventSystem;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class CardPileCountView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private PileDisplayTarget pileTarget;
        [SerializeField] private TMP_Text countText;

        private void OnEnable() =>
            battleEventChannel.AddListener<PileCountChangedEvent>(OnPileChanged);

        private void OnDisable() =>
            battleEventChannel.RemoveListener<PileCountChangedEvent>(OnPileChanged);

        private void OnPileChanged(PileCountChangedEvent evt)
        {
            countText.text = pileTarget switch
            {
                PileDisplayTarget.Draw        => evt.DrawCount.ToString(),
                PileDisplayTarget.Hand        => evt.HandCount.ToString(),
                PileDisplayTarget.Discard     => evt.DiscardCount.ToString(),
                PileDisplayTarget.Grave       => evt.GraveCount.ToString(),
                PileDisplayTarget.CurrentDeck => evt.CurrentDeckCount.ToString(),
                _                            => "0"
            };
        }
    }

}
