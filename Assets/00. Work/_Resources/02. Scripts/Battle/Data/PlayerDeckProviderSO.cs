using System.Collections.Generic;
using UnityEngine;

namespace Battle.Data
{
    public abstract class PlayerDeckProviderSO : ScriptableObject
    {
        public abstract List<CardDataSO> GetDeck();
        public abstract bool RemoveCard(CardDataSO card);
    }
}
