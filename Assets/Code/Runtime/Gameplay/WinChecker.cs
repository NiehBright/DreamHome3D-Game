using UnityEngine;

public static class WinChecker
{
    public static bool IsLevelComplete(GridState gridState)
    {
        foreach (Vector2Int boxPosition in gridState.Boxes)
        {
            if (!gridState.IsGoal(boxPosition))
            {
                return false;
            }
        }

        return gridState.Boxes.Count > 0;
    }
}

