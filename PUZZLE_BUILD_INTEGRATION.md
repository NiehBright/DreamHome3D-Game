# PUZZLE MODE + BUILD INTEGRATION

## 1. FLOW: BUILD ↔ PUZZLE ↔ MAIN

```
MAIN SCREEN
├─ Hiển thị: BuildPlacedRoot (nội thất đã đặt)
├─ Button: [Build] → Enter Build Mode
└─ Button: [Play] → Enter Puzzle Mode

    ↓

BUILD MODE
├─ Hiển thị: BuildModeRoot + BuildPlacedRoot
├─ Grid overlay (xanh/đỏ ô)
├─ Shop (items động từ catalog)
├─ Drag để đặt nội thất
└─ Button: [Exit] → Return to Main

    ↓

PUZZLE MODE (Chơi game)
├─ Hiển thị: PuzzleRoot (puzzle game)
├─ BuildPlacedRoot: tắt/bật (background tùy ý)
├─ Input: Swipe để di chuyển
├─ Hoàn thành: Kiếm coins
└─ Button: [Exit] → Return to Main

    ↓

MAIN SCREEN (lặp lại)
├─ Coins cập nhật
├─ Nội thất vẫn lưu lại
└─ Có thể build thêm hoặc chơi lại
```

---

## 2. MODE CONTROLLER

**File**: `Assets/_Scripts/Runtime/Build/ModeController.cs`

### Game Modes:

```csharp
public enum GameMode
{
    Puzzle = 0,   // Chơi puzzle
    Build = 1,    // Xây dựng nhà
    Main = 2      // Màn hình chính
}
```

### World Objects:

```
mainRoot           → Main screen world
puzzleRoot         → Puzzle game objects
buildModeRoot      → Build mode editing tools
buildPlacedRoot    → Nội thất đã đặt (persistent)
```

### Logic:

| Mode | mainRoot | puzzleRoot | buildModeRoot | buildPlacedRoot |
|---|---|---|---|---|
| **Main** | ✓ | ✗ | ✗ | ✓ |
| **Build** | ✗ | ✗ | ✓ | ✓ |
| **Puzzle** | ✗ | ✓ | ✗ | ✗/✓* |

*Tùy thuộc vào `showPlacedRootInPuzzleMode`

---

## 3. PUZZLE GAME LOGIC

### Goal: Complete Level

**File**: `Assets/_Scripts/Runtime/Gameplay/GameController.cs`

**Mechanics:**
- Người chơi di chuyển (swipe input)
- Push box vào goal
- Level complete khi tất cả box đúng vị trí
- Nhận coins

### Key Methods:

```csharp
public void LoadLevel()           // Load từ LevelListData
private void HandleSwipe(vec)     // Di chuyển nhân vật
private void RefreshGoalViews()   // Goal ẩn/hiện
```

### Goal Behavior:

**Hiện tại:**
```csharp
// Line 374
pair.Value.gameObject.SetActive(!gridState.HasBox(pair.Key));
```

- Goal ẩn khi có box
- Player có thể đứng vào goal (không restrict)
- ✓ Đã hoàn thiện theo yêu cầu

---

## 4. CURRENCY SYSTEM

### CurrencyWallet

**File**: `Assets/_Scripts/Runtime/Build/CurrencyWallet.cs`

```csharp
public int Coins { get; }
public void Add(int amount)
public bool TrySpend(int amount)
public void SetCoins(int amount)
```

### Flow:

1. **Kiếm coins**
   - Hoàn thành puzzle → onLevelCompleted event
   - Thêm logic reward vào GameController

2. **Tiêu coins**
   - Buy furniture → Trừ tiền
   - BuildModeController.TryCommitPreview()

3. **Refund**
   - Delete furniture → 50% lại (hoặc custom)
   - BuildModeController.RemovePlacement()

### Setup:

```
Scene Hierarchy:
CurrencyWallet (GameObject)
├─ Add Component: CurrencyWallet
├─ Initial Coins: 1000 (hoặc tùy)
└─ Persist (không destroy)
```

---

## 5. SWIPE INPUT SYSTEM

### SwipeInputReader

**File**: `Assets/_Scripts/Runtime/Input/SwipeInputReader.cs`

```csharp
public event System.Action<Vector2Int> OnSwipe;

// Usage:
OnSwipe?.Invoke(direction);  // direction: (-1,0), (1,0), (0,-1), (0,1)
```

### Directions:

```
(-1, 0) ← LEFT
(1, 0)  → RIGHT
(0, -1) ↑ UP
(0, 1)  ↓ DOWN
```

### Setup:

```
Scene Hierarchy:
SwipeInputReader (GameObject)
├─ Add Component: SwipeInputReader
├─ Input System Package: Required
└─ Canvas: (drag Canvas để calculate)
```

**Note**: Phải dùng **New Input System** (không Legacy Input)

---

## 6. INTEGRATION CHECKLIST

### Setup Puzzle Mode:

- [ ] LevelListData có levels?
- [ ] GameController assign LevelListData?
- [ ] SwipeInputReader setup?
- [ ] Prefabs assign (floor, wall, goal, box, player)?
- [ ] Camera (puzzleCamera) assigned?

### Setup Build Mode:

