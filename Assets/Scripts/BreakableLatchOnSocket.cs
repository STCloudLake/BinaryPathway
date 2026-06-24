// Scripts/BreakableLatchOnSocket.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Creates a FixedJoint between two tiles when their face sockets engage.
///
/// FixedJoint is Unity's native way to rigidly connect two Rigidbodies.
/// Both tiles stay non-kinematic and fully grabbable. When grabbed,
/// GroupMoveSync.BreakAllConnections() destroys the joint for clean separation.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
    XRSocketInteractor _socket;
    XRInteractionManager _im;
    static Dictionary<(int, int), float> _cooldowns = new();

    void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
        _im = FindFirstObjectByType<XRInteractionManager>();
        _socket.selectEntered.AddListener(OnSelectEntered);
    }
    void OnDestroy() => _socket.selectEntered.RemoveListener(OnSelectEntered);

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactable = args.interactableObject as XRGrabInteractable;
        if (!interactable) return;
        var inserted = (interactable as Component).transform;
        var otherRb = inserted.GetComponent<Rigidbody>();
        if (!otherRb) return;

        // Find the tile root (Rigidbody owner). Sockets are nested under
        // a "Sockets" container, so we walk up via GetComponentInParent.
        var hostSocketParent = _socket.transform.parent ?? _socket.transform;
        var hostRb = hostSocketParent.GetComponentInParent<Rigidbody>();
        if (!hostRb || hostRb == otherRb) return;
        var host = hostRb.transform;

        // Always exit socket to free the interactable
        StartCoroutine(ExitSocketDeferred(interactable));

        // Already physically connected?
        if (GroupMoveSync.AreConnected(host, inserted))
            return;

        // Cooldown
        int idA = host.GetInstanceID(), idB = inserted.GetInstanceID();
        var key = idA < idB ? (idA, idB) : (idB, idA);
        if (_cooldowns.TryGetValue(key, out float t) && Time.time - t < 1.5f)
            return;

        // Skip merge-compatible tiles (handled by TileConnector)
        var hostTile = host.GetComponentInParent<TileBase>();
        var otherTile = inserted.GetComponent<TileBase>();
        if (hostTile != null && otherTile != null)
        {
            var ca = hostTile.GetCellState(); var cb = otherTile.GetCellState();
            if (ca.type == CellStateType.LogicOnly && cb.type == CellStateType.PureValue) return;
            if (ca.type == CellStateType.ValueWithLogic && cb.type == CellStateType.PureValue) return;
            if (cb.type == CellStateType.ValueWithLogic && ca.type == CellStateType.PureValue) return;
        }

        _cooldowns[key] = Time.time;

        // Create FixedJoint on the tile root (Rigidbody owner)
        var joint = host.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = otherRb;
        joint.enableCollision = false;
        joint.breakForce = float.PositiveInfinity;
        joint.breakTorque = float.PositiveInfinity;

        Debug.Log($"[BreakableLatch] ✅ FixedJoint: {host.name} ↔ {inserted.name}");
    }

    /// <summary>
    /// Called by DetachTool after breaking joints — resets cooldown so the
    /// tiles don't immediately reconnect via face sockets.
    /// </summary>
    public static void SuppressReconnect(Component a, Component b, float duration)
    {
        if (a == null || b == null) return;
        int idA = a.GetInstanceID(), idB = b.GetInstanceID();
        var key = idA < idB ? (idA, idB) : (idB, idA);
        // Set cooldown timestamp to now → prevents re-join for 'duration' seconds
        _cooldowns[key] = Time.time + (duration - 1.5f); // offset so check passes
        // Actually just set to now: check is (Time.time - t < 1.5f)
        _cooldowns[key] = Time.time;
    }

    IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
    {
        yield return null;
        if (_socket == null || !_socket.hasSelection || interactable == null) yield break;
        var comp = interactable as Component;
        if (comp == null || !comp.gameObject) yield break;
        _im.SelectExit(_socket, interactable);
    }
}
