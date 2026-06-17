using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
    public float breakForce = 25f;
    public float breakTorque = 25f;
    public bool useConfigurable = true;
    public bool jointEnableCollision = false;
    public float projectionDistance = 0.005f;
    public float projectionAngle = 1f;

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

    // Prevent immediate re-selection after release
    private float _lastExitTime;
    private GameObject _lastInserted;

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var interactable = args.interactableObject as XRGrabInteractable;
        if (!interactable) return;

        var inserted = (interactable as Component).transform;
        var host = socket.transform;
        var hostRb = host.GetComponentInParent<Rigidbody>();
        var insRb = inserted.GetComponent<Rigidbody>();
        if (!hostRb || !insRb) return;

        // Skip if already connected or just released (prevent re-selection loop)
        if (HasJointTo(inserted.gameObject, hostRb)) return;
        if (inserted.gameObject == _lastInserted && Time.time - _lastExitTime < 0.5f) return;

        // Snap to attach point
        var at = socket.attachTransform;
        if (at != null)
        {
            insRb.MovePosition(at.position);
            insRb.MoveRotation(at.rotation);
        }

        // Create breakable joint
        if (useConfigurable)
        {
            var j = inserted.gameObject.AddComponent<ConfigurableJoint>();
            j.connectedBody = hostRb;
            j.autoConfigureConnectedAnchor = true;
            j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
            j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Free;
            j.breakForce = breakForce;
            j.breakTorque = breakTorque;
            j.enableCollision = jointEnableCollision;
            j.enablePreprocessing = true;
            j.projectionMode = JointProjectionMode.PositionAndRotation;
            j.projectionDistance = projectionDistance;
            j.projectionAngle = projectionAngle;
        }
        else
        {
            var j = inserted.gameObject.AddComponent<FixedJoint>();
            j.connectedBody = hostRb;
            j.breakForce = breakForce;
            j.breakTorque = breakTorque;
            j.enableCollision = jointEnableCollision;
            j.enablePreprocessing = true;
        }

        // Release socket selection (deferred to avoid XRI re-entrancy)
        StartCoroutine(ExitSocketDeferred(interactable));

        // Stabilize physics
        BumpSolverIterations(insRb, 14, 14);
        BumpSolverIterations(hostRb, 14, 14);
    }

    /// <summary>
    /// Check if the target object already has a joint connected to the same host.
    /// Prevents duplicate joint creation.
    /// </summary>
    bool HasJointTo(GameObject target, Rigidbody hostRb)
    {
        var joints = target.GetComponents<Joint>();
        foreach (var j in joints)
        {
            if (j != null && j.connectedBody == hostRb)
                return true;
        }
        return false;
    }

    IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
    {
        yield return null;
        if (socket != null && socket.hasSelection)
        {
            _lastInserted = ((interactable as Component)?.gameObject);
            _lastExitTime = Time.time;
            im.SelectExit((IXRSelectInteractor)socket, interactable);
        }
    }

    static void BumpSolverIterations(Rigidbody rb, int it, int itVel)
    {
#if UNITY_2021_3_OR_NEWER
        if (!rb) return;
        rb.solverIterations = Mathf.Max(rb.solverIterations, it);
        rb.solverVelocityIterations = Mathf.Max(rb.solverVelocityIterations, itVel);
#endif
    }
}
