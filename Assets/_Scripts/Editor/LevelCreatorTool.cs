using UnityEditor;
using UnityEngine;

public class LevelCreatorTool : EditorWindow
{
    private int gridSize = 5;
    private string levelName = "NewLevel";

    [MenuItem("Tools/DreamHome/Create Level (Quick)")]
    public static void ShowWindow()
    {
        GetWindow<LevelCreatorTool>("Create Level");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quick Level Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        levelName = EditorGUILayout.TextField("Level Name", levelName);
        gridSize = Mathf.Max(1, EditorGUILayout.IntField("Grid Size (NxN)", gridSize));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Will create a {gridSize}x{gridSize} level", MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Level", GUILayout.Height(40)))
        {
            CreateLevel();
        }
    }

    private void CreateLevel()
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            EditorUtility.DisplayDialog("Error", "Level name cannot be empty!", "OK");
            return;
        }

        string folderPath = "Assets/_ScriptableObjects/Levels";
        
        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = "Assets/_ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_ScriptableObjects");
            }
            AssetDatabase.CreateFolder(parentFolder, "Levels");
        }

        // Create level asset
        string assetPath = $"{folderPath}/{levelName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
        levelData.Resize(gridSize, gridSize);

        AssetDatabase.CreateAsset(levelData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Created level at:\n{assetPath}", "OK");
        EditorGUIUtility.PingObject(levelData);
        
        Close();
    }
}

