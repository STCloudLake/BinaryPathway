# Cell Logic System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the complete cell logic system: CellState data model, LogicTile/LogicBlock components, TileConnector rewrite, SprayBottle NOT tool, and Grid/BFS integration — all within a single scene testable via Editor Play Mode.

**Architecture:** CellState is the core data structure shared by GridNode and LogicTile. LogicTile extends TileBase with per-cell logic state. TileConnector compatibility rules switch from TileType-based to CellState-based. SprayBottle uses XRSimpleInteractable with a particle-based spray effect. BFS reads CellState instead of TileBase.Value.

**Tech Stack:** Unity 6000.3.6f1, URP, Meta XR SDK v85, XR Interaction Toolkit 3.3.1

---

## Task 0: Verify Current Scene State

**Files:**
- Read: `Assets/Scenes/ComprehensiveRigExample.unity` (via MCP)
- Check: Console for compilation errors

- [ ] **Step 1: Read console to confirm clean state**

```
MCP: read_console
Expected: No compilation errors (OpenXR NRE in simulator is known and ignored)
```

- [ ] **Step 2: Note current state for rollback**

Confirm existing Tile_0, Tile_1, ToggleTile functionality works before making changes.

---

## Task 1: Create CellState.cs

**Files:**
- Create: `Assets/Scripts/Core/CellState.cs`

- [ ] **Step 1: Write CellState.cs**

```csharp
// Scripts/Core/CellState.cs

/// <summary>
/// Logical operations supported by logic blocks and cells.
/// </summary>
public enum LogicOp
{
    AND,    // 与
    NAND,   // 与非
    OR,     // 或
    NOR,    // 或非
    XOR,    // 异或
    XNOR    // 同或
}

/// <summary>
/// Classification of a cell's current state.
/// </summary>
public enum CellStateType
{
    PureValue,      // Only a value (0 or 1), no logic pending
    ValueWithLogic, // Has a value AND a pending logic operation, waiting for second value
    LogicOnly       // Only a logic operation, no value yet (waiting to capture)
}

/// <summary>
/// Immutable-ish struct representing the logical state of a single 1×1 cell.
/// Use the static factory methods to create instances.
/// </summary>
[System.Serializable]
public struct CellState
{
    public CellStateType type;
    public int value;       // 0 or 1; valid for PureValue and ValueWithLogic
    public LogicOp logic;   // valid for ValueWithLogic and LogicOnly

    // --- Factory methods ---

    public static CellState PureValue(int v) => new CellState
    {
        type = CellStateType.PureValue,
        value = Saturate(v),
        logic = LogicOp.AND // unused
    };

    public static CellState ValueWithLogic(int v, LogicOp L) => new CellState
    {
        type = CellStateType.ValueWithLogic,
        value = Saturate(v),
        logic = L
    };

    public static CellState LogicOnly(LogicOp L) => new CellState
    {
        type = CellStateType.LogicOnly,
        value = 0,
        logic = L
    };

    // --- Computation ---

    /// <summary>
    /// Compute: value L operand → result (both inputs must be 0 or 1).
    /// </summary>
    public static int Compute(int left, LogicOp op, int right)
    {
        int a = Saturate(left);
        int b = Saturate(right);
        return op switch
        {
            LogicOp.AND  => a & b,
            LogicOp.NAND => (a & b) ^ 1,
            LogicOp.OR   => a | b,
            LogicOp.NOR  => (a | b) ^ 1,
            LogicOp.XOR  => a ^ b,
            LogicOp.XNOR => (a ^ b) ^ 1,
            _ => 0
        };
    }

    /// <summary>
    /// Flip a logic operation to its negation (AND↔NAND, OR↔NOR, XOR↔XNOR).
    /// </summary>
    public static LogicOp FlipLogic(LogicOp op) => op switch
    {
        LogicOp.AND  => LogicOp.NAND,
        LogicOp.NAND => LogicOp.AND,
        LogicOp.OR   => LogicOp.NOR,
        LogicOp.NOR  => LogicOp.OR,
        LogicOp.XOR  => LogicOp.XNOR,
        LogicOp.XNOR => LogicOp.XOR,
        _ => op
    };

    // --- Helpers ---

    /// <summary>Clamp any int to 0 or 1.</summary>
    public static int Saturate(int v) => v <= 0 ? 0 : 1;

    /// <summary>Whether BFS should treat this cell as a conductive path element.</summary>
    public bool IsConductive() => type == CellStateType.PureValue && value == 1;

    public override string ToString() => type switch
    {
        CellStateType.PureValue      => $"[{value}]",
        CellStateType.ValueWithLogic => $"[{value} {logic} ___]",
        CellStateType.LogicOnly      => $"[{logic} ___]",
        _ => "[?]"
    };
}
```

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/CellState.cs
git commit -m "feat: add CellState data structure with LogicOp enum and Compute"
```

---

## Task 2: Update GridNode — Add CellState

**Files:**
- Modify: `Assets/Scripts/Grid/GridNode.cs`

- [ ] **Step 1: Add cellState field**

Replace GridNode.cs content:

```csharp
// Scripts/Grid/GridNode.cs
using UnityEngine;

