using UnityEngine;
using System;

namespace Runtime.Build
{
    public enum GameMode
    {
        // Keep existing numeric values stable for already-configured scenes.
        Puzzle = 0,
        Build = 1,
        Main = 2,
        PuzzlePaint = 3
    }

    public class ModeController : MonoBehaviour
    {
        [Header("World Roots")]
        [SerializeField] private GameObject mainRoot;
        [SerializeField] private GameObject puzzleRoot;
        [SerializeField] private GameObject puzzlePaintRoot;
        [SerializeField] private GameObject buildModeRoot;
        [SerializeField] private GameObject buildPlacedRoot;

        [Header("UI Roots")]
        [SerializeField] private GameObject uiMainRoot;
        [SerializeField] private GameObject uiBuildRoot;
        [SerializeField] private GameObject uiPuzzleRoot;
        [SerializeField] private GameObject uiPuzzlePaintRoot;

        [Header("Cameras")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera puzzleCamera;
        [SerializeField] private Camera puzzlePaintCamera;
        [SerializeField] private Camera buildCamera;
        [SerializeField] private bool showPlacedRootInPuzzleMode;

        [Header("Controllers")]
        [SerializeField] private BuildModeController buildModeController;

        public GameMode CurrentMode { get; private set; } = GameMode.Main;
        public event Action<GameMode> ModeChanged;

        private void Start()
        {
            ApplyMode(CurrentMode);
        }

        public void EnterMainMode()
        {
            SetMode(GameMode.Main);
        }

        public void EnterBuildMode()
        {
            SetMode(GameMode.Build);
        }

        public void EnterPuzzleMode()
        {
            SetMode(GameMode.Puzzle);
        }

        public bool TryEnterPuzzlePaintMode()
        {
            SetMode(GameMode.PuzzlePaint);
            return true;
        }

        public void SetMode(GameMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            ApplyMode(mode);
            ModeChanged?.Invoke(mode);
        }

        private void ApplyMode(GameMode mode)
        {
            bool isMain = mode == GameMode.Main;
            bool isBuild = mode == GameMode.Build;
            bool isPuzzle = mode == GameMode.Puzzle;
            bool isPuzzlePaint = mode == GameMode.PuzzlePaint;

            SetActiveSafe(mainRoot, isMain);
            SetActiveSafe(puzzleRoot, isPuzzle);
            SetActiveSafe(puzzlePaintRoot, isPuzzlePaint);
            SetActiveSafe(buildModeRoot, isBuild);
            SetActiveSafe(buildPlacedRoot, isMain || isBuild || (isPuzzle && showPlacedRootInPuzzleMode));

            SetActiveSafe(uiMainRoot, isMain);
            SetActiveSafe(uiBuildRoot, isBuild);
            SetActiveSafe(uiPuzzleRoot, isPuzzle);
            SetActiveSafe(uiPuzzlePaintRoot, isPuzzlePaint);

            SetCameraActive(mainCamera, isMain);
            SetCameraActive(puzzleCamera, isPuzzle);
            SetCameraActive(puzzlePaintCamera, isPuzzlePaint);
            SetCameraActive(buildCamera, isBuild);


            if (buildModeController != null)
            {
                buildModeController.SetBuildActive(isBuild);
                // Keep floor/grid visuals visible in Main + Build; BuildModeController controls line-only visibility.
                buildModeController.SetGridVisible(isMain || isBuild);
            }
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetCameraActive(Camera cameraRef, bool active)
        {
            if (cameraRef != null)
            {
                cameraRef.gameObject.SetActive(active);
            }
        }
    }
}
