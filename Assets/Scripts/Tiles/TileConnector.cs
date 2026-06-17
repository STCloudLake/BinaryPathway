// Scripts/Tiles/TileConnector.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manages tile-to-tile connections via face sockets.
/// Compatible cells MERGE (value tile destroyed, logic tile absorbs it).
/// Incompatible cells physical-snap only (joint).
/// </summary>
public class TileConnector : MonoBehaviour
{
    [Header("Face Sockets (auto-detected)")]
    public XRSocketInteractor[] faceSockets = new XRSocketInteractor[6];

    [Header("State")]
    public Dictionary<int, TileBase> connectedTiles = new Dictionary<int, TileBase>();

    public event Action<TileBase, int, int> OnCellsMerged;
    public event Action<TileBase, int> OnTileConnected;
    public event Action<TileBase, int> OnTileDisconnected;

    private TileBase _myTile;
    private BreakableLinkNode _linkNode;

    void Awake()
    {
        _myTile = GetComponent<TileBase>();
        _linkNode = GetComponent<BreakableLinkNode>();

        // Auto-detect face sockets
        bool allNull = true;
        if (faceSockets != null)
            foreach (var s in faceSockets) if (s != null) { allNull = false; break; }
        if (faceSockets == null || allNull)
            faceSockets = GetComponentsInChildren<XRSocketInteractor>();

        foreach (var socket in faceSockets)
            if (socket != null) socket.selectEntered.AddListener(OnSocketSelectEntered);
    }

    void OnDestroy()
    {
        foreach (var socket in faceSockets)
            if (socket != null) socket.selectEntered.RemoveListener(OnSocketSelectEntered);
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

        bool merged = TryMerge(myState, otherState, otherTile, faceIndex);
        if (!merged)
        {
            connectedTiles[faceIndex] = otherTile;
            OnTileConnected?.Invoke(otherTile, faceIndex);
            Debug.Log($"[TileConnector] SNAP: {name} <-> {otherTile.name}");
        }
    }

    bool TryMerge(CellState my, CellState other, TileBase otherTile, int faceIndex)
    {
        // Rule: ValueWithLogic + PureValue → compute and merge
        if (my.type == CellStateType.ValueWithLogic && other.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(my.value, my.logic, other.value);
            ApplyCellState(CellState.PureValue(result));
            AbsorbTile(otherTile);
            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE: {my.value} {my.logic} {other.value} = {result}, absorbed {otherTile.name}");
            return true;
        }

        // Rule: LogicOnly + PureValue → capture and merge
        if (my.type == CellStateType.LogicOnly && other.type == CellStateType.PureValue)
        {
            ApplyCellState(CellState.ValueWithLogic(other.value, my.logic));
            AbsorbTile(otherTile);
            Debug.Log($"[TileConnector] CAPTURE: {my.logic} captured {other.value} from {otherTile.name}");
            return true;
        }

        // Symmetric: other ValueWithLogic + my PureValue → compute and merge
        if (other.type == CellStateType.ValueWithLogic && my.type == CellStateType.PureValue)
        {
            int result = CellState.Compute(other.value, other.logic, my.value);
            // Let the other tile handle the merge (its OnSocketSelectEntered will also fire)
            // Just update my state
            ApplyCellState(CellState.PureValue(result));
            OnCellsMerged?.Invoke(otherTile, faceIndex, result);
            Debug.Log($"[TileConnector] MERGE (self): {other.value} {other.logic} {my.value} = {result}");
            return true;
        }

        // Incompatible → physical snap only (joint via BreakableLatchOnSocket)
        return false;
    }

    /// <summary>
    /// Destroy the value tile and its joints (physically absorbed into this tile).
    /// </summary>
    void AbsorbTile(TileBase tile)
    {
        if (tile == null) return;
        var go = tile.gameObject;

        // Remove all joints (from BreakableLatchOnSocket)
        foreach (var j in go.GetComponents<Joint>())
            Destroy(j);

        // Remove Rigidbody to prevent physics artifacts
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Remove Colliders to prevent interference
        foreach (var c in go.GetComponents<Collider>())
            Destroy(c);

        // Hide immediately
        go.SetActive(false);

        // Destroy after a frame to let physics settle
        Destroy(go, 0.05f);
    }

    void ApplyCellState(CellState newState)
    {
        if (_myTile is LogicTile logicTile)
            logicTile.CellState = newState;
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
