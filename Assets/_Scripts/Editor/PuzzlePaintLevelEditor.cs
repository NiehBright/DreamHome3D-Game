#if UNITY_EDITOR
using PaintCellData = _Scripts.Runtime.PuzzlePaint.CellData;
using PaintTileType = _Scripts.Runtime.PuzzlePaint.PuzzlePaintTileType;
using _Scripts.Runtime.PuzzlePaint;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(PuzzlePaintLevelData))]
    public class PuzzlePaintLevelEditor : UnityEditor.Editor
    {
        private enum PaintMode
        {
            Tile = 0,
            Obstacle = 1,
            PlayerStart = 2
        }

        private const int CellSize = 26;
        private PaintMode _paintMode;

        public override void OnInspectorGUI()
        {
            PuzzlePaintLevelData levelData = (PuzzlePaintLevelData)target;

            EditorGUILayout.LabelField("Edit Level", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            int size = Mathf.Max(1, EditorGUILayout.IntField("Grid Size (NxN)", levelData.Size));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("5x5"))
            {
                size = 5;
            }
            if (GUILayout.Button("6x6"))
            {
                size = 6;
            }
            EditorGUILayout.EndHorizontal();

            if (size != levelData.Size && GUILayout.Button("Apply Size"))
            {
                Resize(levelData, size);
            }

            EditorGUILayout.Space(8f);
            _paintMode = (PaintMode)GUILayout.Toolbar((int)_paintMode, new[] { "Tile", "Obstacle", "Player" });

            EditorGUILayout.Space(8f);
            DrawGrid(levelData);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(levelData);
            }
        }

        private void Resize(PuzzlePaintLevelData levelData, int size)
        {
            Undo.RecordObject(levelData, "Resize Puzzle Paint Level");
            levelData.Resize(size);
            EditorUtility.SetDirty(levelData);
        }

        private void DrawGrid(PuzzlePaintLevelData levelData)
        {
            Event evt = Event.current;
            float gridWidth = levelData.Size * CellSize;
            float gridHeight = levelData.Size * CellSize;
            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

            for (int y = 0; y < levelData.Size; y++)
            {
                for (int x = 0; x < levelData.Size; x++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    Rect cellRect = GetCellRect(gridRect, cellPos, levelData.Size);
                    PaintCellData cell = levelData.GetCell(x, y);
                    DrawCellVisual(cellRect, cell);

                    if ((evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag) && evt.button == 0 && cellRect.Contains(evt.mousePosition))
                    {
                        Undo.RecordObject(levelData, "Paint Puzzle Paint Cell");
                        PaintCell(levelData, cellPos);
                        evt.Use();
                    }
                }
            }

            Handles.color = Color.black;
            for (int x = 0; x <= levelData.Size; x++)
            {
                float lineX = gridRect.x + x * CellSize;
                Handles.DrawLine(new Vector3(lineX, gridRect.y), new Vector3(lineX, gridRect.yMax));
            }

            for (int y = 0; y <= levelData.Size; y++)
            {
                float lineY = gridRect.y + y * CellSize;
                Handles.DrawLine(new Vector3(gridRect.x, lineY), new Vector3(gridRect.xMax, lineY));
            }
        }

        private static Rect GetCellRect(Rect gridRect, Vector2Int logicalPos, int totalRows)
        {
            float drawY = totalRows - 1 - logicalPos.y;
            return new Rect(gridRect.x + logicalPos.x * CellSize, gridRect.y + drawY * CellSize, CellSize, CellSize);
        }

        private static void DrawCellVisual(Rect cellRect, PaintCellData cell)
        {
            Color baseColor = cell.tileType == PaintTileType.Obstacle
                ? new Color(0.2f, 0.2f, 0.2f)
                : new Color(0.88f, 0.88f, 0.88f);

            EditorGUI.DrawRect(cellRect, baseColor);

            if (cell.hasPlayerStart)
            {
                Rect markerRect = Shrink(cellRect, 7f);
                EditorGUI.DrawRect(markerRect, new Color(0.2f, 0.75f, 1f));
            }
        }

        private void PaintCell(PuzzlePaintLevelData levelData, Vector2Int position)
        {
            PaintCellData cell = levelData.GetCell(position.x, position.y);

            switch (_paintMode)
            {
                case PaintMode.Tile:
                    cell.tileType = PaintTileType.Floor;
                    break;
                case PaintMode.Obstacle:
                    cell.tileType = PaintTileType.Obstacle;
                    cell.hasPlayerStart = false;
                    break;
                case PaintMode.PlayerStart:
                    if (cell.tileType == PaintTileType.Obstacle)
                    {
                        return;
                    }

                    ClearPlayerStart(levelData);
                    cell.tileType = PaintTileType.Floor;
                    cell.hasPlayerStart = true;
                    break;
            }

            levelData.SetCell(position.x, position.y, cell);
            EditorUtility.SetDirty(levelData);
        }

        private static void ClearPlayerStart(PuzzlePaintLevelData levelData)
        {
            for (int y = 0; y < levelData.Size; y++)
            {
                for (int x = 0; x < levelData.Size; x++)
                {
                    PaintCellData cell = levelData.GetCell(x, y);
                    if (!cell.hasPlayerStart)
                    {
                        continue;
                    }

                    cell.hasPlayerStart = false;
                    levelData.SetCell(x, y, cell);
                }
            }
        }

        private static Rect Shrink(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
        }
    }
}
#endif