public class GridNode
{
    public GridIndex index;
    public Vector3 worldPos;
    public bool occupied;
    public TileBase placedTile;

    /// <summary>Logical state of this grid cell. Defaults to PureValue(0) when empty.</summary>
    public CellState cellState;

    public GridNode(GridIndex idx, Vector3 pos)
    {
        index = idx;
        worldPos = pos;
        occupied = false;
        placedTile = null;
        cellState = CellState.PureValue(0);
    }
}
```

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Grid/GridNode.cs
git commit -m "feat: add CellState field to GridNode"
```

---

## Task 3: Update TileBase — Add GetCellState()

**Files:**
- Modify: `Assets/Scripts/Tiles/TileBase.cs`

- [ ] **Step 1: Add virtual GetCellState()**

Replace TileBase.cs:

```csharp
// Scripts/Tiles/TileBase.cs
using UnityEngine;

public abstract class TileBase : MonoBehaviour
{
    [Header("Tile Properties")]
    public abstract int Value { get; }

    public virtual bool LockAfterPlace => true;

    /// <summary>
    /// Returns the CellState of this tile. Default: PureValue based on Value property.
    /// Override in LogicTile to return ValueWithLogic or LogicOnly states.
    /// </summary>
    public virtual CellState GetCellState() => CellState.PureValue(Value);

    public virtual void OnPlaced(GridContainer container, GridIndex index) { }
    public virtual void OnRemoved(GridContainer container, GridIndex index) { }
}
```

- [ ] **Step 2: Verify TileZero and TileOne still compile**

Both inherit from TileBase and override `Value`. The new `GetCellState()` virtual uses `Value` by default, so they automatically return `PureValue(0)` / `PureValue(1)`. No changes needed.

- [ ] **Step 3: Verify ToggleTile still compiles**

ToggleTile overrides `Value` to return `_value`. `GetCellState()` will use that, returning `PureValue(_value)`. No changes needed.

- [ ] **Step 4: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tiles/TileBase.cs
git commit -m "feat: add GetCellState() virtual to TileBase"
```

---

## Task 4: Create LogicTile.cs

**Files:**
- Create: `Assets/Scripts/Tiles/LogicTile.cs`

- [ ] **Step 1: Write LogicTile.cs**

```csharp
// Scripts/Tiles/LogicTile.cs
using UnityEngine;

/// <summary>
/// A single 1×1 cell within a logic block or standalone logic tile.
/// Carries a CellState that can be PureValue, ValueWithLogic, or LogicOnly.
/// Extends TileBase — compatible with GridContainer.Place() and GridSocket.
/// </summary>
public class LogicTile : TileBase
{
    [Header("Logic Tile State")]
    [SerializeField] private CellStateType _stateType = CellStateType.LogicOnly;
    [SerializeField] private int _value;
    [SerializeField] private LogicOp _logicOp = LogicOp.AND;

    /// <summary>Exposed CellState for external read/write (TileConnector, LogicBlock).</summary>
    public CellState CellState
    {
        get
        {
            return _stateType switch
            {
                CellStateType.PureValue      => CellState.PureValue(_value),
                CellStateType.ValueWithLogic => CellState.ValueWithLogic(_value, _logicOp),
                CellStateType.LogicOnly      => CellState.LogicOnly(_logicOp),
                _ => CellState.PureValue(0)
            };
        }
        set
        {
            _stateType = value.type;
            _value = value.value;
            _logicOp = value.logic;
            ApplyLook();
        }
    }

    public override int Value
    {
        get
        {
            // Only PureValue state has a "committed" BFS-relevant value.
            // ValueWithLogic returns 0 (treated as broken circuit by BFS).
            // LogicOnly returns 0.
            if (_stateType == CellStateType.PureValue)
                return _value;
            return 0;
        }
    }

    public override bool LockAfterPlace => false;

    public override CellState GetCellState() => CellState;

