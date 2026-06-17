using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Parents inserted tile to host. Tiles move as one group.
/// DetachOnGrab handles physics disable during grab.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
    XRSocketInteractor socket;
    XRInteractionManager im;
    float _lastExitTime;
    Transform _lastInserted;

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
        var hostRb = host.GetComponentInParent<Rigidbody>();
        var insRb = inserted.GetComponent<Rigidbody>();
        if (!hostRb || !insRb) return;

        // Cooldown
        if (inserted == _lastInserted && Time.time - _lastExitTime < 0.3f) return;

        // Skip if merge-compatible
        var myTile = host.GetComponentInParent<TileBase>();
        var otherTile = inserted.GetComponent<TileBase>();
        if (myTile != null && otherTile != null)
        {
            var a = myTile.GetCellState(); var b = otherTile.GetCellState();
            if (a.type == CellStateType.LogicOnly && b.type == CellStateType.PureValue) return;
            if (a.type == CellStateType.ValueWithLogic && b.type == CellStateType.PureValue) return;
            if (b.type == CellStateType.ValueWithLogic && a.type == CellStateType.PureValue) return;
        }

        // Parent
        inserted.SetParent(host.parent ?? host, true);
        StartCoroutine(ExitSocketDeferred(interactable));
    }

    IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
    {
        yield return null;
        if (socket == null || !socket.hasSelection) yield break;
        var comp = interactable as Component;
        if (comp == null || !comp.gameObject) yield break;
        _lastInserted = comp.transform;
        _lastExitTime = Time.time;
        im.SelectExit((IXRSelectInteractor)socket, interactable);
    }
}
