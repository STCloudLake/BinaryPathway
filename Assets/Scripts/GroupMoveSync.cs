// Scripts/GroupMoveSync.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Static-group-based movement sync. No Transform parenting.
/// When grabbed, all tiles in the same group follow via direct Transform set.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class GroupMoveSync : MonoBehaviour
{
    // Static group registry: groupId → members
    private static Dictionary<int, HashSet<GroupMoveSync>> _groups = new Dictionary<int, HashSet<GroupMoveSync>>();
    private static int _nextGroupId;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Rigidbody _rb;
    private int _groupId = -1;
    private Vector3[] _offsets;
    private Rigidbody[] _followerRbs;
    private Transform[] _followers;

    public int GroupId => _groupId;

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
        LeaveGroup();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Save group members BEFORE leaving
        var members = new List<GroupMoveSync>();
        if (_groupId >= 0 && _groups.TryGetValue(_groupId, out var set))
        {
            foreach (var m in set)
                if (m != this && m != null)
                    members.Add(m);
        }

        // Leave group now (I'm pulled away, group dissolves if only 1 left)
        LeaveGroup();

        // Re-group remaining members if they're still connected (they keep groupId)
        if (members.Count > 0)
        {
            // They stay in their group; we've left it
        }

        if (members.Count == 0) return;

        _followers = new Transform[members.Count];
        _followerRbs = new Rigidbody[members.Count];
        _offsets = new Vector3[members.Count];
        for (int i = 0; i < members.Count; i++)
        {
            _followers[i] = members[i].transform;
            _followerRbs[i] = members[i].GetComponent<Rigidbody>();
            _offsets[i] = _followers[i].position - transform.position;
            if (_followerRbs[i] != null) _followerRbs[i].isKinematic = true;
        }
    }

    void Update()
    {
        if (!_grab.isSelected || _followers == null) return;
        for (int i = 0; i < _followers.Length; i++)
        {
            if (_followers[i] == null) continue;
            _followers[i].position = transform.position + _offsets[i];
            _followers[i].rotation = transform.rotation;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (_followerRbs == null) return;
        for (int i = 0; i < _followerRbs.Length; i++)
        {
            if (_followerRbs[i] == null) continue;
            _followerRbs[i].isKinematic = false;
            _followerRbs[i].linearVelocity = _rb.linearVelocity;
            _followerRbs[i].angularVelocity = _rb.angularVelocity;
        }
        _followers = null;
        _followerRbs = null;
        _offsets = null;
    }

    /// <summary>Called by BreakableLatchOnSocket to add a tile to this group.</summary>
    public static void JoinGroup(GroupMoveSync a, GroupMoveSync b)
    {
        if (a == null || b == null || a == b) return;
        // Remove both from existing groups
        a.LeaveGroup();
        b.LeaveGroup();
        // Create new shared group
        int id = _nextGroupId++;
        var set = new HashSet<GroupMoveSync> { a, b };
        _groups[id] = set;
        a._groupId = id;
        b._groupId = id;
    }

    public void LeaveGroup()
    {
        if (_groupId < 0) return;
        if (_groups.TryGetValue(_groupId, out var set))
        {
            set.Remove(this);
            if (set.Count <= 1)
            {
                foreach (var m in set) m._groupId = -1;
                _groups.Remove(_groupId);
            }
        }
        _groupId = -1;
    }
}
