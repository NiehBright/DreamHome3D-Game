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
3. Gan `Puzzle Root`, `Build Root`, `Build Mode Controller`.
4. UI nut `Build` -> goi `ModeController.EnterBuildMode()`.
5. UI nut `Exit` -> goi `ModeController.EnterPuzzleMode()`.

## 4) Hook UI Build
- Shop item button -> goi `BuildModeController.SelectFurnitureById(string id)`.
- Nut `Rotate` -> goi `BuildModeController.RotatePreview()`.
- Nut `Cancel` -> goi `BuildModeController.CancelPlacement()`.
- Nut `Delete` -> goi `BuildModeController.DeleteSelected()`.

## 5) Input
- Mobile: drag/touch de di chuyen preview, nha tay de dat.
- Editor: giu chuot trai va keo de dat.

## 6) Save/Load
- Tu dong save vao `PlayerPrefs` theo `Save Key` trong `BuildModeController`.
- Khi vao Build mode, he thong load va spawn lai noi that da dat.

