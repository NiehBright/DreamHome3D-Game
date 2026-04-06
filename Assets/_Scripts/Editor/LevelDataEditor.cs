using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private enum EditMode
    {
        Tile = 0,
        Box = 1,
        Goal = 2,
        Player = 3
    }

    private const int CellSize = 26;

    private EditMode editMode;

    public override void OnInspectorGUI()
    {
        LevelData levelData = (LevelData)target;

        DrawSizeControls(levelData);
        EditorGUILayout.Space();

        editMode = (EditMode)GUILayout.Toolbar((int)editMode, new[]
        {
            "Tile",
            "Box",
            "Goal",
            "Player"
        });

        EditorGUILayout.Space();
        DrawGrid(levelData);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(levelData);
        }
    }

    private static void DrawSizeControls(LevelData levelData)
    {
        EditorGUILayout.LabelField("Edit Level", EditorStyles.boldLabel);

        SerializedObject serializedLevel = new SerializedObject(levelData);
        SerializedProperty optimalStepsProp = serializedLevel.FindProperty("optimalSteps");
        SerializedProperty coinRewardProp = serializedLevel.FindProperty("coinReward");

        if (optimalStepsProp != null)
        {
            EditorGUILayout.PropertyField(optimalStepsProp, new GUIContent("Optimal Steps"));
        }

        if (coinRewardProp != null)
        {
            EditorGUILayout.PropertyField(coinRewardProp, new GUIContent("Coin Reward"));
        }

        serializedLevel.ApplyModifiedProperties();
        EditorGUILayout.Space();

        int newWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", levelData.Width));
        int newHeight = Mathf.Max(1, EditorGUILayout.IntField("Height", levelData.Height));

        if ((newWidth != levelData.Width || newHeight != levelData.Height) && GUILayout.Button("Resize Grid"))
        {
            Undo.RecordObject(levelData, "Resize Level Grid");
            levelData.Resize(newWidth, newHeight);
            EditorUtility.SetDirty(levelData);
        }
    }

    private void DrawGrid(LevelData levelData)
    {
        Event evt = Event.current;

        float gridWidth = levelData.Width * CellSize;
        float gridHeight = levelData.Height * CellSize;
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                var logicalPos = new Vector2Int(x, y);
                Rect cellRect = GetCellRect(gridRect, logicalPos, levelData.Height);

                CellData cell = levelData.GetCell(x, y);
                DrawCellVisual(cellRect, cell);

                if (evt.button == 0 && (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag) && cellRect.Contains(evt.mousePosition))
                {
                    Undo.RecordObject(levelData, "Paint Level Cell");
                    PaintCell(levelData, logicalPos);
                    evt.Use();
                }
            }
        }

        Handles.color = Color.black;
        for (int x = 0; x <= levelData.Width; x++)
        {
            float lineX = gridRect.x + x * CellSize;
            Handles.DrawLine(new Vector3(lineX, gridRect.y), new Vector3(lineX, gridRect.yMax));
        }

        for (int y = 0; y <= levelData.Height; y++)
        {
            float lineY = gridRect.y + y * CellSize;
            Handles.DrawLine(new Vector3(gridRect.x, lineY), new Vector3(gridRect.xMax, lineY));
        }
    }

    private static Rect GetCellRect(Rect gridRect, Vector2Int logicalPos, int totalRows)
    {
        float drawY = totalRows - 1 - logicalPos.y;
        return new Rect(
            gridRect.x + logicalPos.x * CellSize,
            gridRect.y + drawY * CellSize,
            CellSize,
            CellSize);
    }

    private static void DrawCellVisual(Rect cellRect, CellData cell)
    {
        Color baseColor = cell.tileType == TileType.Wall
            ? new Color(0.25f, 0.25f, 0.25f)
            : new Color(0.85f, 0.85f, 0.85f);

        EditorGUI.DrawRect(cellRect, baseColor);

        if (cell.isGoal)
        {
            Rect goalRect = Shrink(cellRect, 4f);
            EditorGUI.DrawRect(goalRect, new Color(1f, 0.9f, 0.2f));
        }

        if (cell.hasBox)
        {
            Rect boxRect = Shrink(cellRect, 7f);
            EditorGUI.DrawRect(boxRect, new Color(0.7f, 0.45f, 0.2f));
        }

        if (cell.hasPlayerStart)
        {
            Rect playerRect = Shrink(cellRect, 9f);
            EditorGUI.DrawRect(playerRect, new Color(0.25f, 0.8f, 1f));
        }
    }

    private static Rect Shrink(Rect rect, float amount)
    {
        return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
    }

    private void PaintCell(LevelData levelData, Vector2Int position)
    {
        CellData cell = levelData.GetCell(position.x, position.y);

        switch (editMode)
        {
            case EditMode.Tile:
                cell.tileType = cell.tileType == TileType.Empty ? TileType.Wall : TileType.Empty;
                if (cell.tileType == TileType.Wall)
                {
                    cell.hasBox = false;
                    cell.isGoal = false;
                    cell.hasPlayerStart = false;
                }
                break;
            case EditMode.Box:
                if (cell.tileType == TileType.Wall)
                {
                    return;
                }

                cell.hasBox = !cell.hasBox;
                if (cell.hasBox)
                {
                    cell.hasPlayerStart = false;
                }
                break;
            case EditMode.Goal:
                if (cell.tileType == TileType.Wall)
                {
                    return;
                }

                cell.isGoal = !cell.isGoal;
                break;
            case EditMode.Player:
                if (cell.tileType == TileType.Wall || cell.hasBox)
                {
                    return;
                }

                ClearAllPlayerFlags(levelData);
                cell.hasPlayerStart = true;
                break;
        }

        levelData.SetCell(position.x, position.y, cell);
        EditorUtility.SetDirty(levelData);
    }

    private static void ClearAllPlayerFlags(LevelData levelData)
    {
        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                CellData cell = levelData.GetCell(x, y);
                if (!cell.hasPlayerStart)
                {
                    continue;
                }

                cell.hasPlayerStart = false;
                levelData.SetCell(x, y, cell);
            }
        }
    }
}