    [Header("Visuals")]
    public Renderer targetRenderer;
    public Material matZero;
    public Material matOne;
    public Material matLogicWaiting; // for ValueWithLogic or LogicOnly states

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
        ApplyLook();
    }

    private void ApplyLook()
    {
        if (targetRenderer == null) return;
        Material target = _stateType switch
        {
            CellStateType.PureValue      => (_value == 1) ? matOne : matZero,
            CellStateType.ValueWithLogic => matLogicWaiting,
            CellStateType.LogicOnly      => matLogicWaiting,
            _ => matZero
        };
        if (target != null)
            targetRenderer.material = target;
    }

    /// <summary>
    /// Accept a value into a waiting logic cell. Returns the computed result, or -1 if invalid.
    /// If this cell is v L ___, computes v L incoming = result, sets this cell to PureValue(result).
    /// If this cell is L ___ (LogicOnly), captures the value: becomes incoming L ___.
    /// </summary>
    public int AcceptValue(int incomingValue)
    {
        if (_stateType == CellStateType.PureValue)
        {
            // Already pure value — cannot accept another value without logic
            return -1;
        }

        if (_stateType == CellStateType.ValueWithLogic)
        {
            int result = CellState.Compute(_value, _logicOp, incomingValue);
            CellState = CellState.PureValue(result);
            return result;
        }

        if (_stateType == CellStateType.LogicOnly)
        {
            CellState = CellState.ValueWithLogic(incomingValue, _logicOp);
            return -1; // not yet resolved, waiting for second value
        }

        return -1;
    }

    /// <summary>
    /// Accept a logic operation. Only valid when this cell is PureValue.
    /// </summary>
    public bool AcceptLogic(LogicOp L)
    {
        if (_stateType != CellStateType.PureValue)
            return false;
        CellState = CellState.ValueWithLogic(_value, L);
        return true;
    }

    /// <summary>
    /// Apply NOT spray to this cell.
    /// </summary>
    public void ApplyNot()
    {
        if (_stateType == CellStateType.PureValue)
        {
            CellState = CellState.PureValue(1 - _value);
        }
        else if (_stateType == CellStateType.ValueWithLogic)
        {
            CellState = CellState.ValueWithLogic(1 - _value, _logicOp);
        }
        else if (_stateType == CellStateType.LogicOnly)
        {
            CellState = CellState.LogicOnly(CellState.FlipLogic(_logicOp));
        }
    }
}
```

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Tiles/LogicTile.cs
git commit -m "feat: add LogicTile with CellState, AcceptValue, AcceptLogic, ApplyNot"
```

---

## Task 5: Create LogicBlock.cs

**Files:**
- Create: `Assets/Scripts/Tiles/LogicBlock.cs`

- [ ] **Step 1: Write LogicBlock.cs**

