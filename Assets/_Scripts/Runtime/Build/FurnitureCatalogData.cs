using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Build
{
    [CreateAssetMenu(fileName = "FurnitureCatalog", menuName = "DreamHome/Build/Furniture Catalog")]
    public class FurnitureCatalogData : ScriptableObject
    {
        [SerializeField] private List<FurnitureItemData> items = new List<FurnitureItemData>();

        public IReadOnlyList<FurnitureItemData> Items => items;

        public bool TryGetById(string itemId, out FurnitureItemData item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var candidate = items[i];
                if (candidate != null && candidate.ItemId == itemId)
                {
                    item = candidate;
                    return true;
                }
            }

            item = null;
            return false;
        }
    }
}

