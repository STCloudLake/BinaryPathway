# BinaryPathway — Project Documentation

## Overview

**BinaryPathway** is a mixed reality (MR) puzzle game built with **Unity 6 (6000.3.6f1)** and **Meta XR SDK v85.0**, targeting **Meta Quest** devices via OpenXR with URP rendering.

### Core Gameplay

Players drag 0/1-valued tiles onto a configurable 3D grid in mixed reality space, forming a connected path from a start point to a goal point. The system performs real-time BFS connectivity checks — the puzzle is solved when value=1 tiles form an unbroken chain linking start to goal.

### Elevator Pitch

> "Binary switches on a spatial circuit board" — construct a conductive pathway through a 3D grid in your living room.

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Engine | Unity | 6000.3.6f1 |
| Rendering | URP (Universal Render Pipeline) | 17.3.0 |
| XR Platform | OpenXR + Meta OpenXR | 2.4.0 |
| XR SDK | Meta XR All-in-One SDK | 85.0.0 |
| XR Interaction | XR Interaction Toolkit | 3.3.1 |
| Hand Tracking | XR Hands | 1.7.2 |
| AR/Scene | AR Foundation | 6.3.2 |
| MR Utility | Meta MR Utility Kit | 85.0.0 |
| Input | Unity Input System | 1.18.0 |
| Voice AI | Meta Wit.ai / Voice SDK | (bundled) |
| Physics | Unity Physics (Rigidbody + Joints) | — |
| AI Integration | CoplayDev Unity-MCP (MCP For Unity) | main |
| Version Control | Git + Git LFS | — |

---

## Project Structure

```
BinaryPathway/
├── Assets/
│   ├── Scripts/                    # 🎯 Custom game logic
│   │   ├── Grid/
│   │   │   ├── GridContainer.cs    # Grid generation, BFS connectivity
│   │   │   ├── GridNode.cs         # Node data (index, position, occupancy)
│   │   │   └── GridIndex.cs        # 3D integer coordinate (x, y, z)
│   │   ├── Tiles/
│   │   │   ├── TileBase.cs         # Abstract base: Value, LockAfterPlace
│   │   │   ├── Tile0.cs            # Value = 0 ("empty" tile)
│   │   │   ├── Tile1.cs            # Value = 1 ("path" tile)
│   │   │   └── TileToggle.cs       # Player-togglable 0↔1 tile
│   │   ├── Interaction/
│   │   │   ├── GridSocket.cs       # XR Socket with 3-stage snap system
│   │   │   └── ConnectivityVisualizer.cs # Real-time connectivity indicator
│   │   ├── Puzzle/
│   │   │   └── PuzzleInitializer.cs # Auto-generates puzzles (3 algorithms)
│   │   ├── BreakableLatchOnSocket.cs # Physical joint locking on socket
│   │   ├── BreakableLinkNode.cs    # Multi-face joint manager with break detection
│   │   └── FinalLabelScaler.cs     # Billboard + distance-adaptive scaling for labels
│   ├── Prefabs/
│   │   ├── GridBase.prefab         # Grid socket prefab
│   │   ├── Tile_0.prefab           # Value=0 tile prefab
│   │   └── Tile_1.prefab           # Value=1 tile prefab
│   ├── Scenes/
│   │   ├── ComprehensiveRigExample.unity  # 🎮 Main scene
│   │   └── SampleScene.unity              # Debug/test scene
│   ├── Resources/                  # Runtime assets (XR configs, input actions)
│   └── StreamingAssets/            # Empty (reserved for runtime data)
├── Packages/
│   └── manifest.json               # Unity package dependencies
├── ProjectSettings/                # Unity project configuration
├── .gitignore                      # Unity-optimized gitignore
├── .gitattributes                  # Git LFS tracking rules
├── .mcp.json                       # Claude Code ↔ Unity MCP bridge config
└── CLAUDE.md                       # This file
```

---

## Architecture

### System Design