```csharp
// Scripts/Tiles/LogicBlock.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a multi-unit logic block composed of child LogicTile cells.
/// Handles grabbing, grid placement, and cell state queries.
/// For 1×1 blocks, this is a thin wrapper around a single LogicTile.
/// </summary>
public class LogicBlock : MonoBehaviour
{
    [Header("Block Config")]
    [Tooltip("Base logic operation for all cells in this block.")]
    public LogicOp baseLogicOp = LogicOp.AND;

    [Tooltip("Block dimensions in grid units.")]
    [Min(1)] public int blockWidth = 1;
    [Min(1)] public int blockHeight = 1;

    [Header("Cells (auto-detected)")]
    public LogicTile[] cells;

    private TileBase _myTile;

    void Awake()
    {
        _myTile = GetComponent<TileBase>();
        if (cells == null || cells.Length == 0)
            cells = GetComponentsInChildren<LogicTile>();

        // Sync logic op to all cells
        foreach (var cell in cells)
        {
            if (cell != null && cell.CellState.type != CellStateType.PureValue)
            {
                var st = cell.CellState;
                if (st.type == CellStateType.ValueWithLogic || st.type == CellStateType.LogicOnly)
                {
                    cell.CellState = st.type == CellStateType.ValueWithLogic
                        ? CellState.ValueWithLogic(st.value, baseLogicOp)
                        : CellState.LogicOnly(baseLogicOp);
                }
            }
        }
    }

    /// <summary>
    /// Get cell at a specific local position within the block (0,0 is first cell).
    /// </summary>
    public LogicTile GetCell(int localX, int localY)
    {
        int index = localY * blockWidth + localX;
        if (index >= 0 && index < cells.Length)
            return cells[index];
        return null;
    }

    /// <summary>
    /// Attempt to place this entire block onto the Grid at the specified anchor position.
    /// Anchor maps to the block's (0,0) cell. Returns true if all cells placed successfully.
    /// </summary>
    public bool PlaceOnGrid(GridContainer grid, GridIndex anchor)
    {
        if (grid == null) return false;

        // Phase 1: Validate — all cells must be in bounds and not conflicting
        for (int ly = 0; ly < blockHeight; ly++)
        {
            for (int lx = 0; lx < blockWidth; lx++)
            {
                var cell = GetCell(lx, ly);
                if (cell == null) continue;

                var gridIdx = new GridIndex(anchor.x + lx, anchor.y + ly, anchor.z);
                if (!grid.InBounds(gridIdx))
                {
                    Debug.LogWarning($"[LogicBlock] Cell ({lx},{ly}) out of grid bounds at {gridIdx}");
                    return false;
                }

                var node = grid.GetNode(gridIdx);
                if (node != null && node.occupied)
                {
                    Debug.LogWarning($"[LogicBlock] Cell ({lx},{ly}) conflicts with occupied grid cell at {gridIdx}");
                    return false;
                }
            }
        }

        // Phase 2: Place — each cell interacts with grid independently
        for (int ly = 0; ly < blockHeight; ly++)
        {
            for (int lx = 0; lx < blockWidth; lx++)
            {
                var cell = GetCell(lx, ly);
                if (cell == null) continue;

                var gridIdx = new GridIndex(anchor.x + lx, anchor.y + ly, anchor.z);
                grid.PlaceLogicCell(gridIdx, cell);
            }
        }

        return true;
    }

    /// <summary>
    /// Returns the total number of cells that are in a "waiting" state (ValueWithLogic or LogicOnly).
    /// </summary>
    public int WaitingCellCount
    {
        get
        {
            int count = 0;
            foreach (var cell in cells)
            {
                if (cell != null && cell.CellState.type != CellStateType.PureValue)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// True if all cells in this block have resolved to PureValue.
    /// </summary>
    public bool IsFullyResolved
    {
        get
        {
            foreach (var cell in cells)
            {
                if (cell != null && cell.CellState.type != CellStateType.PureValue)
                    return false;
            }
            return true;
        }
    }
}
```

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Tiles/LogicBlock.cs
git commit -m "feat: add LogicBlock — multi-unit logic block with grid placement"
```

---

## Task 6: Update GridContainer — PlaceLogicCell + BFS Update

**Files:**
- Modify: `Assets/Scripts/Grid/GridContainer.cs`

- [ ] **Step 1: Add PlaceLogicCell method**

Add after the existing `Place()` method (after line ~349):

```csharp
/// <summary>
/// Place a LogicTile cell onto a grid node. Handles CellState-based logic
/// interaction with the existing grid state. Does NOT check bounds or conflicts
/// (caller is responsible). Used by LogicBlock.PlaceOnGrid().
/// </summary>
public void PlaceLogicCell(GridIndex i, LogicTile logicCell)
{
    var node = GetNode(i);
    if (node == null) return;

    CellState incoming = logicCell.CellState;
    CellState existing = node.cellState;

    if (incoming.type == CellStateType.ValueWithLogic)
    {
        if (existing.type == CellStateType.PureValue)
        {
            // Compute: incoming.v L existing.v = result
            int result = CellState.Compute(incoming.value, incoming.logic, existing.value);
            node.cellState = CellState.PureValue(result);
            if (debugConnectivityLogs)
                Debug.Log($"[GridContainer] Logic merge at {i}: {incoming.value} {incoming.logic} {existing.value} = {result}");
        }
        else
        {
            // Existing is also waiting or logic-only — just overwrite
            node.cellState = incoming;
        }
    }
    else if (incoming.type == CellStateType.LogicOnly)
    {
        if (existing.type == CellStateType.PureValue)
        {
            // Capture: grid value becomes value for the logic
            node.cellState = CellState.ValueWithLogic(existing.value, incoming.logic);
        }
        else
        {
            node.cellState = incoming;
        }
    }
    else // PureValue
    {
        if (existing.type == CellStateType.PureValue)
        {
            // Both pure values — keep existing (incoming just attaches physically)
            // No change needed
        }
        else
        {
            // Overwrite waiting/logic-only with pure value
            node.cellState = incoming;
        }
    }

    // Mark as occupied
    node.occupied = true;
    node.placedTile = logicCell;

    // Position the LogicTile at the grid cell
    logicCell.transform.position = node.worldPos;
    logicCell.transform.rotation = Quaternion.identity;
    logicCell.OnPlaced(this, i);
    OnTilePlaced?.Invoke(logicCell, i);
}
```

- [ ] **Step 2: Update BFS IsOne() to use CellState**

Replace the `IsOne()` method:

```csharp
private bool IsOne(GridNode node)
{
    if (node == null || !node.occupied) return false;
    // Use CellState for BFS — only PureValue(1) is conductive
    return node.cellState.IsConductive();
}
```

- [ ] **Step 3: Update Place() to sync CellState**

In the existing `Place()` method, after `n.placedTile = tile;`, add:

```csharp
// Sync CellState from tile to node
n.cellState = tile.GetCellState();
```

Add this line after `n.placedTile = tile;` (after line 344 in the current file).

- [ ] **Step 4: Update Remove() to reset CellState**

In the existing `Remove()` method, after `n.placedTile = null;`, add:

```csharp
n.cellState = CellState.PureValue(0);
```

Add this line after `n.placedTile = null;` (after line 357).

- [ ] **Step 5: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Grid/GridContainer.cs
git commit -m "feat: add PlaceLogicCell, update BFS/Pack/Remove to use CellState"
```

---

## Task 7: Rewrite TileConnector — CellState Compatibility

**Files:**
- Modify: `Assets/Scripts/Tiles/TileConnector.cs`

- [ ] **Step 1: Replace TileConnector.cs**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manages logical tile-to-tile connections via face sockets using CellState rules.
/// Compatible cells merge-compute; incompatible cells physical-snap only.
/// </summary>
public class TileConnector : MonoBehaviour
{
    [Header("Face Sockets (auto-detected)")]
    public XRSocketInteractor[] faceSockets = new XRSocketInteractor[6];

    [Header("State")]
    public Dictionary<int, TileBase> connectedTiles = new Dictionary<int, TileBase>();

    /// <summary>Fired when two cells complete a logical merge computation.</summary>
    public event Action<TileBase, int, int> OnCellsMerged; // (otherTile, faceIndex, result)
    public event Action<TileBase, int> OnTileConnected;
    public event Action<TileBase, int> OnTileDisconnected;

