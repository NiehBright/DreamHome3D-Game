using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Runtime.Build
{
    public class BuildModeController : MonoBehaviour
    {
        private const string BoundsObjectName = "MainCameraBounds";
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("References")]
        [SerializeField] private Camera buildCamera;
        [SerializeField] private Transform buildRoot;
        [SerializeField] private FurnitureCatalogData catalog;
        [SerializeField] private CurrencyWallet wallet;

        [Header("Default Room Visuals")]
        [SerializeField] private bool spawnDefaultSideWalls;
        [SerializeField] private FurnitureItemData defaultLeftWallItem;
        [SerializeField] private FurnitureItemData defaultRightWallItem;
        [SerializeField] private Vector3 roomWallOffset = Vector3.zero;

        [Header("Selection Highlight")]
        [SerializeField] private bool highlightSelectedPlacement = true;
        [SerializeField] private Color selectedHighlightColor = new Color(1f, 0.95f, 0.45f, 1f);
        [SerializeField, Range(0f, 5f)] private float selectedEmissionIntensity = 1.2f;

        [Header("Grid")]
        [SerializeField, Min(1)] private int gridWidth = 8;
        [SerializeField, Min(1)] private int gridHeight = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        [SerializeField] private global::LevelData blockedSourceLevel;
        [SerializeField] private bool useWallCellsAsBlocked = true;

        [Header("Placement")]
        [SerializeField] private Color validPreviewColor = new Color(0.2f, 1f, 0.3f, 0.7f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        [SerializeField] private float previewYOffset = 0.05f;
        [SerializeField] private float placedYOffset = 0f;
        [SerializeField, Range(0f, 1f)] private float deleteRefundRate = 0.5f;

        [Header("Camera Pan")]
        [SerializeField] private bool enableCameraPan = true;
        [SerializeField, Min(1f)] private float tapMaxMovementPixels = 14f;
        [SerializeField, Min(1f)] private float cameraPanStartPixels = 28f;
        [SerializeField, Min(0.01f)] private float tapMaxDurationSeconds = 0.25f;

        [Header("Camera Zoom")]
        [SerializeField] private bool enableCameraZoom = true;
        [SerializeField] private bool allowMouseWheelZoomOverUi = true;
        [SerializeField, Min(0.0001f)] private float pinchZoomSensitivity = 0.01f;
        [SerializeField, Min(0.0001f)] private float mouseWheelZoomSensitivity = 0.1f;
        [SerializeField, Min(0.0001f)] private float orthographicZoomSpeed = 0.02f;
        [SerializeField, Min(0.0001f)] private float perspectiveZoomSpeed = 0.1f;
        [SerializeField, Min(0.1f)] private float minOrthographicSize = 3f;
        [SerializeField, Min(0.1f)] private float maxOrthographicSize = 12f;
        [SerializeField, Range(1f, 179f)] private float minPerspectiveFov = 25f;
        [SerializeField, Range(1f, 179f)] private float maxPerspectiveFov = 70f;

        [Header("Camera Bounds")]
        [SerializeField] private bool clampCameraToBounds = true;
        [SerializeField] private BoxCollider cameraBoundsCollider;
        [SerializeField] private bool useColliderBoundsOnly = true;
        [SerializeField] private Vector2 cameraMinXZ = new Vector2(-4f, -4f);
        [SerializeField] private Vector2 cameraMaxXZ = new Vector2(12f, 12f);

        [Header("Grid Visual")]
        [SerializeField] private bool showGridOverlay = true;
        [SerializeField] private bool useGridCellPrefabs;
        [SerializeField] private GameObject gridCellPrefabA;
        [SerializeField] private GameObject gridCellPrefabB;
        [SerializeField] private bool autoFitGridCellPrefabs = true;
        [SerializeField] private bool showGridLines = true;
        [SerializeField] private bool showGridLinesOnlyInBuildMode = true;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField, Min(0.005f)] private float gridLineWidth = 0.04f;
        [SerializeField] private float gridLineYOffset = 0.08f;
        [SerializeField] private Color gridLightColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color gridDarkColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
        [SerializeField] private float gridVisualYOffset = 0.01f;
        [SerializeField] private Material gridMaterial;

        [Header("Save")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private string saveKey = "dreamhome_build_mvp";

        private readonly Dictionary<string, RuntimePlacement> runtimePlacements = new Dictionary<string, RuntimePlacement>();
        private BuildGridState gridState;

        private FurnitureItemData selectedItem;
        private PreviewState activePreview;
        private string selectedPlacementId;
        private bool isBuildActive;
        private bool isDeleteMode;
        private Transform gridVisualRoot;
        private Transform gridLinesRoot;
        private Material runtimeGridLightMaterial;
        private Material runtimeGridDarkMaterial;
        private Material runtimeGridLineMaterial;
        private bool gridVisible = true;
        private Transform roomWallRoot;
        private bool pointerIsDown;
        private bool pointerMovedAsDrag;
        private bool pointerBlockedByUi;
        private int activePointerId = -1;
        private Vector2 pointerDownScreenPosition;
        private Vector2 pointerLastScreenPosition;
        private float pointerDownTime;
        private int lastPreviewRotateFrame = -1;
        private int lastSelectedRotateFrame = -1;
        private bool isPinchZoomActive;
        private float previousPinchDistance;
        private string highlightedPlacementId;
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>(8);

        public event Action<string> SelectionChanged;

        public Camera BuildCamera => buildCamera;
        public bool HasSelectedPlacement => !string.IsNullOrEmpty(selectedPlacementId);
        public bool IsDeleteMode => isDeleteMode;

        private class RuntimePlacement
        {
            public PlacedFurnitureData data;
            public FurnitureItemData item;
            public Transform view;
            public bool isWallVisual;
        }

        private class PreviewState
        {
            public string editingPlacementId;
            public FurnitureItemData item;
            public Transform view;
            public int rotationQuarterTurns;
            public Vector2Int origin;
            public bool isValid;
            public bool isNewPurchase;
        }

        private void Awake()
        {
        if (buildCamera == null)
        {
            buildCamera = Camera.main;
        }

        TryAutoAssignBoundsCollider();

        CreateGridState();
        BuildRoomWallsVisual();
        BuildGridVisual();
        ApplyGridVisibility();
        LoadFromSave();
    }

        [ContextMenu("Create/Assign Camera Bounds Collider")]
        private void CreateOrAssignCameraBoundsCollider()
        {
            if (cameraBoundsCollider != null)
            {
                return;
            }

            GameObject boundsObject = GameObject.Find(BoundsObjectName);
            if (boundsObject == null)
            {
                boundsObject = new GameObject(BoundsObjectName);
                boundsObject.transform.SetParent(transform, false);
                boundsObject.transform.localPosition = Vector3.zero;
            }

            BoxCollider createdCollider = boundsObject.GetComponent<BoxCollider>();
            if (createdCollider == null)
            {
                createdCollider = boundsObject.AddComponent<BoxCollider>();
            }

            createdCollider.isTrigger = true;
            createdCollider.size = new Vector3(gridWidth * cellSize, 20f, gridHeight * cellSize);
            createdCollider.center = new Vector3((gridWidth - 1) * cellSize * 0.5f, 0f, (gridHeight - 1) * cellSize * 0.5f);

            cameraBoundsCollider = createdCollider;
        }

        private void TryAutoAssignBoundsCollider()
        {
            if (cameraBoundsCollider != null)
            {
                return;
            }

            if (buildCamera != null)
            {
                cameraBoundsCollider = buildCamera.GetComponentInChildren<BoxCollider>(true);
            }

            if (cameraBoundsCollider == null)
            {
                cameraBoundsCollider = GetComponentInChildren<BoxCollider>(true);
            }

            if (cameraBoundsCollider == null)
            {
                GameObject boundsObject = GameObject.Find(BoundsObjectName);
                if (boundsObject != null)
                {
                    cameraBoundsCollider = boundsObject.GetComponent<BoxCollider>();
                }
            }
        }

        private void OnDestroy()
        {
        ClearGridVisual();
        ClearRoomWallsVisual();

        if (runtimeGridLightMaterial != null)
        {
            Destroy(runtimeGridLightMaterial);
        }

        if (runtimeGridDarkMaterial != null)
        {
            Destroy(runtimeGridDarkMaterial);
        }

        if (runtimeGridLineMaterial != null)
        {
            Destroy(runtimeGridLineMaterial);
        }
    }

        private void Update()
        {
        if (!isBuildActive)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            ToggleDeleteMode();
        }

        if (TryHandleCameraZoom())
        {
            // Zoom gesture should not also place/select furniture in the same frame.
            pointerIsDown = false;
            pointerMovedAsDrag = false;
            pointerBlockedByUi = false;
            activePointerId = -1;
            return;
        }

        if (!TryGetPointerState(out Vector2 pointerScreenPosition, out bool pressedThisFrame, out bool releasedThisFrame, out bool isPressed, out int pointerId))
        {
            return;
        }

        if (pressedThisFrame)
        {
            if (pointerIsDown)
            {
                pointerIsDown = false;
                pointerMovedAsDrag = false;
                pointerBlockedByUi = false;
                activePointerId = -1;
            }

            pointerIsDown = true;
            pointerMovedAsDrag = false;
            activePointerId = pointerId;
            pointerDownScreenPosition = pointerScreenPosition;
            pointerLastScreenPosition = pointerScreenPosition;
            pointerDownTime = Time.unscaledTime;
            pointerBlockedByUi = IsPointerOverUi(activePointerId, pointerScreenPosition);
        }

        if (pointerIsDown && !isPressed && !releasedThisFrame)
        {
            pointerIsDown = false;
            pointerMovedAsDrag = false;
            pointerBlockedByUi = false;
            activePointerId = -1;
        }

        if (pointerIsDown && isPressed)
        {
            if (IsPointerOverUi(activePointerId, pointerScreenPosition))
            {
                pointerBlockedByUi = true;
            }

            if (pointerBlockedByUi)
            {
                pointerLastScreenPosition = pointerScreenPosition;
                return;
            }

            if (!pointerMovedAsDrag)
            {
                float tapMoveThresholdSqr = tapMaxMovementPixels * tapMaxMovementPixels;
                pointerMovedAsDrag = (pointerScreenPosition - pointerDownScreenPosition).sqrMagnitude > tapMoveThresholdSqr;
            }

            if (enableCameraPan)
            {
                float panThreshold = Mathf.Max(tapMaxMovementPixels, cameraPanStartPixels);
                float panThresholdSqr = panThreshold * panThreshold;
                float dragDistanceSqr = (pointerScreenPosition - pointerDownScreenPosition).sqrMagnitude;

                if (dragDistanceSqr > panThresholdSqr)
                {
                    PanCamera(pointerLastScreenPosition, pointerScreenPosition);
                }
            }

            pointerLastScreenPosition = pointerScreenPosition;
        }

        if (!releasedThisFrame)
        {
            return;
        }

        bool isTap = pointerIsDown
            && !pointerMovedAsDrag
            && Time.unscaledTime - pointerDownTime <= tapMaxDurationSeconds;

        pointerIsDown = false;
        bool blockedByUiThisGesture = pointerBlockedByUi;
        pointerBlockedByUi = false;
        activePointerId = -1;

        if (!isTap)
        {
            return;
        }

        if (blockedByUiThisGesture || IsPointerOverUi(pointerId, pointerScreenPosition))
        {
            return;
        }

        HandleTap(pointerScreenPosition);
    }

        private void HandleTap(Vector2 pointerScreenPosition)
        {
        if (isDeleteMode)
        {
            if (TryGetPlacementAtPointer(pointerScreenPosition, out string deletePlacementId))
            {
                RemovePlacement(deletePlacementId, true);
                SaveIfNeeded();
            }

            if (TryGetGridCell(pointerScreenPosition, out Vector2Int deleteCell)
                && gridState.TryGetPlacementAtCell(deleteCell, out string deleteCellPlacementId))
            {
                RemovePlacement(deleteCellPlacementId, true);
                SaveIfNeeded();
            }

            return;
        }

        if (!TryGetGridCell(pointerScreenPosition, out Vector2Int cell))
        {
            return;
        }

        if (activePreview != null)
        {
            activePreview.origin = cell;
            ApplyPreviewTransform();
            TryCommitPreview();
            return;
        }

        if (gridState.TryGetPlacementAtCell(cell, out string placementId))
        {
            if (selectedPlacementId == placementId)
            {
                BeginMovePlacement(placementId);
            }
            else
            {
                SetSelectedPlacement(placementId);
                selectedItem = null;
            }

            return;
        }

        SetSelectedPlacement(null);

        if (selectedItem != null)
        {
            BeginNewPlacement(selectedItem, cell);
            TryCommitPreview();
        }
    }

        public void SetBuildActive(bool active)
        {
        isBuildActive = active;

        if (!active)
        {
            isDeleteMode = false;
            CancelPlacement();
            SetSelectedPlacement(null);
        }

        ApplyGridVisibility();
    }

        public void SetGridVisible(bool visible)
        {
        gridVisible = visible;
        ApplyGridVisibility();
    }

        public void SelectFurnitureById(string itemId)
        {
        if (catalog == null || !catalog.TryGetById(itemId, out FurnitureItemData item))
        {
            Debug.LogWarning($"Furniture id not found: {itemId}");
            return;
        }

        SelectFurniture(item);
    }

        public void SelectFurniture(FurnitureItemData item)
        {
        selectedItem = item;
        SetSelectedPlacement(null);
        isDeleteMode = false;
    }

        public void RotatePreview()
        {
        if (lastPreviewRotateFrame == Time.frameCount)
        {
            return;
        }

        lastPreviewRotateFrame = Time.frameCount;

        if (activePreview == null || activePreview.item == null || !activePreview.item.CanRotate)
        {
            return;
        }

        int nextRotation = (activePreview.rotationQuarterTurns + 1) % 4;
        if (TryFindRotationOrigin(
            activePreview.item,
            activePreview.origin,
            activePreview.rotationQuarterTurns,
            nextRotation,
            activePreview.editingPlacementId,
            out Vector2Int resolvedOrigin))
        {
            activePreview.origin = resolvedOrigin;
        }

        activePreview.rotationQuarterTurns = nextRotation;
        ApplyPreviewTransform();
    }

        public void CancelPlacement()
        {

        if (activePreview == null)
        {
            return;
        }

        if (activePreview.editingPlacementId != null)
        {
            var placement = runtimePlacements[activePreview.editingPlacementId];
            gridState.AddPlacement(placement.data, placement.item);
            placement.view.gameObject.SetActive(true);
        }

        Destroy(activePreview.view.gameObject);
        activePreview = null;
    }

        public void DeleteSelected()
        {
        DeleteSelectedPlacement();
    }

        public void ClearSelection()
        {
        SetSelectedPlacement(null);
        }

        public bool TryGetSelectedPlacementWorldPosition(out Vector3 worldPosition)
        {
        worldPosition = default;

        if (string.IsNullOrEmpty(selectedPlacementId)
            || !runtimePlacements.TryGetValue(selectedPlacementId, out RuntimePlacement placement)
            || placement.view == null)
        {
            return false;
        }

        worldPosition = placement.view.position;
        return true;
    }

        public void RotateSelectedPlacement()
        {
        if (lastSelectedRotateFrame == Time.frameCount)
        {
            return;
        }

        lastSelectedRotateFrame = Time.frameCount;

        if (string.IsNullOrEmpty(selectedPlacementId)
            || !runtimePlacements.TryGetValue(selectedPlacementId, out RuntimePlacement placement)
            || placement.item == null
            || !placement.item.CanRotate)
        {
            return;
        }

        int nextRotation = (placement.data.rotationQuarterTurns + 1) % 4;

        gridState.RemovePlacement(selectedPlacementId, placement.item);
        Vector2Int nextOrigin = placement.data.Origin;
        if (TryFindRotationOrigin(
            placement.item,
            placement.data.Origin,
            placement.data.rotationQuarterTurns,
            nextRotation,
            null,
            out Vector2Int resolvedOrigin))
        {
            nextOrigin = resolvedOrigin;
        }

        PlacementValidationResult validation = gridState.ValidatePlacement(placement.item, nextOrigin, nextRotation);
        if (!validation.isValid)
        {
            gridState.AddPlacement(placement.data, placement.item);
            return;
        }

        placement.data.x = nextOrigin.x;
        placement.data.y = nextOrigin.y;
        placement.data.rotationQuarterTurns = nextRotation;
        placement.view.rotation = GetPlacementRotation(placement.item, nextRotation);
        placement.view.position = GridToWorld(nextOrigin, placement.item.Size, nextRotation, placedYOffset);
        gridState.AddPlacement(placement.data, placement.item);
        SaveIfNeeded();
    }

        public void DeleteSelectedPlacement()
        {
        if (string.IsNullOrEmpty(selectedPlacementId))
        {
            return;
        }

        string placementId = selectedPlacementId;
        SetSelectedPlacement(null);
        RemovePlacement(placementId, true);
        SaveIfNeeded();
    }

        public void EnableDeleteMode()
        {
        isDeleteMode = true;
        CancelPlacement();
        SetSelectedPlacement(null);
        Debug.Log("Delete mode ON: tap furniture to delete.");
    }

        public void DisableDeleteMode()
        {
        isDeleteMode = false;
        Debug.Log("Delete mode OFF.");
    }

        public void ToggleDeleteMode()
        {
        SetDeleteMode(!isDeleteMode);
    }

        public void SetDeleteMode(bool enabled)
        {
        if (isDeleteMode == enabled)
        {
            return;
        }

        isDeleteMode = enabled;

        if (isDeleteMode)
        {
            CancelPlacement();
            SetSelectedPlacement(null);
            Debug.Log("Delete mode ON: tap furniture to delete.");
        }
        else
        {
            Debug.Log("Delete mode OFF.");
        }
    }

        private void PanCamera(Vector2 fromScreenPosition, Vector2 toScreenPosition)
        {
        if (buildCamera == null)
        {
            return;
        }

        if (!TryScreenToGroundPoint(fromScreenPosition, out Vector3 fromWorld)
            || !TryScreenToGroundPoint(toScreenPosition, out Vector3 toWorld))
        {
            return;
        }

        Vector3 delta = toWorld - fromWorld;
        buildCamera.transform.position += new Vector3(delta.x, 0f, delta.z);
        ClampBuildCameraPosition();
    }

        private void ClampBuildCameraPosition()
        {
        if (!clampCameraToBounds || buildCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = buildCamera.transform.position;

        if (cameraBoundsCollider != null)
        {
            Bounds bounds = cameraBoundsCollider.bounds;
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, bounds.min.x, bounds.max.x);
            cameraPosition.z = Mathf.Clamp(cameraPosition.z, bounds.min.z, bounds.max.z);
        }
        else if (useColliderBoundsOnly)
        {
            return;
        }
        else
        {
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, Mathf.Min(cameraMinXZ.x, cameraMaxXZ.x), Mathf.Max(cameraMinXZ.x, cameraMaxXZ.x));
            cameraPosition.z = Mathf.Clamp(cameraPosition.z, Mathf.Min(cameraMinXZ.y, cameraMaxXZ.y), Mathf.Max(cameraMinXZ.y, cameraMaxXZ.y));
        }

        buildCamera.transform.position = cameraPosition;
    }

        private bool TryScreenToGroundPoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
        worldPoint = default;

        if (buildCamera == null)
        {
            return false;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, gridOrigin.y, 0f));
        Ray ray = buildCamera.ScreenPointToRay(screenPosition);
        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        worldPoint = ray.GetPoint(distance);
        return true;
    }

        private bool TryHandleCameraZoom()
        {
        if (!enableCameraZoom || buildCamera == null)
        {
            return false;
        }

        if (Touchscreen.current != null)
        {
            var touch0 = Touchscreen.current.touches[0];
            var touch1 = Touchscreen.current.touches[1];
            bool hasPinch = touch0.press.isPressed && touch1.press.isPressed;

            if (hasPinch)
            {
                Vector2 p0 = touch0.position.ReadValue();
                Vector2 p1 = touch1.position.ReadValue();
                float pinchDistance = Vector2.Distance(p0, p1);

                if (isPinchZoomActive)
                {
                    float pinchDelta = pinchDistance - previousPinchDistance;
                    if (Mathf.Abs(pinchDelta) > 0.01f)
                    {
                        ApplyZoomDelta(pinchDelta * pinchZoomSensitivity);
                    }
                }

                previousPinchDistance = pinchDistance;
                isPinchZoomActive = true;
                return true;
            }

            isPinchZoomActive = false;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < 0.001f)
        {
            return false;
        }

        if (!allowMouseWheelZoomOverUi && IsPointerOverUi(-1, Mouse.current.position.ReadValue()))
        {
            return false;
        }

        ApplyZoomDelta(scrollY * mouseWheelZoomSensitivity);
        return true;
    }

        private void ApplyZoomDelta(float zoomDelta)
        {
        if (buildCamera == null)
        {
            return;
        }

        if (buildCamera.orthographic)
        {
            float minSize = Mathf.Min(minOrthographicSize, maxOrthographicSize);
            float maxSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
            float nextSize = buildCamera.orthographicSize - (zoomDelta * orthographicZoomSpeed);
            buildCamera.orthographicSize = Mathf.Clamp(nextSize, minSize, maxSize);
            return;
        }

        float minFov = Mathf.Min(minPerspectiveFov, maxPerspectiveFov);
        float maxFov = Mathf.Max(minPerspectiveFov, maxPerspectiveFov);
        float nextFov = buildCamera.fieldOfView - (zoomDelta * perspectiveZoomSpeed);
        buildCamera.fieldOfView = Mathf.Clamp(nextFov, minFov, maxFov);
    }

        private void BeginNewPlacement(FurnitureItemData item, Vector2Int origin)
        {
        if (item == null || item.Prefab == null)
        {
            return;
        }

        if (wallet != null && wallet.Coins < item.Price)
        {
            Debug.Log("Not enough coins.");
            return;
        }

        CancelPlacement();

        Transform preview = Instantiate(item.Prefab, buildRoot != null ? buildRoot : transform).transform;
        SetPreviewVisual(preview, invalidPreviewColor);

        activePreview = new PreviewState
        {
            item = item,
            view = preview,
            origin = origin,
            rotationQuarterTurns = 0,
            isNewPurchase = true
        };

        ApplyPreviewTransform();
    }

        private void BeginMovePlacement(string placementId)
        {
        if (!runtimePlacements.TryGetValue(placementId, out RuntimePlacement runtimePlacement))
        {
            return;
        }

        SetSelectedPlacement(null);

        CancelPlacement();

        gridState.RemovePlacement(placementId, runtimePlacement.item);

        Transform preview = Instantiate(runtimePlacement.item.Prefab, buildRoot != null ? buildRoot : transform).transform;
        SetPreviewVisual(preview, invalidPreviewColor);

        activePreview = new PreviewState
        {
            editingPlacementId = placementId,
            item = runtimePlacement.item,
            view = preview,
            origin = runtimePlacement.data.Origin,
            rotationQuarterTurns = runtimePlacement.data.rotationQuarterTurns,
            isNewPurchase = false
        };

        runtimePlacement.view.gameObject.SetActive(false);
        ApplyPreviewTransform();
    }

        private void TryCommitPreview()
        {
        if (activePreview == null || !activePreview.isValid)
        {
            if (activePreview != null && activePreview.isNewPurchase)
            {
                CancelPlacement();
            }
            return;
        }

        if (activePreview.isNewPurchase && wallet != null && !wallet.TrySpend(activePreview.item.Price))
        {
            Debug.Log("Not enough coins.");
            return;
        }

        if (!string.IsNullOrEmpty(activePreview.editingPlacementId))
        {
            UpdateMovedPlacement(activePreview.editingPlacementId);
        }
        else
        {
            CreatePlacementFromPreview();
        }

        Destroy(activePreview.view.gameObject);
        activePreview = null;
        SaveIfNeeded();
    }

        private void CreatePlacementFromPreview()
        {
        string placementId = Guid.NewGuid().ToString("N");
        var data = new PlacedFurnitureData
        {
            placementId = placementId,
            furnitureId = activePreview.item.ItemId,
            x = activePreview.origin.x,
            y = activePreview.origin.y,
            rotationQuarterTurns = activePreview.rotationQuarterTurns
        };

        Transform placedView = Instantiate(activePreview.item.Prefab, buildRoot != null ? buildRoot : transform).transform;
        placedView.position = GridToWorld(activePreview.origin, activePreview.item.Size, activePreview.rotationQuarterTurns, placedYOffset);
        placedView.rotation = GetPlacementRotation(activePreview.item, activePreview.rotationQuarterTurns);

        runtimePlacements[placementId] = new RuntimePlacement
        {
            data = data,
            item = activePreview.item,
            view = placedView
        };

        gridState.AddPlacement(data, activePreview.item);
    }

        private void UpdateMovedPlacement(string placementId)
        {
        RuntimePlacement runtimePlacement = runtimePlacements[placementId];

        runtimePlacement.data.x = activePreview.origin.x;
        runtimePlacement.data.y = activePreview.origin.y;
        runtimePlacement.data.rotationQuarterTurns = activePreview.rotationQuarterTurns;

        runtimePlacement.view.position = GridToWorld(activePreview.origin, runtimePlacement.item.Size, activePreview.rotationQuarterTurns, placedYOffset);
        runtimePlacement.view.rotation = GetPlacementRotation(runtimePlacement.item, activePreview.rotationQuarterTurns);
        runtimePlacement.view.gameObject.SetActive(true);

        gridState.AddPlacement(runtimePlacement.data, runtimePlacement.item);
    }

        private void UpdatePreviewPosition(Vector2 pointerScreenPosition)
        {
        if (!TryGetGridCell(pointerScreenPosition, out Vector2Int cell))
        {
            return;
        }

        activePreview.origin = cell;
        ApplyPreviewTransform();
    }

        private void ApplyPreviewTransform()
        {
        if (activePreview == null)
        {
            return;
        }

        string ignoredPlacementId = activePreview.editingPlacementId;
        PlacementValidationResult validation = gridState.ValidatePlacement(
            activePreview.item,
            activePreview.origin,
            activePreview.rotationQuarterTurns,
            ignoredPlacementId);

        activePreview.isValid = validation.isValid;

        activePreview.view.position = GridToWorld(
            activePreview.origin,
            activePreview.item.Size,
            activePreview.rotationQuarterTurns,
            previewYOffset);
        activePreview.view.rotation = GetPlacementRotation(activePreview.item, activePreview.rotationQuarterTurns);

        SetPreviewVisual(activePreview.view, activePreview.isValid ? validPreviewColor : invalidPreviewColor);
    }

        private void RemovePlacement(string placementId, bool refund)
        {
        if (!runtimePlacements.TryGetValue(placementId, out RuntimePlacement runtimePlacement))
        {
            return;
        }

        if (runtimePlacement.isWallVisual)
        {
            if (selectedPlacementId == placementId)
            {
                SetSelectedPlacement(null);
            }

            if (runtimePlacement.view != null)
            {
                Destroy(runtimePlacement.view.gameObject);
            }

            runtimePlacements.Remove(placementId);
            return;
        }

        if (selectedPlacementId == placementId)
        {
            SetSelectedPlacement(null);
        }

        if (runtimePlacement.item != null)
        {
            gridState.RemovePlacement(placementId, runtimePlacement.item);
        }

        if (runtimePlacement.view != null)
        {
            Destroy(runtimePlacement.view.gameObject);
        }

        if (highlightedPlacementId == placementId)
        {
            highlightedPlacementId = null;
        }

        if (refund && wallet != null && runtimePlacement.item != null)
        {
            int refundAmount = Mathf.RoundToInt(runtimePlacement.item.Price * deleteRefundRate);
            wallet.Add(refundAmount);
        }

        runtimePlacements.Remove(placementId);
    }

        private void BuildRoomWallsVisual()
        {
        if (!spawnDefaultSideWalls)
        {
            ClearRoomWallsVisual();
            return;
        }

        ClearRoomWallsVisual();

        roomWallRoot = new GameObject("BuildRoomWalls").transform;
        roomWallRoot.SetParent(buildRoot != null ? buildRoot : transform, false);
        roomWallRoot.position = gridOrigin + roomWallOffset;

        CreateWallVisual("default_wall_left", defaultLeftWallItem, true);
        CreateWallVisual("default_wall_right", defaultRightWallItem, false);
    }

        private void CreateWallVisual(string placementId, FurnitureItemData item, bool isLeftWall)
        {
        GameObject wallObject = null;

        if (item != null && item.Prefab != null)
        {
            wallObject = Instantiate(item.Prefab, roomWallRoot);
        }
        else
        {
            wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObject.transform.SetParent(roomWallRoot, false);
        }

        wallObject.name = placementId;
        wallObject.transform.localScale = new Vector3(
            Mathf.Max(0.2f, cellSize * 0.35f),
            Mathf.Max(1.5f, cellSize * 2f),
            Mathf.Max(1f, gridHeight * cellSize));
        wallObject.transform.localPosition = new Vector3(
            isLeftWall ? -0.5f * cellSize : gridWidth * cellSize + 0.5f * cellSize,
            Mathf.Max(1f, cellSize),
            gridHeight * cellSize * 0.5f);
        wallObject.transform.localRotation = isLeftWall ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.Euler(0f, 270f, 0f);

        RuntimePlacement placement = new RuntimePlacement
        {
            data = null,
            item = item,
            view = wallObject.transform,
            isWallVisual = true
        };

        runtimePlacements[placementId] = placement;
    }

        private void ClearRoomWallsVisual()
        {
        if (roomWallRoot != null)
        {
            Destroy(roomWallRoot.gameObject);
            roomWallRoot = null;
        }

        runtimePlacements.Remove("default_wall_left");
        runtimePlacements.Remove("default_wall_right");
    }

        private bool TryGetPlacementAtPointer(Vector2 pointerScreenPosition, out string placementId)
        {
        placementId = null;

        if (buildCamera == null)
        {
            return false;
        }

        Ray ray = buildCamera.ScreenPointToRay(pointerScreenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (TryResolvePlacementFromTransform(hits[i].transform, out placementId))
            {
                return true;
            }
        }

        return false;
    }

        private bool TryResolvePlacementFromTransform(Transform hitTransform, out string placementId)
        {
        foreach (KeyValuePair<string, RuntimePlacement> pair in runtimePlacements)
        {
            RuntimePlacement placement = pair.Value;
            if (placement.view == null)
            {
                continue;
            }

            if (hitTransform == placement.view || hitTransform.IsChildOf(placement.view))
            {
                placementId = pair.Key;
                return true;
            }
        }

        placementId = null;
        return false;
    }

        private void CreateGridState()
        {
        var blocked = new HashSet<Vector2Int>();

        if (useWallCellsAsBlocked && blockedSourceLevel != null)
        {
            gridWidth = blockedSourceLevel.Width;
            gridHeight = blockedSourceLevel.Height;

            for (int y = 0; y < blockedSourceLevel.Height; y++)
            {
                for (int x = 0; x < blockedSourceLevel.Width; x++)
                {
                    if (blockedSourceLevel.GetCell(x, y).tileType == TileType.Wall)
                    {
                        blocked.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        gridState = new BuildGridState(gridWidth, gridHeight, blocked);
    }

        private void LoadFromSave()
        {
        if (catalog == null)
        {
            return;
        }

        BuildSaveData saveData = BuildSaveService.Load(saveKey);

        if (wallet != null)
        {
            wallet.SetCoins(saveData.coins > 0 ? saveData.coins : wallet.Coins);
        }

        for (int i = 0; i < saveData.placedFurniture.Count; i++)
        {
            PlacedFurnitureData data = saveData.placedFurniture[i];
            if (!catalog.TryGetById(data.furnitureId, out FurnitureItemData item) || item.Prefab == null)
            {
                continue;
            }

            var validation = gridState.ValidatePlacement(item, data.Origin, data.rotationQuarterTurns);
            if (!validation.isValid)
            {
                continue;
            }

            Transform view = Instantiate(item.Prefab, buildRoot != null ? buildRoot : transform).transform;
            view.position = GridToWorld(data.Origin, item.Size, data.rotationQuarterTurns, placedYOffset);
            view.rotation = GetPlacementRotation(item, data.rotationQuarterTurns);

            runtimePlacements[data.placementId] = new RuntimePlacement
            {
                data = data,
                item = item,
                view = view
            };

            gridState.AddPlacement(data, item);
        }
    }

        private void SaveIfNeeded()
        {
        if (!autoSave)
        {
            return;
        }

        var data = new BuildSaveData
        {
            coins = wallet != null ? wallet.Coins : 0,
            placedFurniture = new List<PlacedFurnitureData>(runtimePlacements.Count)
        };

        foreach (RuntimePlacement placement in runtimePlacements.Values)
        {
            if (placement.isWallVisual || placement.data == null)
            {
                continue;
            }

            data.placedFurniture.Add(placement.data);
        }

        BuildSaveService.Save(saveKey, data);
    }

        private Quaternion GetPlacementRotation(FurnitureItemData item, int rotationQuarterTurns)
        {
        float baseYaw = rotationQuarterTurns * 90f;
        float offsetYaw = item != null ? item.VisualRotationOffsetDegrees : 0f;
        return Quaternion.Euler(0f, baseYaw + offsetYaw, 0f);
    }

        private Vector3 GridToWorld(Vector2Int origin, Vector2Int itemSize, int rotationQuarterTurns, float yOffset)
        {
        Vector2Int rotatedSize = BuildGridState.RotateSize(itemSize, rotationQuarterTurns);
        float centerX = origin.x + (rotatedSize.x - 1) * 0.5f;
        float centerY = origin.y + (rotatedSize.y - 1) * 0.5f;

        return gridOrigin + new Vector3(centerX * cellSize, yOffset, centerY * cellSize);
    }

        private bool TryFindRotationOrigin(
            FurnitureItemData item,
            Vector2Int currentOrigin,
            int currentRotationQuarterTurns,
            int nextRotationQuarterTurns,
            string ignoredPlacementId,
            out Vector2Int resolvedOrigin)
        {
        resolvedOrigin = currentOrigin;

        if (item == null)
        {
            return false;
        }

        Vector2Int currentSize = BuildGridState.RotateSize(item.Size, currentRotationQuarterTurns);
        Vector2Int nextSize = BuildGridState.RotateSize(item.Size, nextRotationQuarterTurns);
        Vector2 center = new Vector2(
            currentOrigin.x + (currentSize.x - 1) * 0.5f,
            currentOrigin.y + (currentSize.y - 1) * 0.5f);

        Vector2Int preferredOrigin = new Vector2Int(
            Mathf.RoundToInt(center.x - (nextSize.x - 1) * 0.5f),
            Mathf.RoundToInt(center.y - (nextSize.y - 1) * 0.5f));

        Vector2Int clampedPreferred = ClampOriginToGrid(preferredOrigin, nextSize);
        Vector2Int clampedCurrent = ClampOriginToGrid(currentOrigin, nextSize);

        if (TryValidateCandidate(item, clampedPreferred, nextRotationQuarterTurns, ignoredPlacementId, out resolvedOrigin))
        {
            return true;
        }

        if (TryValidateCandidate(item, clampedCurrent, nextRotationQuarterTurns, ignoredPlacementId, out resolvedOrigin))
        {
            return true;
        }

        const int maxSearchRadius = 2;
        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    Vector2Int candidate = new Vector2Int(clampedPreferred.x + dx, clampedPreferred.y + dy);
                    candidate = ClampOriginToGrid(candidate, nextSize);

                    if (TryValidateCandidate(item, candidate, nextRotationQuarterTurns, ignoredPlacementId, out resolvedOrigin))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

        private bool TryValidateCandidate(
            FurnitureItemData item,
            Vector2Int candidateOrigin,
            int rotationQuarterTurns,
            string ignoredPlacementId,
            out Vector2Int resolvedOrigin)
        {
        PlacementValidationResult validation = gridState.ValidatePlacement(item, candidateOrigin, rotationQuarterTurns, ignoredPlacementId);
        if (validation.isValid)
        {
            resolvedOrigin = candidateOrigin;
            return true;
        }

        resolvedOrigin = default;
        return false;
    }

        private Vector2Int ClampOriginToGrid(Vector2Int origin, Vector2Int rotatedSize)
        {
        int maxX = Mathf.Max(0, gridState.Width - rotatedSize.x);
        int maxY = Mathf.Max(0, gridState.Height - rotatedSize.y);

        return new Vector2Int(
            Mathf.Clamp(origin.x, 0, maxX),
            Mathf.Clamp(origin.y, 0, maxY));
    }

        private bool TryGetGridCell(Vector2 pointerScreenPosition, out Vector2Int cell)
        {
        cell = default;

        if (buildCamera == null)
        {
            return false;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, gridOrigin.y, 0f));
        Ray ray = buildCamera.ScreenPointToRay(pointerScreenPosition);
        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        Vector3 world = ray.GetPoint(distance) - gridOrigin;
        cell = new Vector2Int(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.z / cellSize));
        return true;
    }

        private void SetSelectedPlacement(string placementId)
        {
        if (selectedPlacementId == placementId)
        {
            return;
        }

        string previousPlacementId = selectedPlacementId;
        selectedPlacementId = placementId;
        RefreshSelectionHighlight(previousPlacementId, selectedPlacementId);
        SelectionChanged?.Invoke(selectedPlacementId);
    }

        private void RefreshSelectionHighlight(string previousPlacementId, string nextPlacementId)
        {
        if (!highlightSelectedPlacement)
        {
            if (!string.IsNullOrEmpty(previousPlacementId))
            {
                SetPlacementHighlight(previousPlacementId, false);
            }

            highlightedPlacementId = null;
            return;
        }

        if (!string.IsNullOrEmpty(previousPlacementId) && previousPlacementId != nextPlacementId)
        {
            SetPlacementHighlight(previousPlacementId, false);
        }

        if (!string.IsNullOrEmpty(nextPlacementId))
        {
            SetPlacementHighlight(nextPlacementId, true);
            highlightedPlacementId = nextPlacementId;
        }
        else
        {
            highlightedPlacementId = null;
        }
    }

        private void SetPlacementHighlight(string placementId, bool highlighted)
        {
        if (string.IsNullOrEmpty(placementId)
            || !runtimePlacements.TryGetValue(placementId, out RuntimePlacement placement)
            || placement.view == null)
        {
            return;
        }

        Renderer[] renderers = placement.view.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            if (!highlighted)
            {
                block.Clear();
                renderer.SetPropertyBlock(block);
                continue;
            }

            if (renderer.sharedMaterial != null)
            {
                if (renderer.sharedMaterial.HasProperty(BaseColorId))
                {
                    block.SetColor(BaseColorId, selectedHighlightColor);
                }
                else if (renderer.sharedMaterial.HasProperty(ColorId))
                {
                    block.SetColor(ColorId, selectedHighlightColor);
                }

                if (renderer.sharedMaterial.HasProperty(EmissionColorId))
                {
                    block.SetColor(EmissionColorId, selectedHighlightColor * selectedEmissionIntensity);
                }
            }

            renderer.SetPropertyBlock(block);
        }
    }

        private static bool IsPointerOverUi(int pointerId, Vector2 screenPosition)
        {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0 && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            return true;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        UiRaycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        EventSystem.current.RaycastAll(eventData, UiRaycastResults);
        return UiRaycastResults.Count > 0;
    }

        private static bool TryGetPointerState(
            out Vector2 position,
            out bool pressedThisFrame,
            out bool releasedThisFrame,
            out bool isPressed,
            out int pointerId)
        {
        position = default;
        pressedThisFrame = false;
        releasedThisFrame = false;
        isPressed = false;
        pointerId = -1;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            position = touch.position.ReadValue();
            pressedThisFrame = touch.press.wasPressedThisFrame;
            releasedThisFrame = touch.press.wasReleasedThisFrame;
            isPressed = touch.press.isPressed;
            pointerId = touch.touchId.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            isPressed = Mouse.current.leftButton.isPressed;
            return true;
        }

        return false;
    }

        private static void SetPreviewVisual(Transform root, Color color)
        {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderers[i].GetPropertyBlock(block);
            if (renderers[i].sharedMaterial != null && renderers[i].sharedMaterial.HasProperty("_BaseColor"))
            {
                block.SetColor("_BaseColor", color);
            }
            renderers[i].SetPropertyBlock(block);
        }
    }

        private void BuildGridVisual()
        {
        ClearGridVisual();

        if (!showGridOverlay)
        {
            return;
        }

        bool usePrefabs = useGridCellPrefabs && (gridCellPrefabA != null || gridCellPrefabB != null);
        if (!usePrefabs)
        {
            runtimeGridLightMaterial = CreateGridMaterial(gridLightColor);
            runtimeGridDarkMaterial = CreateGridMaterial(gridDarkColor);
        }

        gridVisualRoot = new GameObject("BuildGridOverlay").transform;
        gridVisualRoot.SetParent(buildRoot != null ? buildRoot : transform, false);
        gridVisualRoot.gameObject.SetActive(gridVisible);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                bool useLight = ((x + y) & 1) == 0;
                GameObject sourcePrefab = useLight
                    ? (gridCellPrefabA != null ? gridCellPrefabA : gridCellPrefabB)
                    : (gridCellPrefabB != null ? gridCellPrefabB : gridCellPrefabA);

                GameObject tile;
                if (usePrefabs && sourcePrefab != null)
                {
                    tile = Instantiate(sourcePrefab, gridVisualRoot);
                    if (autoFitGridCellPrefabs)
                    {
                        FitGridCellPrefabToCellSize(tile.transform);
                    }

                    tile.transform.position = gridOrigin + new Vector3(x * cellSize, gridVisualYOffset, y * cellSize);
                }
                else
                {
                    tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.transform.SetParent(gridVisualRoot, false);
                    tile.transform.position = gridOrigin + new Vector3(x * cellSize, gridVisualYOffset, y * cellSize);
                    tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    tile.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                    Renderer renderer = tile.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = useLight ? runtimeGridLightMaterial : runtimeGridDarkMaterial;
                    }

                    Collider collider = tile.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }
                }

                tile.name = $"Cell_{x}_{y}";
            }
        }

        BuildGridLinesVisual();
    }

        private void BuildGridLinesVisual()
        {
        if (!showGridLines || gridVisualRoot == null)
        {
            return;
        }

        runtimeGridLineMaterial = CreateGridMaterial(gridLineColor);

        gridLinesRoot = new GameObject("BuildGridLines").transform;
        gridLinesRoot.SetParent(gridVisualRoot, false);

        float minX = -0.5f * cellSize;
        float maxX = (gridWidth - 0.5f) * cellSize;
        float minZ = -0.5f * cellSize;
        float maxZ = (gridHeight - 0.5f) * cellSize;
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;

        float width = maxX - minX;
        float depth = maxZ - minZ;
        float lineWidth = Mathf.Max(0.005f, gridLineWidth);
        float y = gridOrigin.y + gridLineYOffset;

        for (int x = 0; x <= gridWidth; x++)
        {
            float lineX = minX + (x * cellSize);
            CreateGridLineQuad(
                $"GridLine_V_{x}",
                new Vector3(gridOrigin.x + lineX, y, gridOrigin.z + centerZ),
                new Vector3(lineWidth, depth + lineWidth, 1f));
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            float lineZ = minZ + (z * cellSize);
            CreateGridLineQuad(
                $"GridLine_H_{z}",
                new Vector3(gridOrigin.x + centerX, y, gridOrigin.z + lineZ),
                new Vector3(width + lineWidth, lineWidth, 1f));
        }
    }

        private void CreateGridLineQuad(string name, Vector3 position, Vector3 scale)
        {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = name;
        line.transform.SetParent(gridLinesRoot != null ? gridLinesRoot : gridVisualRoot, false);
        line.transform.position = position;
        line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        line.transform.localScale = scale;

        Renderer renderer = line.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = runtimeGridLineMaterial;
        }

        Collider collider = line.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

        private void FitGridCellPrefabToCellSize(Transform tileTransform)
        {
        if (tileTransform == null || cellSize <= 0f)
        {
            return;
        }

        Renderer[] renderers = tileTransform.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float width = Mathf.Max(0.0001f, bounds.size.x);
        float depth = Mathf.Max(0.0001f, bounds.size.z);

        Vector3 localScale = tileTransform.localScale;
        tileTransform.localScale = new Vector3(
            localScale.x * (cellSize / width),
            localScale.y,
            localScale.z * (cellSize / depth));
    }

        private void ClearGridVisual()
        {
        if (gridVisualRoot != null)
        {
            Destroy(gridVisualRoot.gameObject);
            gridVisualRoot = null;
        }
    }

        private void ApplyGridVisibility()
        {
        if (gridVisualRoot != null)
        {
            gridVisualRoot.gameObject.SetActive(gridVisible);
        }

        if (gridLinesRoot != null)
        {
            bool showLines = showGridLines && (!showGridLinesOnlyInBuildMode || isBuildActive);
            gridLinesRoot.gameObject.SetActive(gridVisible && showLines);
        }
    }

        private Material CreateGridMaterial(Color color)
        {
        Material material;

        if (gridMaterial != null)
        {
            material = new Material(gridMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
        }

        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(BaseColorId, color);
        }
        else if (material.HasProperty(ColorId))
        {
            material.SetColor(ColorId, color);
        }

        return material;
    }
}

}

