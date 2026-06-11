using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "TempStartCards", menuName = "Battle/Temp Start Cards")]
    public class TempStartCardSO : PlayerDeckProviderSO
    {
        [SerializeField] private List<CardDataSO> cards;

        public override List<CardDataSO> GetDeck() => cards;

        public void SetDeck(List<CardDataSO> deck) => cards = new List<CardDataSO>(deck);
    }
}
