// Scripts/GroupMoveSync.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Minimal component. Sets selectMode=Multiple so socket selection doesn't
/// block hand grab. Connection/disconnection is handled by FixedJoint
/// (created by BreakableLatchOnSocket, broken by DetachTool).
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GroupMoveSync : MonoBehaviour
{
    void Awake()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectMode = InteractableSelectMode.Multiple;
    }

    /// <summary>True if two tiles are connected via a Joint.</summary>
    public static bool AreConnected(Component a, Component b)
    {
        if (a == null || b == null || a == b) return false;
        var rbA = a.GetComponent<Rigidbody>();
        var rbB = b.GetComponent<Rigidbody>();
        if (rbA == null || rbB == null) return false;
        foreach (var j in a.GetComponents<Joint>())
            if (j.connectedBody == rbB) return true;
        foreach (var j in b.GetComponents<Joint>())
            if (j.connectedBody == rbA) return true;
        return false;
    }
}
