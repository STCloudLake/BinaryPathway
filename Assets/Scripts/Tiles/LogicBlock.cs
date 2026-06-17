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
