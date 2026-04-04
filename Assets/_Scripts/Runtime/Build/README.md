# Building + Mode System MVP (Setup nhanh)

README nay cap nhat theo he thong moi:
- 1 scene
- 3 mode: `Main`, `Build`, `Puzzle`
- 1 `ModeController` quan ly bat/tat UI + object + camera

## 1) Tao du lieu noi that
1. Right click trong Project -> `Create/DreamHome/Build/Furniture Item`.
2. Dien `Item Id`, `Size`, `Price`, `Prefab`, `Can Rotate`.
3. Neu prefab bi xoay sai huong, chinh them `Visual Rotation Offset Degrees`.
4. Tao catalog: `Create/DreamHome/Build/Furniture Catalog` va keo cac item vao list.

## 2) Chuan bi scene theo 3 mode
Tao/cau truc root ro rang trong hierarchy:
- `MainRoot`
- `BuildModeRoot`
- `BuildPlacedRoot`
- `PuzzleRoot`
- `UI_Main`
- `UI_Build`
- `UI_Puzzle`

Goi y:
- `BuildPlacedRoot` la noi chua tat ca noi that da dat.
- `BuildModeRoot` la tools de build/edit.
- `Main` co the dung chung floor/grid 1x1 voi `Build`.

## 3) Setup BuildModeController
1. Tao GameObject (vi du `BuildModeSystem`) va add `BuildModeController`.
2. Gan reference:
   - `Build Camera`
   - `Build Root` (thuong la `BuildPlacedRoot`)
   - `Catalog`
   - `Wallet`
3. Chinh grid:
   - `Grid Width`, `Grid Height`, `Cell Size` (1 = 1x1)
   - `Grid Origin`
4. Neu muon chan o tuong theo level puzzle:
   - Gan `Blocked Source Level`
   - Bat `Use Wall Cells As Blocked`
5. `Delete` button hien tai se bat delete mode, click vao furniture nao thi furniture do bi xoa ngay.

## 4) Setup ModeController (quan trong nhat)
1. Tao GameObject `ModeSystem` va add `ModeController`.
2. Gan cac field:

### World Roots
- `mainRoot` -> `MainRoot`
- `puzzleRoot` -> `PuzzleRoot`
- `buildModeRoot` -> `BuildModeRoot`
- `buildPlacedRoot` -> `BuildPlacedRoot`

### UI Roots
- `uiMainRoot` -> `UI_Main`
- `uiBuildRoot` -> `UI_Build`
- `uiPuzzleRoot` -> `UI_Puzzle`

### Cameras
- `mainCamera` -> camera Main
- `puzzleCamera` -> camera Puzzle
- `buildCamera` -> camera Build (use the scene-authored pose; no forced top-down rotation)
- `showPlacedRootInPuzzleMode`:
  - Bat neu muon thay noi that khi choi puzzle
  - Tat neu muon an hoan toan

### Controller
- `buildModeController` -> object chua `BuildModeController`

Mac dinh mode khoi dong la `Main`.

## 5) Setup 2 camera (hoac 3 camera)
Code hien tai ho tro 3 camera rieng (`mainCamera`, `puzzleCamera`, `buildCamera`).
Neu anh muon toi gian thi co the de 2 camera (Puzzle + Build), `mainCamera` de trong va dung camera chung cua mode Main.

Goi y camera Build:
- Dat camera theo goc nhin anh muon
- Neu muon build de nhin grid ro hon, co the dung `Orthographic`
- Neu muon camera tu do, giu nguyen transform trong Scene

## 6) Mapping button UI bang ModeUiBindings
1. Add `ModeUiBindings` vao 1 object UI manager.
2. Gan `modeController` va `buildModeController`.
3. Gan button:

### Main UI
- `buildButton` -> nut Build
- `puzzleButton` -> nut Play Puzzle

### Build UI
- `buildExitButton` -> Exit (ve Main)
- `rotateButton` -> Rotate
- `cancelButton` -> Cancel
- `deleteButton` -> Delete (vao delete mode)

### Puzzle UI
- `puzzleExitButton` -> Exit (ve Main)

## 7) Mapping ham theo yeu cau
- Main:
  - Build -> `EnterBuildMode()`
  - Play Puzzle -> `EnterPuzzleMode()`
- Build:
  - Exit -> `EnterMainMode()`
  - Rotate -> `RotatePreview()`
  - Cancel -> `CancelPlacement()`
  - Delete -> `DeleteSelected()`
- Puzzle:
  - Exit -> `EnterMainMode()`

Neu da dung `ModeUiBindings` thi khong can tu gan tung onClick thu cong nua.

## 8) Coin UI
Add `CoinTextBinder` vao object text coin:
- `wallet` -> `CurrencyWallet`
- `coinText` -> TMP text coin
- `prefix` -> vi du `Coin: `

Text coin se tu dong update theo su kien `OnCoinsChanged`.

## 9) Input va thao tac Build
- Mobile:
  - Nhan giu + truot: pan camera
  - Tap nhanh: dat/chon/xoa vat
- Editor:
  - Giu chuot trai + keo: pan camera
  - Click nhanh: dat/chon/xoa vat

Luu y:
- Dang drag thi khong dat vat
- Delete mode bat -> tap vao vat nao, vat do bi xoa ngay

## 10) Hien luoi caro 1x1
Trong `BuildModeController`:
- Bat `Show Grid Overlay`
- Chinh `Grid Light Color` / `Grid Dark Color`
- Chinh `Grid Visual Y Offset` de tranh z-fighting
- Neu can material rieng, gan vao `Grid Material`
- `ModeController` hien tai se cho hien grid nay ca o `Main` va `Build`, de `Main` co san 1x1 nhu `Build`.

## 11) Fix xoay trong Build
Neu xoay vat ma anh thay chi ro huong truoc/sau:
1. Dam bao `buildCamera` dang o top-down hoac gan `forceTopDownBuildCamera = true`.
2. Neu mot do vat van bi lech, set `Visual Rotation Offset Degrees` trong `Furniture Item`.
   - Vi du: `90`, `180`, `270`
3. Phan xoay lưới va save/load khong doi, chi la chinh huong hien thi cua prefab.

## 12) Save/Load
- Tu dong save vao `PlayerPrefs` theo `Save Key`
- Khi vao scene/build mode, he thong load va spawn lai noi that da dat

## 13) Flow trai nghiem
1. Vao game -> `Main`
2. Bam Build -> `Build`
3. Trang tri, xoay, xoa
4. Bam Exit -> ve `Main`
5. Bam Play Puzzle -> `Puzzle`
6. Choi xong -> Exit -> ve `Main`
