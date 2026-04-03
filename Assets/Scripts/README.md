# DreamHome Grid Puzzle MVP

## What is included

- `LevelData` ScriptableObject for level storage (`width`, `height`, per-cell tile/box/goal/player data)
- Step-based gameplay logic (`MovementResolver`, `WinChecker`, `GridState`)
- Mobile swipe input (`SwipeInputReader`) with mouse simulation in Editor
- Runtime orchestration (`GameController`) for loading, spawning, moving, and win check
- In-Inspector level editing via custom editor (`LevelDataEditor`) with click/drag paint modes

## Quick setup

1. Create a level asset:
   - Project window -> `Create` -> `DreamHome` -> `Level Data`
2. Edit the level in Inspector:
   - Set `Width`/`Height`, press `Resize Grid`
   - Use mode toolbar: `Tile`, `Box`, `Goal`, `Player`
   - Click or drag on cells to paint
3. In scene, create an empty `GameObject` and add:
   - `GameController`
   - `SwipeInputReader` (can be on same object)
4. Assign references in `GameController`:
   - `Level Data`
   - `Swipe Input Reader`
   - Prefabs (`floor`, `wall`, `goal`, `box`, `player`)
5. Press Play and swipe (or drag mouse in Editor simulation) to move.

## Data rules enforced

- `Player + Box` on same tile is not allowed
- Walls cannot contain player/box/goal
- Only one player start is kept
- `Goal + Box` is valid

## Notes

- Win condition is true when **all boxes are on goals**.
- If no player start is set, runtime picks the first walkable empty cell.

