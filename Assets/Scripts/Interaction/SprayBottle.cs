// Scripts/Interaction/SprayBottle.cs
using UnityEngine;

/// <summary>
/// NOT-spray bottle. F key or right-click sprays. Raycasts forward.
/// </summary>
public class SprayBottle : MonoBehaviour
{
    [Header("Spray Config")]
    [Min(1)] public int maxUses = 3;
    public float sprayRange = 5f;
    public LayerMask sprayLayerMask = ~0;

    [Header("State")]
    [SerializeField] private int _remainingUses;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake()
    {
        _remainingUses = maxUses;
    }

    void Update()
    {
        if (IsEmpty) return;

        // Multiple triggers for Editor testing
        bool trigger = Input.GetKeyDown(KeyCode.F)
                    || Input.GetKeyDown(KeyCode.Space)
                    || Input.GetMouseButtonDown(1); // right-click

        if (trigger)
        {
            Debug.Log($"[SprayBottle] TRIGGER! Remaining={_remainingUses} pos={transform.position}");
            PerformSpray();
        }
    }

    public void PerformSpray()
    {
        if (IsEmpty) return;

        // In Editor, raycast from camera if nothing hit from bottle
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Vector3 direction = transform.forward;

        bool hitSomething = Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, sprayLayerMask);

        // Fallback: raycast from main camera (for Editor testing)
        if (!hitSomething && Camera.main != null)
        {
            origin = Camera.main.transform.position;
            direction = Camera.main.transform.forward;
            hitSomething = Physics.Raycast(origin, direction, out hit, sprayRange, sprayLayerMask);
        }

        if (hitSomething)
        {
            Debug.Log($"[SprayBottle] Hit: {hit.collider.name}");
            var target = hit.collider.GetComponentInParent<TileBase>();
            if (target is LogicTile logicTile)
            {
                logicTile.ApplyNot();
                Debug.Log($"[SprayBottle] NOT: {logicTile.CellState}");
            }
        }
        else
        {
            Debug.Log("[SprayBottle] Miss — nothing hit");
        }

        _remainingUses--;
        Debug.Log($"[SprayBottle] Uses left: {_remainingUses}");
    }

    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[SprayBottle] Refilled: {uses}");
    }
}
