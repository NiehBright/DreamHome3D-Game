using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using _Scripts.Runtime.PuzzlePaint;
using PaintCellData = _Scripts.Runtime.PuzzlePaint.CellData;
using PaintTileType = _Scripts.Runtime.PuzzlePaint.PuzzlePaintTileType;
using TMPro;
using Runtime.Build;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Runtime.Gameplay
{
    public class PuzzlePaintController : MonoBehaviour
    {
        private const string SelectedLevelKey = "PuzzlePaint.SelectedLevel";
        private const string DefaultLevelsResourcePath = "PuzzlePaint/Levels";
        private const string UiResourcePath = "UI/PuzzlePaintUI";

        [Header("References")]
        [SerializeField] private ModeController modeController;
        [SerializeField] private SwipeInputReader swipeInputReader;
        [SerializeField] private bool createDedicatedSwipeReaderIfMissing = true;

        [Header("Level Loading")]
        [SerializeField] private PuzzlePaintLevelListData manualLevelListData;
        [SerializeField] private bool useManualLevelListOrder = true;
        [SerializeField] private string levelsResourcePath = DefaultLevelsResourcePath;
        [SerializeField] private bool autoLoadLevelsFromResources = true;

        [Header("Board")]
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Min(0.01f)] private float floorHeight = 0.12f;
        [SerializeField, Min(0.1f)] private float obstacleHeight = 1f;
        [SerializeField, Min(0.1f)] private float playerHeight = 0.7f;
        [SerializeField] private Vector3 boardOffset;
        [SerializeField] private Color unpaintedColor = new Color(0.88f, 0.88f, 0.9f, 1f);
        [SerializeField] private Color paintedColor = new Color(0.56f, 0.3f, 0.9f, 1f);
        [SerializeField] private Color obstacleColor = new Color(0.22f, 0.22f, 0.26f, 1f);
        [SerializeField] private Color playerColor = new Color(0.98f, 0.98f, 1f, 1f);
        [SerializeField] private Color startColor = new Color(0.32f, 0.78f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float playerYOffset = 0.05f;

        [Header("Camera")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 12f, -12f);
        [SerializeField] private Vector3 cameraRotation = new Vector3(55f, 0f, 0f);
        [SerializeField, Min(1f)] private float cameraSize = 6f;
        [SerializeField] private bool useOrthographicCamera = true;
        [SerializeField] private bool forceTopDownView = true;
        [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 14f, -2f);
        [SerializeField, Range(70f, 90f)] private float topDownPitch = 82f;
        [SerializeField] private bool keepBoardCenteredOnScreen = true;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float moveStepDuration = 0.08f;

        private GameObject _runtimeRoot;
        private GameObject _boardRoot;
        private Camera _paintCamera;
        private GameObject _uiInstance;
        private GameObject _playerView;

        private TMP_Text _levelText;
        private TMP_Text _progressText;
        private Button _resetButton;
        private GameObject _resultRoot;
        private TMP_Text _resultText;
        private Button _nextButton;
        private Button _mainMenuButton;

        private readonly Dictionary<Vector2Int, CellView> _cellViews = new Dictionary<Vector2Int, CellView>();
        private readonly List<Vector2Int> _slidePath = new List<Vector2Int>();

        private PuzzlePaintLevelData[] _levels = Array.Empty<PuzzlePaintLevelData>();
        private PuzzlePaintLevelData _currentLevelData;
        private int _currentLevelIndex;
        private Vector2Int _playerCell;
        private Vector2 _boardOriginOffset;
        private bool[,] _obstacleMap;
        private bool[,] _paintedMap;
        private bool _isModeActive;
        private bool _isAnimating;
        private bool _isCompleted;
        private Coroutine _moveRoutine;

        private class CellView
        {
            public GameObject Root;
            public Renderer Renderer;
            public bool IsObstacle;
            public bool IsPainted;
            public bool IsStart;
        }

        private void Awake()
        {
            ResolveReferences();
            LoadLevels();
            CreateRuntimeObjects();
            SubscribeSwipe();
            SubscribeMode();
            ApplyModeState(modeController != null && modeController.CurrentMode == GameMode.PuzzlePaint);
        }

        private void OnEnable()
        {
            SubscribeSwipe();
            SubscribeMode();

            if (modeController != null && modeController.CurrentMode == GameMode.PuzzlePaint)
            {
                EnterPuzzlePaintMode();
            }
        }

        private void Start()
        {
            if (modeController != null && modeController.CurrentMode == GameMode.PuzzlePaint)
            {
                EnterPuzzlePaintMode();
            }
        }

        private void OnDisable()
        {
            UnsubscribeSwipe();
            UnsubscribeMode();
            StopMovement();
            ApplyModeState(false);
        }

        private void ResolveReferences()
        {
            if (modeController == null)
            {
                modeController = FindFirstObjectByType<ModeController>();
            }

            if (swipeInputReader == null)
            {
                swipeInputReader = FindFirstObjectByType<SwipeInputReader>();
            }

            if ((swipeInputReader == null || !swipeInputReader.isActiveAndEnabled) && createDedicatedSwipeReaderIfMissing)
            {
                GameObject inputObject = new GameObject("PuzzlePaintSwipeInputReader");
                swipeInputReader = inputObject.AddComponent<SwipeInputReader>();
            }
        }

        private void LoadLevels()
        {
            if (useManualLevelListOrder && manualLevelListData != null && manualLevelListData.Count > 0)
            {
                _levels = manualLevelListData
                    .GetAllLevelsOrdered()
                    .Where(level => level != null)
                    .ToArray();
                return;
            }

            if (!autoLoadLevelsFromResources)
            {
                _levels = Array.Empty<PuzzlePaintLevelData>();
                return;
            }

            PuzzlePaintLevelData[] loadedLevels = Resources.LoadAll<PuzzlePaintLevelData>(levelsResourcePath);
            _levels = loadedLevels
                .Where(level => level != null)
                .OrderBy(level => GetLevelSortKey(level.name))
                .ThenBy(level => level.name)
                .ToArray();
        }

        private static int GetLevelSortKey(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
            {
                return int.MaxValue;
            }

            Match match = Regex.Match(levelName, @"(\d+)(?!.*\d)");
            if (match.Success && int.TryParse(match.Value, out int index))
            {
                return index;
            }

            return int.MaxValue;
        }

        private void SubscribeMode()
        {
            if (modeController == null)
            {
                return;
            }

            modeController.ModeChanged -= HandleModeChanged;
            modeController.ModeChanged += HandleModeChanged;
        }

        private void UnsubscribeMode()
        {
            if (modeController == null)
            {
                return;
            }

            modeController.ModeChanged -= HandleModeChanged;
        }

        private void SubscribeSwipe()
        {
            if (swipeInputReader == null)
            {
                return;
            }

            swipeInputReader.OnSwipe -= HandleSwipe;
            swipeInputReader.OnSwipe += HandleSwipe;
        }

        private void UnsubscribeSwipe()
        {
            if (swipeInputReader == null)
            {
                return;
            }

            swipeInputReader.OnSwipe -= HandleSwipe;
        }

        private void HandleModeChanged(GameMode mode)
        {
            ApplyModeState(mode == GameMode.PuzzlePaint);

            if (mode == GameMode.PuzzlePaint)
            {
                EnterPuzzlePaintMode();
            }
        }

        private void EnterPuzzlePaintMode()
        {
            if (_levels == null || _levels.Length <= 0)
            {
                Debug.LogWarning($"Puzzle Paint has no levels. Create Puzzle Paint level assets under Resources/{levelsResourcePath}.");
                return;
            }

            _currentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedLevelKey, 0), 0, _levels.Length - 1);
            LoadCurrentLevel();
            ApplyModeState(true);
        }

        private void ApplyModeState(bool active)
        {
            _isModeActive = active;

            if (_runtimeRoot != null)
            {
                _runtimeRoot.SetActive(active);
            }

            if (_uiInstance != null)
            {
                _uiInstance.SetActive(active);
            }

            if (_paintCamera != null)
            {
                _paintCamera.gameObject.SetActive(active);
            }
        }

        private void CreateRuntimeObjects()
        {
            if (_runtimeRoot != null)
            {
                return;
            }

            _runtimeRoot = new GameObject("PuzzlePaintRuntime");
            _boardRoot = new GameObject("BoardRoot");
            _boardRoot.transform.SetParent(_runtimeRoot.transform, false);

            GameObject cameraObject = new GameObject("PuzzlePaintCamera");
            cameraObject.transform.SetParent(_runtimeRoot.transform, false);
            _paintCamera = cameraObject.AddComponent<Camera>();
            _paintCamera.clearFlags = CameraClearFlags.SolidColor;
            _paintCamera.backgroundColor = new Color(0.95f, 0.95f, 0.97f, 1f);
            _paintCamera.orthographic = useOrthographicCamera;
            _paintCamera.orthographicSize = cameraSize;
            // Safe default so camera starts top-down even before first level is loaded.
            _paintCamera.transform.position = new Vector3(0f, Mathf.Max(8f, topDownOffset.y), 0f);
            _paintCamera.transform.rotation = Quaternion.Euler(topDownPitch, 0f, 0f);
            _paintCamera.gameObject.SetActive(false);

            EnsureUiInstance();
            _runtimeRoot.SetActive(false);
        }

        private void EnsureUiInstance()
        {
            if (_uiInstance != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            GameObject prefab = Resources.Load<GameObject>(UiResourcePath);
            _uiInstance = prefab != null ? Instantiate(prefab, parent, false) : CreateFallbackUi(parent);
            _uiInstance.name = "PuzzlePaintUI";

            BindUi(_uiInstance.transform);
            _uiInstance.SetActive(false);
        }

        private void BindUi(Transform root)
        {
            _levelText = FindText(root, "LevelText");
            _progressText = FindText(root, "ProgressText");
            _resetButton = FindButton(root, "ResetButton");
            _resultRoot = FindDeepChild(root, "ResultPanel")?.gameObject;
            _resultText = FindText(root, "ResultText");
            _nextButton = FindButton(root, "NextButton");
            _mainMenuButton = FindButton(root, "ReplayButton");

            AddListener(_resetButton, HandleResetClicked);
            AddListener(_nextButton, HandleNextClicked);
            AddListener(_mainMenuButton, HandleMainMenuClicked);

            if (_mainMenuButton != null)
            {
                TMP_Text mainLabel = _mainMenuButton.GetComponentInChildren<TMP_Text>(true);
                if (mainLabel != null)
                {
                    mainLabel.text = "Main";
                }
            }

            if (_resultRoot != null)
            {
                _resultRoot.SetActive(false);
            }
        }

        private GameObject CreateFallbackUi(Transform parent)
        {
            GameObject root = new GameObject("PuzzlePaintUI", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            StretchFull(root.GetComponent<RectTransform>());

            Image rootBg = root.GetComponent<Image>();
            rootBg.color = new Color(0f, 0f, 0f, 0.01f);
            rootBg.raycastTarget = false;

            GameObject hud = new GameObject("HUD", typeof(RectTransform), typeof(Image));
            hud.transform.SetParent(root.transform, false);
            RectTransform hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(20f, -20f);
            hudRect.sizeDelta = new Vector2(380f, 150f);
            hud.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

            CreateText(hud.transform, "LevelText", "Level 1", 34f, TextAlignmentOptions.Left, new Vector2(16f, -16f), new Vector2(320f, 44f));
            CreateText(hud.transform, "ProgressText", "Painted 0/0", 26f, TextAlignmentOptions.Left, new Vector2(16f, -66f), new Vector2(320f, 36f));
            CreateButton(hud.transform, "ResetButton", "Reset", new Color(0.24f, 0.58f, 0.96f), new Vector2(260f, -96f), new Vector2(100f, 54f));

            GameObject resultPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(root.transform, false);
            RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = Vector2.zero;
            resultRect.sizeDelta = new Vector2(520f, 300f);
            resultPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            CreateText(resultPanel.transform, "ResultText", "Level Complete!", 38f, TextAlignmentOptions.Center, new Vector2(0f, -28f), new Vector2(440f, 100f));
            CreateButton(resultPanel.transform, "NextButton", "Next Level", new Color(0.46f, 0.2f, 0.74f), new Vector2(0f, 54f), new Vector2(220f, 62f));
            CreateButton(resultPanel.transform, "ReplayButton", "Main", new Color(0.42f, 0.42f, 0.42f), new Vector2(0f, -20f), new Vector2(220f, 62f));
            LayoutResultFallback(resultPanel.transform);
            resultPanel.SetActive(false);

            return root;
        }

        private void LayoutResultFallback(Transform parent)
        {
            TMP_Text resultText = FindText(parent, "ResultText");
            if (resultText != null)
            {
                RectTransform rect = resultText.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -30f);
                rect.sizeDelta = new Vector2(440f, 100f);
            }

            Button nextButton = FindButton(parent, "NextButton");
            if (nextButton != null)
            {
                RectTransform rect = nextButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 66f);
                rect.sizeDelta = new Vector2(220f, 64f);
            }

            Button replayButton = FindButton(parent, "ReplayButton");
            if (replayButton != null)
            {
                RectTransform rect = replayButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 12f);
                rect.sizeDelta = new Vector2(220f, 64f);
            }
        }

        private static void CreateButton(Transform parent, string name, string label, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = root.GetComponent<Image>();
            image.color = color;

            Button button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            button.colors = colors;

            TMP_Text text = CreateText(root.transform, "Label", label, 28f, TextAlignmentOptions.Center, Vector2.zero, sizeDelta);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }

        private void LoadCurrentLevel()
        {
            if (_levels == null || _levels.Length <= 0)
            {
                Debug.LogWarning($"Puzzle Paint has no levels. Create Puzzle Paint level assets under Resources/{levelsResourcePath}.");
                return;
            }

            _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, _levels.Length - 1);
            _currentLevelData = _levels[_currentLevelIndex];

            if (_currentLevelData == null)
            {
                Debug.LogWarning("Puzzle Paint current level is null.");
                return;
            }

            BuildLevelBoard(_currentLevelData);
            UpdateHud();
            HideResult();
            _isCompleted = false;
        }

        private void BuildLevelBoard(PuzzlePaintLevelData levelData)
        {
            StopMovement();
            ClearBoard();

            int size = levelData.Size;
            _obstacleMap = new bool[size, size];
            _paintedMap = new bool[size, size];
            _boardOriginOffset = new Vector2(-((size - 1) * 0.5f), -((size - 1) * 0.5f));

            if (levelData.TryGetPlayerStart(out Vector2Int startCell))
            {
                _playerCell = startCell;
            }
            else
            {
                _playerCell = FindFallbackSpawn(levelData);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    PaintCellData cellData = levelData.GetCell(x, y);
                    bool obstacle = cellData.tileType == PaintTileType.Obstacle;

                    _obstacleMap[x, y] = obstacle;
                    _paintedMap[x, y] = false;

                    GameObject cellObject = CreateCellObject(cell, obstacle);
                    CellView cellView = new CellView
                    {
                        Root = cellObject,
                        Renderer = cellObject.GetComponent<Renderer>(),
                        IsObstacle = obstacle,
                        IsPainted = false,
                        IsStart = cellData.hasPlayerStart
                    };

                    _cellViews[cell] = cellView;
                    ApplyCellVisual(cell);
                }
            }

            if (_playerView != null)
            {
                Destroy(_playerView);
            }

            _playerView = CreatePlayerObject();
            _playerView.transform.localPosition = GetPlayerLocalPosition(_playerCell);
            PaintCell(_playerCell);
            UpdateCameraPosition(size);
        }

        private Vector2Int FindFallbackSpawn(PuzzlePaintLevelData levelData)
        {
            for (int y = 0; y < levelData.Size; y++)
            {
                for (int x = 0; x < levelData.Size; x++)
                {
                    PaintCellData cell = levelData.GetCell(x, y);
                    if (cell.tileType != PaintTileType.Obstacle)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return Vector2Int.zero;
        }

        private GameObject CreateCellObject(Vector2Int cell, bool obstacle)
        {
            GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cellObject.name = $"Cell_{cell.x}_{cell.y}";
            cellObject.transform.SetParent(_boardRoot.transform, false);

            Vector3 size = obstacle
                ? new Vector3(cellSize, obstacleHeight, cellSize)
                : new Vector3(cellSize, floorHeight, cellSize);

            cellObject.transform.localScale = size;
            cellObject.transform.localPosition = GetCellLocalPosition(cell, obstacle ? obstacleHeight : floorHeight);

            Collider tileCollider = cellObject.GetComponent<Collider>();
            if (tileCollider != null)
            {
                Destroy(tileCollider);
            }

            Renderer renderer = cellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateRuntimeMaterial();
            }

            return cellObject;
        }

        private GameObject CreatePlayerObject()
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerObject.name = "PlayerPaint";
            playerObject.transform.SetParent(_boardRoot.transform, false);
            playerObject.transform.localScale = new Vector3(cellSize * 0.7f, playerHeight, cellSize * 0.7f);

            Collider playerCollider = playerObject.GetComponent<Collider>();
            if (playerCollider != null)
            {
                Destroy(playerCollider);
            }

            Renderer renderer = playerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateRuntimeMaterial();
                renderer.material.color = playerColor;
            }

            return playerObject;
        }

        private static Material CreateRuntimeMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
        }

        private void UpdateCameraPosition(int size)
        {
            if (_paintCamera == null)
            {
                return;
            }

            float maxSize = Mathf.Max(size, size) * 0.75f + 1.5f;
            _paintCamera.orthographicSize = Mathf.Max(cameraSize, maxSize);

            Vector3 boardCenter = new Vector3((size - 1) * 0.5f * cellSize, 0f, (size - 1) * 0.5f * cellSize) + boardOffset;
            if (forceTopDownView)
            {
                _paintCamera.transform.position = boardCenter + new Vector3(topDownOffset.x, topDownOffset.y, topDownOffset.z);
                if (keepBoardCenteredOnScreen)
                {
                    _paintCamera.transform.LookAt(boardCenter, Vector3.up);
                }
                else
                {
                    _paintCamera.transform.rotation = Quaternion.Euler(topDownPitch, 0f, 0f);
                }
            }
            else
            {
                _paintCamera.transform.position = boardCenter + new Vector3(cameraOffset.x, cameraOffset.y, cameraOffset.z);
                if (keepBoardCenteredOnScreen)
                {
                    _paintCamera.transform.LookAt(boardCenter, Vector3.up);
                }
                else
                {
                    _paintCamera.transform.rotation = Quaternion.Euler(cameraRotation);
                }
            }
        }

        private void HandleSwipe(Vector2Int direction)
        {
            if (!_isModeActive || _isAnimating || _isCompleted || direction == Vector2Int.zero || _currentLevelData == null)
            {
                return;
            }

            _slidePath.Clear();
            Vector2Int cursor = _playerCell;

            while (true)
            {
                Vector2Int next = cursor + direction;
                if (!IsInside(next) || IsObstacle(next))
                {
                    break;
                }

                _slidePath.Add(next);
                cursor = next;
            }

            if (_slidePath.Count == 0)
            {
                return;
            }

            _moveRoutine = StartCoroutine(AnimateMove(new List<Vector2Int>(_slidePath)));
        }

        private IEnumerator AnimateMove(IReadOnlyList<Vector2Int> path)
        {
            _isAnimating = true;

            foreach (Vector2Int targetCell in path)
            {
                Vector3 beginLocal = _playerView.transform.localPosition;
                Vector3 targetLocal = GetPlayerLocalPosition(targetCell);
                float elapsed = 0f;

                while (elapsed < moveStepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveStepDuration);
                    _playerView.transform.localPosition = Vector3.Lerp(beginLocal, targetLocal, t);
                    yield return null;
                }

                _playerView.transform.localPosition = targetLocal;
                _playerCell = targetCell;
                PaintCell(targetCell);
            }

            _isAnimating = false;
            _moveRoutine = null;
            UpdateHud();
            CheckWin();
        }

        private Vector3 GetCellLocalPosition(Vector2Int cell, float height)
        {
            return new Vector3(
                boardOffset.x + (cell.x + _boardOriginOffset.x) * cellSize,
                height * 0.5f,
                boardOffset.z + (cell.y + _boardOriginOffset.y) * cellSize);
        }

        private Vector3 GetPlayerLocalPosition(Vector2Int cell)
        {
            return new Vector3(
                boardOffset.x + (cell.x + _boardOriginOffset.x) * cellSize,
                floorHeight * 0.5f + playerHeight * 0.5f + playerYOffset,
                boardOffset.z + (cell.y + _boardOriginOffset.y) * cellSize);
        }

        private void PaintCell(Vector2Int cell)
        {
            if (!IsInside(cell))
            {
                return;
            }

            _paintedMap[cell.x, cell.y] = true;
            if (_cellViews.TryGetValue(cell, out CellView cellView))
            {
                cellView.IsPainted = true;
                ApplyCellVisual(cell);
            }
        }

        private void ApplyCellVisual(Vector2Int cell)
        {
            if (!_cellViews.TryGetValue(cell, out CellView cellView) || cellView.Renderer == null)
            {
                return;
            }

            Color color = cellView.IsObstacle ? obstacleColor : (cellView.IsPainted ? paintedColor : unpaintedColor);
            if (cellView.IsStart && !cellView.IsObstacle && !cellView.IsPainted)
            {
                color = startColor;
            }

            cellView.Renderer.material.color = color;
        }

        private bool IsInside(Vector2Int cell)
        {
            return _currentLevelData != null && _currentLevelData.IsInside(cell.x, cell.y);
        }

        private bool IsObstacle(Vector2Int cell)
        {
            return _obstacleMap != null && IsInside(cell) && _obstacleMap[cell.x, cell.y];
        }

        private void CheckWin()
        {
            if (!AreAllPaintableCellsPainted())
            {
                return;
            }

            _isCompleted = true;
            ShowResult($"Level {_currentLevelIndex + 1} complete!\nAll valid tiles painted.");
        }

        private bool AreAllPaintableCellsPainted()
        {
            if (_currentLevelData == null || _paintedMap == null)
            {
                return false;
            }

            for (int y = 0; y < _currentLevelData.Size; y++)
            {
                for (int x = 0; x < _currentLevelData.Size; x++)
                {
                    if (_obstacleMap[x, y])
                    {
                        continue;
                    }

                    if (!_paintedMap[x, y])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateHud()
        {
            if (_levelText != null)
            {
                _levelText.text = _currentLevelData != null ? $"Level {_currentLevelIndex + 1}" : "Level -";
            }

            if (_progressText != null)
            {
                _progressText.text = _currentLevelData != null
                    ? $"Painted {CountPaintedCells()}/{CountPaintableCells()}"
                    : "Painted 0/0";
            }
        }

        private int CountPaintableCells()
        {
            if (_currentLevelData == null)
            {
                return 0;
            }

            int count = 0;
            for (int y = 0; y < _currentLevelData.Size; y++)
            {
                for (int x = 0; x < _currentLevelData.Size; x++)
                {
                    if (!_obstacleMap[x, y])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountPaintedCells()
        {
            if (_currentLevelData == null || _paintedMap == null)
            {
                return 0;
            }

            int count = 0;
            for (int y = 0; y < _currentLevelData.Size; y++)
            {
                for (int x = 0; x < _currentLevelData.Size; x++)
                {
                    if (_paintedMap[x, y])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private void ShowResult(string message)
        {
            if (_resultRoot != null)
            {
                _resultRoot.SetActive(true);
            }

            if (_resultText != null)
            {
                _resultText.text = message;
            }

            if (_nextButton != null)
            {
                _nextButton.interactable = _levels != null && _currentLevelIndex < _levels.Length - 1;
            }
        }

        private void HideResult()
        {
            if (_resultRoot != null)
            {
                _resultRoot.SetActive(false);
            }
        }

        private void HandleResetClicked()
        {
            if (_isModeActive)
            {
                LoadCurrentLevel();
            }
        }

        private void HandleMainMenuClicked()
        {
            if (!_isModeActive)
            {
                return;
            }

            HideResult();
            modeController?.EnterMainMode();
        }

        private void HandleNextClicked()
        {
            if (!_isModeActive || _levels == null || _levels.Length <= 0)
            {
                return;
            }

            if (_currentLevelIndex >= _levels.Length - 1)
            {
                LoadCurrentLevel();
                return;
            }

            _currentLevelIndex++;
            PlayerPrefs.SetInt(SelectedLevelKey, _currentLevelIndex);
            PlayerPrefs.Save();
            LoadCurrentLevel();
        }

        private void ClearBoard()
        {
            _cellViews.Clear();

            if (_boardRoot == null)
            {
                return;
            }

            for (int i = _boardRoot.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_boardRoot.transform.GetChild(i).gameObject);
            }
        }

        private void StopMovement()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            _isAnimating = false;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static Button FindButton(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            Transform child = FindDeepChild(root, name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == targetName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