    private TileBase _myTile;
    private BreakableLinkNode _linkNode;

    void Awake()
    {
        _myTile = GetComponent<TileBase>();
        _linkNode = GetComponent<BreakableLinkNode>();

        if (faceSockets == null || faceSockets.Length == 0)
            faceSockets = GetComponentsInChildren<XRSocketInteractor>();

        foreach (var socket in faceSockets)
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnSocketSelectEntered);
        }
    }

    void OnDestroy()
    {
        foreach (var socket in faceSockets)
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
        }
    }

    void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        var otherGO = args.interactableObject.transform.gameObject;
        var otherTile = otherGO.GetComponent<TileBase>();
        if (otherTile == null) return;

        int faceIndex = GetFaceIndex(args.interactorObject as XRSocketInteractor);
        if (faceIndex < 0) return;

        CellState myState = _myTile.GetCellState();
        CellState otherState = otherTile.GetCellState();

        // Apply §2.1 compatibility rules
        bool merged = TryMerge(myState, otherState, otherTile, faceIndex);
        if (!merged)
        {
            // Incompatible — physical connection only
            connectedTiles[faceIndex] = otherTile;
            OnTileConnected?.Invoke(otherTile, faceIndex);
            Debug.Log($"[TileConnector] Physical snap: {name}(face {faceIndex}) <-> {otherTile.name}");
        }
    }

    /// <summary>
    /// Try to merge two cells according to the CellState compatibility table (§2.1).
    /// Returns true if a merge was performed, false if only physical connection.
    /// </summary>
    bool TryMerge(CellState my, CellState other, TileBase otherTile, int faceIndex)
    {
        // Rule: PureValue + ValueWithLogic → merge compute
        if (my.type == CellStateType.ValueWithLogic && other.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(my.value, my.logic, other.value);

            // Resolve my cell state
            ApplyCellState(CellState.PureValue(result));

            // Resolve other cell state if it's a LogicTile
            if (otherTile is LogicTile otherLogic)
                otherLogic.CellState = CellState.PureValue(result);

            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE: {my.value} {my.logic} {other.value} = {result}");
            return true;
        }

        // Rule: PureValue + LogicOnly → capture
        if (my.type == CellStateType.LogicOnly && other.type == CellStateType.PureValue)
        {
            ApplyCellState(CellState.ValueWithLogic(other.value, my.logic));
            Debug.Log($"[TileConnector] CAPTURE: {my.logic} captured value {other.value}");
            return true;
        }

        // Symmetric: other is waiting, mine is pure value
        if (other.type == CellStateType.ValueWithLogic && my.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(other.value, other.logic, my.value);
            ApplyCellState(CellState.PureValue(result));
            if (otherTile is LogicTile otherLogic)
                otherLogic.CellState = CellState.PureValue(result);

            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE (reverse): {other.value} {other.logic} {my.value} = {result}");
            return true;
        }

        // Rule: PureValue + PureValue → no merge, just snap
        // Rule: ValueWithLogic + ValueWithLogic → no merge, just snap
        // Rule: LogicOnly + LogicOnly → no merge, just snap
        return false;
    }

    void ApplyCellState(CellState newState)
    {
        if (_myTile is ToggleTile toggle)
        {
            // For ToggleTile, only pure values can be set
            if (newState.type == CellStateType.PureValue)
            {
                while (toggle.Value != newState.value) toggle.Toggle();
            }
            // ValueWithLogic/LogicOnly on ToggleTile: not applicable (ignore for now)
        }
        else if (_myTile is LogicTile logicTile)
        {
            logicTile.CellState = newState;
        }
        // Tile_0/Tile_1 are immutable — their CellState is always PureValue(0/1)
    }

    int GetFaceIndex(XRSocketInteractor socket)
    {
        for (int i = 0; i < faceSockets.Length; i++)
            if (faceSockets[i] == socket) return i;
        return -1;
    }

    public void OnJointBroken(int faceIndex)
    {
        if (connectedTiles.TryGetValue(faceIndex, out var tile))
        {
            OnTileDisconnected?.Invoke(tile, faceIndex);
            connectedTiles.Remove(faceIndex);
            Debug.Log($"[TileConnector] Disconnected: face {faceIndex} from {tile.name}");
        }
    }

    public int ConnectionCount => connectedTiles.Count;
}
```

- [ ] **Step 2: Remove unused imports in TileConnector.cs**

`ConnectionRule` import and usage is removed. The file no longer depends on `ConnectionRule.cs`.

- [ ] **Step 3: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Tiles/TileConnector.cs
git commit -m "feat: rewrite TileConnector with CellState compatibility rules"
```

---

## Task 8: Create SprayBottle.cs

**Files:**
- Create: `Assets/Scripts/Interaction/SprayBottle.cs`

- [ ] **Step 1: Write SprayBottle.cs**

