using System;
using System.IO;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.SaveSystem.Savers
{
    /// <summary>
    /// 이 클래스는 IDataSave 인터페이스를 구현하여 JSON 파일을 저장하고 불러오며,
    /// System.IO의 StreamReader 또는 StreamWriter를 사용합니다. 이 기능은 SaveManager 클래스와 연동됩니다.
    /// 각 게임은 저장 데이터를 포함하는 고유한 System.Object를 사용하므로,
    /// 애플리케이션마다 고유하게 설정할 수 있습니다.
    /// </summary>
    public class JsonSaver : IDataSaver
    {
        // 이 코드는 게임 데이터가 포함된 System.Object 객체를 지정된 saveFile 파일 스트림에 저장합니다.
        public void Save(object objectToSave, FileStream saveFile)
        {
            // 저장 데이터 객체를 JSON으로 직렬화합니다
            string json = JsonUtility.ToJson(objectToSave);

            // FileStream 및 StreamWriter를 작성하고 자동으로 닫습니다
            using StreamWriter streamWriter = new StreamWriter(saveFile, System.Text.Encoding.UTF8);
            streamWriter.Write(json);
        }

        // typeToLoad는 애플리케이션마다 다르므로 저장 데이터 파일을 구분할 수 있습니다.
        // 또는 동일한 애플리케이션 내의 여러 데이터 유형을 구분할 수 있습니다.
        public object Load(Type typeToLoad, FileStream saveFile)
        {
            // FileStream과 StreamReader를 읽고 자동으로 닫습니다
            using StreamReader streamReader = new StreamReader(saveFile, System.Text.Encoding.UTF8);
            string json = streamReader.ReadToEnd();
            return JsonUtility.FromJson(json, typeToLoad);
        }
    }
}