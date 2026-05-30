namespace Agents.StatSystem
{
    public interface IStatModule
    {
        StatSO[] GetAllStats();
        StatSO GetStat(int index);
        bool TryGetStat(int index, out StatSO outStat);
        void AddModifier(int index, object key, float value);
        void RemoveModifier(int index, object key);
        float SubscribeStat(int index, StatSO.ValueChangeHandler handler, float defaultValue);
        void UnSubscribeStat(int index, StatSO.ValueChangeHandler handler);
    }
}