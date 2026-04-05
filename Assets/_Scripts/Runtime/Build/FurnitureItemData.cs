using UnityEngine;

namespace Runtime.Build
{
    [CreateAssetMenu(fileName = "FurnitureItem", menuName = "DreamHome/Build/Furniture Item")]
    public class FurnitureItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "chair_01";
        [SerializeField] private string displayName = "Chair";
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [SerializeField] private bool canRotate = true;
        [SerializeField, Min(0)] private int price = 100;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private float visualRotationOffsetDegrees;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Vector2Int Size => new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        public bool CanRotate => canRotate;
        public int Price => Mathf.Max(0, price);
        public GameObject Prefab => prefab;
        public Sprite ShopIcon => shopIcon;
        public float VisualRotationOffsetDegrees => visualRotationOffsetDegrees;
    }
}
