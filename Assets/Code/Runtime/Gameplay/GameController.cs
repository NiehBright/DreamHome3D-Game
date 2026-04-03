using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private LevelData levelData;
    [SerializeField] private SwipeInputReader swipeInputReader;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject playerPrefab;

    [Header("Layout")]
    [SerializeField] private float cellSize = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private GridState gridState;
    private readonly Dictionary<Vector2Int, Transform> boxViews = new Dictionary<Vector2Int, Transform>();
    private Transform playerView;
    private Transform boardRoot;
    private bool completed;

    private void Awake()
    {
        if (swipeInputReader == null)
        {
            swipeInputReader = FindFirstObjectByType<SwipeInputReader>();
        }
    }

    private void OnEnable()
    {
        if (swipeInputReader != null)
        {
            swipeInputReader.OnSwipe += HandleSwipe;
        }
    }

    private void OnDisable()
    {
        if (swipeInputReader != null)
        {
            swipeInputReader.OnSwipe -= HandleSwipe;
        }
    }

    private void Start()
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        if (levelData == null)
        {
            Debug.LogWarning("GameController missing LevelData reference.");
            return;
        }

        completed = false;
        gridState = LevelLoader.Load(levelData);

        RebuildBoard();
        RefreshDynamicViews();
    }

    private void HandleSwipe(Vector2Int direction)
    {
        if (completed || gridState == null)
        {
            return;
        }

        MovementResolver.MoveResult moveResult = MovementResolver.TryStep(gridState, direction);
        if (!moveResult.moved)
        {
            return;
        }

        if (moveResult.pushedBox)
        {
            Transform movedBox = boxViews[moveResult.previousBoxPosition];
            boxViews.Remove(moveResult.previousBoxPosition);
            boxViews[moveResult.currentBoxPosition] = movedBox;
            movedBox.position = GridToWorld(moveResult.currentBoxPosition);
        }

        playerView.position = GridToWorld(moveResult.currentPlayerPosition);

        if (WinChecker.IsLevelComplete(gridState))
        {
            completed = true;
            Debug.Log("Level completed.");
            onLevelCompleted?.Invoke();
        }
    }

    private void RebuildBoard()
    {
        if (boardRoot != null)
        {
            Destroy(boardRoot.gameObject);
        }

        boxViews.Clear();
        playerView = null;

        boardRoot = new GameObject("BoardRoot").transform;
        boardRoot.SetParent(transform, false);

        for (int y = 0; y < levelData.Height; y++)
        {
            for (int x = 0; x < levelData.Width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                CellData cell = levelData.GetCell(x, y);
                Vector3 worldPosition = GridToWorld(position);

                SpawnIfAssigned(floorPrefab, worldPosition, "Floor", boardRoot);

                if (cell.tileType == TileType.Wall)
                {
                    SpawnIfAssigned(wallPrefab, worldPosition, "Wall", boardRoot);
                }
                else
                {
                    if (cell.isGoal)
                    {
                        SpawnIfAssigned(goalPrefab, worldPosition, "Goal", boardRoot);
                    }

                    if (cell.hasBox)
                    {
                        Transform box = SpawnIfAssigned(boxPrefab, worldPosition, "Box", boardRoot);
                        if (box != null)
                        {
                            boxViews[position] = box;
                        }
                    }

                    if (cell.hasPlayerStart)
                    {
                        playerView = SpawnIfAssigned(playerPrefab, worldPosition, "Player", boardRoot);
                    }
                }
            }
        }

        if (playerView == null)
        {
            playerView = SpawnIfAssigned(playerPrefab, GridToWorld(gridState.PlayerPosition), "Player", boardRoot);
        }
    }

    private void RefreshDynamicViews()
    {
        if (playerView != null)
        {
            playerView.position = GridToWorld(gridState.PlayerPosition);
        }

        foreach (KeyValuePair<Vector2Int, Transform> pair in boxViews)
        {
            pair.Value.position = GridToWorld(pair.Key);
        }
    }

    private Transform SpawnIfAssigned(GameObject prefab, Vector3 worldPosition, string fallbackName, Transform parent)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        instance.name = fallbackName;
        return instance.transform;
    }

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize, 0f, gridPosition.y * cellSize);
    }
}

