using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// On face socket select: just releases the socket immediately.
/// No joint, no parenting — tiles remain independent.
/// Logical merging is handled by TileConnector.
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

        // Skip if merge-compatible (TileConnector handles absorption)
        var host = socket.transform;
        var myTile = host.GetComponentInParent<TileBase>();
        var otherTile = (interactable as Component).GetComponent<TileBase>();
        if (myTile != null && otherTile != null)
        {
            var myCs = myTile.GetCellState();
            var otherCs = otherTile.GetCellState();
            if (IsMergeCompatible(myCs, otherCs)) return;
        }

        // Just release the socket — no physical connection
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