```
┌─────────────────────────────────────────────────────────┐
│                    PuzzleInitializer                     │
│  Generates puzzle state on Start():                      │
│  1. GeneratePath(start→goal) via selected algorithm     │
│  2. FillGrid(): instantiate pathTile/emptyTile prefabs  │
│  3. Place startMarker/goalMarker                         │
└─────────────────────┬───────────────────────────────────┘
                      │ calls Place() on each node
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    GridContainer                         │
│  - [ExecuteAlways] runs in editor & play mode           │
│  - Dynamic grid: W×H×L, configurable cell size          │
│  - Generates GridSocket children at each node           │
│  - BFS connectivity: CheckConnectivity(start, goal)     │
│  - Node state: occupied bool + placedTile reference     │
└─────────────────────┬───────────────────────────────────┘
                      │ each node → GridSocket
                      ▼
┌─────────────────────────────────────────────────────────┐
│                      GridSocket                          │
│  Wraps XRSocketInteractor with 3-stage snap:            │
│  1. WeakSnapOnce (35% lerp on hover enter)              │
│  2. Smooth preview (per-frame lerp in Update)           │
│  3. HardSnap (instant align + zero velocity on select)  │
│  Material states: default → hover → occupied            │
└─────────────────────────────────────────────────────────┘
                      │ tile placed → check
                      ▼
┌─────────────────────────────────────────────────────────┐
│               ConnectivityVisualizer                     │
│  Per-interval polling of CheckConnectivity()            │
│  Switches material: green (connected) / red (broken)    │
└─────────────────────────────────────────────────────────┘
```

### Data Flow

```
Tile (XR Grab) → Hover over GridSocket → Weak Snap Preview
  → Smooth Snap (per-frame lerp) → Select (drop)
  → GridSocket.HardSnap → GridContainer.Place(idx, tile)
  → Node.occupied = true, Node.placedTile = tile
  → ConnectivityVisualizer polls → BFS check
  → If connected: victory (TODO)
```

### Grid Data Model

```
GridContainer
  ├── _nodes[width, height, layers] : GridNode[,,]
  │   └── GridNode { index, worldPos, occupied, placedTile }
  ├── socketsRoot
  │   └── GridSocket × (W×H×L)  [generated at runtime/edit-time]
  └── Connectivity: BFS over IsOne(node) neighbors
      ├── 2D: 4-neighbor (optional 8-neighbor with diagonals)
      └── 3D: 6-neighbor (optional 26-neighbor with diagonals)
```

---

## Key Scripts Reference

### GridContainer.cs
- **Role**: Central grid authority
- **Key Methods**:
  - `Regenerate()` / `RegenerateSafeRuntime()` / `RegenerateSafeEditor()` — Build grid
  - `Place(idx, tile)` / `Remove(idx)` / `CanPlace(idx, tile)` — Node management
  - `CheckConnectivity(start, goal)` — BFS pathfinding
  - `GetNeighbors(idx)` — Returns valid adjacent indices
  - `OnDrawGizmos()` — Editor visualization (⚠ uses `_nodes.GetLength()` for safety)
- **Configuration**: width=6, height=6, layers=1, cellSize=0.3, allowDiagonals2D=true

### GridSocket.cs
- **Role**: XR interaction endpoint at each grid cell
- **3-Stage Snap System**:
  1. HoverEnter → WeakSnapOnce(35% lerp)
  2. Update() → Smooth preview snap (damped lerp)
  3. SelectEnter → HardSnap(instant) + Place tile
- **Material States**: defaultMat → hoverMat → occupiedMat

### PuzzleInitializer.cs
- **Role**: Auto-generate puzzle configurations
- **Path Algorithms**:
  - 0: Straight (Manhattan-style direct path)
  - 1: Random (random walk with 30% goal bias)
  - 2: Maze (DFS backtracking + Fisher-Yates shuffle)
- **Current Config**: Algorithm=2 (maze), start=(1,1,0)→goal=(5,5,0), autoInit=true

### TileBase.cs / Tile0.cs / Tile1.cs / TileToggle.cs
- **TileBase**: Abstract — defines `Value` (int), `LockAfterPlace` (bool), OnPlaced/OnRemoved callbacks
- **Tile0**: Value=0 (empty/non-conductive)
- **Tile1**: Value=1 (path/conductive)
- **TileToggle**: Player-togglable, LockAfterPlace=false, Toggle() swaps 0↔1 and updates material

### BreakableLatchOnSocket.cs
- Creates ConfigurableJoint/FixedJoint on inserted objects with breakForce/breakTorque
- Locks object physically to socket, releases on force threshold
- Uses deferred coroutine for SelectExit to avoid XRI state machine re-entrancy

### BreakableLinkNode.cs
- Manages multiple face-indexed Joints
- Polls for broken joints in Update() (replaces deprecated OnJointBreak callback)
- Cooldown system prevents immediate reconnection after break

### FinalLabelScaler.cs
- Billboard + distance-adaptive world-space text scaling
- Maintains constant screen-space size regardless of viewer distance

---

## Scene Configuration

### ComprehensiveRigExample.unity (Main Scene)