```csharp
// Scripts/Interaction/SprayBottle.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// NOT-spray bottle tool. Point at any tile and press trigger to flip its value
/// or logic operation. Limited uses per level (configured via LevelData.sprayUses).
/// </summary>
public class SprayBottle : MonoBehaviour
{
    [Header("Spray Config")]
    [Min(1)] public int maxUses = 3;
    [Tooltip("Max spray distance in meters.")]
    public float sprayRange = 3f;
    public LayerMask sprayLayerMask = ~0;

    [Header("Effects")]
    public ParticleSystem sprayParticles;
    public AudioSource spraySound;

    [Header("State")]
    [SerializeField] private int _remainingUses;

    private XRGrabInteractable _grabInteractable;
    private XRBaseControllerInteractor _activeInteractor;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _remainingUses = maxUses;

        if (_grabInteractable != null)
        {
            _grabInteractable.activated.AddListener(OnActivated);
        }
    }

    void OnDestroy()
    {
        if (_grabInteractable != null)
            _grabInteractable.activated.RemoveListener(OnActivated);
    }

    /// <summary>Called when the player pulls the trigger while holding the bottle.</summary>
    void OnActivated(ActivateEventArgs args)
    {
        if (IsEmpty)
        {
            Debug.Log("[SprayBottle] Empty — no uses remaining.");
            return;
        }

        _activeInteractor = args.interactorObject as XRBaseControllerInteractor;
        PerformSpray();
    }

    void PerformSpray()
    {
        // Raycast from the bottle nozzle (forward direction)
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, sprayLayerMask))
        {
            var target = hit.collider.GetComponentInParent<TileBase>();
            if (target != null)
            {
                ApplyNot(target);
            }
            else
            {
                // Check for LogicBlock at hit point
                var block = hit.collider.GetComponentInParent<LogicBlock>();
                if (block != null)
                {
                    ApplyNotToBlock(block, hit.point);
                }
            }

            // Visual feedback at hit point
            if (sprayParticles != null)
            {
                sprayParticles.transform.position = hit.point;
                sprayParticles.Play();
            }
        }

        _remainingUses--;
        if (spraySound != null)
            spraySound.Play();

        Debug.Log($"[SprayBottle] Spray used. Remaining: {_remainingUses}");

        if (IsEmpty)
        {
            Debug.Log("[SprayBottle] Bottle is now empty!");
            // TODO: visual feedback — bottle turns empty/dry
        }
    }

    void ApplyNot(TileBase tile)
    {
        if (tile is LogicTile logicTile)
        {
            logicTile.ApplyNot();
            Debug.Log($"[SprayBottle] NOT applied to LogicTile: {logicTile.CellState}");
        }
        else if (tile is TileToggle toggle)
        {
            toggle.Toggle();
            Debug.Log($"[SprayBottle] NOT applied to ToggleTile: now {toggle.Value}");
        }
        else
        {
            // Tile_0 or Tile_1 cannot be flipped — they are immutable
            Debug.Log($"[SprayBottle] Cannot NOT-flip immutable tile: {tile.GetType().Name}");
        }
    }

    void ApplyNotToBlock(LogicBlock block, Vector3 hitPoint)
    {
        // Find the closest cell in the block
        LogicTile closest = null;
        float minDist = float.MaxValue;
        foreach (var cell in block.cells)
        {
            float d = Vector3.Distance(hitPoint, cell.transform.position);
            if (d < minDist) { minDist = d; closest = cell; }
        }
        if (closest != null)
            closest.ApplyNot();
    }

    /// <summary>Refill the bottle (for level restart or power-up).</summary>
    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[SprayBottle] Refilled with {uses} uses.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * sprayRange);
    }
#endif
}
```

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Interaction/SprayBottle.cs
git commit -m "feat: add SprayBottle — NOT spray with limited uses per level"
```

---

## Task 9: Update LevelData — Add sprayUses

**Files:**
- Modify: `Assets/Scripts/Core/LevelData.cs`

- [ ] **Step 1: Add sprayUses field**

Add the spray-related field inside the `Difficulty` region of LevelData.cs:

```csharp
[Header("Tools")]
[Tooltip("Number of SprayBottle (NOT) uses available in this level")]
[Min(0)] public int sprayUses = 1;
```

Insert this after the `timeLimit` field (after line 30 in current file).

- [ ] **Step 2: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/LevelData.cs
git commit -m "feat: add sprayUses field to LevelData"
```

---

## Task 10: Update PuzzleInitializer — Logic Block Support

**Files:**
- Modify: `Assets/Scripts/Puzzle/PuzzleInitializer.cs`

- [ ] **Step 1: Add logic block prefab references**

Add the following fields to PuzzleInitializer after the existing `emptyTilePrefab` field (after line ~22):

```csharp
[Header("Logic Block Prefabs")]
[Tooltip("Optional prefabs for logic blocks spawned in the scene")]
public GameObject[] logicBlockPrefabs;

[Header("SprayBottle")]
[Tooltip("Reference to the spray bottle in the scene (for NOT operation)")]
public SprayBottle sprayBottle;
```

- [ ] **Step 2: Apply LevelData.sprayUses in InitializePuzzle()**

