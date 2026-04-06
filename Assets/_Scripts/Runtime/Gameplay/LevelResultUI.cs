using TMPro;
using UnityEngine;

public class LevelResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameController gameController;
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject[] stars;

    [Header("Layout")]
    [SerializeField, Min(0f)] private float resultLineSpacing = 12f;

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = FindFirstObjectByType<GameController>();
        }

        // Keep result popup hidden until level is completed.
        HideResult();
    }

    private void OnEnable()
    {
        if (gameController != null)
        {
            gameController.LevelLoaded += HandleLevelLoaded;
            gameController.LevelCompleted += HandleLevelCompleted;
        }
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.LevelLoaded -= HandleLevelLoaded;
            gameController.LevelCompleted -= HandleLevelCompleted;
        }
    }

    // Can be called from Inspector event, but it is also triggered automatically on LevelCompleted.
    public void ShowResult()
    {
        if (resultRoot != null)
        {
            resultRoot.SetActive(true);
        }

        int steps = gameController != null && gameController.StepCounter != null
            ? gameController.StepCounter.CurrentSteps
            : 0;

        int starsEarned = gameController != null && gameController.StepCounter != null
            ? gameController.StepCounter.GetStarsEarned()
            : 3;

        int earnedCoins = gameController != null
            ? gameController.LastCompletedCoinsAwarded
            : 0;

        int levelCoins = gameController != null
            ? gameController.LastCompletedLevelCoinsAwarded
            : 0;

        int starBonusCoins = gameController != null
            ? gameController.LastCompletedStarBonusCoinsAwarded
            : 0;

        if (resultText != null)
        {
            resultText.lineSpacing = resultLineSpacing;
            resultText.text =
                $"Stars: {starsEarned}/3\n" +
                $"Level Coins: +{levelCoins}\n" +
                $"Star Bonus: +{starBonusCoins}\n" +
                $"Coins Earned: +{earnedCoins}";
        }

        if (stars == null)
        {
            return;
        }

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetActive(i < starsEarned);
            }
        }
    }

    public void HideResult()
    {
        if (resultRoot != null)
        {
            resultRoot.SetActive(false);
        }
    }

    private void HandleLevelLoaded(int _)
    {
        HideResult();
    }

    private void HandleLevelCompleted(int _)
    {
        ShowResult();
    }
}

