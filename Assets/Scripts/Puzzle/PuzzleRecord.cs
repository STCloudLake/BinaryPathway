// Scripts/Puzzle/PuzzleRecord.cs
using UnityEngine;

/// <summary>
/// Types of corruption that can be applied to a path tile during puzzle generation.
/// </summary>
public enum TransformationType
{
    /// <summary>PureValue(1) -> PureValue(0). Fixed by NOT spray.</summary>
    NotFlip,

    /// <summary>PureValue(1) -> LogicOnly(op). Fixed by merging two PureValue(1)
    /// tiles through TileConnector (capture then compute).</summary>
    LogicCapture
}

/// <summary>
/// Immutable record of a single transformation applied to a path tile.
/// Drives resource calculation and solvability verification.
/// </summary>
[System.Serializable]
public struct PuzzleRecord
{
    [Tooltip("Grid position where the transformation was applied.")]
    public GridIndex index;

    [Tooltip("What kind of corruption was applied.")]
    public TransformationType type;

    [Tooltip("For LogicCapture: which logic operation was imprinted on the cell.")]
    public LogicOp logicOp;

    /// <summary>State before corruption — always PureValue(1) for path tiles.</summary>
    public CellState originalState;

    /// <summary>State after corruption.</summary>
    public CellState corruptedState;

    /// <summary>
    /// How many PureValue(1) tile merges the player must perform to fix this cell.
    /// NotFlip = 0 (spray fixes it directly).
    /// LogicCapture = 2 (capture merge + compute merge via TileConnector).
    /// </summary>
    public int valuesRequired =>
        type == TransformationType.NotFlip ? 0 : 2;

    /// <summary>Whether NOT spray can fix this corruption directly.</summary>
    public bool fixableByNot =>
        type == TransformationType.NotFlip;

    // ---- Factory methods ----

    public static PuzzleRecord NotFlip(GridIndex idx)
    {
        return new PuzzleRecord
        {
            index = idx,
            type = TransformationType.NotFlip,
            logicOp = LogicOp.AND, // unused
            originalState = CellState.PureValue(1),
            corruptedState = CellState.PureValue(0)
        };
    }

    public static PuzzleRecord LogicCapture(GridIndex idx, LogicOp op)
    {
        return new PuzzleRecord
        {
            index = idx,
            type = TransformationType.LogicCapture,
            logicOp = op,
            originalState = CellState.PureValue(1),
            corruptedState = CellState.LogicOnly(op)
        };
    }

    public override string ToString() =>
        $"[PuzzleRecord] {index} {type} -> {corruptedState}";
}
