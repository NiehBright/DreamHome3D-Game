using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Runtime.Build
{
    public class FurnitureShopItem : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Image itemIcon;

        private string furnitureId;
        private System.Action<string> onSelected;

        private void OnEnable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleSelect);
            }
        }

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleSelect);
            }
        }

        public void Initialize(FurnitureItemData itemData, System.Action<string> onItemSelected)
        {
            furnitureId = itemData.ItemId;
            onSelected = onItemSelected;

            if (nameText != null)
            {
                nameText.text = itemData.DisplayName;
            }

            if (priceText != null)
            {
                priceText.text = $"${itemData.Price}";
            }

            // Optionally set icon if prefab has a visual representation
            // This is simplified - you might want to generate a thumbnail from the prefab
        }

        private void HandleSelect()
        {
            onSelected?.Invoke(furnitureId);
        }
    }
}

