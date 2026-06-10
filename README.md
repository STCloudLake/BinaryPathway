# BinaryPathway

> **"Binary switches on a spatial circuit board"** — construct a conductive pathway through a 3D grid in your living room.

A mixed reality puzzle game for **Meta Quest** built with Unity 6 and Meta XR SDK v85.0. Drag binary tiles (0/1) onto a spatial grid to form a connected path from start to goal.

---

## Gameplay

Players see a floating 3D grid in their physical space. Each cell is a socket waiting for a tile. Tiles have a binary value:

| Tile | Value | Meaning |
|------|-------|---------|
| `Tile 0` | 0 | Blocked — current cannot pass |
| `Tile 1` | 1 | Conductive — current flows through |

**Goal**: Place `Tile 1` pieces to form an unbroken path connecting the start point 🟢 to the goal point 🔴. The system runs real-time BFS connectivity checks — a green indicator lights up when the circuit is complete.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Unity 6000.3.6f1 |
| Rendering | URP (Universal Render Pipeline) |
| XR | OpenXR + Meta OpenXR 2.4.0 |
| Interaction | XR Interaction Toolkit 3.3.1 |
| Hand Tracking | XR Hands 1.7.2 |
| MR | Meta XR SDK 85.0 + MR Utility Kit |
| Input | Unity Input System |
| AI Dev | [Unity-MCP](https://github.com/CoplayDev/unity-mcp) (Claude Code ↔ Unity bridge) |

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Grid/          # Grid generation, node data, BFS pathfinding
│   ├── Tiles/         # TileBase, Tile0/1, TileToggle (player-switchable)
│   ├── Interaction/   # XR Socket snap system, connectivity visualizer
│   └── Puzzle/        # Auto-generate puzzles (3 algorithms)
├── Prefabs/           # GridBase, Tile prefabs
├── Scenes/            # ComprehensiveRigExample (main), SampleScene
└── Resources/         # XR configs, input actions
```

See [CLAUDE.md](CLAUDE.md) for detailed architecture documentation and development roadmap.

---

## Getting Started

### Prerequisites
- Unity 6000.0+ with Android Build Support
- Meta Quest 3/3S or Quest Pro
- [Meta XR All-in-One SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657) v85

### Open in Unity
```bash
git clone https://github.com/STCloudLake/BinaryPathway.git
# Open the project folder in Unity Hub
```

### Build for Quest
```
File → Build Settings → Android → Switch Platform → Build
```

### AI-Assisted Development
This project integrates [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) for Claude Code-powered Unity automation:
```bash
# In Unity: Window → MCP for Unity → Start Server
# Claude Code connects automatically via .mcp.json
```

---

## Documentation

| Document | Audience |
|----------|----------|
| [CLAUDE.md](CLAUDE.md) | AI agents & developers — architecture, APIs, roadmap |
| [README.md](README.md) | Everyone — project intro, setup, gameplay |

---

## License

MIT
