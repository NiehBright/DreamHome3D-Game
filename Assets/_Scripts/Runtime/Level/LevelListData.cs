using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelListData", menuName = "DreamHome/Level List Data")]
public class LevelListData : ScriptableObject
{
    [SerializeField] private List<LevelData> levels = new List<LevelData>();

    public int Count => levels.Count;

    public bool TryGetLevel(int index, out LevelData levelData)
    {
        if (index < 0 || index >= levels.Count)
        {
            levelData = null;
            return false;
        }

        levelData = levels[index];
        return levelData != null;
    }
}