In the `InitializePuzzle()` method, inside the `if (levelData != null)` block, after `pathRemovalRatio = levelData.pathRemovalRatio;`, add:

```csharp
// Apply spray bottle uses from LevelData
if (sprayBottle != null && levelData.sprayUses > 0)
{
    sprayBottle.Refill(levelData.sprayUses);
}
```

- [ ] **Step 3: Spawn logic blocks in scene**

Add a new method and call it at the end of `InitializePuzzle()`, after marker creation:

```csharp
/// <summary>
/// Spawn logic blocks from prefabs near the grid for player pickup.
/// </summary>
private void SpawnLogicBlocks()
{
    if (logicBlockPrefabs == null || logicBlockPrefabs.Length == 0)
    {
        if (debugLogs)
            Debug.Log("[PuzzleInitializer] No logic block prefabs configured — skipping.");
        return;
    }

    Vector3 gridCenter = gridContainer.GetWorldPos(
        new GridIndex(gridContainer.width / 2, gridContainer.height / 2, 0));
    Vector3 spawnOrigin = gridCenter + Vector3.back * (gridContainer.cellSize * gridContainer.height / 2f + 0.5f);

    for (int i = 0; i < logicBlockPrefabs.Length; i++)
    {
        if (logicBlockPrefabs[i] == null) continue;
        Vector3 spawnPos = spawnOrigin + Vector3.right * i * 0.4f;
        var block = Instantiate(logicBlockPrefabs[i], spawnPos, Quaternion.identity);
        block.name = logicBlockPrefabs[i].name;
        if (debugLogs)
            Debug.Log($"[PuzzleInitializer] Spawned logic block: {block.name} at {spawnPos}");
    }
}
```

Call this at the end of `InitializePuzzle()`, after the goal marker setup and before the final debug log line:

```csharp
// Spawn logic blocks for this level
SpawnLogicBlocks();
```

- [ ] **Step 4: Wait for compilation, check console**

```
MCP: read_console
Expected: no errors.
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Puzzle/PuzzleInitializer.cs
git commit -m "feat: add logic block spawn + sprayBottle config to PuzzleInitializer"
```

---

## Task 11: Create LogicBlock Prefabs (MCP / Unity Editor)

**Files:**
- Create: `Assets/Prefabs/LogicBlock_AND_1x1.prefab`
- Create: `Assets/Prefabs/LogicBlock_OR_1x1.prefab`
- Create: `Assets/Prefabs/LogicBlock_XOR_1x1.prefab`

- [ ] **Step 1: Create LogicBlock_AND_1x1 prefab via MCP**

```
MCP: manage_asset create_prefab "LogicBlock_AND_1x1" "Assets/Prefabs/"
```

Then add components:
```
MCP: manage_gameobject create "LogicBlock_AND_Temp"
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "LogicBlock"
     Set: baseLogicOp=AND, blockWidth=1, blockHeight=1
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "LogicTile"
     Set: stateType=LogicOnly, logicOp=AND
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "TileConnector"
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "BreakableLinkNode"
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "BoxCollider"
     Set: size=(0.3, 0.3, 0.3)
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "Rigidbody"
MCP: manage_gameobject add_component "LogicBlock_AND_Temp" "XRGrabInteractable"
MCP: manage_asset save_prefab "LogicBlock_AND_Temp" "Assets/Prefabs/LogicBlock_AND_1x1.prefab"
MCP: manage_gameobject destroy "LogicBlock_AND_Temp"
```

- [ ] **Step 2: Create LogicBlock_OR_1x1 and LogicBlock_XOR_1x1**

Same as Step 1 but with `baseLogicOp=OR` and `baseLogicOp=XOR` respectively.

- [ ] **Step 3: Add visual mesh to prefabs**

Each logic block needs a visual representation:
- Colored cube with text label showing the logic operation
- AND=blue, OR=yellow, XOR=purple
- Add TextMeshPro label child showing "AND", "OR", "XOR"

```
MCP: manage_asset instantiate "Assets/Prefabs/LogicBlock_AND_1x1.prefab"
MCP: manage_gameobject create_child "LogicBlock_AND_1x1(Clone)" "Label"
MCP: manage_gameobject add_component "Label" "TextMeshPro"
     Set: text="AND", fontSize=0.15, color=white, alignment=center
MCP: manage_asset save_prefab ... (overwrite)
MCP: manage_gameobject destroy "LogicBlock_AND_1x1(Clone)"
```

- [ ] **Step 4: Wire materials for LogicTile visual states**

Assign matZero (red), matOne (green), and matLogicWaiting (white/translucent) to each LogicTile component on the prefab.

- [ ] **Step 5: Commit**

```bash
git add Assets/Prefabs/LogicBlock_AND_1x1.prefab Assets/Prefabs/LogicBlock_OR_1x1.prefab Assets/Prefabs/LogicBlock_XOR_1x1.prefab
git commit -m "feat: add LogicBlock prefabs (AND/OR/XOR 1x1)"
```

---

