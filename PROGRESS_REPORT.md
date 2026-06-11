# BinaryPathway — Project Progress Report

**Date**: 2026-06-11  
**Author**: SHI Yunze (25126879G)  
**Supervisor**: Dr. PANG Wai Man Raymond  
**Course**: COMP5925 Metaverse Project I  

---

## 1. Executive Summary

BinaryPathway is a mixed reality puzzle game for Meta Quest, where players construct connected binary pathways through a 3D grid. The project has progressed from a basic grid prototype to a fully functional game with state management, victory detection, tile label display, level configuration, logical tile connection system, distance grab interaction, and successful Android APK build capability.

**Key Metrics:**
- Git commits: 22
- Custom C# scripts: 20
- Binary assets under LFS: 508 files (1.5 GB)
- Scene objects: ~399 in main scene
- Build: Android APK successful (144 MB)

---

## 2. Completed Features

### 2.1 Core Game Systems (Phase 1)

| Feature | Status | Files |
|---------|--------|-------|
| Grid System (6×6×1, configurable) | ✅ Complete | `GridContainer.cs`, `GridNode.cs`, `GridIndex.cs` |
| BFS Connectivity Validation | ✅ Complete | `GridContainer.cs` — cached Queue/HashSet |
| Tile System (0/1 values) | ✅ Complete | `TileBase.cs`, `Tile0.cs`, `Tile1.cs`, `TileToggle.cs` |
| XR Socket 3-Stage Snap | ✅ Complete | `GridSocket.cs` — WeakSnap → SmoothPreview → HardSnap |
| Puzzle Generation (3 algorithms) | ✅ Complete | `PuzzleInitializer.cs` — Straight/Random/Maze |
| Auto Start/Goal Markers | ✅ Complete | `PuzzleInitializer.cs` — procedural colored spheres |
| Path Removal for Playability | ✅ Complete | 40% random path tile removal |
| GameManager State Machine | ✅ Complete | `GameManager.cs` — Idle→Playing→Won |
| Victory Detection | ✅ Complete | `GameManager.cs` + `ConnectivityVisualizer.cs` event system |
| Win Feedback | ✅ Complete | `WinFeedbackController.cs` — particles + TMP text |
| Move Counter | ✅ Complete | World-space TMP "Moves: 0" |
| Tile Value Labels | ✅ Complete | `TileLabel.cs` — 0(red)/1(green), adaptive scaling, billboard |

### 2.2 Interaction Systems (Phase 2A-2B)

| Feature | Status | Files |
|---------|--------|-------|
| Tile 6-Face Socket System | ✅ Prefab design | `Tile_0.prefab`, `Tile_1.prefab` — 49 sub-objects each |
| Physical Joint Locking | ✅ Complete | `BreakableLatchOnSocket.cs`, `BreakableLinkNode.cs` |
| Tile Logical Connection | ✅ Complete | `TileConnector.cs` — validates against ConnectionRule |
| Connection Rule Table | ✅ Complete | `ConnectionRule.cs` — ScriptableObject, 5 logic operations |
| Distance Grab (SnapInteractor) | ✅ Complete | Prefab — Oculus ISDK SnapInteractor |
| Distance Grab (SnapInteractable) | ✅ Complete | Prefab — ray target for hand pointing |
| Distance Grab (GrabInteractor) | ✅ Complete | Prefab — Oculus ISDK GrabInteractor |
| Preview Line (TriggerBroadcaster) | ✅ Complete | Prefab — InteractableTriggerBroadcaster |
| Reticle + Auto Move | ✅ Complete | Prefab — ReticleDataIcon + AutoMoveTowardsTargetProvider |
| Tile Prefab Cleanup | ✅ Complete | Tile_0 fixed: TileOne→TileZero component |

### 2.3 Level & Configuration

| Feature | Status | Files |
|---------|--------|-------|
| LevelData ScriptableObject | ✅ Complete | `LevelData.cs` — grid size, algorithm, difficulty, limits |
| Level_01_Easy (4×4, 30%) | ✅ Complete | `Assets/Levels/Level_01_Easy.asset` |
| Level_02_Medium (6×6, 40%, 50 moves) | ✅ Complete | `Assets/Levels/Level_02_Medium.asset` |
| Level_03_Hard (8×6, 50%, 120s) | ✅ Complete | `Assets/Levels/Level_03_Hard.asset` |
| DefaultConnectionRule | ✅ Complete | `Assets/Levels/DefaultConnectionRule.asset` |

### 2.4 Performance & Stability

