using System;
using Battle.Data;

namespace Battle.Instances
{
    [Serializable]
    public class CardInstance
    {
        public string instanceId;
        public CardDataSO data;

        public CardInstance(CardDataSO data)
        {
            this.data = data;
            instanceId = Guid.NewGuid().ToString();
        }
    }
}
