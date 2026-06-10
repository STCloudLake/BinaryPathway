using UnityEngine;

/// <summary>
/// Defines the logical rule for tile-to-tile connections.
/// Specifies which tile types can connect and what the result means.
/// </summary>
[CreateAssetMenu(fileName = "ConnectionRule_", menuName = "BinaryPathway/Connection Rule", order = 2)]
public class ConnectionRule : ScriptableObject
{
    [Header("Rule Identity")]
    public string ruleName = "Default";
    [TextArea(2, 4)]
    public string description = "";

    [Header("Connection Types")]
    [Tooltip("Value tiles (0/1) can connect to each other")]
    public bool valueToValue = true;
    [Tooltip("Value tiles can connect to logic tiles (creates combined logic tile)")]
    public bool valueToLogic = true;
    [Tooltip("Logic tiles can connect to each other")]
    public bool logicToLogic = false;

    [Header("Connection Results")]
    [Tooltip("When value+value connect, what value results? (-1 = no change, keep both)")]
    public int valueMergeResult = -1;

    [Tooltip("When value+logic connect, how is the logic tile modified?")]
    public LogicOperation valueOnLogicOperation = LogicOperation.SetValue;

    [Tooltip("Max connections per tile face (1 = one tile per face)")]
    [Min(1)] public int maxPerFace = 1;

    [Tooltip("Max total connections per tile")]
    [Min(1)] public int maxTotalConnections = 6;

    public enum LogicOperation
    {
        SetValue,   // Logic tile takes the value tile's value
        Toggle,     // Logic tile flips its current value
        And,        // Logic tile's value AND new value
        Or,         // Logic tile's value OR new value
        Xor         // Logic tile's value XOR new value
    }

    /// <summary>
    /// Evaluate whether two tiles can connect based on their types.
    /// Returns null if connection is invalid.
    /// </summary>
    public bool CanConnect(TileType a, TileType b)
    {
        if (a == TileType.Value && b == TileType.Value) return valueToValue;
        if (a == TileType.Value && b == TileType.Logic) return valueToLogic;
        if (a == TileType.Logic && b == TileType.Value) return valueToLogic;
        if (a == TileType.Logic && b == TileType.Logic) return logicToLogic;
        return false;
    }
}

/// <summary>
/// Tile type classification for connection rules.
/// </summary>
public enum TileType
{
    Value,  // Tile0 or Tile1 (static 0 or 1)
    Logic   // ToggleTile (player-togglable)
}
