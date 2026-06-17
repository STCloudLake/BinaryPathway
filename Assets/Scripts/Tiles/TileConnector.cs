// Scripts/Tiles/TileConnector.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manages logical tile-to-tile connections via face sockets using CellState rules.
/// Compatible cells merge-compute; incompatible cells physical-snap only.
/// </summary>
public class TileConnector : MonoBehaviour
{
    [Header("Face Sockets (auto-detected)")]
    public XRSocketInteractor[] faceSockets = new XRSocketInteractor[6];

    [Header("State")]
    public Dictionary<int, TileBase> connectedTiles = new Dictionary<int, TileBase>();

    /// <summary>Fired when two cells complete a logical merge computation.</summary>
    public event Action<TileBase, int, int> OnCellsMerged; // (otherTile, faceIndex, result)
    public event Action<TileBase, int> OnTileConnected;
    public event Action<TileBase, int> OnTileDisconnected;

    private TileBase _myTile;
    private BreakableLinkNode _linkNode;

    void Awake()
    {
        _myTile = GetComponent<TileBase>();
        _linkNode = GetComponent<BreakableLinkNode>();

        if (faceSockets == null || faceSockets.Length == 0)
            faceSockets = GetComponentsInChildren<XRSocketInteractor>();

        foreach (var socket in faceSockets)
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnSocketSelectEntered);
        }
    }

    void OnDestroy()
    {
        foreach (var socket in faceSockets)
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnSocketSelectEntered);
        }
    }

    void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        var otherGO = args.interactableObject.transform.gameObject;
        var otherTile = otherGO.GetComponent<TileBase>();
        if (otherTile == null) return;

        int faceIndex = GetFaceIndex(args.interactorObject as XRSocketInteractor);
        if (faceIndex < 0) return;

        CellState myState = _myTile.GetCellState();
        CellState otherState = otherTile.GetCellState();

        // Apply CellState compatibility rules
        bool merged = TryMerge(myState, otherState, otherTile, faceIndex);
        if (!merged)
        {
            // Incompatible — physical connection only
            connectedTiles[faceIndex] = otherTile;
            OnTileConnected?.Invoke(otherTile, faceIndex);
        }
    }

    /// <summary>
    /// Try to merge two cells according to the CellState compatibility table.
    /// Returns true if a merge was performed, false if only physical connection.
    /// </summary>
    bool TryMerge(CellState my, CellState other, TileBase otherTile, int faceIndex)
    {
        // Rule: ValueWithLogic + PureValue → merge compute
        if (my.type == CellStateType.ValueWithLogic && other.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(my.value, my.logic, other.value);
            ApplyCellState(CellState.PureValue(result));

            if (otherTile is LogicTile otherLogic)
                otherLogic.CellState = CellState.PureValue(result);

            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE: {my.value} {my.logic} {other.value} = {result}");
            return true;
        }

        // Rule: LogicOnly + PureValue → capture
        if (my.type == CellStateType.LogicOnly && other.type == CellStateType.PureValue)
        {
            ApplyCellState(CellState.ValueWithLogic(other.value, my.logic));
            Debug.Log($"[TileConnector] CAPTURE: {my.logic} captured value {other.value}");
            return true;
        }

        // Symmetric: other ValueWithLogic + my PureValue
        if (other.type == CellStateType.ValueWithLogic && my.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(other.value, other.logic, my.value);
            ApplyCellState(CellState.PureValue(result));

            if (otherTile is LogicTile otherLogic)
                otherLogic.CellState = CellState.PureValue(result);

            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE (reverse): {other.value} {other.logic} {my.value} = {result}");
            return true;
        }

        // Rule: PureValue + PureValue → no merge, just snap
        // Rule: ValueWithLogic + ValueWithLogic → no merge, just snap
        // Rule: LogicOnly + LogicOnly → no merge, just snap
        return false;
    }

    void ApplyCellState(CellState newState)
    {
        if (_myTile is TileToggle toggle)
        {
            // For ToggleTile, only pure values can be set
            if (newState.type == CellStateType.PureValue)
            {
                while (toggle.Value != newState.value) toggle.Toggle();
            }
            // ValueWithLogic/LogicOnly on ToggleTile: not applicable (ignore for now)
        }
        else if (_myTile is LogicTile logicTile)
        {
            logicTile.CellState = newState;
        }
        // Tile_0/Tile_1 are immutable — their CellState is always PureValue(0/1)
    }

    int GetFaceIndex(XRSocketInteractor socket)
    {
        for (int i = 0; i < faceSockets.Length; i++)
            if (faceSockets[i] == socket) return i;
        return -1;
    }

    public void OnJointBroken(int faceIndex)
    {
        if (connectedTiles.TryGetValue(faceIndex, out var tile))
        {
            OnTileDisconnected?.Invoke(tile, faceIndex);
            connectedTiles.Remove(faceIndex);
        }
    }

    public int ConnectionCount => connectedTiles.Count;
}
