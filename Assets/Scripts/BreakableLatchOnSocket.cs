using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Parents inserted tile to host's parent to form a group.
/// GroupMoveSync handles smooth group movement via Transform sync (no physics).
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
    XRSocketInteractor socket;
    XRInteractionManager im;

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
        var host = socket.transform;
        var hostParent = host.parent ?? host;
        var insRb = inserted.GetComponent<Rigidbody>();
        if (!insRb) return;

        // Skip if already in same group
        if (inserted.parent == hostParent) return;

        // Skip if merge-compatible (TileConnector handles)
        var myTile = host.GetComponentInParent<TileBase>();
        var otherTile = inserted.GetComponent<TileBase>();
        if (myTile != null && otherTile != null)
        {
            var a = myTile.GetCellState(); var b = otherTile.GetCellState();
            if (a.type == CellStateType.LogicOnly && b.type == CellStateType.PureValue) return;
            if (a.type == CellStateType.ValueWithLogic && b.type == CellStateType.PureValue) return;
            if (b.type == CellStateType.ValueWithLogic && a.type == CellStateType.PureValue) return;
        }

        // Join same movement group (no parenting)
        var mySync = hostParent.GetComponent<GroupMoveSync>();
        var otherSync = inserted.GetComponent<GroupMoveSync>();
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