```
Hierarchy:
├── Debug/
│   └── ConnectTest (68412)
│       ├── Sphere Mesh + SphereCollider
│       └── ConnectivityVisualizer
│           └── startIndex=(0,0,0), goalIndex=(5,5,0)
├── GridContainer (68564)
│   ├── GridContainer — 6×6×1, cell=0.3, diagonals=true
│   ├── XRInteractionManager
│   ├── PuzzleInitializer — algorithm=maze(2), autoInit=true
│   │   ├── pathTilePrefab: Tile_1.prefab
│   │   └── emptyTilePrefab: Tile_0.prefab
│   └── SocketsRoot (68352) — populated at runtime/edit-time
└── [XR Rig — CenterEyeAnchor, controllers, hand tracking]
    └── MR Interaction Setup (Meta XR template)
```

### Known Scene Issues
- `startMarker` and `goalMarker` on PuzzleInitializer are **null** (no visual indicators)
- ConnectivityVisualizer is connected to GridContainer but starts with different indices (0,0,0→5,5,0) than PuzzleInitializer (1,1,0→5,5,0)

---

## MCP Integration (Claude Code ↔ Unity)

### Configuration
```json
// .mcp.json
{
  "mcpServers": {
    "UnityMCP": {
      "type": "http",
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}
```

### Bridge Process
- Runs as `mcp-for-unity.exe` (uvx-managed), auto-started by Unity MCP plugin
- HTTP transport on `localhost:8080`
- Health check: `GET /health` → 200 OK
- MCP endpoint: `POST /mcp` (SSE protocol)
- PID file: `Library/MCPForUnity/RunState/mcp_http_8080.pid`

### Available Capabilities
- Read/write scripts with compilation feedback
- Search/find GameObjects by name, tag, component, path
- Read/control Editor state (Play/Pause/Stop, scenes)
- Manage GameObjects, components, materials
- Screenshot capture (Game View, Scene View)
- Console log reading
- Batch operations (up to 25 parallel commands)

---

## Bug Fix History

| Date | Issue | Fix |
|------|-------|-----|
| 2026-06-10 | GridNode.cs: `using UnityEngine.Tilemaps` → TileBase type ambiguity | Removed unused import |
| 2026-06-10 | GridContainer.OnDrawGizmos: IndexOutOfRange risk | Use `_nodes.GetLength()` instead of field values |
| 2026-06-10 | GridContainer.CheckConnectivity: Debug.Log spam in builds | Added `debugConnectivityLogs` toggle |
| 2026-06-10 | GridContainer._needsRebuild: serialization edge case | Removed `[SerializeField]` |
| 2026-06-10 | PuzzleInitializer.ReinitializePuzzle: GameObject leak | Destroy old tiles before re-initializing |
| 2026-06-10 | PuzzleInitializer.FillGrid: silent Place failure | Check return value, log warning |
| 2026-06-10 | BreakableLinkNode: OnJointBreak never fires | Replaced with Update() polling |
| 2026-06-10 | BreakableLatchOnSocket: SelectExit re-entrancy | Deferred via coroutine (yield return null) |
| 2026-06-10 | TileToggle.cs: wrong file header comment | Fixed to "TileToggle.cs" |
| 2026-06-10 | ConnectivityVisualizer: pointless `using static GridContainer` | Removed |
| 2026-06-10 | Deprecated FindObjectOfType warnings | Replaced with FindFirstObjectByType |

---

## Development Notes

### Active Scene
- Primary: `Assets/Scenes/ComprehensiveRigExample.unity`
- Debug: `Assets/Scenes/SampleScene.unity`
- Recovery files in `Assets/_Recovery/` (81 files — Unity crash auto-saves)

### Build Target
- Platform: **Android** (Meta Quest)
- Rendering: URP, Stereo Rendering Path: Multi Pass
- Color Space: Linear
- XR Plugin: Meta OpenXR

