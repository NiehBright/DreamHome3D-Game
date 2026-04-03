using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Build
{
    public class BuildModeController : MonoBehaviour
    {
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
        [SerializeField, Min(0.01f)] private float tapMaxDurationSeconds = 0.25f;

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
        private bool pointerIsDown;
        private bool pointerMovedAsDrag;
        private Vector2 pointerDownScreenPosition;
        private Vector2 pointerLastScreenPosition;
        private float pointerDownTime;

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

        CreateGridState();
        BuildGridVisual();
        LoadFromSave();
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

        if (!TryGetPointerState(out Vector2 pointerScreenPosition, out bool pressedThisFrame, out bool releasedThisFrame, out bool isPressed))
        {
            return;
        }

        if (pressedThisFrame)
        {
            pointerIsDown = true;
            pointerMovedAsDrag = false;
            pointerDownScreenPosition = pointerScreenPosition;
            pointerLastScreenPosition = pointerScreenPosition;
            pointerDownTime = Time.unscaledTime;
        }

        if (pointerIsDown && isPressed)
        {
            if (!pointerMovedAsDrag)
            {
                float tapMoveThresholdSqr = tapMaxMovementPixels * tapMaxMovementPixels;
                pointerMovedAsDrag = (pointerScreenPosition - pointerDownScreenPosition).sqrMagnitude > tapMoveThresholdSqr;
            }

            if (pointerMovedAsDrag && enableCameraPan)
            {
                PanCamera(pointerLastScreenPosition, pointerScreenPosition);
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

        if (!isTap)
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
            selectedPlacementId = placementId;
            BeginMovePlacement(placementId);
            return;
        }

        if (selectedItem != null)
        {
            BeginNewPlacement(selectedItem, cell);
            TryCommitPreview();
        }
    }

        public void SetBuildActive(bool active)
        {
        isBuildActive = active;

        if (gridVisualRoot != null)
        {
            gridVisualRoot.gameObject.SetActive(active);
        }

        if (!active)
        {
            CancelPlacement();
            selectedPlacementId = null;
            isDeleteMode = false;
        }
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
        selectedPlacementId = null;
        isDeleteMode = false;
    }

        public void RotatePreview()
        {
        if (activePreview == null || activePreview.item == null || !activePreview.item.CanRotate)
        {
            return;
        }

        activePreview.rotationQuarterTurns = (activePreview.rotationQuarterTurns + 1) % 4;
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
        ToggleDeleteMode();
    }

        public void ToggleDeleteMode()
        {
        isDeleteMode = !isDeleteMode;

        if (isDeleteMode)
        {
            CancelPlacement();
            selectedPlacementId = null;
        }

        Debug.Log(isDeleteMode
            ? "Delete mode ON: tap furniture to delete."
            : "Delete mode OFF.");
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
        placedView.rotation = Quaternion.Euler(0f, activePreview.rotationQuarterTurns * 90f, 0f);

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
        runtimePlacement.view.rotation = Quaternion.Euler(0f, activePreview.rotationQuarterTurns * 90f, 0f);
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
        activePreview.view.rotation = Quaternion.Euler(0f, activePreview.rotationQuarterTurns * 90f, 0f);

        SetPreviewVisual(activePreview.view, activePreview.isValid ? validPreviewColor : invalidPreviewColor);
    }

        private void RemovePlacement(string placementId, bool refund)
        {
        if (!runtimePlacements.TryGetValue(placementId, out RuntimePlacement runtimePlacement))
        {
            return;
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
            view.rotation = Quaternion.Euler(0f, data.rotationQuarterTurns * 90f, 0f);

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

        private Vector3 GridToWorld(Vector2Int origin, Vector2Int itemSize, int rotationQuarterTurns, float yOffset)
        {
        Vector2Int rotatedSize = BuildGridState.RotateSize(itemSize, rotationQuarterTurns);
        float centerX = origin.x + (rotatedSize.x - 1) * 0.5f;
        float centerY = origin.y + (rotatedSize.y - 1) * 0.5f;

        return gridOrigin + new Vector3(centerX * cellSize, yOffset, centerY * cellSize);
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

        private static bool TryGetPointerState(out Vector2 position, out bool pressedThisFrame, out bool releasedThisFrame, out bool isPressed)
        {
        position = default;
        pressedThisFrame = false;
        releasedThisFrame = false;
        isPressed = false;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            position = touch.position.ReadValue();
            pressedThisFrame = touch.press.wasPressedThisFrame;
            releasedThisFrame = touch.press.wasReleasedThisFrame;
            isPressed = touch.press.isPressed;
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
        gridVisualRoot.gameObject.SetActive(isBuildActive);

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
