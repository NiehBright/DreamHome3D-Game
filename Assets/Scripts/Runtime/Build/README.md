# Building System MVP (Setup nhanh)

## 1) Tao du lieu noi that
1. Right click trong Project -> `Create/DreamHome/Build/Furniture Item`.
2. Dien `Item Id`, `Size`, `Price`, `Prefab`, `Can Rotate`.
3. Tao catalog: `Create/DreamHome/Build/Furniture Catalog` va keo cac item vao list.

## 2) Gan controller vao scene
1. Tao 1 GameObject moi: `BuildMode`.
2. Add component `BuildModeController`.
3. Gan: `Build Camera`, `Catalog`, `Wallet`, `Build Root`.
4. Chon kich thuoc luoi (`Grid Width`, `Grid Height`, `Cell Size`).
5. Neu muon chan tuong theo level puzzle, gan `LevelData` vao `Blocked Source Level` va bat `Use Wall Cells As Blocked`.

## 3) Chuyen mode Puzzle <-> Build
1. Tao GameObject `ModeController`.
2. Add component `ModeController`.
3. Gan `Puzzle Root`, `Build Root`, `Puzzle Camera`, `Build Camera`, `Build Mode Controller`.
4. UI nut `Build` -> goi `ModeController.EnterBuildMode()`.
5. UI nut `Exit` -> goi `ModeController.EnterPuzzleMode()`.

## 3.1) Setup 2 camera
- `Puzzle Camera`: camera danh cho gameplay puzzle.
- `Build Camera`: camera danh cho build mode (goc tu tren xuong).
- Goi y top-down:
  - Position: `(centerX, 12, centerY)`
  - Rotation: `(90, 0, 0)`
  - Projection: `Orthographic` (de canh grid de nhin)
  - Orthographic Size: tang/giam theo kich thuoc phong.
- Khi doi mode, `ModeController` se tu bat/tat dung camera.

## 4) Hook UI Build
- Shop item button -> goi `BuildModeController.SelectFurnitureById(string id)`.
- Nut `Rotate` -> goi `BuildModeController.RotatePreview()`.
- Nut `Cancel` -> goi `BuildModeController.CancelPlacement()`.
- Nut `Delete` -> goi `BuildModeController.DeleteSelected()` (nut nay bat/tat Delete Mode).

## 5) Input
- Mobile: an giu + truot de pan camera theo huong tay.
- Editor: giu chuot trai + keo de pan camera.
- Dat vat chi bang thao tac tap/nhan nhanh (khong dat khi dang drag).
- Khi Delete Mode dang bat: cham vao vat nao thi vat do bi xoa ngay.

## 5.1) Hien luoi caro 1x1
- Trong `BuildModeController`, bat `Show Grid Overlay`.
- Chinh mau o qua `Grid Light Color` va `Grid Dark Color`.
- `Grid Visual Y Offset` dung de nang luoi len 1 chut tranh z-fighting.
- Neu can shader rieng, gan vao `Grid Material`.

## 6) Save/Load
- Tu dong save vao `PlayerPrefs` theo `Save Key` trong `BuildModeController`.
- Khi vao Build mode, he thong load va spawn lai noi that da dat.

