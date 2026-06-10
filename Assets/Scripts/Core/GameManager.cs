using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central game state controller for BinaryPathway.
/// Manages game state machine (Idle → Playing → Won),
/// subscribes to connectivity and tile placement events,
/// and orchestrates win condition and feedback.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Idle, Playing, Won }

    [Header("References")]
    public GridContainer gridContainer;
    public ConnectivityVisualizer connectivityVisualizer;
    public PuzzleInitializer puzzleInitializer;
    public WinFeedbackController winFeedback;
    public TMPro.TextMeshPro movesText;

    [Header("State")]
    [SerializeField] private GameState _currentState = GameState.Idle;
    public GameState CurrentState => _currentState;

    [Header("Events")]
    public UnityEvent<GameState> onStateChanged;
    public UnityEvent onPuzzleWon;
    public UnityEvent<int> onMoveCountChanged;

    private int _moveCount;

    void Awake()
    {
        if (gridContainer == null)
            gridContainer = FindFirstObjectByType<GridContainer>();
        if (connectivityVisualizer == null)
            connectivityVisualizer = FindFirstObjectByType<ConnectivityVisualizer>();
        if (puzzleInitializer == null)
            puzzleInitializer = FindFirstObjectByType<PuzzleInitializer>();
        if (winFeedback == null)
            winFeedback = GetComponent<WinFeedbackController>();
    }

    void OnEnable()
    {
        if (connectivityVisualizer != null)
            connectivityVisualizer.OnConnectivityChanged += HandleConnectivityChanged;

        if (gridContainer != null)
        {
            gridContainer.OnTilePlaced += HandleTilePlaced;
            gridContainer.OnTileRemoved += HandleTileRemoved;
        }
    }

    void OnDisable()
    {
        if (connectivityVisualizer != null)
            connectivityVisualizer.OnConnectivityChanged -= HandleConnectivityChanged;

        if (gridContainer != null)
        {
            gridContainer.OnTilePlaced -= HandleTilePlaced;
            gridContainer.OnTileRemoved -= HandleTileRemoved;
        }
    }

    void Start()
    {
        StartCoroutine(DelayedSetPlaying());
    }

    IEnumerator DelayedSetPlaying()
    {
        yield return null;
        SetState(GameState.Playing);
    }

    void HandleConnectivityChanged(bool connected)
    {
        if (connected && _currentState == GameState.Playing)
        {
            Debug.Log("[GameManager] Path connected! Puzzle won.");
            SetState(GameState.Won);
        }
    }

    void HandleTilePlaced(TileBase tile, GridIndex index)
    {
    _moveCount++;
    UpdateMovesDisplay();
    }

	void HandleTileRemoved(TileBase tile, GridIndex index)
    {
     _moveCount++;
    UpdateMovesDisplay();
    }

    void SetState(GameState newState)
    {
    if (_currentState == newState) return;
    _currentState = newState;
    onStateChanged?.Invoke(newState);

    if (newState == GameState.Won)
    {
    onPuzzleWon?.Invoke();
    if (winFeedback != null)
    winFeedback.PlayWinEffects();
    }
		else if (newState == GameState.Playing)
		{
			// Reset move count after puzzle initialization completes
			_moveCount = 0;
			UpdateMovesDisplay();
		}
    }

    void UpdateMovesDisplay()
    {
        if (movesText != null)
            movesText.text = $"Moves: {_moveCount}";
        onMoveCountChanged?.Invoke(_moveCount);
    }

    public void RestartPuzzle()
    {
        _moveCount = 0;
        UpdateMovesDisplay();
        if (puzzleInitializer != null)
            puzzleInitializer.ReinitializePuzzle();
        StartCoroutine(DelayedSetPlaying());
    }
}
