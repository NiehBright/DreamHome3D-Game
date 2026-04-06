using System;
using UnityEngine;

public class StepCounter : MonoBehaviour
{
    public event Action<int> OnStepCountChanged;
    public event Action<int, int> OnOptimalStepsCompared; // currentSteps, optimalSteps

    private int currentSteps;
    private int optimalSteps;

    public int CurrentSteps => currentSteps;
    public int OptimalSteps => optimalSteps;
    public bool IsOptimal => currentSteps <= optimalSteps && optimalSteps > 0;

    public void Initialize(int optimalSteps = 0)
    {
        this.optimalSteps = optimalSteps;
        ResetSteps();
    }

    public void ResetSteps()
    {
        currentSteps = 0;
        OnStepCountChanged?.Invoke(currentSteps);
    }

    public void IncrementStep()
    {
        currentSteps++;
        OnStepCountChanged?.Invoke(currentSteps);

        if (optimalSteps > 0)
        {
            OnOptimalStepsCompared?.Invoke(currentSteps, optimalSteps);
        }
    }

    public void DecrementStep()
    {
        if (currentSteps > 0)
        {
            currentSteps--;
            OnStepCountChanged?.Invoke(currentSteps);

            if (optimalSteps > 0)
            {
                OnOptimalStepsCompared?.Invoke(currentSteps, optimalSteps);
            }
        }
    }

    public void SetOptimalSteps(int steps)
    {
        optimalSteps = steps;
        if (optimalSteps > 0)
        {
            OnOptimalStepsCompared?.Invoke(currentSteps, optimalSteps);
        }
    }

    public int GetStarsEarned()
    {
        if (optimalSteps <= 0) return 3;

        if (currentSteps <= optimalSteps) return 3;
        if (currentSteps <= optimalSteps * 1.5f) return 2;
        return 1;
    }
}