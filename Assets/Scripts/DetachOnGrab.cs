// Scripts/DetachOnGrab.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// On grab: disables Rigidbody+Collider on all connected children → moves as one.
/// On release: re-enables them and copies velocity.
/// Children detach on their own grab.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class DetachOnGrab : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Rigidbody _rb;
    private List<Rigidbody> _childRbs = new List<Rigidbody>();
    private List<Collider> _childCols = new List<Collider>();

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
        // Detach from parent (someone else grabbed us)
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        // Disable physics on all DIRECT children (connected tiles parented to us)
        _childRbs.Clear();
        _childCols.Clear();

        foreach (Transform child in transform)
        {
            if (child == transform) continue;
            var crb = child.GetComponent<Rigidbody>();
            if (crb != null && !crb.isKinematic)
            {
                crb.isKinematic = false;
                crb.detectCollisions = false;
                crb.useGravity = false;
                _childRbs.Add(crb);
            }
            foreach (var col in child.GetComponentsInChildren<Collider>())
            {
                if (col.enabled) { col.enabled = false; _childCols.Add(col); }
            }
        }

        // Keep our own RB active and non-kinematic so XR grab can move it
        _rb.isKinematic = false;
        _rb.detectCollisions = true;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Re-enable child physics
        foreach (var crb in _childRbs)
        {
            if (crb == null) continue;
            crb.detectCollisions = true;
            crb.useGravity = false; // tiles float
            crb.linearVelocity = _rb.linearVelocity;
            crb.angularVelocity = _rb.angularVelocity;
        }

        foreach (var col in _childCols)
            if (col != null) col.enabled = true;

        _childRbs.Clear();
        _childCols.Clear();
    }
}