| Fix | Impact | Commit |
|-----|--------|--------|
| Debug.Log spam removal | CPU frame time: 41ms→1.15ms (35×) | `e3c52e1` |
| BFS container caching | Zero per-frame GC allocation | `e3c52e1` |
| OnDrawGizmos crash fix | Eliminated domain-reload NRE | `2eb73db` |
| GetNode/GetWorldPos null guard | Editor stability | `2eb73db` |
| Kinematic Rigidbody velocity fix | Runtime error eliminated | `4a1c627` |
| EndManualInteraction guard | Runtime error eliminated | `4a1c627` |
| FindObjectOfType deprecation | Clean compilation | `26f4c09` |
| Shader.Find NRE fix | Marker creation fixed | `a9ecfa9` |

### 2.5 Build & Deployment

| Item | Status |
|------|--------|
| Android IL2CPP Build | ✅ Successful (144 MB APK) |
| Build Scene | ✅ `ComprehensiveRigExample.unity` |
| Editor-only script isolation | ✅ `#if UNITY_EDITOR` guards |
| Quest Link stereo fix | ✅ `QuestStartupCleanup.cs` — disables screen-space canvases |
| Git LFS | ✅ 508 binary files tracked |

### 2.6 AI-Assisted Development

| Tool | Purpose |
|------|---------|
| Unity-MCP (CoplayDev) | Claude Code ↔ Unity Editor bridge |
| HTTP transport on :8080 | Scene manipulation, script editing, build control |
| 22 automated commits | Full git history with detailed messages |
| CLAUDE.md | AI-oriented project documentation |

---

## 3. Current Architecture

### Scene Hierarchy (ComprehensiveRigExample.unity)

```
Scene Root
├── GameManager (144842)
│   ├── GameManager component — State: Idle→Playing→Won
│   ├── WinFeedbackController — Particles + TMP text
│   ├── QuestStartupCleanup — Disables problematic canvases on Quest
│   ├── WinParticles — ParticleSystem for victory effects
│   ├── WinText — "Puzzle Solved!" (initially hidden)
│   └── MovesText — "Moves: 0"
├── Debug/
│   └── ConnectTest (ConnectivityVisualizer)
├── GridContainer (144448)
│   ├── GridContainer — 6×6×1, cell=0.3, diagonals=true
│   ├── XRInteractionManager
│   ├── PuzzleInitializer — maze(2), auto markers, path removal
│   └── SocketsRoot — GridSocket ×36 (runtime)
├── SnapZone-InteractableToHand — Distance grab target
├── Stone-InteractableToHand — Reference for tile distance grab
├── Stone-HandToInteractable — Distance grab reference
├── Dialog (from Meta XR template, disabled on Quest)
└── [XR Rig — CenterEyeAnchor, controllers, hand tracking]
```

### Tile Prefab Component Stack (16 components each)

```
Tile_0 / Tile_1
├── Transform, Rigidbody
├── XRGrabInteractable (XRI grab)
├── TileZero / TileOne (value logic)
├── Grabbable (Oculus ISDK grab)
├── GrabInteractor (distance grab execute)
├── SnapInteractor (distance grab control)
├── SnapInteractable (ray target)
├── InteractableTriggerBroadcaster (preview line)
├── ReticleDataIcon (reticle display)
├── AutoMoveTowardsTargetProvider (smooth movement)
├── BreakableLinkNode (joint management)
├── TileLabel (value label)
├── TileConnector (logical connection)
├── RespawnOnDrop, PointableUnityEventWrapper
└── Sockets/ (6 face sockets per tile)
    ├── Top/Bottom/Left/Right/Front/Back
    └── Each: XRSocketInteractor + BoxCollider + BreakableLatchOnSocket
```

---

## 4. Gaps Analysis (vs. Interim Report Objectives)

### Objective 1 — Gamification Aligned with Learning Mechanics
| Requirement | Status |
|-------------|--------|
| Place-test-revise loop | ✅ BFS connectivity + real-time feedback |
| Tile budgets | ❌ Not yet implemented |
| Penalties for illegal branches | ❌ Not yet implemented |
| Micro-achievements | ❌ Not yet implemented |
| Multimodal feedback | ⚠️ Visual only (particles + labels); audio/haptics TBD |

### Objective 2 — Embodied Interaction with Multimodal Feedback
| Requirement | Status |
|-------------|--------|
| Light-orb traversal visualization | ❌ Not yet implemented |
| Path highlighting | ❌ Not yet implemented |
| Haptic feedback | ❌ Not yet implemented |
| Sound effects | ⚠️ Prefab has AudioSources; not wired |
| World-space socket snapping | ✅ Complete |
| BFS rule-checking | ✅ Complete |
| Hand tracking gestures | ⚠️ Prefab has HandGrab poses; not tested |

