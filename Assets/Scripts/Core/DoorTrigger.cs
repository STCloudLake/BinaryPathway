using UnityEngine;

/// <summary>
/// Opens a door when the player enters a proximity zone.
/// Uses distance checking (more reliable than OnTriggerEnter with ISDK character setup).
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    [Header("Door")]
    public string doorName = "door_3";
    public Animator doorAnimator;
    public bool openDoor = true;

    [Header("Proximity")]
    public float triggerRadius = 2.5f;

    private Transform _playerHead;
    private bool _hasOpened;

    void Start()
    {
        // Find door animator (only parent has the controller)
        if (doorAnimator == null)
        {
            var door = GameObject.Find(doorName);
            if (door != null)
                doorAnimator = door.GetComponent<Animator>();
        }

        // Find player head for distance checks
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null) _playerHead = centerEye.transform;

        // Also enable trigger collider as fallback
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (_hasOpened || _playerHead == null) return;

        float dist = Vector3.Distance(_playerHead.position, transform.position);
        if (dist < triggerRadius)
        {
            _hasOpened = true;
            SetDoor(openDoor);
            Debug.Log($"[DoorTrigger] Player within {triggerRadius}m — setting {doorName} character_nearby={openDoor}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Fallback — fires for any collider-based detection
        if (_hasOpened) return;
        if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("Camera"))
        {
            _hasOpened = true;
            SetDoor(openDoor);
            Debug.Log($"[DoorTrigger] Collider trigger — opening {doorName}");
        }
    }

    void SetDoor(bool open)
    {
        if (doorAnimator != null)
            doorAnimator.SetBool("character_nearby", open);
    }
}
