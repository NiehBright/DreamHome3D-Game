using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Build
{
    public class BuildGridState
    {
        private readonly HashSet<Vector2Int> blockedCells;
        private readonly Dictionary<Vector2Int, string> occupiedCells;
        private readonly Dictionary<string, PlacedFurnitureData> placements;

        public int Width { get; }
        public int Height { get; }

        public BuildGridState(int width, int height, HashSet<Vector2Int> blocked)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            blockedCells = blocked ?? new HashSet<Vector2Int>();
            occupiedCells = new Dictionary<Vector2Int, string>();
            placements = new Dictionary<string, PlacedFurnitureData>();
        }

        public bool TryGetPlacement(string placementId, out PlacedFurnitureData data)
        {
            return placements.TryGetValue(placementId, out data);
        }

        public bool TryGetPlacementAtCell(Vector2Int cell, out string placementId)
        {
            return occupiedCells.TryGetValue(cell, out placementId);
        }

        public List<PlacedFurnitureData> GetAllPlacements()
        {
            return new List<PlacedFurnitureData>(placements.Values);
        }

        public PlacementValidationResult ValidatePlacement(FurnitureItemData item, Vector2Int origin, int rotationQuarterTurns, string ignorePlacementId = null)
        {
            if (item == null)
            {
                return new PlacementValidationResult(false, "Missing item.");
            }

            foreach (Vector2Int cell in GetFootprintCells(item.Size, origin, rotationQuarterTurns))
            {
                if (!IsInside(cell))
                {
                    return new PlacementValidationResult(false, "Out of bounds.");
                }

                if (blockedCells.Contains(cell))
                {
                    return new PlacementValidationResult(false, "Blocked tile.");
                }

                if (occupiedCells.TryGetValue(cell, out string ownerId) && ownerId != ignorePlacementId)
                {
                    return new PlacementValidationResult(false, "Overlap.");
                }
            }

            return PlacementValidationResult.Valid;
        }

        public void AddPlacement(PlacedFurnitureData data, FurnitureItemData item)
        {
            placements[data.placementId] = data;

            foreach (Vector2Int cell in GetFootprintCells(item.Size, data.Origin, data.rotationQuarterTurns))
            {
                occupiedCells[cell] = data.placementId;
            }
        }

        public void RemovePlacement(string placementId, FurnitureItemData item)
        {
            if (!placements.TryGetValue(placementId, out PlacedFurnitureData data))
            {
                return;
            }

            foreach (Vector2Int cell in GetFootprintCells(item.Size, data.Origin, data.rotationQuarterTurns))
            {
                occupiedCells.Remove(cell);
            }

            placements.Remove(placementId);
        }

        public static IEnumerable<Vector2Int> GetFootprintCells(Vector2Int size, Vector2Int origin, int rotationQuarterTurns)
        {
            Vector2Int rotated = RotateSize(size, rotationQuarterTurns);
            for (int y = 0; y < rotated.y; y++)
            {
                for (int x = 0; x < rotated.x; x++)
                {
                    yield return new Vector2Int(origin.x + x, origin.y + y);
                }
            }
        }

        public static Vector2Int RotateSize(Vector2Int size, int rotationQuarterTurns)
        {
            return Mathf.Abs(rotationQuarterTurns % 2) == 1
                ? new Vector2Int(size.y, size.x)
                : size;
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
    }
}


