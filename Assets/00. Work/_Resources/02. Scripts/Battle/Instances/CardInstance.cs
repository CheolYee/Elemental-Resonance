using System;
using Battle.Data;
using Battle.Enums;

namespace Battle.Instances
{
    [Serializable]
    public class CardInstance
    {
        public string instanceId;
        public CardDataSO data;
        public CardGrade grade;

        public CardInstance(CardDataSO data, CardGrade grade = CardGrade.Normal)
        {
            this.data = data;
            this.grade = grade;
            instanceId = Guid.NewGuid().ToString();
        }
    }
}
