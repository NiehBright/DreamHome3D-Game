# BUILD SYSTEM ARCHITECTURE - CHI TIẾT ĐẦY ĐỦ

## 1. SYSTEM OVERVIEW

```
┌─────────────────────────────────────────────────────┐
│            BUILDING / BUILD SYSTEM                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  INPUT (Touch/Click)                               │
│       ↓                                             │
│  BuildModeController.Update()                      │
│  ├─ TryGetPointerState()  [Touch/Mouse]            │
│  ├─ Detect Tap vs Drag                            │
│  └─ HandleTap(screenPosition)                      │
│       ↓                                             │
│  Select Item from Shop                            │
│  ├─ FurnitureShopUI.OnEnable() → PopulateShop()   │
│  ├─ User clicks item → SelectFurnitureById()      │
│  └─ BuildModeController.SelectFurniture()         │
│       ↓                                             │
│  Create Preview (Ghost Object)                    │
│  ├─ BeginNewPlacement() → Instantiate prefab      │
│  ├─ Set preview color (red/green)                 │
│  └─ ApplyPreviewTransform()                       │
│       ↓                                             │
│  Validate Placement                               │
│  ├─ BuildGridState.ValidatePlacement()            │
│  ├─ Check: InBounds? Blocked? Overlap?            │
│  └─ Update preview color based on validity        │
│       ↓                                             │
│  Place or Cancel                                  │
│  ├─ TryCommitPreview() [if valid]                 │
│  ├─ CreatePlacementFromPreview()                  │
│  ├─ Subtract coins                                │
│  └─ Save to BuildSaveService                      │
│       ↓                                             │
│  Load from Save                                   │
│  └─ LoadFromSave() → Rebuild all placements       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 2. KEY COMPONENTS

### 2.1 BuildModeController
**File**: `Assets/_Scripts/Runtime/Build/BuildModeController.cs`

**Trách nhiệm:**
- Nhận input từ Touch/Mouse
- Quản lý preview object
- Validate placement
- Save/Load data

**Key Methods:**
```csharp
public void SetBuildActive(bool active)           // Bật/tắt build mode
public void SelectFurniture(FurnitureItemData)    // Chọn item từ shop
public void RotatePreview()                        // Xoay 90° (quarter turn)
public void CancelPlacement()                      // Hủy preview
public void DeleteSelected()                       // Enable delete mode
```

### 2.2 BuildGridState
**File**: `Assets/_Scripts/Runtime/Build/BuildGridState.cs`

**Trách nhiệm:**
- Track occupied cells
- Validate placements
- Calculate rotated sizes

**Key Methods:**
```csharp
ValidatePlacement(item, origin, rotation)         // Kiểm tra hợp lệ
AddPlacement(data, item)                           // Thêm vào grid
RemovePlacement(id, item)                          // Xóa khỏi grid
RotateSize(size, rotationQuarterTurns)            // Tính size sau xoay
```

### 2.3 FurnitureShopUI (NEW)
**File**: `Assets/_Scripts/Runtime/Build/FurnitureShopUI.cs`

**Trách nhiệm:**
- Tự động populate shop từ FurnitureCatalog
- Quản lý lifecycle của shop items
- Delegate selection event

**Key Methods:**
```csharp
private void PopulateShop()                        // Auto-populate items
private void OnFurnitureSelected(string id)        // Item selected callback
```

### 2.4 FurnitureShopItem (NEW)
**File**: `Assets/_Scripts/Runtime/Build/FurnitureShopItem.cs`

**Trách nhiệm:**
- Hiển thị item cụ thể
- Detect button click
- Invoke selection callback

---

## 3. DATA FLOW

### 3.1 PURCHASE FLOW

```
User Click Item in Shop
       ↓
FurnitureShopItem.HandleSelect()
       ↓
onSelected?.Invoke(furnitureId)
       ↓
FurnitureShopUI.OnFurnitureSelected(furnitureId)
       ↓
BuildModeController.SelectFurnitureById(furnitureId)
       ↓
BuildModeController.SelectFurniture(item)
       ↓
User Tap Grid → HandleTap()
       ↓
