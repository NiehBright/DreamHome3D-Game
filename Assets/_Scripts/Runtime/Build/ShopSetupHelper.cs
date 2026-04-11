using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Build
{
    /// <summary>
    /// Helper script để setup Shop UI một cách nhanh chóng
    /// Kèm debugging tools
    /// </summary>
    public class ShopSetupHelper : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = false;


        private FurnitureShopUI shopUI;
        private FurnitureCatalogData catalog;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                AutoSetup();
            }
        }

        [ContextMenu("Auto Setup Shop UI")]
        public void AutoSetup()
        {
            shopUI = FindFirstObjectByType<FurnitureShopUI>();
            if (shopUI == null)
            {
                Debug.LogError("FurnitureShopUI không tìm thấy trong scene!");
                return;
            }

            Debug.Log("✓ Tìm thấy FurnitureShopUI");

            // Check catalog
            catalog = Resources.Load<FurnitureCatalogData>("FurnitureCatalog");
            if (catalog == null)
            {
                Debug.LogError("FurnitureCatalog không tìm thấy trong Resources!");
                return;
            }

            Debug.Log($"✓ Tìm thấy FurnitureCatalog với {catalog.Items.Count} items");

            // Log items
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                Debug.Log($"  [{i}] {catalog.Items[i].ItemId} - {catalog.Items[i].DisplayName} (${catalog.Items[i].Price})");
            }
        }

        [ContextMenu("Test SelectFurniture (First Item)")]
        public void TestSelectFirst()
        {
            var buildController = FindFirstObjectByType<BuildModeController>();
            if (buildController == null)
            {
                Debug.LogError("BuildModeController không tìm thấy!");
                return;
            }

            if (catalog == null)
            {
                catalog = Resources.Load<FurnitureCatalogData>("FurnitureCatalog");
            }

            if (catalog.Items.Count == 0)
            {
                Debug.LogError("FurnitureCatalog trống!");
                return;
            }

            var firstItem = catalog.Items[0];
            Debug.Log($"Testing: Select {firstItem.ItemId}");
            buildController.SelectFurnitureById(firstItem.ItemId);
        }

        [ContextMenu("Test SelectFurniture (Wall)")]
        public void TestSelectWall()
        {
            var buildController = FindFirstObjectByType<BuildModeController>();
            if (buildController == null)
            {
                Debug.LogError("BuildModeController không tìm thấy!");
                return;
            }

            if (catalog == null)
            {
                catalog = Resources.Load<FurnitureCatalogData>("FurnitureCatalog");
            }

            if (catalog.TryGetById("wall_front", out var wallItem))
            {
                Debug.Log($"Testing: Select {wallItem.ItemId}");
                buildController.SelectFurnitureById("wall_front");
            }
            else
            {
                Debug.LogError("wall_front không tìm thấy trong catalog!");
            }
        }

        [ContextMenu("Log All Furniture")]
        public void LogAllFurniture()
        {
            if (catalog == null)
            {
                catalog = Resources.Load<FurnitureCatalogData>("FurnitureCatalog");
            }

            Debug.Log("=== FURNITURE CATALOG ===");
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                var item = catalog.Items[i];
                Debug.Log($"{i}: {item.ItemId}" +
                    $"\n  - Name: {item.DisplayName}" +
                    $"\n  - Price: {item.Price}" +
                    $"\n  - Size: {item.Size.x}x{item.Size.y}" +
                    $"\n  - CanRotate: {item.CanRotate}" +
                    $"\n  - Prefab: {(item.Prefab != null ? item.Prefab.name : "NULL")}");
            }
        }

        [ContextMenu("Check Shop UI Setup")]
        public void CheckShopSetup()
        {
            Debug.Log("=== CHECKING SHOP UI SETUP ===");

            var shopUI = FindFirstObjectByType<FurnitureShopUI>();
            if (shopUI != null)
            {
                Debug.Log("✓ FurnitureShopUI found");
            }
            else
            {
                Debug.LogError("✗ FurnitureShopUI NOT found");
            }

            var modeUiBindings = FindFirstObjectByType<ModeUiBindings>();
            if (modeUiBindings != null)
            {
                Debug.Log("✓ ModeUiBindings found");
            }
            else
            {
                Debug.LogError("✗ ModeUiBindings NOT found");
            }

            var buildController = FindFirstObjectByType<BuildModeController>();
            if (buildController != null)
            {
                Debug.Log("✓ BuildModeController found");
            }
            else
            {
                Debug.LogError("✗ BuildModeController NOT found");
            }

            var catalog = Resources.Load<FurnitureCatalogData>("FurnitureCatalog");
            if (catalog != null)
            {
                Debug.Log($"✓ FurnitureCatalog found ({catalog.Items.Count} items)");
            }
            else
            {
                Debug.LogError("✗ FurnitureCatalog NOT found in Resources");
            }
        }
    }
}

