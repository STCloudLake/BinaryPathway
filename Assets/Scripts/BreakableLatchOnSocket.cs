using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// On socket select: parents the inserted tile to the host.
/// On grab of a child tile: detaches it from parent.
/// No physics joints — uses Transform parenting for clean multi-tile handling.
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

    void OnDestroy()
    {
        socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactable = args.interactableObject as XRGrabInteractable;
        if (!interactable) return;

        var inserted = (interactable as Component).transform;
        var host = socket.transform;
        var hostRb = host.GetComponentInParent<Rigidbody>();
        var insRb = inserted.GetComponent<Rigidbody>();
        if (!hostRb || !insRb) return;

        // Skip if already parented to same host
        if (inserted.parent == host.parent) return;

        // Skip if merge-compatible
        var myTile = host.GetComponentInParent<TileBase>();
        var otherTile = inserted.GetComponent<TileBase>();
        if (myTile != null && otherTile != null)
        {
            var myCs = myTile.GetCellState();
            var otherCs = otherTile.GetCellState();
            if (IsMergeCompatible(myCs, otherCs)) return;
        }

        // Snap position
        var at = socket.attachTransform;
        if (at != null)
        {
            insRb.MovePosition(at.position);
            insRb.MoveRotation(at.rotation);
        }

        // Parent: inserted tile becomes child of host tile
        inserted.SetParent(host.parent, true);

        // Release socket
        StartCoroutine(ExitSocketDeferred(interactable));
    }

    bool IsMergeCompatible(CellState a, CellState b)
    {
        if (a.type == CellStateType.LogicOnly && b.type == CellStateType.PureValue) return true;
        if (a.type == CellStateType.ValueWithLogic && b.type == CellStateType.PureValue) return true;
        if (b.type == CellStateType.ValueWithLogic && a.type == CellStateType.PureValue) return true;
        return false;
    }

    IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
    {
        yield return null;
        if (socket != null && socket.hasSelection && interactable != null)
        {
            var comp = interactable as Component;
            if (comp == null || comp.gameObject == null) yield break;
            im.SelectExit((IXRSelectInteractor)socket, interactable);
        }
    }
}
