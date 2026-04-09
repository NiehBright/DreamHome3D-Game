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

        EnsureResultRoot();
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
        EnsureResultRoot();

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

    private void EnsureResultRoot()
    {
        if (resultRoot != null && resultText != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        if (resultRoot == null)
        {
            resultRoot = CreateFallbackResultRoot(parent);
        }

        if (resultText == null)
        {
            resultText = resultRoot.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private GameObject CreateFallbackResultRoot(Transform parent)
    {
        GameObject root = new GameObject("ResultPanel", typeof(RectTransform), typeof(CanvasRenderer));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 320f);

        GameObject textRoot = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textRoot.transform.SetParent(root.transform, false);
        RectTransform textRect = textRoot.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(460f, 200f);

        TextMeshProUGUI tmp = textRoot.GetComponent<TextMeshProUGUI>();
        tmp.text = "Level Complete!";
        tmp.fontSize = 34f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        root.SetActive(false);
        return root;
    }
}
