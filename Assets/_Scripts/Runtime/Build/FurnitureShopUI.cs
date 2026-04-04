using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Runtime.Build
{
    public class FurnitureShopUI : MonoBehaviour
    {
        [SerializeField] private FurnitureCatalogData catalog;
        [SerializeField] private BuildModeController buildModeController;
        [SerializeField] private Transform shopContentParent;
        [SerializeField] private FurnitureShopItem shopItemPrefab;
        [SerializeField] private ScrollRect shopScrollRect;

        private List<FurnitureShopItem> instantiatedItems = new List<FurnitureShopItem>();

        private void OnEnable()
        {
            if (shopContentParent != null && shopItemPrefab != null && catalog != null)
            {
                PopulateShop();
            }
        }

        private void OnDisable()
        {
            ClearShop();
        }

        private void PopulateShop()
        {
            ClearShop();

            foreach (FurnitureItemData itemData in catalog.Items)
            {
                if (itemData != null && shopItemPrefab != null)
                {
                    FurnitureShopItem shopItem = Instantiate(shopItemPrefab, shopContentParent);
                    shopItem.Initialize(itemData, OnFurnitureSelected);
                    instantiatedItems.Add(shopItem);
                }
            }
        }

        private void ClearShop()
        {
            foreach (FurnitureShopItem item in instantiatedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            instantiatedItems.Clear();
        }

        private void OnFurnitureSelected(string furnitureId)
        {
            if (buildModeController != null)
            {
                buildModeController.SelectFurnitureById(furnitureId);
            }
        }
    }
}

