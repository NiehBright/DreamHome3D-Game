using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Build
{
    public class ModeUiBindings : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private ModeController modeController;
        [SerializeField] private BuildModeController buildModeController;
        [SerializeField] private FurnitureShopUI furnitureShopUI;

        [Header("Main UI")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button puzzleButton;

        [Header("Build UI")]
        [SerializeField] private Button buildExitButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button deleteButton;

        [Header("Puzzle UI")]
        [SerializeField] private Button puzzleExitButton;

        private void Awake()
        {
            if (modeController == null)
            {
                modeController = FindFirstObjectByType<ModeController>();
            }

            if (buildModeController == null)
            {
                buildModeController = FindFirstObjectByType<BuildModeController>();
            }
        }

        private void OnEnable()
        {
            AddListener(buildButton, HandleBuildButton);
            AddListener(puzzleButton, HandlePuzzleButton);

            AddListener(buildExitButton, HandleExitToMainButton);
            AddListener(rotateButton, HandleRotateButton);
            AddListener(cancelButton, HandleCancelButton);
            AddListener(deleteButton, HandleDeleteButton);

            AddListener(puzzleExitButton, HandleExitToMainButton);
        }

        private void OnDisable()
        {
            RemoveListener(buildButton, HandleBuildButton);
            RemoveListener(puzzleButton, HandlePuzzleButton);

            RemoveListener(buildExitButton, HandleExitToMainButton);
            RemoveListener(rotateButton, HandleRotateButton);
            RemoveListener(cancelButton, HandleCancelButton);
            RemoveListener(deleteButton, HandleDeleteButton);

            RemoveListener(puzzleExitButton, HandleExitToMainButton);
        }

        private void HandleBuildButton()
        {
            modeController?.EnterBuildMode();
        }

        private void HandlePuzzleButton()
        {
            modeController?.EnterPuzzleMode();
        }

        private void HandleExitToMainButton()
        {
            modeController?.EnterMainMode();
        }

        private void HandleRotateButton()
        {
            buildModeController?.RotatePreview();
        }

        private void HandleCancelButton()
        {
            buildModeController?.CancelPlacement();
        }

        private void HandleDeleteButton()
        {
            buildModeController?.DeleteSelected();
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}

