public static class LevelLoader
{
    public static GridState Load(LevelData levelData)
    {
        return new GridState(levelData);
    }
}

