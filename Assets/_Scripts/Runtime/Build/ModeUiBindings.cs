using UnityEngine;
using UnityEngine.UI;
using _Scripts.Runtime.Gameplay;

namespace Runtime.Build
{
    public class ModeUiBindings : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private ModeController modeController;
        [SerializeField] private BuildModeController buildModeController;
        [SerializeField] private FurnitureShopUI furnitureShopUI;
        [SerializeField] private PuzzleLevelSelectController puzzleLevelSelectController;
        [SerializeField] private PuzzleGameSelectController puzzleGameSelectController;
        [SerializeField] private PuzzlePaintController puzzlePaintController;

        [Header("Main UI")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button puzzleButton;

        [Header("Build UI")]
        [SerializeField] private Button buildExitButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button deleteButton;

        [Header("Build Context UI")]
        [SerializeField] private RectTransform furnitureContextPanel;
        [SerializeField] private Button contextRotateButton;
        [SerializeField] private Button contextDeleteButton;
        [SerializeField] private Button contextCancelButton;
        [SerializeField] private Vector2 contextPanelScreenOffset = new Vector2(0f, 120f);

        private RectTransform contextPanelParent;

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

            if (puzzleGameSelectController == null)
            {
                puzzleGameSelectController = FindFirstObjectByType<PuzzleGameSelectController>();
            }

            if (puzzleGameSelectController == null)
            {
                GameObject controllerHost = new GameObject("PuzzleGameSelectController");
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    controllerHost.transform.SetParent(canvas.transform, false);
                }

                puzzleGameSelectController = controllerHost.AddComponent<PuzzleGameSelectController>();
            }

            if (puzzlePaintController == null)
            {
                GameObject controllerHost = new GameObject("PuzzlePaintController");
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    controllerHost.transform.SetParent(canvas.transform, false);
                }

                puzzlePaintController = controllerHost.AddComponent<PuzzlePaintController>();
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
            AddListener(contextRotateButton, HandleContextRotateButton);
            AddListener(contextDeleteButton, HandleContextDeleteButton);
            AddListener(contextCancelButton, HandleContextCancelButton);

            AddListener(puzzleExitButton, HandleExitToMainButton);

            if (furnitureContextPanel != null)
            {
                contextPanelParent = furnitureContextPanel.parent as RectTransform;
                furnitureContextPanel.gameObject.SetActive(false);
            }

            if (buildModeController != null)
            {
                buildModeController.SelectionChanged += HandleSelectionChanged;
            }
        }

        private void OnDisable()
        {
            RemoveListener(buildButton, HandleBuildButton);
            RemoveListener(puzzleButton, HandlePuzzleButton);

            RemoveListener(buildExitButton, HandleExitToMainButton);
            RemoveListener(rotateButton, HandleRotateButton);
            RemoveListener(cancelButton, HandleCancelButton);
            RemoveListener(deleteButton, HandleDeleteButton);
            RemoveListener(contextRotateButton, HandleContextRotateButton);
            RemoveListener(contextDeleteButton, HandleContextDeleteButton);
            RemoveListener(contextCancelButton, HandleContextCancelButton);

            RemoveListener(puzzleExitButton, HandleExitToMainButton);

            if (buildModeController != null)
            {
                buildModeController.SelectionChanged -= HandleSelectionChanged;
            }

            SetContextPanelVisible(false);
        }

        private void Update()
        {
            UpdateContextPanelPosition();
        }

        private void HandleBuildButton()
        {
            puzzleGameSelectController?.CloseSelector();
            modeController?.EnterBuildMode();
        }

        private void HandlePuzzleButton()
        {
            puzzleGameSelectController?.OpenSelector();
        }

        private void HandleExitToMainButton()
        {
            puzzleGameSelectController?.CloseSelector();
            puzzleLevelSelectController?.CloseLevelSelect();
            modeController?.EnterMainMode();
        }

        private void HandleRotateButton()
        {
            if (buildModeController == null)
            {
                return;
            }

            if (buildModeController.HasSelectedPlacement)
            {
                buildModeController.RotateSelectedPlacement();
                return;
            }

            buildModeController.RotatePreview();
        }

        private void HandleCancelButton()
        {
            if (buildModeController == null)
            {
                return;
            }

            if (buildModeController.HasSelectedPlacement)
            {
                buildModeController.ClearSelection();
                return;
            }

            buildModeController.CancelPlacement();
        }

        private void HandleDeleteButton()
        {
            if (buildModeController == null)
            {
                return;
            }

            if (buildModeController.HasSelectedPlacement)
            {
                buildModeController.DeleteSelectedPlacement();
            }
        }

        private void HandleContextRotateButton()
        {
            buildModeController?.RotateSelectedPlacement();
        }

        private void HandleContextDeleteButton()
        {
            buildModeController?.DeleteSelectedPlacement();
        }

        private void HandleContextCancelButton()
        {
            buildModeController?.ClearSelection();
        }

        private void HandleSelectionChanged(string _)
        {
            UpdateContextPanelPosition();
        }

        private void UpdateContextPanelPosition()
        {
            if (furnitureContextPanel == null || buildModeController == null)
            {
                return;
            }

            if (modeController != null && modeController.CurrentMode != GameMode.Build)
            {
                SetContextPanelVisible(false);
                return;
            }

            if (!buildModeController.TryGetSelectedPlacementWorldPosition(out Vector3 selectedWorldPosition)
                || buildModeController.BuildCamera == null)
            {
                SetContextPanelVisible(false);
                return;
            }

            Vector3 screenPoint = buildModeController.BuildCamera.WorldToScreenPoint(selectedWorldPosition);
            if (screenPoint.z < 0f)
            {
                SetContextPanelVisible(false);
                return;
            }

            SetContextPanelVisible(true);

            Camera uiCamera = null;
            Canvas parentCanvas = furnitureContextPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = parentCanvas.worldCamera;
            }

            if (contextPanelParent != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(contextPanelParent, screenPoint, uiCamera, out Vector2 localPoint))
            {
                furnitureContextPanel.anchoredPosition = localPoint + contextPanelScreenOffset;
            }
            else
            {
                furnitureContextPanel.position = screenPoint + (Vector3)contextPanelScreenOffset;
            }
        }

        private void SetContextPanelVisible(bool visible)
        {
            if (furnitureContextPanel != null)
            {
                furnitureContextPanel.gameObject.SetActive(visible);
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
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