## Task 12: Modify Scene — Wire Logic Blocks + SprayBottle

**Files:**
- Modify: `Assets/Scenes/ComprehensiveRigExample.unity` (via MCP)

- [ ] **Step 1: Add SprayBottle to scene**

Use the existing SprayBottle GameObject in the scene (user mentioned it exists). Wire it to PuzzleInitializer.

```
MCP: manage_scene load "ComprehensiveRigExample"
MCP: manage_gameobject find "SprayBottle"
     If exists: add SprayBottle component
     If not: create a new SprayBottle GameObject with the component
MCP: manage_component set_property "PuzzleInitializer" sprayBottle → SprayBottle reference
```

- [ ] **Step 2: Assign logic block prefabs to PuzzleInitializer**

```
MCP: manage_component set_property "PuzzleInitializer" logicBlockPrefabs → [LogicBlock_AND_1x1, LogicBlock_OR_1x1]
```

- [ ] **Step 3: Create matLogicWaiting material**

```
MCP: manage_asset create_material "matLogicWaiting" 
     Set: shader=URP/Lit, color=grey/translucent, emission=subtle pulse
```

- [ ] **Step 4: Save scene**

```
MCP: manage_scene save
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/ComprehensiveRigExample.unity
git commit -m "feat: wire SprayBottle + LogicBlock prefabs to scene and PuzzleInitializer"
```

---

## Task 13: End-to-End Test in Editor Play Mode

- [ ] **Step 1: Enter Play Mode**

```
MCP: manage_editor play
```

- [ ] **Step 2: Wait for scene initialization**

Wait 3 seconds for PuzzleInitializer and GameManager to initialize.

- [ ] **Step 3: Read console for errors**

```
MCP: read_console
Expected: No compilation/runtime errors (OpenXR Simulator NRE is known, ignore).
Expected: [PuzzleInitializer] messages about logic block spawning.
Expected: [GameManager] State → Playing.
```

- [ ] **Step 4: Capture Game View screenshot**

```
MCP: manage_camera screenshot game
```

Verify: Grid is visible, start/goal markers present, logic blocks visible near grid, SprayBottle visible.

- [ ] **Step 5: Test basic interaction (via XR Simulator)**

- Grab Tile_1 and place on empty grid socket → should snap and increment moves
- Grab LogicBlock_AND → should be grabbable
- Bring Tile_1 close to LogicBlock_AND face → should merge (cell becomes [1 AND ___])
- Place merged LogicBlock on grid → should apply logic to grid cell
- Grab SprayBottle → aim at tile → activate → should flip value

- [ ] **Step 6: Test BFS with logic state**

- Place Tile_1 at start → place Tile_1 at goal → path incomplete (gaps)
- Place LogicBlock with [1 AND ___] on a gap cell → BFS treats as disconnected
- Complete the AND by providing second Tile_1 → cell resolves to [1] → BFS connects
- Verify: ConnectivityVisualizer turns green → GameManager shows Won

- [ ] **Step 7: Stop Play Mode**

```
MCP: manage_editor stop
```

- [ ] **Step 8: Fix any issues found during testing**

Read console for errors encountered during test. Fix and re-test.

---

## Task 14: Update LevelData Assets with sprayUses

**Files:**
- Modify: `Assets/Levels/Level_01_Easy.asset`
- Modify: `Assets/Levels/Level_02_Medium.asset`
- Modify: `Assets/Levels/Level_03_Hard.asset`

- [ ] **Step 1: Set sprayUses on each LevelData asset**

```
MCP: manage_asset select "Assets/Levels/Level_01_Easy.asset"
     Set sprayUses = 2
MCP: manage_asset select "Assets/Levels/Level_02_Medium.asset"
     Set sprayUses = 1
MCP: manage_asset select "Assets/Levels/Level_03_Hard.asset"
     Set sprayUses = 1
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Levels/Level_01_Easy.asset Assets/Levels/Level_02_Medium.asset Assets/Levels/Level_03_Hard.asset
git commit -m "feat: configure sprayUses per level (2/1/1)"
```

---

## Task 15: Final Verification & Cleanup

- [ ] **Step 1: Full Play Mode test**

Enter Play Mode, verify:
1. Grid generates correctly
2. Path tiles placed with 40% removal
3. Logic blocks spawned near grid
4. Tile_1 can connect to LogicBlock AND cell → cell becomes [1 AND ___]
5. Completing AND with second Tile_1 → cell resolves to [1] or [0]
6. BFS detects connectivity when all path cells are PureValue(1)
7. Win effects trigger
8. SprayBottle flips values and logic types
9. SprayBottle runs out after configured uses

- [ ] **Step 2: Read final console**

```
MCP: read_console
Expected: No new errors beyond known Simulator warnings.
```

- [ ] **Step 3: Capture final screenshot**

```
MCP: manage_camera screenshot game
```

- [ ] **Step 4: Commit any final fixes**

```bash
git add -A
git commit -m "chore: final tweaks after end-to-end testing"
```

---

**End of Plan.** Total: 15 tasks, ~14 commits.
