using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelDataMenu
{
    [MenuItem("Tools/DreamHome/Create Sample Level (5x5)")]
    public static void CreateSampleLevel()
    {
        const int width = 5;
        const int height = 5;

        var levelData = ScriptableObject.CreateInstance<LevelData>();
        levelData.Resize(width, height);

        // Build border walls.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                var cell = levelData.GetCell(x, y);
                cell.tileType = isBorder ? TileType.Wall : TileType.Empty;
                cell.hasBox = false;
                cell.isGoal = false;
                cell.hasPlayerStart = false;
                levelData.SetCell(x, y, cell);
            }
        }

        // Simple solvable setup:
        // #####
        // # . #
        // # B #
        // # P #
        // #####
        SetGoal(levelData, 2, 3);
        SetBox(levelData, 2, 2);
        SetPlayer(levelData, 2, 1);

        string folder = "Assets/Levels";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Levels");
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "Level_01.asset"));
        AssetDatabase.CreateAsset(levelData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = levelData;
        Debug.Log($"Created sample level at {assetPath}");
    }

    private static void SetGoal(LevelData levelData, int x, int y)
    {
        var cell = levelData.GetCell(x, y);
        if (cell.tileType == TileType.Wall)
        {
            return;
        }

        cell.isGoal = true;
        levelData.SetCell(x, y, cell);
    }

    private static void SetBox(LevelData levelData, int x, int y)
    {
        var cell = levelData.GetCell(x, y);
        if (cell.tileType == TileType.Wall)
        {
            return;
        }

        cell.hasBox = true;
        cell.hasPlayerStart = false;
        levelData.SetCell(x, y, cell);
    }

    private static void SetPlayer(LevelData levelData, int x, int y)
    {
        for (int row = 0; row < levelData.Height; row++)
        {
            for (int col = 0; col < levelData.Width; col++)
            {
                var clearCell = levelData.GetCell(col, row);
                clearCell.hasPlayerStart = false;
                levelData.SetCell(col, row, clearCell);
            }
        }

        var cell = levelData.GetCell(x, y);
        if (cell.tileType == TileType.Wall || cell.hasBox)
        {
            return;
        }

        cell.hasPlayerStart = true;
        levelData.SetCell(x, y, cell);
    }
}

