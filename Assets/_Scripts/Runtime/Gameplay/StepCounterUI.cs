using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepCounterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text stepCountText;
    [SerializeField] private TMP_Text optimalStepsText;
    [SerializeField] private Image stepCountBackground;

    [Header("Colors")]
    [SerializeField] private Color optimalColor = Color.green;
    [SerializeField] private Color goodColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Star Display (Optional)")]
    [SerializeField] private GameObject[] stars;

    private StepCounter stepCounter;

    private void OnEnable()
    {
        if (stepCounter != null)
        {
            stepCounter.OnStepCountChanged += UpdateStepDisplay;
            stepCounter.OnOptimalStepsCompared += UpdateOptimalComparison;
        }
    }

    private void OnDisable()
    {
        if (stepCounter != null)
        {
            stepCounter.OnStepCountChanged -= UpdateStepDisplay;
            stepCounter.OnOptimalStepsCompared -= UpdateOptimalComparison;
        }
    }

    public void Initialize(StepCounter counter)
    {
        if (stepCounter != null)
        {
            stepCounter.OnStepCountChanged -= UpdateStepDisplay;
            stepCounter.OnOptimalStepsCompared -= UpdateOptimalComparison;
        }

        stepCounter = counter;

        if (stepCounter != null)
        {
            stepCounter.OnStepCountChanged += UpdateStepDisplay;
            stepCounter.OnOptimalStepsCompared += UpdateOptimalComparison;
            UpdateStepDisplay(stepCounter.CurrentSteps);
            UpdateOptimalDisplay();
        }
    }

    private void UpdateStepDisplay(int steps)
    {
        if (stepCountText != null)
        {
            stepCountText.text = $"Steps: {steps}";
        }

        UpdateStarDisplay();
    }

    private void UpdateOptimalDisplay()
    {
        if (optimalStepsText != null && stepCounter != null)
        {
            if (stepCounter.OptimalSteps > 0)
            {
                optimalStepsText.text = $"Optimal: {stepCounter.OptimalSteps}";
                optimalStepsText.gameObject.SetActive(true);
            }
            else
            {
                optimalStepsText.gameObject.SetActive(false);
            }
        }

        UpdateStarDisplay();
    }

    private void UpdateOptimalComparison(int currentSteps, int optimalSteps)
    {
        if (stepCountBackground != null)
        {
            if (currentSteps <= optimalSteps)
            {
                stepCountBackground.color = optimalColor;
            }
            else if (currentSteps <= optimalSteps * 1.5f)
            {
                stepCountBackground.color = goodColor;
            }
            else
            {
                stepCountBackground.color = normalColor;
            }
        }

        UpdateStarDisplay();
    }

    private void UpdateStarDisplay()
    {
        if (stars == null || stars.Length == 0 || stepCounter == null) return;

        int starsEarned = stepCounter.GetStarsEarned();
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetActive(i < starsEarned);
            }
        }
    }
}