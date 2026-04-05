# QUICK SETUP CHECKLIST - DYNAMIC SHOP + 3 WALLS

## ✅ ĐÃ LÀMTỐI - CODE:

- [x] **FurnitureShopUI.cs** - Auto-populate shop items từ catalog
- [x] **FurnitureShopItem.cs** - Individual item button trong shop
- [x] **3 Wall Assets** - wall_front, wall_left, wall_right
- [x] **FurnitureCatalog.asset** - Thêm 3 wall items
- [x] **ModeUiBindings.cs** - Thêm reference FurnitureShopUI
- [x] **ShopSetupHelper.cs** - Debug tool để test

---

## 🎮 CỦA ANH CẦN LÀM - UI SETUP:

### Phase 1: Tạo Shop UI Structure

- [ ] **ShopRoot** (Panel under UI_Build)
  - [ ] Add **FurnitureShopUI** component
  - [ ] Add Image component (background tùy ý)
  
- [ ] **ShopScroll** (ScrollRect con của ShopRoot)
  - [ ] Assign Content → ShopContent
  
- [ ] **ShopContent** (Panel con của ShopScroll/Viewport)
  - [ ] Add **VerticalLayoutGroup**
  - [ ] Config: Child Force Expand Width ✓, Height ✗

### Phase 2: Tạo Shop Item Template

- [ ] **ShopItemTemplate** (Button prefab)
  - [ ] Add **FurnitureShopItem** component
  - [ ] Con là TextMeshProUGUI **ItemName**
  - [ ] Con là TextMeshProUGUI **ItemPrice**
  - [ ] Con là Image **ItemIcon** (optional)
  - [ ] Save prefab vào Assets/_Prefab/

### Phase 3: Assign References

**FurnitureShopUI Inspector:**
- [ ] Catalog → FurnitureCatalog.asset (Assets/_ScriptableObjects/Build/)
- [ ] BuildModeController → (tìm trong scene)
- [ ] ShopContentParent → Panel ShopContent
- [ ] ShopItemPrefab → ShopItemTemplate prefab
- [ ] ShopScrollRect → ScrollRect ShopScroll

**FurnitureShopItem (trên Prefab):**
- [ ] SelectButton → Button component
- [ ] NameText → ItemName child
- [ ] PriceText → ItemPrice child
- [ ] ItemIcon → ItemIcon child

**ModeUiBindings Inspector:**
- [ ] Furniture Shop UI → FurnitureShopUI component

---

## 🧪 TEST SETUP:

1. **Enter Build Mode** → Shop panel nhìn thấy không?
2. **Shop đã có 5 items?** (2 chairs + 3 walls)
3. **Click item** → Selected không?
4. **Tap grid** → Item place được không?

---

## 📊 CATALOG CURRENT STATE:

```
FurnitureCatalog.asset
├─ chair_01 (1x1, $100)
├─ chair_02 (1x1, $100)
├─ wall_front (1x1, $50, no rotate)
├─ wall_left (1x1, $50, no rotate)
└─ wall_right (1x1, $50, no rotate)
```

---

## 🐛 NẾUỴLỖI:

### Shop không appear?
1. Check: UI_Build root bật/tắt?
2. Check: FurnitureShopUI component trên ShopRoot?
3. Log: `ShopSetupHelper → Check Shop UI Setup` (ContextMenu)

### Item không click?
1. Check: Button component trên prefab?
2. Check: FurnitureShopItem component?
3. Log: `SelectFurnitureById` có gọi không? (add Debug.Log)

### Item không hiển thị?
1. Check: catalog.Items.Count > 0?
2. Check: ShopContent có VerticalLayoutGroup?
3. Log: `FurnitureShopUI.PopulateShop()` có chạy không?

---

## 🎯 EXPECTED RESULT:

Khi vào Build Mode:
```
┌──────────────────────────┐
│   FURNITURE SHOP         │
├──────────────────────────┤
│  [Chair]              100  ← Scrollable
│  [Chair]              100  │
│  [Wall]                50  │
│  [Wall]                50  │
│  [Wall]                50  │
├──────────────────────────┤
│ [Rotate] [Cancel] [Del]  │
└──────────────────────────┘
```

Click item → preview appear → tap grid → place

---

## 🔗 FILES REFERENCES:

**Code Created:**
- Assets/_Scripts/Runtime/Build/FurnitureShopUI.cs
- Assets/_Scripts/Runtime/Build/FurnitureShopItem.cs
- Assets/_Scripts/Runtime/Build/ShopSetupHelper.cs

**Assets Created:**
- Assets/_ScriptableObjects/Build/wall/wall_front.asset
- Assets/_ScriptableObjects/Build/wall/wall_left.asset
- Assets/_ScriptableObjects/Build/wall/wall_right.asset
- Assets/_ScriptableObjects/Build/FurnitureCatalog.asset (updated)

**Docs:**
- SETUP_DYNAMIC_SHOP.md
- SETUP_SHOP_VISUAL.md
- THIS FILE

---

## 🚀 NEXT STEPS:

1. **Setup UI** theo checklist trên
2. **Test** bằng ContextMenu ShopSetupHelper
3. **Debug** nếu có issue
4. **Enjoy** dynamic shop! 🎉

---

**CẦN HỎI GÌ? Check docs hoặc log với ShopSetupHelper!**

