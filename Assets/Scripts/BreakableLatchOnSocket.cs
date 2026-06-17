using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Groups nearby tiles. Has per-pair cooldown to prevent socket loop jitter.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
    XRSocketInteractor socket;
    XRInteractionManager im;
    static Dictionary<(int, int), float> _cooldowns = new Dictionary<(int, int), float>();

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        im = FindFirstObjectByType<XRInteractionManager>();
        socket.selectEntered.AddListener(OnSelectEntered);
    }
    void OnDestroy() => socket.selectEntered.RemoveListener(OnSelectEntered);

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactable = args.interactableObject as XRGrabInteractable;
        if (!interactable) return;
        var inserted = (interactable as Component).transform;
        var insRb = inserted.GetComponent<Rigidbody>();
        if (!insRb) return;

        var host = socket.transform;
        var hoseParent = host.parent ?? host;
        var mySync = hostParent.GetComponent<GroupMoveSync>();
        var otherSync = inserted.GetComponent<GroupMoveSync>();
        if (mySync == null || otherSync == null) return;

        // Already in same group? Skip
        if (mySync.GroupId >= 0 && otherSync.GroupId >= 0 && mySync.GroupId == otherSync.GroupId)
            return;

        // Cooldown per (myInstance, otherInstance) pair
        int a = mySync.GetInstanceID(), b = otherSync.GetInstanceID();
        var key = a < b ? (a, b) : (b, a);
        if (_cooldowns.TryGetValue(key, out float t) && Time.time - t < 1.5f)
            return;

        // Skip if merge-compatible
        var myTile = host.GetComponentInParent<TileBase>();
        var otherTile = inserted.GetComponent<TileBase>();
        if (myTile != null && otherTile != null)
        {
            var ca = myTile.GetCellState(); var cb = otherTile.GetCellState();
            if (ca.type == CellStateType.LogicOnly && cb.type == CellStateType.PureValue) return;
            if (ca.type == CellStateType.ValueWithLogic && cb.type == CellStateType.PureValue) return;
            if (cb.type == CellStateType.ValueWithLogic && ca.type == CellStateType.PureValue) return;
        }

        _cooldowns[key] = Time.time;
        GroupMoveSync.JoinGroup(mySync, otherSync);
        StartCoroutine(ExitSocketDeferred(interactable));
    }

    IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
    {
        yield return null;
        if (socket == null || !socket.hasSelection || interactable == null) yield break;
        var comp = interactable as Component;
        if (comp == null || !comp.gameObject) yield break;
        im.SelectExit((IXRSelectInteractor)socket, interactable);
    }
}