- [ ] FurnitureCatalog có items?
- [ ] FurnitureShopUI component + references?
- [ ] BuildModeController assign references?
- [ ] GridOrigin, GridWidth, GridHeight setup?
- [ ] Camera (buildCamera) assigned?

### Setup Main Screen:

- [ ] ModeController assign all roots?
- [ ] UI buttons assign (Build, Play)?
- [ ] ModeUiBindings assign controllers?

### Setup Cameras:

```
Scene Cameras:
├─ Main Camera (mainCamera)
│  └─ For Main Screen
├─ Puzzle Camera (puzzleCamera)
│  └─ For Puzzle Mode
└─ Build Camera (buildCamera)
   └─ For Build Mode
```

---

## 7. COIN REWARD SYSTEM

### Currently:

Không có reward logic trong GameController. Cần thêm:

```csharp
[Header("Rewards")]
[SerializeField] private int levelCompleteReward = 100;

// Trong WinChecker:
if (WinChecker.IsLevelComplete(gridState))
{
    completed = true;
    
    // Kiếm coins
    if (wallet != null)
    {
        wallet.Add(levelCompleteReward);
    }
    
    onLevelCompleted?.Invoke();
}
```

### Implementation:

**File**: `Assets/_Scripts/Runtime/Gameplay/GameController.cs`

Thêm vào line 166:

```csharp
[Header("Rewards")]
[SerializeField] private CurrencyWallet wallet;
[SerializeField] private int levelCompleteReward = 100;

// Trong HandleSwipe() line 166-174
if (WinChecker.IsLevelComplete(gridState))
{
    completed = true;
    
    if (wallet != null)
    {
        wallet.Add(levelCompleteReward);
        Debug.Log($"Level complete! +{levelCompleteReward} coins");
    }
    
    onLevelCompleted?.Invoke();
    SetNextLevelButtonVisible(HasNextLevel());
    SetBackButtonVisible(false);
    SetResetButtonVisible(false);
}
```

---

## 8. UI INTEGRATION

### ModeUiBindings

**File**: `Assets/_Scripts/Runtime/Build/ModeUiBindings.cs`

**Buttons:**

```
Main Screen:
├─ buildButton → EnterBuildMode()
└─ puzzleButton → EnterPuzzleMode()

Build Screen:
├─ buildExitButton → EnterMainMode()
├─ rotateButton → RotatePreview()
├─ cancelButton → CancelPlacement()
└─ deleteButton → DeleteSelected()

Puzzle Screen:
├─ puzzleExitButton → EnterMainMode()
└─ [Others assigned by other controllers]
```

### Coin Display:

**File**: `Assets/_Scripts/Runtime/Build/CoinTextBinder.cs`

```csharp
// Auto-bind coin display to wallet
// Setup in UI:
Canvas
└─ CoinText
    └─ Add Component: CoinTextBinder
       └─ Wallet: (assign CurrencyWallet)
```

---

## 9. SAVE/LOAD ACROSS MODES

### Persistent Data:

```
CurrencyWallet
├─ Coins: PlayerPrefs["dreamhome_coins"]
└─ Persist across modes

BuildSaveService
├─ Placements: PlayerPrefs["dreamhome_build_mvp"]
└─ Auto-load on scene start

Level Progress
├─ Current level: PlayerPrefs["dreamhome_level"]
└─ (Optional: implement if needed)
```

### Implementation:

```csharp
// In ModeController.Start()
ApplyMode(CurrentMode);

// In ModeController.ApplyMode()
// BuildModeController.SetBuildActive() triggers LoadFromSave()
// Which reloads all placed furniture
```

---

## 10. COMMON ISSUES & FIXES

### Issue: Puzzle mode shows build objects

**Fix**: Set `showPlacedRootInPuzzleMode = false` in ModeController

### Issue: Coins not saving

**Fix**: Ensure CurrencyWallet added to scene, not destroyed

### Issue: Build objects disappear after puzzle

**Fix**: Ensure buildPlacedRoot active in Main mode

### Issue: Can't swipe in puzzle

**Fix**: Check SwipeInputReader enabled, Canvas assigned

### Issue: Camera stuck after switching mode

**Fix**: Verify ModeController.ApplyMode() disables other cameras

---

## 11. GAME LOOP SUMMARY

```
START
  ↓
Load from Save
  ├─ Coins: wallet.SetCoins()
  └─ Placements: BuildModeController.LoadFromSave()
  ↓
Show Main Screen
  ├─ Display: BuildPlacedRoot (your home)
  ├─ Display: Coins
  └─ Buttons: Build, Play
  ↓
[User chooses]
  ├─ BUILD: Xây dựng nhà (spend coins, place furniture)
  └─ PLAY: Chơi puzzle (earn coins, push boxes)
  ↓
Save to Disk
  ├─ wallet.Coins
  └─ buildplacements
  ↓
Back to Main (loop)
```

---

## 12. NEXT FEATURES

1. **Level Progression**
   - Track current level
   - Unlock based on progression

2. **Difficulty Scaling**
   - More levels = more complex
   - Higher reward

3. **Daily Challenges**
   - Special levels each day
   - Bonus rewards

4. **Statistics**
   - Total coins earned
   - Levels completed
   - Time played

5. **Leaderboards**
   - Local high scores
   - Cloud sync (later)

---

**PUZZLE + BUILD INTEGRATION COMPLETE! 🎮🏠**

