using UnityEngine;

public static class MovementResolver
{
    public struct MoveResult
    {
        public bool moved;
        public bool pushedBox;
        public Vector2Int previousPlayerPosition;
        public Vector2Int currentPlayerPosition;
        public Vector2Int previousBoxPosition;
        public Vector2Int currentBoxPosition;
    }

    public static MoveResult TryStep(GridState gridState, Vector2Int direction)
    {
        var result = new MoveResult
        {
            moved = false,
            pushedBox = false,
            previousPlayerPosition = gridState.PlayerPosition,
            currentPlayerPosition = gridState.PlayerPosition
        };

        if (direction == Vector2Int.zero)
        {
            return result;
        }

        Vector2Int target = gridState.PlayerPosition + direction;
        if (!gridState.IsInside(target) || gridState.IsWall(target))
        {
            return result;
        }

        if (gridState.HasBox(target))
        {
            Vector2Int boxTarget = target + direction;
            if (!gridState.CanOccupy(boxTarget))
            {
                return result;
            }

            gridState.MoveBox(target, boxTarget);
            result.pushedBox = true;
            result.previousBoxPosition = target;
            result.currentBoxPosition = boxTarget;
        }

        gridState.MovePlayer(target);
        result.moved = true;
        result.currentPlayerPosition = target;
        return result;
    }
}

