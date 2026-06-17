// Scripts/DetachOnGrab.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// When this tile is grabbed, detach it from its parent.
/// This allows individual tiles in a parented chain to be pulled apart.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class DetachOnGrab : MonoBehaviour
{
    private XRGrabInteractable _grab;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Detach from parent to allow independent movement
        if (transform.parent != null)
        {
            Debug.Log($"[DetachOnGrab] {name} detached from {transform.parent.name}");
            transform.SetParent(null, true);
        }
    }
}
