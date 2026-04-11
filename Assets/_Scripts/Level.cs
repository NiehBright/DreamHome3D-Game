using UnityEngine;
using Runtime.Build;

public class Level : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private string buildSaveKey = "dreamhome_build_mvp";

    [ContextMenu("Reset Build Mode & Level")]
    public void ResetLevel()
    {
        // Clear the build mode save data
        BuildSaveService.Clear(buildSaveKey);
        Debug.Log("Build mode save cleared. Level reset to initial state.");
        
        ReloadLevel();
    }

    [ContextMenu("Reload Level")]
    private void ReloadLevel()
    {
        if (gameController == null)
        {
            gameController = GetComponent<GameController>();
        }

        if (gameController != null)
        {
            gameController.LoadLevel();
        }
    }
}
