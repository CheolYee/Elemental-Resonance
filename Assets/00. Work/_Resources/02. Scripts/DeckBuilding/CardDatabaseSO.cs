using System.Collections.Generic;
using Battle.Data;
using UnityEngine;

namespace DeckBuilding
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "DeckBuilding/Card Database")]
    public class CardDatabaseSO : ScriptableObject
    {
        public List<CardDataSO> allCards = new();
        public int maxDeckSize = 12;
    }
}
