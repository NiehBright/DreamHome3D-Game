using TMPro;
using Runtime.Build;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _Scripts.Runtime.Gameplay
{
    public class PuzzleLevelSelectController : MonoBehaviour
    {
        private const string HighestUnlockedKey = "Puzzle.HighestUnlocked";
        private const string SelectedLevelKey = "Puzzle.SelectedLevel";

    [Header("References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private ModeController modeController;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private Button playButton;

    [Header("Visible Level Slots (max 4)")]
    [SerializeField] private Button[] levelButtons = new Button[4];
    [SerializeField] private TMP_Text[] levelLabels = new TMP_Text[4];

    [Header("Optional")]
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private bool openSelectorOnPuzzleMode = true;

        private int _highestUnlockedIndex;
        private int _selectedLevelIndex;
        private UnityAction[] _levelSlotActions;

        private void Awake()
        {
        if (gameController == null)
        {
            gameController = FindFirstObjectByType<GameController>();
        }

        if (modeController == null)
        {
            modeController = FindFirstObjectByType<ModeController>();
        }

            _levelSlotActions = new UnityAction[levelButtons != null ? levelButtons.Length : 0];

            InitializeProgress();
        }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(HandlePlayClicked);
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int slotIndex = i;
            if (levelButtons[slotIndex] != null)
            {
                _levelSlotActions[slotIndex] = () => HandleLevelSlotClicked(slotIndex);
                levelButtons[slotIndex].onClick.AddListener(_levelSlotActions[slotIndex]);
            }
        }

        if (gameController != null)
        {
            gameController.LevelCompleted += HandleLevelCompleted;
            gameController.LevelLoaded += HandleLevelLoaded;
        }

        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int slotIndex = i;
            if (levelButtons[slotIndex] != null)
            {
                if (_levelSlotActions != null && slotIndex < _levelSlotActions.Length && _levelSlotActions[slotIndex] != null)
                {
                    levelButtons[slotIndex].onClick.RemoveListener(_levelSlotActions[slotIndex]);
                }
            }
        }

        if (gameController != null)
        {
            gameController.LevelCompleted -= HandleLevelCompleted;
            gameController.LevelLoaded -= HandleLevelLoaded;
        }
    }

    public void EnterPuzzleAndOpenSelector()
    {
        modeController?.EnterPuzzleMode();

        if (openSelectorOnPuzzleMode)
        {
            OpenLevelSelect();
        }
    }

    public void OpenLevelSelect()
    {
        InitializeProgress();
        _selectedLevelIndex = Mathf.Clamp(_highestUnlockedIndex, 0, Mathf.Max(0, GetTotalLevelCount() - 1));
        RefreshView();

        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(true);
        }

        gameController?.SetInputBlocked(true);
    }

    public void CloseLevelSelect()
    {
        if (levelSelectPanel != null)
        {
            levelSelectPanel.SetActive(false);
        }

        gameController?.SetInputBlocked(false);
    }

    private void HandlePlayClicked()
    {
        int totalLevelCount = GetTotalLevelCount();
        if (totalLevelCount <= 0)
        {
            return;
        }

        _selectedLevelIndex = Mathf.Clamp(_selectedLevelIndex, 0, _highestUnlockedIndex);
        _selectedLevelIndex = Mathf.Clamp(_selectedLevelIndex, 0, totalLevelCount - 1);

        SaveSelectedLevel(_selectedLevelIndex);
        gameController?.LoadLevelByIndex(_selectedLevelIndex);
        CloseLevelSelect();
    }

    private void HandleLevelSlotClicked(int slotIndex)
    {
        int levelIndex = GetWindowStartIndex() + slotIndex;
        if (levelIndex > _highestUnlockedIndex || levelIndex >= GetTotalLevelCount())
        {
            return;
        }

        _selectedLevelIndex = levelIndex;
        SaveSelectedLevel(_selectedLevelIndex);
        RefreshView();
    }

    private void HandleLevelCompleted(int completedLevelIndex)
    {
        int nextLevelIndex = completedLevelIndex + 1;
        int maxLevelIndex = Mathf.Max(0, GetTotalLevelCount() - 1);

        if (nextLevelIndex <= maxLevelIndex && nextLevelIndex > _highestUnlockedIndex)
        {
            _highestUnlockedIndex = nextLevelIndex;
            _selectedLevelIndex = _highestUnlockedIndex;
            SaveProgress();
        }

        RefreshView();
    }

    private void HandleLevelLoaded(int levelIndex)
    {
        _selectedLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, GetTotalLevelCount() - 1));
        SaveSelectedLevel(_selectedLevelIndex);
        RefreshView();
    }

    private void InitializeProgress()
    {
        int totalLevelCount = GetTotalLevelCount();
        if (totalLevelCount <= 0)
        {
            _highestUnlockedIndex = 0;
            _selectedLevelIndex = 0;
            return;
        }

        int fallbackStart = gameController != null ? gameController.StartingLevelIndex : 0;
        int maxIndex = totalLevelCount - 1;

        _highestUnlockedIndex = Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedKey, fallbackStart), 0, maxIndex);
        _selectedLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedLevelKey, _highestUnlockedIndex), 0, _highestUnlockedIndex);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(HighestUnlockedKey, _highestUnlockedIndex);
        PlayerPrefs.SetInt(SelectedLevelKey, _selectedLevelIndex);
        PlayerPrefs.Save();
    }

    private void SaveSelectedLevel(int selectedIndex)
    {
        _selectedLevelIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _highestUnlockedIndex));
        PlayerPrefs.SetInt(SelectedLevelKey, _selectedLevelIndex);
        PlayerPrefs.Save();
    }

    private void RefreshView()
    {
        int totalLevelCount = GetTotalLevelCount();
        int windowStart = GetWindowStartIndex();

        if (currentLevelText != null)
        {
            currentLevelText.text = totalLevelCount > 0
                ? $"Current: {_selectedLevelIndex + 1}"
                : "Current: -";
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = windowStart + i;
            bool hasLevel = levelIndex < totalLevelCount;
            bool isUnlocked = levelIndex <= _highestUnlockedIndex;

            if (levelButtons[i] != null)
            {
                levelButtons[i].gameObject.SetActive(hasLevel);
                levelButtons[i].interactable = hasLevel && isUnlocked;
            }

            if (hasLevel && levelLabels != null && i < levelLabels.Length && levelLabels[i] != null)
            {
                levelLabels[i].text = (levelIndex + 1).ToString();
                levelLabels[i].alpha = isUnlocked ? 1f : 0.4f;
            }
        }

        if (playButton != null)
        {
            playButton.interactable = totalLevelCount > 0;
        }
    }

    private int GetWindowStartIndex()
    {
        int total = GetTotalLevelCount();
        if (total <= 0)
        {
            return 0;
        }

        // Sliding window follows the player's furthest progress: 1,2,3,4 -> 2,3,4,5.
        int maxStart = Mathf.Max(0, total - 4);
        return Mathf.Clamp(_highestUnlockedIndex, 0, maxStart);
    }

    private int GetTotalLevelCount()
    {
        return gameController != null ? gameController.TotalLevelCount : 0;
    }

    public void ResetProgress()
    {
        int totalLevelCount = GetTotalLevelCount();
        int fallbackStart = gameController != null ? gameController.StartingLevelIndex : 0;
        int maxIndex = Mathf.Max(0, totalLevelCount - 1);

        PlayerPrefs.DeleteKey(HighestUnlockedKey);
        PlayerPrefs.DeleteKey(SelectedLevelKey);

        _highestUnlockedIndex = Mathf.Clamp(fallbackStart, 0, maxIndex);
        _selectedLevelIndex = _highestUnlockedIndex;
        SaveProgress();
        RefreshView();
    }
    }
}
