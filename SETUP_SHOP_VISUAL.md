# DYNAMIC SHOP - VISUAL SETUP GUIDE

## UI HIERARCHY CẦN TẠO

```
Canvas (Main Canvas)
│
└─ UI_Build (Game Object)
   │
   ├─ ShopRoot (Panel)
   │  ├─ ShopScroll (ScrollRect)
   │  │  └─ ShopContent (Panel + VerticalLayoutGroup)
   │  │     ├─ ShopItem_1 (Button)
   │  │     │  ├─ ItemName (TextMeshProUGUI) - "Chair"
   │  │     │  ├─ ItemPrice (TextMeshProUGUI) - "$100"
   │  │     │  └─ ItemIcon (Image)
   │  │     ├─ ShopItem_2 (Button)
   │  │     │  ├─ ItemName - "Wall Front"
   │  │     │  ├─ ItemPrice - "$50"
   │  │     │  └─ ItemIcon
   │  │     └─ ... (tự động generate từ code)
   │
   ├─ RotateButton (Button)
   ├─ CancelButton (Button)
   ├─ DeleteButton (Button)
   └─ ExitButton (Button)
```

## SCRIPT STRUCTURE

```
FurnitureShopUI (trên ShopRoot)
├─ catalog → FurnitureCatalog.asset
├─ buildModeController → BuildModeController
├─ shopContentParent → Panel "ShopContent"
├─ shopItemPrefab → Prefab "ShopItemTemplate"
└─ shopScrollRect → ScrollRect "ShopScroll"

     ↓ (OnEnable)
     
PopulateShop()
├─ Lấy mỗi item từ catalog.Items
├─ Instantiate FurnitureShopItem
└─ Initialize với itemData

     ↓ (User click item)
     
FurnitureShopItem.HandleSelect()
└─ onSelected?.Invoke(furnitureId)
   └─ FurnitureShopUI.OnFurnitureSelected()
      └─ buildModeController.SelectFurnitureById(furnitureId)
```

## STEP BY STEP SETUP

### 1️⃣ Tạo ShopRoot Panel

```
Right-click UI_Build
→ Create Empty
→ Rename: ShopRoot
→ Add Component: Image (RawImage để làm background)
→ Layout: Anchor Presets → Stretch → Stretch
```

**Components:**
- ✓ Rect Transform (Anchor: Stretch - Stretch)
- ✓ Image (Color: tùy ý, có thể trong suốt)
- ✓ **FurnitureShopUI (ADD NEW)**

### 2️⃣ Tạo ScrollRect

```
Right-click ShopRoot
→ Create - UI → ScrollView → Scroll Rect
→ Rename: ShopScroll
→ Delete "Scrollbar Horizontal"
```

**Cấu hình:**
- Content: ShopContent (sẽ tạo ở bước 3)
- Horizontal: ✗
- Vertical: ✓
- Vertical Scrollbar: (empty)

### 3️⃣ Tạo ShopContent (VerticalLayoutGroup)

```
Tìm trong hierarchy: ScrollView/Viewport/Content
Rename: ShopContent
Add Component: VerticalLayoutGroup
```

**Components:**
- ✓ Rect Transform
- ✓ VerticalLayoutGroup (cập nhật)
  - Child Force Expand: Width ✓, Height ✗
  - Preferred Height: ✓
  - Child Control Height: ✓

### 4️⃣ Tạo ShopItemTemplate Prefab

**Cách 1: Tạo trong scene rồi save làm prefab**

```
Right-click ShopContent
→ Create Empty
→ Rename: ShopItemTemplate
→ Add Component: Button
→ Add Component: FurnitureShopItem
```

**Tạo con cho ShopItemTemplate:**

```
ShopItemTemplate
├─ ItemName (Text - "Item Name")
│  └─ Add Component: TextMeshProUGUI
├─ ItemPrice (Text - "$0")
│  └─ Add Component: TextMeshProUGUI
└─ ItemIcon (Image)
   └─ Add Component: Image
```

**Layout:**

```
ShopItemTemplate
├─ Anchors: Stretch X, Min Y
├─ Height: 80 pixels
├─ Background Color: Light Gray

ItemName (child)
├─ Position: Top Left
├─ Size: 300x30

ItemPrice (child)
├─ Position: Bottom Left
├─ Size: 100x30

ItemIcon (child)
├─ Position: Right
├─ Size: 70x70
```

**Drag ShopItemTemplate vào Assets/_Prefab/ → Save as Prefab**

### 5️⃣ Assign References trong FurnitureShopUI

Lúc này ShopItemTemplate vẫn trong scene. Cần:

1. Delete ShopItemTemplate từ ShopContent (khi nào setup xong)
2. Mở FurnitureShopUI Inspector
3. Assign:
   ```
   Catalog: (drag FurnitureCatalog.asset)
   BuildModeController: (tìm trong scene)
   ShopContentParent: (drag panel ShopContent)
   ShopItemPrefab: (drag prefab ShopItemTemplate từ Assets/_Prefab/)
   ShopScrollRect: (drag ScrollRect ShopScroll)
   ```

### 6️⃣ Setup FurnitureShopItem Prefab

Mở prefab ShopItemTemplate:

```
Button component:
├─ Target Graphic: (drag image từ con)
└─ (Empty Interactable On Start)

FurnitureShopItem component:
├─ Select Button: (drag Button component)
├─ Name Text: (drag ItemName)
├─ Price Text: (drag ItemPrice)
├─ Item Icon: (drag ItemIcon)
```

### 7️⃣ Assign FurnitureShopUI trong ModeUiBindings

Inspector ModeUiBindings:
```
Furniture Shop UI: (drag FurnitureShopUI component)
```

---

## RESULT EXPECTED

Khi vào Build Mode:
1. ✓ Shop panel hiển thị
2. ✓ Hiển thị 5 items (2 chairs + 3 walls)
3. ✓ Mỗi item có nút click
4. ✓ Click item → auto select
5. ✓ Tap grid → đặt vật

---

## DEBUGGING

### Log mọi thứ:

Thêm vào FurnitureShopUI.OnEnable():
```csharp
Debug.Log($"FurnitureShopUI.OnEnable() - catalog items: {catalog.Items.Count}");
```

Thêm vào FurnitureShopItem.HandleSelect():
```csharp
Debug.Log($"Selected furniture: {furnitureId}");
```

Thêm vào FurnitureShopUI.OnFurnitureSelected():
```csharp
Debug.Log($"FurnitureShopUI.OnFurnitureSelected({furnitureId})");
```

---

## COMMON MISTAKES

❌ **Quên assign catalog → NullReferenceException**
✓ Kiểm tra FurnitureCatalog.asset được assign

❌ **ShopItemPrefab là scene object → không được copy**
✓ Phải là prefab từ Assets/

❌ **ShopContent không có VerticalLayoutGroup → items không sắp xếp**
✓ Thêm VerticalLayoutGroup + cấu hình Child Force Expand

❌ **Button không click được → không respond**
✓ Kiểm tra Interactable = true, Target Graphic được assign

---

## PREVIEW

Sau setup, shop sẽ trông như thế này:

```
┌─────────────────────────┐
│      SHOP LIST          │
├─────────────────────────┤
│ [Icon] Chair        100 │◄─── Scrollable
│ [Icon] Chair 2      100 │
│ [Icon] Wall Front    50 │
│ [Icon] Wall Left     50 │
│ [Icon] Wall Right    50 │
├─────────────────────────┤
│ [Rotate] [Cancel] [Del] │
└─────────────────────────┘
```

---

**Xong! Ready to setup? 🚀**

