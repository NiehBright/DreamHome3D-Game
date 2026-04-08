using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Runtime.PuzzlePaint
{
    [CreateAssetMenu(fileName = "PuzzlePaintLevelData", menuName = "DreamHome/Puzzle Paint/Level Data")]
    public class PuzzlePaintLevelData : ScriptableObject
    {
        [SerializeField, Min(1)] private int size = 5;
        [SerializeField] private List<CellData> cells = new List<CellData>();

        public int Size => size;
        public int CellCount => size * size;

        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < size && y >= 0 && y < size;
        }

        public CellData GetCell(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return default;
            }

            EnsureGridSize();
            return cells[ToIndex(x, y)];
        }

        public void SetCell(int x, int y, CellData data)
        {
            if (!IsInside(x, y))
            {
                return;
            }

            EnsureGridSize();
            cells[ToIndex(x, y)] = data;
        }

        public void Resize(int newSize)
        {
            size = Mathf.Max(1, newSize);
            EnsureGridSize();
            Normalize();
        }

        public bool TryGetPlayerStart(out Vector2Int position)
        {
            EnsureGridSize();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
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

        private void OnValidate()
        {
            size = Mathf.Max(1, size);
            EnsureGridSize();
            Normalize();
        }

        private int ToIndex(int x, int y)
        {
            return y * size + x;
        }

        private void EnsureGridSize()
        {
            int required = size * size;
            if (cells == null)
            {
                cells = new List<CellData>(required);
            }

            while (cells.Count < required)
            {
                cells.Add(new CellData { tileType = PuzzlePaintTileType.Floor });
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
                CellData cell = cells[i];

                if (cell.tileType == PuzzlePaintTileType.Obstacle)
                {
                    cell.hasPlayerStart = false;
                }

                if (cell.hasPlayerStart)
                {
                    if (cell.tileType == PuzzlePaintTileType.Obstacle)
                    {
                        cell.hasPlayerStart = false;
                    }
                    else if (foundPlayer)
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
        public PuzzlePaintTileType tileType;
        public bool hasPlayerStart;
    }

    public enum PuzzlePaintTileType
    {
        Floor = 0,
        Obstacle = 1
    }
}

