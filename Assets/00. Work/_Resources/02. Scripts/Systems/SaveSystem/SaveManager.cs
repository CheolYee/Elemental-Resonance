using System;
using System.IO;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem.Savers;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems.SaveSystem
{
    public static class SaveManager
    {
        // 저장된 객체를 새로운 파일 형식으로 변환하는 직렬화기; 기본값은 JSON입니다
        private static IDataSaver _dataSaver = new JsonSaver();

        // 모든 파일을 체계적으로 정리하기 위해
        // 다음 폴더 및 하위 폴더 이름을 사용하세요(예: 게임이나 앱마다 새로운 하위 폴더를 생성하세요).
        private const string SaveFolder = "Saves";
        private const string DefaultSubFolder = "Default";
        private const string DefaultFileName = "Data.save";

        // Properties

        // 직렬화기의 유형 (JSON, XML 등)
        public static IDataSaver SaveDataSerializer
        {
            get => _dataSaver;
            set => _dataSaver = value ?? throw new ArgumentNullException(nameof(value));
        }

        // 이 메서드는 전체 저장 경로를 반환합니다
        public static string GetSavePath(string subFolder = DefaultSubFolder)
        {
            return Path.Combine(Application.persistentDataPath, SaveFolder, subFolder);
        }
        
        // 이 메서드는 전체 저장 파일 경로에서 서브 폴더의 파일 경로를 반환합니다.
        public static string GetSaveFilePath(string fileName = DefaultFileName, string subFolder = DefaultSubFolder)
        {
            return Path.Combine(GetSavePath(subFolder), fileName);
        }

        // 저장 파일이 이미 있는지 확인합니다
        public static bool IsSaveFile(string fileName = DefaultFileName, string subFolder = DefaultSubFolder)
        {
            return File.Exists(GetSaveFilePath(fileName, subFolder));
        }

        // 필요한 경우 디렉터리를 생성합니다.
        // 그런 다음 선택한 저장기를 사용하여 파일을 저장하고, 그 후 FileStream을 닫습니다.
        public static void Save(object objectToSave, string fileName = DefaultFileName, string subFolder = DefaultSubFolder)
        {
            string savePath = GetSavePath(subFolder);

            // 저장 경로가 존재하는지 확인하고, 존재하지 않으면 생성합니다.
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string filePath = GetSaveFilePath(fileName, subFolder);

            using FileStream saveFile = File.Create(filePath);
            _dataSaver.Save(objectToSave, saveFile);

            Debug.Log(Application.persistentDataPath);
        }

        // 디스크에서 저장된 파일을 불러옵니다.
        // 유형, 파일 이름, 서브 폴더를 매개변수로 전달하십시오.
        public static object Load(Type typeToLoad, string fileName = DefaultFileName, string subFolder = DefaultSubFolder)
        {
            string filePath = GetSaveFilePath(fileName, subFolder);
            if (!File.Exists(filePath))
            {
                Debug.LogError("SaveManager: Save file not found at: " + filePath);
                return null;
            }

            using FileStream saveFile = File.Open(filePath, FileMode.Open);
            return _dataSaver.Load(typeToLoad, saveFile);
        }

        // 디스크에서 텍스트 파일을 읽습니다. 파일 이름과 하위 폴더를 매개변수로 전달하십시오.
        public static string LoadTextFile(string fileName = DefaultFileName, string subFolder = DefaultSubFolder)
        {
            string savePath = GetSavePath(subFolder);

            if (File.Exists(savePath + fileName))
            {
                return File.ReadAllText(savePath + fileName);
            }
            Debug.Log($"<color=green>[SaveManager] 파일 불러오기 성공!</color>\n경로: {savePath}");
            Debug.LogError("SaveManager: Save file not found at: " + savePath + fileName);
            return string.Empty;
        }
        public static void DeleteSave(string fileName,string subFolder = DefaultSubFolder)
        {
            string filePath = GetSaveFilePath(fileName, subFolder);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                Debug.Log($"Deleted Save : {filePath}");
            }
            else
            {
                Debug.LogError(
                    $"Save file not found at : {filePath}");
            }
        }
        // 디스크에서 저장된 파일을 삭제합니다.
        // 파일 이름과 폴더를 매개변수로 전달하십시오.
        //public static void DeleteSave(string fileName, string subFolder = DefaultSubFolder)
        //{
        //    string savePath = GetSavePath(subFolder);

        //    if (File.Exists(savePath + fileName))
        //    {
        //        File.Delete(savePath + fileName);
        //    }
        //    else
        //    {
        //        Debug.LogError("SaveManager: Save file not found at: " + savePath + fileName);
        //    }
        //}
    }
}