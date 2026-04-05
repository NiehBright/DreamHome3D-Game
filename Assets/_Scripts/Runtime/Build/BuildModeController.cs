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

        [Header("References")]
        [SerializeField] private Camera buildCamera;
        [SerializeField] private Transform buildRoot;
        [SerializeField] private FurnitureCatalogData catalog;
        [SerializeField] private CurrencyWallet wallet;

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
        private Material runtimeGridLightMaterial;
        private Material runtimeGridDarkMaterial;
        private bool gridVisible = true;
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
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>(8);

        public event Action<string> SelectionChanged;

        public Camera BuildCamera => buildCamera;

        private class RuntimePlacement
        {
            public PlacedFurnitureData data;
            public FurnitureItemData item;
            public Transform view;
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

        if (runtimeGridLightMaterial != null)
        {
            Destroy(runtimeGridLightMaterial);
        }

        if (runtimeGridDarkMaterial != null)
        {
            Destroy(runtimeGridDarkMaterial);
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
            pointerIsDown = true;
            pointerMovedAsDrag = false;
            activePointerId = pointerId;
            pointerDownScreenPosition = pointerScreenPosition;
            pointerLastScreenPosition = pointerScreenPosition;
            pointerDownTime = Time.unscaledTime;
            pointerBlockedByUi = IsPointerOverUi(activePointerId, pointerScreenPosition);
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
        if (!TryGetGridCell(pointerScreenPosition, out Vector2Int cell))
        {
            return;
        }

        if (isDeleteMode)
        {
            if (gridState.TryGetPlacementAtCell(cell, out string deletePlacementId))
            {
                RemovePlacement(deletePlacementId, true);
                SaveIfNeeded();
            }

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
        EnableDeleteMode();
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

        if (selectedPlacementId == placementId)
        {
            SetSelectedPlacement(null);
        }

        gridState.RemovePlacement(placementId, runtimePlacement.item);

        if (runtimePlacement.view != null)
        {
            Destroy(runtimePlacement.view.gameObject);
        }

        if (refund && wallet != null)
        {
            int refundAmount = Mathf.RoundToInt(runtimePlacement.item.Price * deleteRefundRate);
            wallet.Add(refundAmount);
        }

        runtimePlacements.Remove(placementId);
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

        selectedPlacementId = placementId;
        SelectionChanged?.Invoke(selectedPlacementId);
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

        runtimeGridLightMaterial = CreateGridMaterial(gridLightColor);
        runtimeGridDarkMaterial = CreateGridMaterial(gridDarkColor);

        gridVisualRoot = new GameObject("BuildGridOverlay").transform;
        gridVisualRoot.SetParent(buildRoot != null ? buildRoot : transform, false);
        gridVisualRoot.gameObject.SetActive(gridVisible);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Cell_{x}_{y}";
                tile.transform.SetParent(gridVisualRoot, false);
                tile.transform.position = gridOrigin + new Vector3(x * cellSize, gridVisualYOffset, y * cellSize);
                tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                tile.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    bool useLight = ((x + y) & 1) == 0;
                    renderer.sharedMaterial = useLight ? runtimeGridLightMaterial : runtimeGridDarkMaterial;
                }

                Collider collider = tile.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
        }
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

