using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Manages logical tile-to-tile connections via the six face sockets.
/// Listens to XRSocketInteractor events on each face, validates against
/// ConnectionRule, tracks connected tiles, and fires connection events.
/// </summary>
public class TileConnector : MonoBehaviour
{
    [Header("Connection Rule")]
    public ConnectionRule connectionRule;

    [Header("Face Sockets (auto-detected)")]
    public XRSocketInteractor[] faceSockets = new XRSocketInteractor[6];

    [Header("State")]
    public Dictionary<int, TileBase> connectedTiles = new Dictionary<int, TileBase>();

    // Events — fired when tiles logically connect/disconnect
    public event Action<TileBase, int> OnTileConnected;
    public event Action<TileBase, int> OnTileDisconnected;

    private TileBase _myTile;
    private BreakableLinkNode _linkNode;

    void Awake()
    {
        _myTile = GetComponent<TileBase>();
        _linkNode = GetComponent<BreakableLinkNode>();
        if (connectionRule == null)
            connectionRule = ScriptableObject.CreateInstance<ConnectionRule>();

        // Auto-detect face sockets from Sockets/ child
        if (faceSockets == null || faceSockets.Length == 0)
            faceSockets = GetComponentsInChildren<XRSocketInteractor>();

        // Subscribe to socket events
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
        var otherConnector = otherGO.GetComponent<TileConnector>();
        if (otherTile == null || otherConnector == null) return;

        int faceIndex = GetFaceIndex(args.interactorObject as XRSocketInteractor);
        if (faceIndex < 0) return;

        // Validate connection
        var myType = GetTileType(_myTile);
        var otherType = GetTileType(otherTile);

        if (connectionRule != null && !connectionRule.CanConnect(myType, otherType))
        {
            Debug.Log($"[TileConnector] Connection rejected: {myType}({_myTile.Value}) + {otherType}({otherTile.Value})");
            // TODO: visual/audio rejection feedback
            return;
        }

        // Apply logic operation if relevant
        if (connectionRule != null && otherType == TileType.Value)
        {
            ApplyLogicOperation(otherTile.Value);
        }

        // Track connection
        connectedTiles[faceIndex] = otherTile;
        OnTileConnected?.Invoke(otherTile, faceIndex);

        Debug.Log($"[TileConnector] Connected: {name}(face {faceIndex}) -> {otherTile.name} (myVal={_myTile.Value})");
    }

    void ApplyLogicOperation(int otherValue)
    {
        if (connectionRule == null) return;

        switch (connectionRule.valueOnLogicOperation)
        {
            case ConnectionRule.LogicOperation.SetValue:
                // Only applies to ToggleTile-like logic tiles
                if (_myTile is ToggleTile toggle)
                {
                    // Store the value by toggling to match
                    while (toggle.Value != otherValue) toggle.Toggle();
                }
                break;
            case ConnectionRule.LogicOperation.Toggle:
                if (_myTile is ToggleTile t) t.Toggle();
                break;
            case ConnectionRule.LogicOperation.And:
                if (_myTile is ToggleTile tAnd)
                {
                    int result = tAnd.Value & otherValue;
                    while (tAnd.Value != result) tAnd.Toggle();
                }
                break;
            case ConnectionRule.LogicOperation.Or:
                if (_myTile is ToggleTile tOr)
                {
                    int result = tOr.Value | otherValue;
                    while (tOr.Value != result) tOr.Toggle();
                }
                break;
            case ConnectionRule.LogicOperation.Xor:
                if (_myTile is ToggleTile tXor)
                {
                    int result = tXor.Value ^ otherValue;
                    while (tXor.Value != result) tXor.Toggle();
                }
                break;
        }
    }

    TileType GetTileType(TileBase tile)
    {
        if (tile is ToggleTile) return TileType.Logic;
        return TileType.Value;
    }

    int GetFaceIndex(XRSocketInteractor socket)
    {
        for (int i = 0; i < faceSockets.Length; i++)
            if (faceSockets[i] == socket) return i;
        return -1;
    }

    /// <summary>
    /// Called by BreakableLinkNode when a joint breaks.
    /// </summary>
    public void OnJointBroken(int faceIndex)
    {
        if (connectedTiles.TryGetValue(faceIndex, out var tile))
        {
            OnTileDisconnected?.Invoke(tile, faceIndex);
            connectedTiles.Remove(faceIndex);
            Debug.Log($"[TileConnector] Disconnected: face {faceIndex} from {tile.name}");
        }
    }

    public int ConnectionCount => connectedTiles.Count;

    public bool HasFreeFace => connectionRule == null ||
        ConnectionCount < connectionRule.maxTotalConnections;
}
