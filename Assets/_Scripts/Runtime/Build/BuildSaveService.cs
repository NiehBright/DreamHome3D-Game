using UnityEngine;

namespace Runtime.Build
{
    /// <summary>
    /// Handles persistence for Build Mode furniture placements and wallet.
    /// Optimized with error handling and utility methods.
    /// </summary>
    public static class BuildSaveService
    {
        /// <summary>
        /// Load saved build data from PlayerPrefs.
        /// </summary>
        public static BuildSaveData Load(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("BuildSaveService: Save key is empty!");
                return new BuildSaveData();
            }

            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new BuildSaveData();
            }

            try
            {
                var data = JsonUtility.FromJson<BuildSaveData>(json);
                return data ?? new BuildSaveData();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"BuildSaveService: Failed to load save data. {ex.Message}");
                return new BuildSaveData();
            }
        }

        /// <summary>
        /// Save build data to PlayerPrefs.
        /// </summary>
        public static void Save(string key, BuildSaveData data)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("BuildSaveService: Save key is empty!");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning("BuildSaveService: Attempt to save null data!");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BuildSaveService: Failed to save data. {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all saved data for a specific save key.
        /// Useful for resetting puzzles and levels.
        /// </summary>
        public static void Clear(string key)
        {
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Check if save data exists.
        /// </summary>
        public static bool HasSaveData(string key)
        {
            return !string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// Clear all build saves (useful for reset/restart scenarios).
        /// </summary>
        public static void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}

