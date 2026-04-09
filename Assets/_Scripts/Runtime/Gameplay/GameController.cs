using System;
using System.Collections.Generic;
using Runtime.Build;
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
    [SerializeField] private StepCounterUI stepCounterUI; // Thêm dòng này

    [Header("Prefabs")]
    [SerializeField] private GameObject floorLightPrefab;
    [SerializeField] private GameObject floorDarkPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject playerPrefab;

    [Header("Layout")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float floorYOffset = -0.5f;
    [SerializeField] private float playerGoalYOffset = 0f;

    [Header("Player Facing")]
    [SerializeField] private bool faceCameraOnSpawn = true;
    [SerializeField] private bool faceMoveDirection = true;

    [Header("Object Scale")]
    [SerializeField] private float floorScale = 1f;
    [SerializeField] private float wallScale = 1f;
    [SerializeField] private float goalScale = 1f;
    [SerializeField] private float boxScale = 1f;
    [SerializeField] private float playerScale = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelCompleted;

    [Header("Rewards")]
    [SerializeField] private CurrencyWallet currencyWallet;
    [SerializeField, Min(0)] private int threeStarBonusCoins = 10;
    [SerializeField, Min(0)] private int twoStarBonusCoins = 7;
    [SerializeField, Min(0)] private int oneStarBonusCoins = 5;

    public event Action<int> LevelLoaded;
    public event Action<int> LevelCompleted;

    private GridState gridState;
    private readonly Dictionary<Vector2Int, Transform> boxViews = new Dictionary<Vector2Int, Transform>();
    private readonly Dictionary<Vector2Int, Transform> goalViews = new Dictionary<Vector2Int, Transform>();
    private readonly Stack<MovementResolver.MoveResult> moveHistory = new Stack<MovementResolver.MoveResult>();
    private Transform playerView;
    private Transform boardRoot;
    private bool completed;

    private int currentLevelIndex;
    private LevelData currentLevelData;
    private bool inputBlocked;

    private int lastAdvanceFrame = -1;
    private int lastCompletedCoinsAwarded;
    private int lastCompletedLevelCoinsAwarded;
    private int lastCompletedStarBonusCoinsAwarded;
    
    private StepCounter stepCounter; // Thêm dòng này

    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevelCount => levelListData != null ? levelListData.Count : 0;
    public int StartingLevelIndex => Mathf.Max(0, startingLevelIndex);
    public StepCounter StepCounter => stepCounter; // Thêm property này
    public int LastCompletedCoinsAwarded => lastCompletedCoinsAwarded;
    public int LastCompletedLevelCoinsAwarded => lastCompletedLevelCoinsAwarded;
    public int LastCompletedStarBonusCoinsAwarded => lastCompletedStarBonusCoinsAwarded;
    public int CurrentWalletCoins => currencyWallet != null ? currencyWallet.Coins : 0;

    private void Awake()
    {
        if (swipeInputReader == null)
        {
            swipeInputReader = FindFirstObjectByType<SwipeInputReader>();
        }

        if (stepCounterUI == null)
        {
            stepCounterUI = FindFirstObjectByType<StepCounterUI>();
        }

        if (currencyWallet == null)
        {
            currencyWallet = FindFirstObjectByType<CurrencyWallet>();
        }
        
        // Khởi tạo StepCounter
        stepCounter = gameObject.AddComponent<StepCounter>();
        if (stepCounterUI != null)
        {
            stepCounterUI.Initialize(stepCounter);
        }

        EnsureLevelResultUi();
    }

    private void EnsureLevelResultUi()
    {
        LevelResultUI existing = FindFirstObjectByType<LevelResultUI>();
        if (existing != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        GameObject root = new GameObject("LevelResultUI");
        root.transform.SetParent(canvas != null ? canvas.transform : transform, false);
        root.AddComponent<LevelResultUI>();
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

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(HandleNextLevelButtonClicked);
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

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(HandleNextLevelButtonClicked);
        }
    }

    private void Start()
    {
        LoadLevelByIndex(Mathf.Max(0, startingLevelIndex));
    }

    public bool LoadLevelByIndex(int levelIndex)
    {
        if (levelListData == null || levelListData.Count == 0)
        {
            Debug.LogWarning("GameController missing LevelListData or level list is empty.");
            return false;
        }

        int clampedIndex = Mathf.Clamp(levelIndex, 0, levelListData.Count - 1);
        currentLevelIndex = clampedIndex;
        LoadLevel();
        return true;
    }

    public void LoadLevel()
    {
        if (levelListData == null || !levelListData.TryGetLevel(currentLevelIndex, out currentLevelData))
        {
            Debug.LogWarning("GameController missing LevelListData or level index is invalid.");
            return;
        }

        completed = false;
        lastCompletedCoinsAwarded = 0;
        lastCompletedLevelCoinsAwarded = 0;
        lastCompletedStarBonusCoinsAwarded = 0;
        gridState = LevelLoader.Load(currentLevelData);
        moveHistory.Clear();

        // Khởi tạo step counter với số bước tối ưu từ level data
        if (stepCounter != null)
        {
            stepCounter.Initialize(currentLevelData.OptimalSteps);
        }

        SetBackButtonVisible(true);
        SetResetButtonVisible(true);
        SetResetButtonVisible(true);
        SetNextLevelButtonVisible(false);
        UpdateLevelLabel();
        RebuildBoard();
        RefreshDynamicViews();
        FacePlayerTowardsCamera();

        UpdateBackButtonState();
        LevelLoaded?.Invoke(currentLevelIndex);
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
        if (inputBlocked || completed || gridState == null)
        {
            return;
        }

        MovementResolver.MoveResult moveResult = MovementResolver.TryStep(gridState, direction);
        if (!moveResult.moved)
        {
            return;
        }

        moveHistory.Push(moveResult);

        // Tăng số bước khi di chuyển thành công
        if (stepCounter != null)
        {
            stepCounter.IncrementStep();
        }

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
        FacePlayerTowardsDirection(direction);

        if (WinChecker.IsLevelComplete(gridState))
        {
            completed = true;
            int starsEarned = stepCounter != null ? stepCounter.GetStarsEarned() : 3;
            lastCompletedCoinsAwarded = AwardLevelCompletionCoins(starsEarned);
            Debug.Log($"Level completed with {stepCounter?.CurrentSteps} steps! Stars earned: {starsEarned}, coins +{lastCompletedCoinsAwarded}");
            onLevelCompleted?.Invoke();
            LevelCompleted?.Invoke(currentLevelIndex);
            SetNextLevelButtonVisible(HasNextLevel());
            SetBackButtonVisible(false);
            SetResetButtonVisible(false);
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
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
        
        // Giảm số bước khi undo
        if (stepCounter != null)
        {
            stepCounter.DecrementStep();
        }

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
                Vector3 floorWorldPosition = GridToWorldFloor(position);
                GameObject floorPrefabToUse = ((position.x + position.y) % 2 == 0) ? floorLightPrefab : floorDarkPrefab;
                SpawnIfAssigned(floorPrefabToUse, floorWorldPosition, "Floor", boardRoot, floorScale);

                if (cell.isGoal)
                {
                    Transform goal = SpawnIfAssigned(goalPrefab, worldPosition, "Goal", boardRoot, goalScale);
                    if (goal != null)
                    {
                        goalViews[position] = goal;
                    }
                }

                if (cell.hasBox)
                {
                    Transform box = SpawnIfAssigned(boxPrefab, worldPosition, "Box", boardRoot, boxScale);
                    if (box != null)
                    {
                        boxViews[position] = box;
                    }
                }

                if (cell.hasPlayerStart)
                {
                    playerView = SpawnIfAssigned(playerPrefab, worldPosition, "Player", boardRoot, playerScale);
                }
            }
        }

        RefreshGoalViews();

        if (playerView == null)
        {
            playerView = SpawnIfAssigned(playerPrefab, GridToWorld(gridState.PlayerPosition), "Player", boardRoot, playerScale);
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

    private void FacePlayerTowardsCamera()
    {
        if (!faceCameraOnSpawn || playerView == null)
        {
            return;
        }

        Camera cameraRef = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cameraRef == null)
        {
            return;
        }

        Vector3 direction = cameraRef.transform.position - playerView.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        playerView.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void FacePlayerTowardsDirection(Vector2Int gridDirection)
    {
        if (!faceMoveDirection || playerView == null)
        {
            return;
        }

        Vector3 direction = new Vector3(gridDirection.x, 0f, gridDirection.y);
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        playerView.rotation = Quaternion.LookRotation(direction, Vector3.up);
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

    private int AwardLevelCompletionCoins(int starsEarned)
    {
        if (currentLevelData == null)
        {
            return 0;
        }

        int levelReward = Mathf.Max(0, currentLevelData.CoinReward);
        int starBonusReward = GetStarBonusCoins(starsEarned);
        int reward = levelReward + starBonusReward;
        if (reward <= 0)
        {
            lastCompletedLevelCoinsAwarded = 0;
            lastCompletedStarBonusCoinsAwarded = 0;
            return 0;
        }

        if (currencyWallet == null)
        {
            currencyWallet = FindFirstObjectByType<CurrencyWallet>();
        }

        if (currencyWallet == null)
        {
            Debug.LogWarning("CurrencyWallet not found. Coin reward is skipped.");
            lastCompletedLevelCoinsAwarded = 0;
            lastCompletedStarBonusCoinsAwarded = 0;
            return 0;
        }

        currencyWallet.Add(reward);
        lastCompletedLevelCoinsAwarded = levelReward;
        lastCompletedStarBonusCoinsAwarded = starBonusReward;
        return reward;
    }

    private int GetStarBonusCoins(int starsEarned)
    {
        if (starsEarned >= 3)
        {
            return Mathf.Max(0, threeStarBonusCoins);
        }

        if (starsEarned == 2)
        {
            return Mathf.Max(0, twoStarBonusCoins);
        }

        return Mathf.Max(0, oneStarBonusCoins);
    }

    private Transform SpawnIfAssigned(GameObject prefab, Vector3 worldPosition, string fallbackName, Transform parent, float scale = 1f)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        instance.name = fallbackName;
        instance.transform.localScale = Vector3.one * scale;
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
        return new Vector3(gridPosition.x * cellSize, playerGoalYOffset, gridPosition.y * cellSize);
    }

    private Vector3 GridToWorldFloor(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * cellSize, floorYOffset, gridPosition.y * cellSize);
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

