using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Minimal: releases socket immediately. No joints, no parenting.
/// TileConnector handles logical merging separately.
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
