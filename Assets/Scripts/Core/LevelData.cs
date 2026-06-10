using UnityEngine;

/// <summary>
/// ScriptableObject defining a single puzzle level configuration.
/// Used by PuzzleInitializer to set up puzzles with different difficulty.
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "BinaryPathway/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    [Min(3)] public int gridWidth = 6;
    [Min(3)] public int gridHeight = 6;
    [Min(1)] public int gridLayers = 1;

    [Header("Path")]
    public GridIndex startIndex = new GridIndex(1, 1, 0);
    public GridIndex goalIndex = new GridIndex(5, 5, 0);

    [Tooltip("0=Straight, 1=Random, 2=Maze (DFS)")]
    [Range(0, 2)] public int pathAlgorithm = 2;

    [Header("Difficulty")]
    [Tooltip("Fraction of path tiles to remove after generation")]
    [Range(0, 1)] public float pathRemovalRatio = 0.4f;

    [Tooltip("Maximum tile moves allowed (0 = unlimited)")]
    [Min(0)] public int maxMoves = 0;

    [Tooltip("Time limit in seconds (0 = unlimited)")]
    [Min(0)] public float timeLimit = 0f;

    [Header("Meta")]
    public string levelName = "Untitled";
    [TextArea(2, 4)]
    public string description = "";
    public int levelNumber = 1;
}
