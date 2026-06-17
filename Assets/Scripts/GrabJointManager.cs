// Scripts/GrabJointManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// One-hand grab → joints hold tiles together.
/// Two-hand grab on connected tiles → joint breaks → tiles separate.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabJointManager : MonoBehaviour
{
    private static HashSet<GrabJointManager> _grabbed = new HashSet<GrabJointManager>();
    private XRGrabInteractable _grab;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(_ => _grabbed.Add(this));
        _grab.selectExited.AddListener(_ => _grabbed.Remove(this));
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveAllListeners();
        _grab.selectExited.RemoveAllListeners();
        _grabbed.Remove(this);
    }

    void Update()
    {
        if (_grabbed.Count < 2 || !_grabbed.Contains(this)) return;

        foreach (var j in GetComponents<Joint>())
        {
            if (j == null || j.connectedBody == null) continue;
            var other = j.connectedBody.GetComponent<GrabJointManager>();
            if (other != null && _grabbed.Contains(other))
            {
                Debug.Log($"[GrabJoint] Dual grab: {name} + {other.name} → break!");
                Destroy(j);
            }
        }
    }
}
