using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private LevelListData levelListData;
    [SerializeField, Min(0)] private int startingLevelIndex;
    [SerializeField] private SwipeInputReader swipeInputReader;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button resetButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorLightPrefab;
    [SerializeField] private GameObject floorDarkPrefab;
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
    private readonly Dictionary<Vector2Int, Transform> goalViews = new Dictionary<Vector2Int, Transform>();
    private readonly Stack<MovementResolver.MoveResult> moveHistory = new Stack<MovementResolver.MoveResult>();
    private Transform playerView;
    private Transform boardRoot;
    private bool completed;

    private int currentLevelIndex;
    private LevelData currentLevelData;

    private int lastAdvanceFrame = -1;

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

        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackButtonClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(HandleResetButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (swipeInputReader != null)
        {
            swipeInputReader.OnSwipe -= HandleSwipe;
        }


        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackButtonClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(HandleResetButtonClicked);
        }
    }

    private void Start()
    {
        currentLevelIndex = Mathf.Max(0, startingLevelIndex);
        LoadLevel();
    }

    public void LoadLevel()
    {
        if (levelListData == null || !levelListData.TryGetLevel(currentLevelIndex, out currentLevelData))
        {
            Debug.LogWarning("GameController missing LevelListData or level index is invalid.");
            return;
        }

        completed = false;
        gridState = LevelLoader.Load(currentLevelData);
        moveHistory.Clear();
        SetBackButtonVisible(true);
        SetResetButtonVisible(true);
SetNextLevelButtonVisible(true);
        SetNextLevelButtonVisible(false);
        UpdateLevelLabel();
        RebuildBoard();
        RefreshDynamicViews();

        UpdateBackButtonState();
    }

    public void LoadNextLevel()
    {
        if (Time.frameCount == lastAdvanceFrame)
        {
            return;
        }

        lastAdvanceFrame = Time.frameCount;

        if (!HasNextLevel())
        {
            Debug.Log("No next level available.");
            return;
        }

        currentLevelIndex++;
        LoadLevel();
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

        moveHistory.Push(moveResult);
        UpdateBackButtonState();

        if (moveResult.pushedBox)
        {
            Transform movedBox = boxViews[moveResult.previousBoxPosition];
            boxViews.Remove(moveResult.previousBoxPosition);
            boxViews[moveResult.currentBoxPosition] = movedBox;
            movedBox.position = GridToWorld(moveResult.currentBoxPosition);
        }

        RefreshGoalViews();
        playerView.position = GridToWorld(moveResult.currentPlayerPosition);

        if (WinChecker.IsLevelComplete(gridState))
        {
            completed = true;
            Debug.Log("Level completed.");
            onLevelCompleted?.Invoke();
            SetNextLevelButtonVisible(HasNextLevel());
            SetBackButtonVisible(false);
            SetResetButtonVisible(false);
        }
    }

    private void HandleBackButtonClicked()
    {
        UndoLastMove();
    }

    private void HandleResetButtonClicked()
    {
        LoadLevel();
    }

    private void UndoLastMove()
    {
        if (moveHistory.Count == 0 || gridState == null)
        {
            return;
        }

        MovementResolver.MoveResult lastMove = moveHistory.Pop();

        if (lastMove.pushedBox)
        {
            gridState.MoveBox(lastMove.currentBoxPosition, lastMove.previousBoxPosition);

            if (boxViews.TryGetValue(lastMove.currentBoxPosition, out Transform movedBox))
            {
                boxViews.Remove(lastMove.currentBoxPosition);
                boxViews[lastMove.previousBoxPosition] = movedBox;
                movedBox.position = GridToWorld(lastMove.previousBoxPosition);
            }
            else
            {
                RebuildBoard();
            }
        }

        RefreshGoalViews();

        gridState.MovePlayer(lastMove.previousPlayerPosition);
        if (playerView != null)
        {
            playerView.position = GridToWorld(lastMove.previousPlayerPosition);
        }

        completed = WinChecker.IsLevelComplete(gridState);
        SetNextLevelButtonVisible(completed && HasNextLevel());
        SetBackButtonVisible(!completed);
        SetResetButtonVisible(!completed);

        UpdateBackButtonState();
    }

    private void RebuildBoard()
    {
        if (boardRoot != null)
        {
            Destroy(boardRoot.gameObject);
        }

        boxViews.Clear();
        goalViews.Clear();
        playerView = null;

        boardRoot = new GameObject("BoardRoot").transform;
        boardRoot.SetParent(transform, false);

        for (int y = 0; y < currentLevelData.Height; y++)
        {
            for (int x = 0; x < currentLevelData.Width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                CellData cell = currentLevelData.GetCell(x, y);

                if (cell.tileType == TileType.Wall)
                {
                    continue;
                }

                Vector3 worldPosition = GridToWorld(position);
                GameObject floorPrefabToUse = ((position.x + position.y) % 2 == 0) ? floorLightPrefab : floorDarkPrefab;
                SpawnIfAssigned(floorPrefabToUse, worldPosition, "Floor", boardRoot);

                if (cell.isGoal)
                {
                    Transform goal = SpawnIfAssigned(goalPrefab, worldPosition, "Goal", boardRoot);
                    if (goal != null)
                    {
                        goalViews[position] = goal;
                    }
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

        RefreshGoalViews();

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

    private void UpdateLevelLabel()
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text = $"Level {currentLevelIndex + 1}";
        levelText.gameObject.SetActive(true);
    }

    private bool HasNextLevel()
    {
        return levelListData != null && currentLevelIndex + 1 < levelListData.Count;
    }

    private void SetNextLevelButtonVisible(bool visible)
    {
        if (nextLevelButton == null)
        {
            return;
        }

        nextLevelButton.gameObject.SetActive(visible);
        nextLevelButton.interactable = visible;
    }

    private void SetBackButtonVisible(bool visible)
    {
        if (backButton == null)
        {
            return;
        }

        backButton.gameObject.SetActive(visible);
        backButton.interactable = visible;
    }

    private void UpdateBackButtonState()
    {
        if (backButton == null)
        {
            return;
        }

        bool canUndo = moveHistory.Count > 0;
        backButton.interactable = canUndo;
    }

    private void HandleNextLevelButtonClicked()
    {
        LoadNextLevel();
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

    private void RefreshGoalViews()
    {
        foreach (KeyValuePair<Vector2Int, Transform> pair in goalViews)
        {
            pair.Value.gameObject.SetActive(!gridState.HasBox(pair.Key));
        }
    }

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize, 0f, gridPosition.y * cellSize);
    }

    private void SetResetButtonVisible(bool visible)
    {
        if (resetButton == null)
        {
            return;
        }

        resetButton.gameObject.SetActive(visible);
        resetButton.interactable = visible;
    }
}

