# HƯỚNG DẪN SETUP DYNAMIC SHOP + 3 TƯỜNG

## 1. TỔNG QUAN CÁC THAY ĐỔI

### Được tạo:
- ✅ **FurnitureShopUI.cs** - quản lý shop list động
- ✅ **FurnitureShopItem.cs** - item trong shop
- ✅ **3 Wall assets** - wall_front, wall_left, wall_right
- ✅ **FurnitureCatalog.asset** - cập nhật thêm 3 walls

### Để cấu hình:
- ModeUiBindings đã thêm reference FurnitureShopUI
- BuildModeController đã có SelectFurnitureById() để gọi từ shop

---

## 2. CÁCH SETUP UI TRONG UNITY

### Bước 1: Tìm Canvas Build UI
Trong hierarchy, tìm:
```
Canvas
  ├─ UI_Build
  │   ├─ BackButton
  │   ├─ RotateButton
  │   ├─ CancelButton
  │   ├─ DeleteButton
  │   └─ [THÊM] ShopRoot
  │       └─ ShopScroll (ScrollRect)
  │           └─ ShopContent (VerticalLayoutGroup)
```

### Bước 2: Tạo Shop UI Structure
1. **Tạo Panel "ShopRoot"** dưới UI_Build
   - Add component: **FurnitureShopUI**

2. **Tạo ScrollRect (ShopScroll)** là con của ShopRoot
   - Add component: ScrollRect
   - Content: ShopContent

3. **Tạo Panel "ShopContent"** là con của ShopScroll
   - Add component: VerticalLayoutGroup
   - Child Force Expand: Width ✓, Height ✓

### Bước 3: Tạo Shop Item Prefab
1. Tạo một GameObject tên "ShopItemTemplate" (hoặc sử dụng trong Resources)
2. Thêm Button component
3. Thêm FurnitureShopItem component
4. Add UI elements con (NameText, PriceText, ItemIcon)
5. Setup references trong FurnitureShopItem:
   - selectButton → Button component
   - nameText → TextMeshProUGUI cho tên
   - priceText → TextMeshProUGUI cho giá
   - itemIcon → Image (optional)

### Bước 4: Assign References trong Inspector

#### Ở FurnitureShopUI:
- **Catalog**: Kéo FurnitureCatalog.asset từ Assets/_ScriptableObjects/Build/
- **BuildModeController**: Tìm BuildModeController trong scene
- **ShopContentParent**: Kéo panel ShopContent vào đây
- **ShopItemPrefab**: Kéo prefab ShopItemTemplate vào đây
- **ShopScrollRect**: Kéo ScrollRect vào đây

#### Ở ModeUiBindings:
- **FurnitureShopUI**: Kéo FurnitureShopUI (component trên ShopRoot) vào đây

---

## 3. CÁCH HOẠT ĐỘNG

### Flow:
1. User vào Build Mode → ModeController bật UI_Build → bật FurnitureShopUI
2. FurnitureShopUI.OnEnable() → gọi PopulateShop()
3. PopulateShop() lặp qua catalog.Items (gồm 2 chairs + 3 walls)
4. Với mỗi item, instantiate FurnitureShopItem + init
5. User click button item → OnFurnitureSelected() → BuildModeController.SelectFurnitureById()
6. BuildModeController.SelectFurniture() → chuyển sang preview mode
7. User tap grid → place furniture

---

## 4. CHI TIẾT TỪNG COMPONENT

### FurnitureShopUI.cs
```csharp
// Tự động populate khi UI được enable
// Lặp qua FurnitureCatalog.Items
// Instantiate FurnitureShopItem cho mỗi item
// Lưu references để xóa sau này
```

### FurnitureShopItem.cs
```csharp
// Gắn vào từng button item trong shop
// Initialize(itemData) để set up tên, giá
// OnSelect callback gọi BuildModeController.SelectFurnitureById()
```

### BuildModeController.SelectFurnitureById()
```csharp
// Đã có sẵn (line 234-243)
// Tìm item trong catalog theo ID
// Gọi SelectFurniture() để bắt đầu preview
```

---

## 5. CÁC WALL ITEM ĐÃ THÊM

### wall_front.asset
- ID: `wall_front`
- Name: "Wall Front"
- Size: 1x1
- Price: 50
- CanRotate: false

### wall_left.asset
- ID: `wall_left`
- Name: "Wall Left"
- Size: 1x1
- Price: 50
- CanRotate: false

### wall_right.asset
- ID: `wall_right`
- Name: "Wall Right"
- Size: 1x1
- Price: 50
- CanRotate: false

### Tất cả sử dụng:
- Prefab: Assets/_Prefab/Wall.prefab
- FurnitureCatalog đã update thêm 3 items này

---

## 6. TROUBLESHOOT

### Shop không hiển thị item?
1. Check: FurnitureCatalog.asset có 5 items không?
   ```
   Items: [chair_01, chair_02, wall_front, wall_left, wall_right]
   ```
2. Check: ShopItemPrefab assign đúng không?
3. Check: ShopContentParent assign đúng không?
4. Check: FurnitureShopUI.OnEnable() có chạy không? (log debug)

### Item click nhưng không select?
1. Check: BuildModeController reference trong FurnitureShopUI đúng không?
2. Check: SelectFurnitureById() có chạy không? (log debug)

### Shop lỗi NullReferenceException?
1. Ensure catalog, buildModeController, shopContentParent, shopItemPrefab không null
2. Hoặc thêm null check trong PopulateShop()

---

## 7. MỞ RỘNG SAU

- Add categories (chairs, walls, tables, etc.)
- Add filter/search
- Add thumbnail images
- Add "Owned" indicator
- Add sorting (price, name, recent)

---

## 8. CÁC LỖI KHÁC ĐÃ KIỂM TRA

### Goal disappear khi có box ✓
- RefreshGoalViews() (line 370-376) sẽ ẩn goal khi box đặt trên nó
- Player có thể đứng vào goal (không restrict)

### Xoay vật chỉ 4 hướng ✓
- RotatePreview() (line 252-261) xoay +90° mỗi lần
- rotationQuarterTurns % 4 đảm bảo 4 state (0, 1, 2, 3)
- Để xoay 8 hướng, đổi: `(activePreview.rotationQuarterTurns + 1) % 8`

### Delete mode hoạt động ✓
- DeleteSelected() enable delete mode
- HandleTap() kiểm tra isDeleteMode, nếu true → RemovePlacement()
- Cần tap vào furniture để xóa

---

**SETUP XỬ LÝ - CHỮ ký anh đã xem rồi không? 😄**

