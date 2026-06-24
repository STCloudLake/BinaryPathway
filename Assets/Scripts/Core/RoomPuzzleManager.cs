using UnityEngine;

/// <summary>
/// Manages a puzzle room. ALL room-related calculations reference the Floor child's center.
/// Floor position = room center. GridAnchor/ToolAnchor = user-placed XZ hints.
/// Spawns a limited-use SprayBottle copy per room; original stays for Test_Tile.
/// </summary>
public class RoomPuzzleManager : MonoBehaviour
{
    [Header("Room Identity")]
    public string roomName = "Room 1";
    public Transform gridAnchor;
    public Transform toolAnchor;
    [SerializeField] private Transform _floorTransform;

    [Header("Puzzle")]
    public LevelData levelData;

    [Header("Next Door")]
    public string nextDoorName;
    public Animator nextDoorAnimator;

    [Header("Entry")]
    public float entryRadius = 3f;

    private GridContainer _gridContainer;
    private PuzzleInitializer _puzzleInitializer;
    private ConnectivityVisualizer _connectivityVis;
    private SprayBottle _sprayBottle;
    private Vector3 _floorCenter;
    private float _floorTop;
    private bool _triggered;
    private bool _solved;
    private int _debugFrameCount;

    void Start()
    {
        _gridContainer = FindFirstObjectByType<GridContainer>();
        _puzzleInitializer = FindFirstObjectByType<PuzzleInitializer>();
        _connectivityVis = FindFirstObjectByType<ConnectivityVisualizer>();
        _sprayBottle = FindFirstObjectByType<SprayBottle>();

        if (_floorTransform == null) _floorTransform = transform.Find("Floor");
        if (_floorTransform != null)
        {
            _floorCenter = _floorTransform.position;
            var col = _floorTransform.GetComponent<BoxCollider>();
            if (col != null) _floorTop = col.bounds.center.y + col.bounds.extents.y;
            else _floorTop = _floorCenter.y + 0.09f;
        }
        else
        {
            _floorCenter = transform.position;
            _floorTop = 0.18f;
        }

        if (toolAnchor == null) toolAnchor = transform.Find("ToolAnchor");

        if (nextDoorAnimator == null && !string.IsNullOrEmpty(nextDoorName))
        {
            var d = GameObject.Find(nextDoorName);
            if (d != null) nextDoorAnimator = d.GetComponent<Animator>();
        }

        if (nextDoorAnimator != null)
            nextDoorAnimator.SetBool("character_nearby", false);

        Debug.Log($"[RoomPuzzle] {roomName} Start: floorCenter={_floorCenter}, floorTop={_floorTop:F3}, gridContainer={(_gridContainer!=null)}, puzzleInit={(_puzzleInitializer!=null)}");
    }

    void Update()
    {
        if (_triggered) return;

        // Find camera fresh each frame (CenterEyeAnchor may not exist at Start)
        var cam = Camera.main;
        if (cam == null) return;

        float dist = Vector3.Distance(
            new Vector3(cam.transform.position.x, 0, cam.transform.position.z),
            new Vector3(_floorCenter.x, 0, _floorCenter.z));

        if (dist < entryRadius)
        {
            _triggered = true;
            Debug.Log($"[RoomPuzzle] {roomName}: TRIGGERED! dist={dist:F1} < {entryRadius}");
            SpawnPuzzle();
        }
    }

    void SpawnPuzzle()
    {
        int gridH = levelData != null ? levelData.gridHeight : 4;
        float cellSz = _gridContainer != null ? _gridContainer.cellSize : 0.3f;
        float gridHalf = (gridH - 1) * cellSz / 2f;
        Vector3 gridPos = gridAnchor != null
            ? new Vector3(gridAnchor.position.x, _floorTop + 0.05f + gridHalf, gridAnchor.position.z)
            : new Vector3(_floorCenter.x, _floorTop + 0.05f + gridHalf, _floorCenter.z);

        Debug.Log($"[RoomPuzzle] {roomName}: SpawnPuzzle gridPos={gridPos}");

        if (_gridContainer != null)
        {
            _gridContainer.transform.position = gridPos;
            _gridContainer.transform.rotation = Quaternion.identity;
        }

        // Create a limited-use spray bottle copy at tool anchor
        if (_sprayBottle != null && toolAnchor != null)
        {
            var sprayCopy = Instantiate(_sprayBottle.gameObject, toolAnchor.position + Vector3.up * 0.3f, Quaternion.identity);
            sprayCopy.name = $"{roomName}_SprayBottle";

            // Disable label rendering on copy to prevent RenderTexture freeze + null texture
            foreach (var label in sprayCopy.GetComponentsInChildren<Oculus.Interaction.Samples.InteractableObjectLabel>())
                label.enabled = false;
            foreach (var crt in sprayCopy.GetComponentsInChildren<Oculus.Interaction.UnityCanvas.CanvasRenderTexture>())
                crt.enabled = false;

            if (levelData != null && levelData.sprayUses > 0)
            {
                var sbComp = sprayCopy.GetComponent<SprayBottle>();
                if (sbComp != null) sbComp.Refill(levelData.sprayUses);
            }
        }

        if (_puzzleInitializer != null && levelData != null)
        {
            _puzzleInitializer.levelData = levelData;
            _puzzleInitializer.spawnOriginOverride = toolAnchor != null
                ? toolAnchor.position + Vector3.up * 0.15f
                : (Vector3?)null;
            _puzzleInitializer.InitializePuzzle();
            _puzzleInitializer.spawnOriginOverride = null; // reset

            // Sync connectivity checkpoints to match the new puzzle
            if (_connectivityVis != null)
            {
                _connectivityVis.SetCheckPoints(_puzzleInitializer.startIndex, _puzzleInitializer.goalIndex);
                Debug.Log($"[RoomPuzzle] {roomName}: synced checkpoints {_puzzleInitializer.startIndex}->{_puzzleInitializer.goalIndex}");
            }
        }

        if (_connectivityVis != null)
            _connectivityVis.OnConnectivityChanged += OnWin;
    }

    public void RestartPuzzle()
    {
        Debug.Log($"[RoomPuzzle] {roomName}: restarting");
        _solved = false;
        _triggered = false;

        if (_connectivityVis != null)
            _connectivityVis.OnConnectivityChanged -= OnWin;

        if (_puzzleInitializer != null)
            _puzzleInitializer.ReinitializePuzzle();

        if (nextDoorAnimator != null)
            nextDoorAnimator.SetBool("character_nearby", false);

        _triggered = true;
        SpawnPuzzle();
    }

    void OnWin(bool connected)
    {
        if (!connected || _solved) return;
        _solved = true;
        Debug.Log($"[RoomPuzzle] {roomName} SOLVED! -> {nextDoorName}");

        if (_connectivityVis != null)
            _connectivityVis.OnConnectivityChanged -= OnWin;

        OpenNextDoor();
    }

    void OpenNextDoor()
    {
        if (nextDoorAnimator != null)
            nextDoorAnimator.SetBool("character_nearby", true);
    }

    void OnDrawGizmosSelected()
    {
        if (_floorTransform != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(_floorTransform.position, entryRadius);
        }
        if (gridAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(gridAnchor.position, Vector3.one * 0.2f);
        }
    }
}
