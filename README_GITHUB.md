# DreamHome3D-MB 🏠

A mobile puzzle game built with **Unity** where players push boxes to designated goal positions.

## 🎮 Game Overview

**DreamHome** is a step-based grid puzzle game featuring:
- 📦 Push boxes onto goal positions
- 🎯 Complete levels by placing all boxes correctly
- 📱 Mobile-friendly swipe controls
- 🗺️ Custom level editor for creating your own puzzles

## 🎮 Gameplay Mechanics

### Core Features
- **Grid-Based Movement**: Navigate a tile-based grid world
- **Box Pushing**: Push boxes by moving into them
- **Goal Positions**: Place boxes on matching goal tiles to complete levels
- **Step-Based**: Actions happen immediately on each move

### Controls
**Mobile (Swipe)**
- 👆 Swipe Up → Move Up
- 👇 Swipe Down → Move Down
- 👈 Swipe Left → Move Left
- 👉 Swipe Right → Move Right

### Win Condition
✅ Place all boxes on their corresponding goal positions to complete the level

## 🏗️ Project Structure

```
Assets/
├── Code/
│   ├── Runtime/
│   │   ├── Input/          # Input handling (SwipeInputReader)
│   │   ├── Game/           # Core game logic (GameController, LevelManager)
│   │   ├── Grid/           # Grid system
│   │   └── UI/             # User interface
│   └── Editor/             # Editor tools & scripts
├── Levels/                 # Level data (ScriptableObjects)
├── Prefab/                 # Prefabs for game objects
├── Scenes/                 # Game scenes
└── Settings/               # Project settings
```

## 🛠️ Tech Stack

- **Engine**: Unity 2022.3+ (or latest)
- **Language**: C#
- **Input System**: Unity Input System
- **Platform**: Mobile (iOS/Android)

## 📋 Known Issues

⚠️ **Input System Error** (Being Fixed)
- Current issue: Using legacy `UnityEngine.Input` instead of Input System package
- Solution: Update `SwipeInputReader.cs` to use the new Input System API

## 🚀 Getting Started

### Prerequisites
- Unity 2022.3+
- Git installed
- GitHub account

### Installation

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/DreamHome3D-MB.git
cd DreamHome3D-MB

# Open in Unity
# File → Open Project → Select the folder
```

### Build & Run
1. Open the project in Unity
2. Go to `Scenes` folder and open `GameScene`
3. Press **Play** to test the game

## 📝 Level System

Levels are stored as **ScriptableObjects** containing:
- Grid dimensions (Width × Height)
- Tile data (empty/wall)
- Box positions
- Goal positions
- Player spawn position

### Creating Levels
1. Create a new ScriptableObject of type `LevelData`
2. Use the **Level Editor** to design your level
3. Place tiles, boxes, goals, and player spawn point
4. Save and load in game

## 🎨 Development

### Current Features
- ✅ Grid-based game world
- ✅ Player movement
- ✅ Box pushing mechanics
- ✅ Level loading system
- ✅ Mobile input

### In Progress
- 🔄 Input System migration (fixing swipe detection)
- 🔄 Level editor UI improvements

### TODO
- [ ] UI/UX Polish
- [ ] Audio/SFX
- [ ] Particle effects
- [ ] Multiple levels
- [ ] Level progression
- [ ] Undo/Redo system
- [ ] Animations

## 🐛 Bug Reports

Found a bug? Please [create an issue](https://github.com/YOUR_USERNAME/DreamHome3D-MB/issues) with:
- Bug description
- Steps to reproduce
- Expected vs actual behavior
- Screenshots/videos if possible

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/YourFeature`)
3. Commit changes (`git commit -m 'Add YourFeature'`)
4. Push to branch (`git push origin feature/YourFeature`)
5. Open a Pull Request

## 📄 License

This project is open source. See [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

Created with ❤️ for puzzle game enthusiasts.

---

**Happy Puzzle Solving!** 🎮✨

