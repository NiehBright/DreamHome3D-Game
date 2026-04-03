using UnityEngine;

namespace Runtime.Build
{
    public enum GameMode
    {
        Puzzle = 0,
        Build = 1
    }

    public class ModeController : MonoBehaviour
    {
        [SerializeField] private GameObject puzzleRoot;
        [SerializeField] private GameObject buildRoot;
        [SerializeField] private Camera puzzleCamera;
        [SerializeField] private Camera buildCamera;
        [SerializeField] private BuildModeController buildModeController;

        public GameMode CurrentMode { get; private set; } = GameMode.Puzzle;

        private void Start()
        {
            ApplyMode(CurrentMode);
        }

        public void EnterBuildMode()
        {
            SetMode(GameMode.Build);
        }

        public void EnterPuzzleMode()
        {
            SetMode(GameMode.Puzzle);
        }

        public void SetMode(GameMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            ApplyMode(mode);
        }

        private void ApplyMode(GameMode mode)
        {
            bool isBuild = mode == GameMode.Build;

            if (puzzleRoot != null)
            {
                puzzleRoot.SetActive(!isBuild);
            }

            if (buildRoot != null)
            {
                buildRoot.SetActive(isBuild);
            }

            if (buildModeController != null)
            {
                buildModeController.SetBuildActive(isBuild);
            }

            if (puzzleCamera != null)
            {
                puzzleCamera.gameObject.SetActive(!isBuild);
            }

            if (buildCamera != null)
            {
                buildCamera.gameObject.SetActive(isBuild);
            }
        }
    }
}


