using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "DreamHome/Level Data")]
public class LevelData : ScriptableObject
{
    [SerializeField, Min(1)] private int width = 5;
    [SerializeField, Min(1)] private int height = 5;
    [SerializeField] private List<CellData> cells = new List<CellData>();
    [SerializeField, Min(0)] private int optimalSteps = 0; // Số bước tối ưu
    [SerializeField, Min(0)] private int coinReward = 50;

    public int Width => width;
    public int Height => height;
    public int OptimalSteps => optimalSteps; // Property mới
    public int CoinReward => coinReward;

    public int CellCount => width * height;

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public int ToIndex(int x, int y)
    {
        return y * width + x;
    }

    public CellData GetCell(int x, int y)
    {
        return cells[ToIndex(x, y)];
    }

    public void SetCell(int x, int y, CellData data)
    {
        cells[ToIndex(x, y)] = data;
    }

    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        var newCells = new List<CellData>(newWidth * newHeight);
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                var copied = new CellData { tileType = TileType.Empty };
                if (x < width && y < height && cells.Count == width * height)
                {
                    copied = cells[ToIndex(x, y)];
                }

                newCells.Add(copied);
            }
        }

        width = newWidth;
        height = newHeight;
        cells = newCells;
        Normalize();
    }

    public bool TryGetPlayerStart(out Vector2Int position)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (GetCell(x, y).hasPlayerStart)
                {
                    position = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        position = default;
        return false;
    }

    public List<Vector2Int> GetBoxPositions()
    {
        var result = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (GetCell(x, y).hasBox)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        return result;
    }

    public bool IsGoal(int x, int y)
    {
        return GetCell(x, y).isGoal;
    }

    private void OnValidate()
    {
        EnsureGridSize();
        Normalize();
    }

    private void EnsureGridSize()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        int required = width * height;

        if (cells == null)
        {
            cells = new List<CellData>(required);
        }

        while (cells.Count < required)
        {
            cells.Add(new CellData { tileType = TileType.Empty });
        }

        if (cells.Count > required)
        {
            cells.RemoveRange(required, cells.Count - required);
        }
    }

    private void Normalize()
    {
        bool foundPlayer = false;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];

            if (cell.tileType == TileType.Wall)
            {
                cell.hasBox = false;
                cell.hasPlayerStart = false;
                cell.isGoal = false;
            }

            if (cell.hasBox)
            {
                cell.hasPlayerStart = false;
            }

            if (cell.hasPlayerStart)
            {
                if (foundPlayer)
                {
                    cell.hasPlayerStart = false;
                }
                else
                {
                    foundPlayer = true;
                }
            }

            cells[i] = cell;
        }
    }
}

[Serializable]
public struct CellData
{
    public TileType tileType;
    public bool hasBox;
    public bool isGoal;
    public bool hasPlayerStart;
}

public enum TileType
{
    Empty = 0,
    Wall = 1
}

