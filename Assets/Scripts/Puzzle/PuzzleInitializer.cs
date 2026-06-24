// Scripts/Puzzle/PuzzleInitializer.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Puzzle generation orchestrator. New pipeline:
/// GeneratePath -> FillGrid -> ApplyTransformations -> AnalyzeRecords -> SpawnRequiredTiles -> Verify
///
/// Every corruption applied to a path tile produces a PuzzleRecord.
/// Records drive resource calculation and spawning.
/// </summary>
public class PuzzleInitializer : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("The GridContainer to populate.")]
    public GridContainer gridContainer;

    [Header("Tile Prefab")]
    [Tooltip("Unified tile prefab. CellState configured per instance at spawn.")]
    public GameObject tilePrefab;

    [Header("Logic Block Spawning")]
    [Tooltip("Logic operation types to spawn as hint blocks.")]
    public LogicOp[] spawnedLogicTypes;

    [Header("SprayBottle")]
    [Tooltip("Reference to the spray bottle in the scene.")]
    public SprayBottle sprayBottle;

    [Header("Start & Goal")]
    public GridIndex startIndex = new GridIndex(0, 0, 0);
    public GridIndex goalIndex = new GridIndex(5, 5, 0);

    [Header("Puzzle Generation")]
    [Tooltip("Force 4-directional BFS (recommended — prevents diagonal bypass).")]
    public bool forceNoDiagonals = true;

    [Tooltip("Number of NOT-flip corruptions (PureValue(1)->PureValue(0)).")]
    [Min(0)] public int notBreakCount = 2;

    [Tooltip("Number of LogicCapture corruptions (PureValue(1)->LogicOnly).")]
    [Min(0)] public int logicCaptureCount = 1;

    [Tooltip("Logic operation to imprint on capture tiles.")]
    public LogicOp captureLogicOp = LogicOp.AND;

    [Header("Spawned Value Tiles")]
    [Tooltip("Optional prefab for PureValue(1) tiles. Falls back to tilePrefab.")]
    public GameObject valueTilePrefab;

    [Tooltip("Distance from grid edge to spawn resources.")]
    public float spawnDistance = 0.8f;
    [System.NonSerialized] public Vector3? spawnOriginOverride;

    [Header("Marker Objects")]
    public GameObject startMarker;
    public GameObject goalMarker;

    [Header("Generation Options")]
    [Range(0, 2)] public int pathAlgorithm = 1;
    public bool autoInitializeOnStart = true;

    [Header("Level Config (optional)")]
    public LevelData levelData;

    [Header("Debug")]
    public bool debugLogs = false;

    [Header("Runtime Records")]
    [SerializeField] private List<PuzzleRecord> _records = new();
    public IReadOnlyList<PuzzleRecord> Records => _records;

    private List<GridIndex> _currentPath;
    private bool _originalAllowDiagonals;
    private HashSet<GridIndex> _emptyCellSet = new();

    void Start()
    {
        if (autoInitializeOnStart && gridContainer != null)
            InitializePuzzle();
    }

    // ============================================================
    // MAIN PIPELINE
    // ============================================================

    public void InitializePuzzle()
    {
        if (gridContainer == null)
        {
            Debug.LogError("[PuzzleInitializer] GridContainer not assigned");
            return;
        }

        // ---- Phase -1: Ensure pristine grid (defeat stale editor state) ----
        gridContainer.Regenerate();

        // ---- Phase 0: Apply LevelData overrides + enforce diagonals ----
        if (levelData != null)
        {
            bool sizeChanged =
                gridContainer.width != levelData.gridWidth ||
                gridContainer.height != levelData.gridHeight ||
                gridContainer.layers != levelData.gridLayers;

            gridContainer.width = levelData.gridWidth;
            gridContainer.height = levelData.gridHeight;
            gridContainer.layers = levelData.gridLayers;
            startIndex = levelData.startIndex;
            goalIndex = levelData.goalIndex;
            pathAlgorithm = levelData.pathAlgorithm;
            notBreakCount = levelData.notBreakCount;
            logicCaptureCount = levelData.logicCaptureCount;
            captureLogicOp = levelData.captureLogicOp;
            forceNoDiagonals = levelData.forceNoDiagonals;
            _emptyCellSet = new HashSet<GridIndex>(levelData.emptyCells ?? new GridIndex[0]);

            // Rebuild grid if LevelData changed the dimensions (Awake already ran with old size)
            if (sizeChanged)
            {
                gridContainer.Regenerate();
                if (debugLogs)
                    Debug.Log($"[PuzzleInitializer] Grid rebuilt: {gridContainer.width}x{gridContainer.height}x{gridContainer.layers}");
            }
        }

        _originalAllowDiagonals = gridContainer.allowDiagonals2D;
        if (forceNoDiagonals)
            gridContainer.allowDiagonals2D = false;

        if (!gridContainer.InBounds(startIndex) || !gridContainer.InBounds(goalIndex))
        {
            Debug.LogError("[PuzzleInitializer] Start or goal out of bounds");
            return;
        }

        if (tilePrefab == null)
        {
            Debug.LogError("[PuzzleInitializer] Tile prefab not assigned");
            return;
        }

        // ---- Phase 1: Generate path ----
        _currentPath = GeneratePath(startIndex, goalIndex);
        if (_currentPath == null || _currentPath.Count == 0)
        {
            Debug.LogError("[PuzzleInitializer] Failed to generate path");
            return;
        }
        if (debugLogs)
            Debug.Log($"[PuzzleInitializer] Path length: {_currentPath.Count}");

        // ---- Phase 2: Fill grid ----
        FillGrid(_currentPath);

        // ---- Phase 2b: Verify initial connectivity ----
        // Temporarily mark empty cells as occupied+1 so BFS can validate the path
        foreach (var idx in _emptyCellSet)
        {
            var n = gridContainer.GetNode(idx);
            if (n != null) { n.occupied = true; n.cellState = CellState.PureValue(1); }
        }
        bool pathOk = gridContainer.CheckConnectivity(startIndex, goalIndex);
        // Restore empty cells
        foreach (var idx in _emptyCellSet)
        {
            var n = gridContainer.GetNode(idx);
            if (n != null) { n.occupied = false; n.cellState = CellState.PureValue(0); }
        }
        if (!pathOk)
        {
            Debug.LogError("[PuzzleInitializer] Filled path is NOT connected! Aborting.");
            return;
        }
        if (debugLogs)
            Debug.Log("[PuzzleInitializer] Initial connectivity VERIFIED (start->goal connected).");

        // ---- Phase 3: Apply transformations ----
        _records.Clear();
        ApplyTransformations();

        // ---- Phase 4: Analyze records ----
        AnalyzeRecords();

        // ---- Phase 5: Spawn required resources ----
        SpawnRequiredTiles();

        // ---- Phase 6: Place markers ----
        PlaceMarkers();

        // ---- Phase 7: Verify ----
        VerifyPuzzleBroken();
        VerifyPuzzleSolvable();

        if (debugLogs)
            Debug.Log($"[PuzzleInitializer] Puzzle initialized — {_records.Count} transformations, " +
                      $"path={_currentPath.Count} cells");
    }

    // ============================================================
    // PATH GENERATION (unchanged)
    // ============================================================

    private List<GridIndex> GeneratePath(GridIndex start, GridIndex goal)
    {
        return pathAlgorithm switch
        {
            0 => GenerateStraightPath(start, goal),
            1 => GenerateRandomPath(start, goal),
            2 => GenerateMazePath(start, goal),
            _ => GenerateStraightPath(start, goal),
        };
    }

    private List<GridIndex> GenerateStraightPath(GridIndex start, GridIndex goal)
    {
        var path = new List<GridIndex> { start };
        var current = start;
        while (!current.Equals(goal))
        {
            // Move ONE axis per step (compatible with 4-directional BFS)
            int dx = goal.x > current.x ? 1 : (goal.x < current.x ? -1 : 0);
            int dy = goal.y > current.y ? 1 : (goal.y < current.y ? -1 : 0);
            int dz = goal.z > current.z ? 1 : (goal.z < current.z ? -1 : 0);

            if (dx != 0)
                current = new GridIndex(current.x + dx, current.y, current.z);
            else if (dy != 0)
                current = new GridIndex(current.x, current.y + dy, current.z);
            else if (dz != 0)
                current = new GridIndex(current.x, current.y, current.z + dz);
            else
                break; // should not happen

            if (gridContainer.InBounds(current))
                path.Add(current);
            else
                return null;
        }
        return path;
    }

    private List<GridIndex> GenerateRandomPath(GridIndex start, GridIndex goal)
    {
        var path = new List<GridIndex> { start };
        var current = start;
        var visited = new HashSet<GridIndex> { start };
        int maxSteps = 1000, step = 0;

        while (!current.Equals(goal) && step < maxSteps)
        {
            var neighbors = new List<GridIndex>(gridContainer.GetNeighbors(current));
            if (neighbors.Count == 0) break;

            GridIndex next = neighbors[Random.Range(0, neighbors.Count)];
            if (Random.value < 0.3f)
            {
                var best = GetNeighborClosestToGoal(neighbors, goal);
                if (best.HasValue) next = best.Value;
            }

            if (!visited.Contains(next))
            {
                visited.Add(next);
                path.Add(next);
                current = next;
            }
            else
            {
                next = neighbors[Random.Range(0, neighbors.Count)];
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    path.Add(next);
                    current = next;
                }
            }
            step++;
        }
        return current.Equals(goal) ? path : null;
    }

    private List<GridIndex> GenerateMazePath(GridIndex start, GridIndex goal)
    {
        var path = new List<GridIndex>();
        var visited = new HashSet<GridIndex>();
        if (DFSPath(start, goal, visited, path)) return path;
        return null;
    }

    private bool DFSPath(GridIndex current, GridIndex goal, HashSet<GridIndex> visited, List<GridIndex> path)
    {
        visited.Add(current);
        path.Add(current);
        if (current.Equals(goal)) return true;

        var neighbors = new List<GridIndex>(gridContainer.GetNeighbors(current));
        ShuffleList(neighbors);

        foreach (var neighbor in neighbors)
        {
            if (!visited.Contains(neighbor))
                if (DFSPath(neighbor, goal, visited, path))
                    return true;
        }
        path.RemoveAt(path.Count - 1);
        return false;
    }

    // ============================================================
    // FILL GRID
    // ============================================================

    private void FillGrid(List<GridIndex> path)
    {
        var pathSet = new HashSet<GridIndex>(path);

        for (int x = 0; x < gridContainer.width; x++)
            for (int y = 0; y < gridContainer.height; y++)
                for (int z = 0; z < gridContainer.layers; z++)
                {
                    var idx = new GridIndex(x, y, z);

                    // Skip empty cells (tutorial: player fills these)
                    if (_emptyCellSet.Contains(idx))
                        continue;

                    var tileGo = Instantiate(tilePrefab, gridContainer.GetWorldPos(idx), Quaternion.identity);
                    var tile = tileGo.GetComponent<TileBase>();
                    var logicTile = tileGo.GetComponent<LogicTile>();

                    if (logicTile != null)
                        logicTile.CellState = CellState.PureValue(pathSet.Contains(idx) ? 1 : 0);

                    if (tile != null)
                    {
                        if (!gridContainer.Place(idx, tile))
                        {
                            Debug.LogWarning($"[PuzzleInitializer] Failed to place tile at {idx}");
                            Destroy(tileGo);
                        }
                        else
                        {
                            // Grid tiles are fixed — disable ISDK grab only
                            // (Keep XRGrabInteractable for face-socket/TileConnector merge)
                            var isdkGrab = tileGo.GetComponent<Oculus.Interaction.Grabbable>();
                            if (isdkGrab != null) isdkGrab.enabled = false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[PuzzleInitializer] Tile prefab has no TileBase component");
                    }
                }

        // Hide grid socket visuals (prevent z-fighting with placed tiles)
        // But keep sockets visible for empty cells (tutorial placement targets)
        if (gridContainer.socketsRoot != null)
        {
            foreach (var r in gridContainer.socketsRoot.GetComponentsInChildren<Renderer>())
                r.enabled = false;

            // Re-enable renderers for empty cells
            if (_emptyCellSet.Count > 0)
            {
                foreach (Transform child in gridContainer.socketsRoot)
                {
                    var gs = child.GetComponent<GridSocket>();
                    // GridSocket stores bound index via Bind(); we check each child
                }
                // Fallback: find sockets by position matching
                for (int x = 0; x < gridContainer.width; x++)
                    for (int y = 0; y < gridContainer.height; y++)
                        for (int z = 0; z < gridContainer.layers; z++)
                        {
                            var idx = new GridIndex(x, y, z);
                            if (!_emptyCellSet.Contains(idx)) continue;
                            Vector3 pos = gridContainer.GetWorldPos(idx);
                            foreach (Transform child in gridContainer.socketsRoot)
                            {
                                if (Vector3.Distance(child.position, pos) < 0.01f)
                                {
                                    var r = child.GetComponent<Renderer>();
                                    if (r != null) r.enabled = true;
                                    break;
                                }
                            }
                        }
            }
        }
    }

    // ============================================================
    // PHASE 3: APPLY TRANSFORMATIONS
    // ============================================================

    private void ApplyTransformations()
    {
        if (_currentPath == null || _currentPath.Count <= 2) return;

        var available = new List<GridIndex>(_currentPath);
        available.Remove(startIndex);
        available.Remove(goalIndex);

        int totalDesired = notBreakCount + logicCaptureCount;
        if (totalDesired > available.Count)
        {
            Debug.LogWarning($"[PuzzleInitializer] Requested {totalDesired} transforms but only " +
                             $"{available.Count} path tiles available. Clamping.");
            float ratio = (float)notBreakCount / Mathf.Max(1, notBreakCount + logicCaptureCount);
            notBreakCount = Mathf.RoundToInt(available.Count * ratio);
            logicCaptureCount = available.Count - notBreakCount;
        }

        ShuffleList(available);
        int applied = 0;

        // NOT breaks
        for (int i = 0; i < notBreakCount && applied < available.Count; i++, applied++)
        {
            var idx = available[applied];
            SetCellState(idx, CellState.PureValue(0));
            _records.Add(PuzzleRecord.NotFlip(idx));
            if (debugLogs)
                Debug.Log($"[PuzzleInitializer] NOT break at {idx}");
        }

        // Logic captures
        for (int i = 0; i < logicCaptureCount && applied < available.Count; i++, applied++)
        {
            var idx = available[applied];
            SetCellState(idx, CellState.LogicOnly(captureLogicOp));
            _records.Add(PuzzleRecord.LogicCapture(idx, captureLogicOp));
            if (debugLogs)
                Debug.Log($"[PuzzleInitializer] LogicCapture({captureLogicOp}) at {idx}");
        }
    }

    void SetCellState(GridIndex idx, CellState newState)
    {
        var node = gridContainer.GetNode(idx);
        if (node == null) return;
        node.cellState = newState;
        if (node.placedTile is LogicTile lt)
            lt.CellState = newState;
    }

    // ============================================================
    // PHASE 4: ANALYZE RECORDS
    // ============================================================

    void AnalyzeRecords()
    {
        int notCount = 0, captureCount = 0;
        foreach (var rec in _records)
        {
            switch (rec.type)
            {
                case TransformationType.NotFlip: notCount++; break;
                case TransformationType.LogicCapture: captureCount++; break;
            }
        }

        // Configure spray bottle
        if (sprayBottle != null)
        {
            int needed = notCount;
            int fromLevel = levelData != null ? levelData.sprayUses : 1;
            sprayBottle.Refill(Mathf.Max(needed, fromLevel));
        }

        if (debugLogs)
            Debug.Log($"[PuzzleInitializer] Analysis: {notCount} NOT + {captureCount} Logic = " +
                      $"{captureCount * 2} value tiles needed");
    }

    // ============================================================
    // PHASE 5: SPAWN REQUIRED RESOURCES
    // ============================================================

    void SpawnRequiredTiles()
    {
        int valueTilesNeeded = 0;
        foreach (var rec in _records)
            valueTilesNeeded += rec.valuesRequired;
        // Add tiles for empty cells (tutorial: player fills gaps)
        valueTilesNeeded += _emptyCellSet.Count;

        if (valueTilesNeeded <= 0)
        {
            Debug.LogWarning("[PuzzleInitializer] No value tiles needed — skipping spawn.");
            return;
        }

        GameObject prefab = valueTilePrefab != null ? valueTilePrefab : tilePrefab;
        if (prefab == null)
        {
            Debug.LogError("[PuzzleInitializer] No tile prefab assigned! Skipping spawn.");
            return;
        }

        // Spawn position: use override if set, otherwise calculate from grid
        Vector3 spawnOrigin;
        if (spawnOriginOverride.HasValue)
        {
            spawnOrigin = spawnOriginOverride.Value;
        }
        else
        {
            Vector3 gridMin = gridContainer.GetWorldPos(new GridIndex(0, 0, 0));
            spawnOrigin = gridMin + Vector3.forward * 3f;
        }
        float spacing = gridContainer.cellSize * 2.5f;

        Debug.Log($"[PuzzleInitializer] Spawning {valueTilesNeeded} tiles at {spawnOrigin}");

        // Spawn PureValue(1) tiles
        for (int i = 0; i < valueTilesNeeded; i++)
        {
            Vector3 pos = spawnOrigin + Vector3.right * i * spacing;
            var go = Instantiate(prefab, pos, Quaternion.identity);
            go.name = $"ValueTile_1_{i}";
            var lt = go.GetComponent<LogicTile>();
            if (lt != null) lt.CellState = CellState.PureValue(1);

            // Disable face sockets on spawned tiles (prevent unwanted connections)
            foreach (var s in go.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>())
                s.enabled = false;
        }

        if (debugLogs)
            Debug.Log($"[PuzzleInitializer] Spawned {valueTilesNeeded} value-1 tiles");
    }

    // ============================================================
    // PHASE 6: MARKERS
    // ============================================================

    void PlaceMarkers()
    {
        if (startMarker == null)
            startMarker = CreateProceduralMarker("StartMarker", Color.green, startIndex, "START");
        else
            startMarker.transform.position = gridContainer.GetWorldPos(startIndex);

        if (goalMarker == null)
            goalMarker = CreateProceduralMarker("GoalMarker", Color.red, goalIndex, "GOAL");
        else
            goalMarker.transform.position = gridContainer.GetWorldPos(goalIndex);
    }

    GameObject CreateProceduralMarker(string name, Color color, GridIndex index, string label)
    {
        if (!gridContainer.InBounds(index)) return null;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = gridContainer.GetWorldPos(index); // centered on cell
        go.transform.localScale = Vector3.one * 0.10f;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
            renderer.material.color = color;
        return go;
    }

    // ============================================================
    // PHASE 7: VERIFY
    // ============================================================

    void VerifyPuzzleBroken()
    {
        bool connected = gridContainer.CheckConnectivity(startIndex, goalIndex);
        if (connected)
        {
            Debug.LogError("[PuzzleInitializer] VERIFY FAIL: puzzle is STILL connected after " +
                           "transformations! Increase notBreakCount or logicCaptureCount.");
        }
        else if (debugLogs)
        {
            Debug.Log("[PuzzleInitializer] VERIFIED: puzzle is broken (BFS disconnected).");
        }
    }

    void VerifyPuzzleSolvable()
    {
        if (_records.Count == 0) return;

        // Temporarily fix all cells (both node + tile), check BFS, restore
        var saved = new Dictionary<GridIndex, CellState>();
        foreach (var rec in _records)
        {
            var node = gridContainer.GetNode(rec.index);
            if (node != null)
            {
                saved[rec.index] = node.cellState;
                node.cellState = CellState.PureValue(1);
                // Also sync the LogicTile so IsOne() sees the fix
                if (node.placedTile is LogicTile lt)
                    lt.CellState = CellState.PureValue(1);
            }
        }

        bool solvable = gridContainer.CheckConnectivity(startIndex, goalIndex);

        // Restore both node + tile
        foreach (var kv in saved)
        {
            var node = gridContainer.GetNode(kv.Key);
            if (node != null)
            {
                node.cellState = kv.Value;
                if (node.placedTile is LogicTile lt)
                    lt.CellState = kv.Value;
            }
        }

        if (!solvable)
        {
            Debug.LogError("[PuzzleInitializer] VERIFY FAIL: puzzle NOT solvable even with all " +
                           "fixes applied! Check path topology.");
        }
        else if (debugLogs)
        {
            Debug.Log("[PuzzleInitializer] VERIFIED: puzzle is solvable (all fixes restore BFS).");
        }
    }

    // ============================================================
    // REINITIALIZE
    // ============================================================

    public void ReinitializePuzzle()
    {
        // Destroy all placed tiles
        for (int x = 0; x < gridContainer.width; x++)
            for (int y = 0; y < gridContainer.height; y++)
                for (int z = 0; z < gridContainer.layers; z++)
                {
                    var idx = new GridIndex(x, y, z);
                    var node = gridContainer.GetNode(idx);
                    if (node != null && node.placedTile != null)
                    {
                        if (Application.isPlaying)
                            Destroy(node.placedTile.gameObject);
                        else
                            DestroyImmediate(node.placedTile.gameObject);
                    }
                    gridContainer.Remove(idx);
                }

        _records.Clear();

        if (forceNoDiagonals)
            gridContainer.allowDiagonals2D = _originalAllowDiagonals;

        InitializePuzzle();
    }

    // ============================================================
    // HELPERS
    // ============================================================

    GridIndex? GetNeighborClosestToGoal(List<GridIndex> neighbors, GridIndex goal)
    {
        if (neighbors.Count == 0) return null;
        GridIndex best = neighbors[0];
        float bestDist = Vector3.Distance(gridContainer.GetWorldPos(best), gridContainer.GetWorldPos(goal));
        foreach (var nb in neighbors)
        {
            float dist = Vector3.Distance(gridContainer.GetWorldPos(nb), gridContainer.GetWorldPos(goal));
            if (dist < bestDist) { bestDist = dist; best = nb; }
        }
        return best;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_currentPath == null || gridContainer == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _currentPath.Count - 1; i++)
        {
            Vector3 p1 = gridContainer.GetWorldPos(_currentPath[i]);
            Vector3 p2 = gridContainer.GetWorldPos(_currentPath[i + 1]);
            Gizmos.DrawLine(p1, p2);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gridContainer.GetWorldPos(startIndex), 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gridContainer.GetWorldPos(goalIndex), 0.1f);
    }
#endif
}