BeginNewPlacement(item, gridCell)
       ├─ Kiểm tra coin đủ?
       ├─ Instantiate preview
       └─ ApplyPreviewTransform()
       ↓
User Release → TryCommitPreview()
       ├─ Validate placement
       ├─ Subtract coins
       ├─ Create permanent object
       └─ Save to disk
```

### 3.2 PLACEMENT VALIDATION

```
ApplyPreviewTransform()
       ↓
BuildGridState.ValidatePlacement(item, origin, rotation)
       ↓
For Each Cell in Footprint:
  ├─ IsInside(cell)? ✓
  ├─ IsBlocked(cell)? ✓
  └─ IsOccupied(cell)? ✓
       ↓
Return: valid (✓ green) or invalid (✗ red)
```

### 3.3 ROTATION SYSTEM

```
rotationQuarterTurns: 0, 1, 2, 3 (0°, 90°, 180°, 270°)

Size Calculation:
  rotationQuarterTurns % 2 == 1:
    1x2 → 2x1   (swap width/height)
    2x3 → 3x2
  else:
    keep same size

Visual Rotation:
  Quaternion.Euler(0, quarterTurns * 90, 0)

When CanRotate = false:
  RotatePreview() disabled
  Walls always same rotation
```

---

## 4. INVENTORY & PRICING

### Current Items:

| ID | Name | Size | Price | Rotate | Prefab |
|---|---|---|---|---|---|
| chair_01 | Chair | 1x1 | 100 | ✓ | chair_model.prefab |
| chair_02 | Chair | 1x1 | 100 | ✓ | chair_model.prefab |
| wall_front | Wall Front | 1x1 | 50 | ✗ | Wall.prefab |
| wall_left | Wall Left | 1x1 | 50 | ✗ | Wall.prefab |
| wall_right | Wall Right | 1x1 | 50 | ✗ | Wall.prefab |

### Thêm Item Mới:

1. Tạo FurnitureItemData asset:
   ```
   Assets/_ScriptableObjects/Build/[category]/[name].asset
   ```

2. Assign prefab + properties

3. Thêm vào FurnitureCatalog.asset → items list

4. Done!

---

## 5. GRID & VALIDATION

### Grid Coordinate System:

```
Origin (0, 0) ← Grid Origin (Vector3)
  +Y (forward/down in grid)
  |
  +──→ +X (right)
  
Blocked cells từ LevelData.Walls → không thể đặt
```

### Footprint Calculation:

```
1x1 item @ (2, 2):
  Cells: (2, 2)

2x2 item @ (1, 1) rotation 0:
  Cells: (1,1), (2,1), (1,2), (2,2)

1x2 item @ (0, 0) rotation 1 (90°):
  Size rotated → 2x1
  Cells: (0,0), (1,0)
```

---

## 6. PREVIEW vs PLACED OBJECT

### Preview (During Placement):

```csharp
activePreview = new PreviewState {
  item,
  view,              // preview transform
  origin,            // grid position
  rotationQuarterTurns,
  isValid,           // green/red
  isNewPurchase,
  editingPlacementId // if moving existing
}
```

- Color: validPreviewColor (green) or invalidPreviewColor (red)
- Y offset: previewYOffset (slightly above)
- Destroyed after commit

### Placed Object (After Confirmation):

```csharp
runtimePlacements[placementId] = new RuntimePlacement {
  data,              // PlacedFurnitureData
  item,              // FurnitureItemData
  view               // permanent transform
}
```

- Stored in runtimePlacements dict
- Saved to disk
- Can be selected + moved again

---

## 7. SAVE/LOAD SYSTEM

### Save Data Format:

```csharp
BuildSaveData {
  coins: int,
  placedFurniture: List<PlacedFurnitureData> {
    placementId: string,
    furnitureId: string,
    x, y: int,
    rotationQuarterTurns: int
  }
}
```

### Save Location:

`PlayerPrefs[saveKey]` (key: "dreamhome_build_mvp")

### Auto-save:

- On commit placement
- On delete placement
- Can disable via `autoSave` toggle

---

## 8. DELETE MODE

### Workflow:

```
User Click Delete Button
       ↓
BuildModeController.DeleteSelected()
       ↓
EnableDeleteMode() = true
       ↓
User Tap Furniture
       ↓
