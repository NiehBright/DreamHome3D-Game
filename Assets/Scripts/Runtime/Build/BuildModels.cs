using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Build
{
    [Serializable]
    public class PlacedFurnitureData
    {
        public string placementId;
        public string furnitureId;
        public int x;
        public int y;
        public int rotationQuarterTurns;

        public Vector2Int Origin => new Vector2Int(x, y);
    }

    [Serializable]
    public class BuildSaveData
    {
        public int coins;
        public List<PlacedFurnitureData> placedFurniture = new List<PlacedFurnitureData>();
    }

    public struct PlacementValidationResult
    {
        public readonly bool isValid;
        public readonly string reason;

        public PlacementValidationResult(bool isValid, string reason)
        {
            this.isValid = isValid;
            this.reason = reason;
        }

        public static PlacementValidationResult Valid => new PlacementValidationResult(true, string.Empty);
    }
}

