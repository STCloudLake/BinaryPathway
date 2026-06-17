// Scripts/DetachOnGrab.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// On grab: suspends child rigidbodies (no physics jitter) but keeps colliders active
/// (so second hand can still grab children). Children detach on their own grab.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DetachOnGrab : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Rigidbody _rb;
    private List<Rigidbody> _suspendedRbs = new List<Rigidbody>();

    void Awake()
    {
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        _grab.selectEntered.AddListener(OnGrabbed);
        _grab.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrabbed);
        _grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Detach from parent (second hand pulls me away)
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        // Re-enable my own physics (might have been disabled by parent's grab)
        _rb.isKinematic = false;
        _rb.detectCollisions = true;

        // Suspend child rigidbodies (keep colliders ON for raycasting)
        _suspendedRbs.Clear();
        foreach (Transform child in transform)
        {
            var crb = child.GetComponent<Rigidbody>();
            if (crb != null)
            {
                crb.isKinematic = true;      // No physics sim
                crb.detectCollisions = false; // No collision response
                // Collider stays ON → second hand can still raycast-grab it
                _suspendedRbs.Add(crb);
            }
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Restore suspended children
        foreach (var crb in _suspendedRbs)
        {
            if (crb == null) continue;
            crb.isKinematic = false;
            crb.detectCollisions = true;
            crb.linearVelocity = _rb.linearVelocity;
            crb.angularVelocity = _rb.angularVelocity;
        }
        _suspendedRbs.Clear();
    }
}