BuildModeController.HandleTap()
  ├─ Check: isDeleteMode?
  ├─ TryGetPlacementAtCell()
  └─ RemovePlacement(placementId, refund=true)
       ↓
Furniture disappears
Coins refunded (50% by default)
```

### Parameters:

- `deleteRefundRate: 0.5` → 50% refund of original price
- Modify in Inspector to change

---

## 9. CAMERA PAN

### Settings:

```csharp
enableCameraPan: bool = true      // Enable/disable
tapMaxMovementPixels: float = 14  // Threshold for drag
tapMaxDurationSeconds: float = 0.25  // Time threshold
```

### Logic:

```
User Press & Hold
  ↓
If movement > tapMaxMovementPixels:
  └─ It's a DRAG (pan camera)
     └─ PanCamera(fromPos, toPos)
     
Else if duration < tapMaxDurationSeconds:
  └─ It's a TAP (place item)
```

---

## 10. GRID VISUALIZATION

### Visual Grid:

```csharp
showGridOverlay: bool = true
gridLightColor: Color (default white)
gridDarkColor: Color (default gray)
gridVisualYOffset: float = 0.01f
```

- Checkerboard pattern
- Can toggle on/off
- Can change colors
- Optional gridMaterial

---

## 11. COMMON CUSTOMIZATIONS

### Change Rotation Behavior:

File: `BuildModeController.cs` line 259

```csharp
// Current: 4 rotations (0, 90, 180, 270)
activePreview.rotationQuarterTurns = (activePreview.rotationQuarterTurns + 1) % 4;

// To 8 rotations (every 45°):
activePreview.rotationQuarterTurns = (activePreview.rotationQuarterTurns + 1) % 8;
// Then update RotateSize() logic for 8 states
```

### Change Preview Colors:

File: `BuildModeController.cs` line 28-29

```csharp
validPreviewColor = new Color(0.2f, 1f, 0.3f, 0.7f);      // Green
invalidPreviewColor = new Color(1f, 0.2f, 0.2f, 0.7f);    // Red
```

### Change Refund Rate:

File: `BuildModeController.cs` line 32

```csharp
deleteRefundRate = 0.5f;  // 50% of price
// Change to 0.75f for 75% refund, 1f for 100%, etc.
```

### Enable/Disable Auto-save:

File: `BuildModeController.cs` line 47

```csharp
autoSave = true;  // Toggle in Inspector
```

---

## 12. DEBUGGING & TESTING

### Use ShopSetupHelper.cs:

```
Right-click component in Inspector
→ ContextMenu options:
  - Auto Setup Shop UI
  - Test SelectFurniture (First Item)
  - Test SelectFurniture (Wall)
  - Log All Furniture
  - Check Shop UI Setup
```

### Add Logs:

```csharp
// BuildModeController.cs line 235
Debug.Log($"[SHOP] Selected: {item.ItemId}");

// BuildModeController.cs line 514
Debug.Log($"[VALIDATE] Valid: {validation.isValid}");

// BuildModeController.cs line 456
Debug.Log($"[PLACE] Created: {placementId}");
```

---

## 13. ARCHITECTURE DECISIONS

### Why separate GridState?

- Grid logic reusable (for future features)
- Easy to test validation
- Decoupled from view

### Why separate ShopUI?

- Dynamic population
- Easy to add categories/filters
- Reusable item prefab

### Why quaternion rotations?

- Standard Unity rotation
- Supports complex rotations
- Easy to extend to 8-way

### Why preview system?

- Visual feedback before commit
- Can cancel at any time
- Prevents accidental placements

---

## 14. NEXT FEATURES TO ADD

1. **Categories**
   - Filter shop by type
   - Add category field to FurnitureItemData

2. **Unlocking System**
   - Track progression
   - Lock items until level X

3. **Bundle Bonuses**
   - Detect complete sets
   - Give bonus coins

4. **Undo/Redo**
   - Stack-based history
   - Undo placement

5. **Grid Snapping Options**
   - Pixel-perfect vs grid
   - Custom snap size

6. **Asset Variants**
   - Different colors/models
   - Purchasable variants

---

**READY TO CUSTOMIZE? Check implementations above! 🚀**

