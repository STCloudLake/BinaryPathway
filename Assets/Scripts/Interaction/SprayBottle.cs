// Scripts/Interaction/SprayBottle.cs
using UnityEngine;

public class SprayBottle : MonoBehaviour
{
    [Min(1)] public int maxUses = 99;
    public float sprayRange = 5f;
    public LayerMask sprayLayerMask = ~0;

    [SerializeField] private int _remainingUses;
    private bool _triggerWasDown;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake() { _remainingUses = maxUses; }

    void Update()
    {
        if (IsEmpty) return;

        bool trigger = false;

        // Quest: controller index trigger
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        // Editor fallbacks
        trigger |= Input.GetKeyDown(KeyCode.F);
        trigger |= Input.GetMouseButtonDown(1);

        if (trigger)
        {
            Debug.Log($"[SprayBottle] TRIGGER, uses={_remainingUses}");
            PerformSpray();
        }
    }

    public void PerformSpray()
    {
        if (IsEmpty) return;

        // Try from bottle nozzle
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Vector3 dir = transform.forward;

        // Use SphereCast for wider detection
        bool hit = Physics.SphereCast(origin, 0.05f, dir, out RaycastHit hitInfo, sprayRange, sprayLayerMask);

        // Fallback: ray from center eye
        if (!hit)
        {
            var cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                hit = Physics.Raycast(cam.transform.position, cam.transform.forward, out hitInfo, sprayRange, sprayLayerMask);
                Debug.Log("[SprayBottle] Camera fallback hit=" + hit);
            }
        }

        if (hit)
        {
            Debug.Log($"[SprayBottle] Hit: {hitInfo.collider.name} path={hitInfo.collider.transform.GetPath()}");
            var tile = hitInfo.collider.GetComponentInParent<TileBase>();
            Debug.Log($"[SprayBottle] TileBase: {(tile != null ? tile.GetType().Name + " " + tile.GetCellState() : "NULL")}");

            if (tile is LogicTile lt)
            {
                lt.ApplyNot();
                Debug.Log($"[SprayBottle] NOT → {lt.CellState}");
                _remainingUses--; // Only consume on actual hit
            }
            else
            {
                Debug.Log($"[SprayBottle] Hit {hitInfo.collider.name} but no LogicTile found");
            }
        }
        else
        {
            Debug.Log($"[SprayBottle] MISS — origin={origin} dir={dir}");
        }
    }

    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[SprayBottle] Refilled: {uses}");
    }
}

public static class TransformExtensions
{
    public static string GetPath(this Transform t)
    {
        var path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
