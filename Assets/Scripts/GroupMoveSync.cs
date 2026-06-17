// Scripts/GroupMoveSync.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// When grabbed, syncs position of all tiles in the same parent to move as one.
/// Uses pure Transform manipulation — no physics, zero jitter.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class GroupMoveSync : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Rigidbody _rb;
    private Vector3[] _offsets;
    private Transform[] _followers;

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
        // Detach from any parent (we use our own sync, not parenting)
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        // Find all sibling tiles (same parent = same group)
        var siblings = new List<Transform>();
        var parent = transform.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                if (child != transform && child.GetComponent<GroupMoveSync>() != null)
                    siblings.Add(child);
            }
        }

        // Save relative offsets
        _followers = siblings.ToArray();
        _offsets = new Vector3[_followers.Length];
        for (int i = 0; i < _followers.Length; i++)
        {
            _offsets[i] = _followers[i].position - transform.position;
            // Freeze follower physics
            var frb = _followers[i].GetComponent<Rigidbody>();
            if (frb != null) frb.isKinematic = true;
        }
    }

    void Update()
    {
        // Sync followers while grabbed
        if (_grab.isSelected && _followers != null)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;
                _followers[i].position = transform.position + _offsets[i];
                _followers[i].rotation = transform.rotation;
            }
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (_followers == null) return;

        // Restore follower physics
        var myVel = _rb.linearVelocity;
        var myAngVel = _rb.angularVelocity;
        for (int i = 0; i < _followers.Length; i++)
        {
            if (_followers[i] == null) continue;
            var frb = _followers[i].GetComponent<Rigidbody>();
            if (frb != null)
            {
                frb.isKinematic = false;
                frb.linearVelocity = myVel;
                frb.angularVelocity = myAngVel;
            }
        }
        _followers = null;
        _offsets = null;
    }
}
