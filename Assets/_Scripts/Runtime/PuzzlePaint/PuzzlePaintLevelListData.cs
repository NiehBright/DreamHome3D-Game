using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Runtime.PuzzlePaint
{
    [CreateAssetMenu(fileName = "PuzzlePaintLevelListData", menuName = "DreamHome/Puzzle Paint/Level List Data")]
    public class PuzzlePaintLevelListData : ScriptableObject
    {
        [SerializeField] private List<PuzzlePaintLevelData> levels = new List<PuzzlePaintLevelData>();

        public int Count => levels != null ? levels.Count : 0;

        public bool TryGetLevel(int index, out PuzzlePaintLevelData level)
        {
            if (levels == null || index < 0 || index >= levels.Count)
            {
                level = null;
                return false;
            }

            level = levels[index];
            return level != null;
        }

        public List<PuzzlePaintLevelData> GetAllLevelsOrdered()
        {
            if (levels == null)
            {
                return new List<PuzzlePaintLevelData>();
            }

            return new List<PuzzlePaintLevelData>(levels);
        }
    }
}

