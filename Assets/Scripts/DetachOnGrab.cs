// Scripts/DetachOnGrab.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// When grabbed: makes self + all connected children kinematic (no physics jitter).
/// When released: restores non-kinematic.
/// When a child is grabbed: detaches from parent.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class DetachOnGrab : MonoBehaviour
{
    private XRGrabInteractable _grab;
    private Rigidbody _rb;
    private bool _wasKinematic;
    private List<Rigidbody> _frozenChildren = new List<Rigidbody>();

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
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
        // Detach from parent
        if (transform.parent != null && transform.parent != transform.root)
        {
            transform.SetParent(null, true);
        }

        // Freeze all children (make kinematic to prevent physics jitter)
        _wasKinematic = _rb.isKinematic;
        _rb.isKinematic = true;

        _frozenChildren.Clear();
        foreach (var childRb in GetComponentsInChildren<Rigidbody>())
        {
            if (childRb != _rb && !childRb.isKinematic)
            {
                _frozenChildren.Add(childRb);
                childRb.isKinematic = true;
            }
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Restore
        _rb.isKinematic = _wasKinematic;
        foreach (var crb in _frozenChildren)
        {
            if (crb != null) crb.isKinematic = false;
        }
        _frozenChildren.Clear();
    }
}
