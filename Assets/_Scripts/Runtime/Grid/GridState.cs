using System.Collections.Generic;
using UnityEngine;

public class GridState
{
    private readonly bool[] walls;
    private readonly bool[] goals;

    public int Width { get; }
    public int Height { get; }
    public Vector2Int PlayerPosition { get; private set; }
    public HashSet<Vector2Int> Boxes { get; }

    public GridState(LevelData levelData)
    {
        Width = levelData.Width;
        Height = levelData.Height;
        walls = new bool[Width * Height];
        goals = new bool[Width * Height];
        Boxes = new HashSet<Vector2Int>();

        bool playerFound = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = ToIndex(x, y);
                CellData cell = levelData.GetCell(x, y);

                walls[index] = cell.tileType == TileType.Wall;
                goals[index] = cell.isGoal;

                if (cell.hasBox)
                {
                    Boxes.Add(new Vector2Int(x, y));
                }

                if (cell.hasPlayerStart && !playerFound)
                {
                    PlayerPosition = new Vector2Int(x, y);
                    playerFound = true;
                }
            }
        }

        if (!playerFound)
        {
            PlayerPosition = FindFallbackSpawn();
        }
    }

    public bool IsInside(Vector2Int position)
    {
        return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
    }

    public bool IsWall(Vector2Int position)
    {
        return walls[ToIndex(position.x, position.y)];
    }

    public bool IsGoal(Vector2Int position)
    {
        return goals[ToIndex(position.x, position.y)];
    }

    public bool HasBox(Vector2Int position)
    {
        return Boxes.Contains(position);
    }

    public bool CanOccupy(Vector2Int position)
    {
        return IsInside(position) && !IsWall(position) && !HasBox(position);
    }

    public void MovePlayer(Vector2Int destination)
    {
        PlayerPosition = destination;
    }

    public void MoveBox(Vector2Int from, Vector2Int to)
    {
        Boxes.Remove(from);
        Boxes.Add(to);
    }

    private int ToIndex(int x, int y)
    {
        return y * Width + x;
    }

    private Vector2Int FindFallbackSpawn()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var candidate = new Vector2Int(x, y);
                if (!IsWall(candidate) && !HasBox(candidate))
                {
                    return candidate;
                }
            }
        }

        return Vector2Int.zero;
    }
}

