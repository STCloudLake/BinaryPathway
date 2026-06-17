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

    public override bool Equals(object obj) =>
        obj is CellState other &&
        type == other.type &&
        value == other.value &&
        logic == other.logic;

    public override int GetHashCode() => (type, value, logic).GetHashCode();

    public static bool operator ==(CellState a, CellState b) => a.Equals(b);
    public static bool operator !=(CellState a, CellState b) => !a.Equals(b);

    public override string ToString() => type switch
    {
        CellStateType.PureValue      => $"[{value}]",
        CellStateType.ValueWithLogic => $"[{value} {logic} ___]",
        CellStateType.LogicOnly      => $"[{logic} ___]",
        _ => "[?]"
    };
}
