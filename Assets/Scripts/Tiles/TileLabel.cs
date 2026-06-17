// Scripts/Tiles/TileLabel.cs
using UnityEngine;

/// <summary>
/// Logs tile CellState changes to console. Visual state is handled by LogicTile.ApplyLook() emission.
/// Creates no GameObjects (safe for Quest IL2CPP).
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    private TileBase _tile;
    private CellState _lastState;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        _lastState = _tile.GetCellState();
        Debug.Log($"[TileLabel] {name} init: {_lastState}");
    }

    void Update()
    {
        if (_tile == null) return;
        var currentState = _tile.GetCellState();
        if (!currentState.Equals(_lastState))
        {
            _lastState = currentState;
            Debug.Log($"[TileLabel] {name}: {currentState}");
        }
    }
}