### Objective 3 — Modular Quest-Ready Framework
| Requirement | Status |
|-------------|--------|
| Configurable grid generation | ✅ Complete |
| Socket-based snapping | ✅ Complete |
| BFS connectivity checks | ✅ Complete |
| Separated modules | ✅ ScriptableObject levels + connection rules |
| URP + SRP Batcher | ✅ Configured |
| 72/80/90 Hz targets | ⚠️ Not yet profiled on device |
| OpenXR alignment | ✅ Complete |
| Documented APIs | ✅ CLAUDE.md + inline comments |

### Tile Logic System (per Interim Report §5.2)
| Requirement | Status |
|-------------|--------|
| Tile with value + logic | ⚠️ ToggleTile exists; logic operations wired |
| 6-face socket connection | ✅ Prefab complete |
| Tile combination (connect) | ✅ Physical: BreakableLatchOnSocket; Logical: TileConnector |
| Tile combination (disconnect) | ⚠️ Pull-apart: not implemented |
| Value tile + logic tile fusion | ✅ ConnectionRule.LogicOperation (SetValue/Toggle/And/Or/Xor) |
| Puzzle generation with logic ops | ❌ Currently only random removal; NOT/XOR/AND transforms not yet |
| QR code level loading | ❌ Future work |
| MR passthrough mode | ❌ Future work |

---

## 5. Known Issues

### In-Scope (planned fixes)

| # | Issue | Priority |
|---|-------|----------|
| 1 | Puzzle generation uses simple removal, not logic transforms | P1 |
| 2 | Tile combination disconnect (pull-apart) not implemented | P1 |
| 3 | No audio feedback for grab/place/win | P2 |
| 4 | No haptic feedback | P2 |
| 5 | Step limit + auto-fail not wired to UI | P2 |
| 6 | MovesText not visually prominent | P3 |
| 7 | Distance grab `_timeOutInteractable` needs manual wiring in prefab | P3 |

### Out-of-Scope (Meta SDK / Simulator)

| # | Issue | Impact |
|---|-------|--------|
| 8 | OpenXR ProcessOpenXRMessageLoop NRE (every frame in Simulator) | Editor only — Quest not affected |
| 9 | 27 duplicate XR subsystem registrations | Simulator only |
| 10 | OVR undestroyed swapchains on Play Mode exit | Editor crash risk, Quest not affected |
| 11 | Quest Link compositor swapchain corruption | Restart Oculus app to fix |

---

## 6. Build & Test Status

| Platform | Status | Notes |
|----------|--------|-------|
| **Editor Play Mode** | ✅ Functional | XR Simulator interaction works (with Simulator limitations) |
| **Android APK Build** | ✅ Successful | 144 MB, IL2CPP Release |
| **Quest 3 Deployment** | ⚠️ Built but visual issues | Stereo fix applied; needs re-build and test |
| **Automated Tests** | ❌ None | EditMode/PlayMode tests planned |
| **Performance Profiling** | ⚠️ Editor only | Editor: ~2ms stable; Quest: not yet profiled |

---

## 7. Next Development Priorities

### Immediate (Week 11-12)
1. Rebuild APK with QuestStartupCleanup + correct scene → test on Quest 3
2. Wire distance grab `_timeOutInteractable` in prefab (manual Editor step)
3. Implement puzzle generation with logic operations (NOT/XOR/AND per report)
4. Add path highlight visualization on win

### Short-term (Week 13-14)
5. Audio feedback system (grab, place, connect, win)
6. Haptic feedback integration
7. Step limit enforcement + auto-fail with UI
8. Multi-level flow (Level 1→2→3 progression)

### Medium-term (Week 15-16)
9. Tile pull-apart disconnect mechanism
10. Quest performance optimization (profiling, FFR, dynamic resolution)
11. EditMode unit tests (BFS, path generation, connection rules)

### Future
12. QR code level loading
13. MR passthrough mode
14. Multiplayer (per report schedule)

---

## 8. Repository

- **GitHub**: https://github.com/STCloudLake/BinaryPathway
- **Branch**: `master` (22 commits)
- **LFS**: 508 binary files
- **Documentation**: `CLAUDE.md` (architecture), `README.md` (intro), `Leveraging VR Technologies...pdf` (academic report)
