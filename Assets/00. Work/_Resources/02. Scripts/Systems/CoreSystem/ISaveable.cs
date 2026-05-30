namespace _00._Work._Resources._02._Scripts.Systems.CoreSystem
{
    public interface ISaveable
    {
        SaveIdData SaveId {get;}
        string GetSaveData();
        void RestoreData(string data);
    }
}