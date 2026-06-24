// Scripts/Core/GameManager.cs
using System.Collections;
using UnityEngine;

public enum GameState { Idle, Playing, Won }

/// <summary>
/// Central game state machine.
/// Idle → Playing (puzzle initialized) → Won (connectivity achieved).
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Auto-Found References")]
    [SerializeField] private GridContainer _gridContainer;
    [SerializeField] private ConnectivityVisualizer _connectivityVisualizer;
    [SerializeField] private PuzzleInitializer _puzzleInitializer;
    [SerializeField] private WinFeedbackController _winFeedback;

    [Header("Level Progression")]
    [SerializeField] private LevelData[] _levels;
    [SerializeField] private int _currentLevelIndex;

    [Header("State")]
    [SerializeField] private GameState _state = GameState.Idle;
    [SerializeField] private int _moveCount;

    public GameState State => _state;
    public int MoveCount => _moveCount;
    public bool IsPlaying => _state == GameState.Playing;
    public bool HasWon => _state == GameState.Won;

    void Start()
    {
        if (_gridContainer == null)
            _gridContainer = FindFirstObjectByType<GridContainer>();
        if (_connectivityVisualizer == null)
            _connectivityVisualizer = FindFirstObjectByType<ConnectivityVisualizer>();
        if (_puzzleInitializer == null)
            _puzzleInitializer = FindFirstObjectByType<PuzzleInitializer>();
        if (_winFeedback == null)
            _winFeedback = GetComponent<WinFeedbackController>();

        if (_connectivityVisualizer != null)
            _connectivityVisualizer.OnConnectivityChanged += OnConnectivityChanged;

        if (_gridContainer != null)
        {
            _gridContainer.OnTilePlaced += OnTilePlaced;
            _gridContainer.OnTileRemoved += OnTileRemoved;
        }

        // Wire transition callback
        if (_winFeedback != null)
            _winFeedback.OnTransitionComplete += LoadNextLevel;

        // Load current level (from inspector or LevelData array)
        LoadCurrentLevel();

        StartCoroutine(EnterPlayingDelayed());
    }

    void OnDestroy()
    {
        if (_connectivityVisualizer != null)
            _connectivityVisualizer.OnConnectivityChanged -= OnConnectivityChanged;
        if (_gridContainer != null)
        {
            _gridContainer.OnTilePlaced -= OnTilePlaced;
            _gridContainer.OnTileRemoved -= OnTileRemoved;
        }
        if (_winFeedback != null)
            _winFeedback.OnTransitionComplete -= LoadNextLevel;
    }

    void LoadCurrentLevel()
    {
        if (_puzzleInitializer != null && _levels != null && _currentLevelIndex < _levels.Length)
        {
            _puzzleInitializer.levelData = _levels[_currentLevelIndex];
            Debug.Log($"[GameManager] Level {_currentLevelIndex}: {_levels[_currentLevelIndex].levelName}");
        }
    }

    void LoadNextLevel()
    {
        _currentLevelIndex++;
        if (_levels != null && _currentLevelIndex < _levels.Length)
        {
            Debug.Log($"[GameManager] Loading next level: {_levels[_currentLevelIndex].levelName}");
            _puzzleInitializer.levelData = _levels[_currentLevelIndex];
            RestartPuzzle();
        }
        else
        {
            Debug.Log("[GameManager] All levels complete!");
        }
    }

    IEnumerator EnterPlayingDelayed()
    {
        yield return null; // PuzzleInitializer.Start
        yield return null; // Extra buffer
        yield return null; // Grid rebuild settle

        SyncConnectivityCheckpoints();

        if (_state != GameState.Won)
        {
            _state = GameState.Playing;
            Debug.Log("[GameManager] State → Playing");
        }

        // Check if already connected (removed path tiles may not disconnect)
        if (_connectivityVisualizer != null)
        {
            bool connected = _connectivityVisualizer.ManualCheckConnectivity();
            Debug.Log($"[GameManager] Initial connectivity: {connected}");
            if (connected && _state == GameState.Playing)
                Win();
        }
    }

    void SyncConnectivityCheckpoints()
    {
        if (_connectivityVisualizer == null || _puzzleInitializer == null) return;
        var ps = _puzzleInitializer.startIndex;
        var pg = _puzzleInitializer.goalIndex;
        var vs = _connectivityVisualizer.startIndex;
        var vg = _connectivityVisualizer.goalIndex;
        if (!vs.Equals(ps) || !vg.Equals(pg))
        {
            _connectivityVisualizer.SetCheckPoints(ps, pg);
            Debug.Log($"[GameManager] Synced checkpoints: {ps} → {pg}");
        }
    }

    void OnConnectivityChanged(bool isConnected)
    {
        Debug.Log($"[GameManager] Connectivity: {isConnected}, state: {_state}");
        if (isConnected && _state == GameState.Playing)
            Win();
    }

    void OnTilePlaced(TileBase tile, GridIndex idx)
    {
        _moveCount++;
        Debug.Log($"[GameManager] Move {_moveCount}: {tile.name} → {idx}");
    }

    void OnTileRemoved(TileBase tile, GridIndex idx)
    {
        _moveCount++;
        Debug.Log($"[GameManager] Move {_moveCount}: {tile.name} ← {idx}");
    }

    void Win()
    {
        _state = GameState.Won;
        Debug.Log($"[GameManager] PUZZLE SOLVED in {_moveCount} moves!");
        if (_winFeedback != null)
            _winFeedback.PlayWinEffects();
    }

    public void RestartPuzzle()
    {
        _state = GameState.Idle;
        _moveCount = 0;
        Debug.Log("[GameManager] Restarting...");
        if (_winFeedback != null)
            _winFeedback.HideImmediately();
        if (_puzzleInitializer != null)
            _puzzleInitializer.ReinitializePuzzle();
        StartCoroutine(EnterPlayingDelayed());
    }
}