### Git Workflow
- Branch: `master`
- LFS: 502 binary files tracked (textures, models, audio, fonts)
- `.csproj` and `.sln` files are **gitignored** (auto-generated by Unity)
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` are **gitignored**

---

## Development Roadmap

### Current Scorecard

| System | Completion | Notes |
|--------|-----------|-------|
| Grid System | ✅ 90% | Working in editor + runtime, bugs fixed |
| Tile System | ✅ 85% | Tile0/1 working, ToggleTile needs interaction hookup |
| XR Interaction (Socket Snap) | ✅ 85% | 3-stage snap working, bugs fixed |
| Puzzle Generation | ✅ 80% | 3 algorithms working, bugs fixed |
| Connectivity Detection | ✅ 85% | BFS working, visualization in place |
| **Game Loop (Win/Lose)** | ❌ 0% | **Nothing happens when connected** |
| **UI/HUD System** | ❌ 0% | No UI exists |
| **Audio Feedback** | ❌ 0% | No audio |
| **Multi-Level System** | ❌ 0% | Single hardcoded puzzle |
| **Tutorial/Onboarding** | ❌ 0% | None |
| **Quest Build & Test** | ❌ 0% | Never built for device |
| **Automated Testing** | ❌ 0% | No unit tests |

### Phase 1 — Minimum Playable Loop (Week 1)

**Goal**: A complete round of puzzle from start to victory.

| # | Task | Files | Effort |
|---|------|-------|--------|
| 1.1 | **Create GameManager** — State machine: Idle → Playing → Won | New: `Assets/Scripts/Core/GameManager.cs` | 2h |
| 1.2 | **Victory Detection** — Subscribe to connectivity changes, trigger win | GameManager.cs | 1h |
| 1.3 | **Win Feedback** — Particle burst + simple UI text "Connected!" | Prefab, GameManager | 2h |
| 1.4 | **Fix Scene Wiring** — Wire GameManager, fix start/goal indices, add markers | ComprehensiveRigExample.unity | 1h |
| 1.5 | **ToggleTile Interaction** — Add XRSimpleInteractable to call Toggle() | TileToggle.cs, Tile_1 1.prefab | 2h |
| 1.6 | **Play Mode End-to-End Test** — Editor: place tiles → toggle → connect → win | — | 1h |

### Phase 2 — Player Experience (Week 2)

**Goal**: The game feels like a game, not a tech demo.

| # | Task | Files | Effort |
|---|------|-------|--------|
| 2.1 | **Puzzle UI Panel** — Instructions, move counter, timer, hints | New Canvas + UXML | 3h |
| 2.2 | **Level Configuration** — ScriptableObject: grid size, start/goal, algorithm, seed | New: `LevelData.cs` (ScriptableObject) | 2h |
| 2.3 | **Level Progression** — 3 levels with increasing difficulty (4×4 → 6×6 → 8×6) | LevelData assets ×3 | 2h |
| 2.4 | **Audio System** — Place sound, connect sound, win sound, ambient | AudioManager.cs + audio clips | 2h |
| 2.5 | **Haptic Feedback** — Hover snap pulse, place thunk, win celebration | GridSocket.cs | 1h |

### Phase 3 — Polish & Ship (Week 3)

**Goal**: Ready for Quest Store / SideQuest.

| # | Task | Files | Effort |
|---|------|-------|--------|
| 3.1 | **Tutorial Level** — Guided first puzzle with voiceover/hints | TutorialLevel.unity | 3h |
| 3.2 | **Quest Performance Optimization** — Profiling, draw calls, URP settings | Multiple | 3h |
| 3.3 | **Quest APK Build** — Android build pipeline, Oculus signing | Build scripts | 2h |
| 3.4 | **Device Testing** — Quest 3/3S hardware validation | — | 3h |
| 3.5 | **Bug Fix Pass** — Issues found during device testing | Multiple | 2h |

### Phase 4 — Polish & Depth (Future)

| # | Task | Notes |
|---|------|-------|
| 4.1 | **3D Puzzles** — Multi-layer grids (layers > 1) | Grid already supports this |
| 4.2 | **Procedural Puzzle Generator** — Infinite puzzles with difficulty curve | Advanced path generation |
| 4.3 | **Multiplayer** — Co-op: two players place tiles together | Photon/Fusion integration |
| 4.4 | **Passthrough Enhancement** — Better MR integration with scene understanding | MRUK Scene API |
| 4.5 | **Leaderboard** — Fastest solve times, fewest moves | Meta Platform SDK |

### Immediate Priority (Next Action)

```
1. Create GameManager.cs — complete game loop
2. Wire victory detection to ConnectivityVisualizer
3. Add visual win feedback (particles + text)
4. Editor Play Mode test of complete flow
```

---

## Building & Testing

### Editor Testing
```bash
# Open project in Unity, enter Play Mode in ComprehensiveRigExample scene
# Use XR Device Simulator or XR Interaction Simulator for input
```

### Quest Build
```bash
# In Unity: File → Build Settings → Android → Switch Platform → Build
# Target: Meta Quest 3/3S, IL2CPP, ARM64
```

### Automated Testing (Planned)
```bash
# Run EditMode tests
Unity -batchmode -projectPath . -runTests -testPlatform EditMode
```

### MCP-Accelerated Development
With Unity MCP connected, Claude Code can:
- Read/write C# scripts with live compilation feedback
- Manipulate scene GameObjects and components
- Take screenshots for visual verification
- Run Play Mode tests and read Console output
- Batch-edit multiple assets in parallel

