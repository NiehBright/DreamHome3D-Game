using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField] private GameController gameController;

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
