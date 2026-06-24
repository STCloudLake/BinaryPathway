using UnityEngine;

/// <summary>
/// ScriptableObject defining a single puzzle level configuration.
/// Used by PuzzleInitializer to set up puzzles with different difficulty.
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "BinaryPathway/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    [Min(1)] public int gridWidth = 6;
    [Min(1)] public int gridHeight = 6;
    [Min(1)] public int gridLayers = 1;

    [Header("Path")]
    public GridIndex startIndex = new GridIndex(1, 1, 0);
    public GridIndex goalIndex = new GridIndex(5, 5, 0);

    [Tooltip("0=Straight, 1=Random, 2=Maze (DFS)")]
    [Range(0, 2)] public int pathAlgorithm = 2;

    [Header("Puzzle Transformations")]
    [Tooltip("Number of path tiles to corrupt with NOT (PureValue(1)->PureValue(0)). Fixed by spray.")]
    [Min(0)] public int notBreakCount = 2;

    [Tooltip("Number of path tiles to corrupt with LogicCapture (PureValue(1)->LogicOnly). Fixed by merging value tiles.")]
    [Min(0)] public int logicCaptureCount = 1;

    [Tooltip("Logic operation to imprint on LogicCapture tiles.")]
    public LogicOp captureLogicOp = LogicOp.AND;

    [Tooltip("Disable diagonal BFS for this level (recommended).")]
    public bool forceNoDiagonals = true;

    [Tooltip("Cells left empty (no tile) — player must fill them. Tutorial use.")]
    public GridIndex[] emptyCells = new GridIndex[0];

    [Tooltip("Maximum tile moves allowed (0 = unlimited)")]
    [Min(0)] public int maxMoves = 0;

    [Tooltip("Time limit in seconds (0 = unlimited)")]
    [Min(0)] public float timeLimit = 0f;

    [Header("Tools")]
    [Tooltip("Number of SprayBottle (NOT) uses available in this level")]
    [Min(0)] public int sprayUses = 1;

    [Header("Meta")]
    public string levelName = "Untitled";
    [TextArea(2, 4)]
    public string description = "";
    public int levelNumber = 1;
}
