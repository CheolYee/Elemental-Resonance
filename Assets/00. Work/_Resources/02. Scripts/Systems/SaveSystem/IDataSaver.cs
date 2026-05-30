using System.IO;

namespace _00._Work._Resources._02._Scripts.Systems.SaveSystem
{
    public interface IDataSaver
    {
        // System.object를 저장 파일에 저장합니다
        void Save(object objectToSave, FileStream saveFile);

        // 저장된 파일에서 System.object를 반환하고, System.Type을 반환합니다(다양한 형식을 지원함).
        object Load(System.Type objectType, FileStream saveFile);
    }
}