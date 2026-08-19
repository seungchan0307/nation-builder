using System;
using System.IO;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Loads/saves NationSaveData as JSON in Application.persistentDataPath,
    /// the same "plain file next to the game" approach as the stock-game project's
    /// game_state.dat, just JSON instead of a custom format.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "nation_save.json");

        public static NationSaveData Load()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<NationSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"세이브 파일을 읽지 못했습니다: {e.Message}");
                return null;
            }
        }

        public static void Save(NationSaveData data)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"세이브 파일을 저장하지 못했습니다: {e.Message}");
            }
        }
    }
}
