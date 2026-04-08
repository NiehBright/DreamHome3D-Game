#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using _Scripts.Runtime.PuzzlePaint;

namespace _Scripts.Editor
{
    public class PuzzlePaintLevelCreatorWindow : EditorWindow
    {
        private string _levelName = "PuzzlePaintLevel_01";
        private int _gridSize = 5;

        [MenuItem("Tools/DreamHome/Create Puzzle Paint Level")]
        public static void ShowWindow()
        {
            GetWindow<PuzzlePaintLevelCreatorWindow>("Puzzle Paint Level");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create standalone Puzzle Paint level", EditorStyles.boldLabel);
            _levelName = EditorGUILayout.TextField("Level Name", _levelName);
            _gridSize = Mathf.Max(1, EditorGUILayout.IntField("Grid Size (NxN)", _gridSize));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("5x5"))
            {
                _gridSize = 5;
            }
            if (GUILayout.Button("6x6"))
            {
                _gridSize = 6;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox($"Will create a {_gridSize}x{_gridSize} Puzzle Paint level asset in Resources/PuzzlePaint/Levels.", MessageType.Info);

            if (GUILayout.Button("Create Level", GUILayout.Height(36f)))
            {
                CreateLevelAsset();
            }
        }

        private void CreateLevelAsset()
        {
            if (string.IsNullOrWhiteSpace(_levelName))
            {
                EditorUtility.DisplayDialog("Error", "Level name cannot be empty.", "OK");
                return;
            }

            const string folderPath = "Assets/Resources/PuzzlePaint/Levels";
            EnsureFolder(folderPath);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{_levelName}.asset");

            PuzzlePaintLevelData levelData = ScriptableObject.CreateInstance<PuzzlePaintLevelData>();
            levelData.Resize(_gridSize);

            AssetDatabase.CreateAsset(levelData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(levelData);
            EditorGUIUtility.PingObject(levelData);
            Close();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif


