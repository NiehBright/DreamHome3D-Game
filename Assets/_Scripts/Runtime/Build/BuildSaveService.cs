using UnityEngine;

namespace Runtime.Build
{
    public static class BuildSaveService
    {
        public static BuildSaveData Load(string key)
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new BuildSaveData();
            }

            var data = JsonUtility.FromJson<BuildSaveData>(json);
            return data ?? new BuildSaveData();
        }

        public static void Save(string key, BuildSaveData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
    }
}

