// Scripts/Interaction/SprayBottle.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// NOT-spray bottle tool. Point at any tile and press trigger to flip its value
/// or logic operation. Limited uses per level (configured via LevelData.sprayUses).
/// </summary>
public class SprayBottle : MonoBehaviour
{
    [Header("Spray Config")]
    [Min(1)] public int maxUses = 3;
    [Tooltip("Max spray distance in meters.")]
    public float sprayRange = 3f;
    public LayerMask sprayLayerMask = ~0;

    [Header("Effects")]
    public ParticleSystem sprayParticles;
    public AudioSource spraySound;

    [Header("State")]
    [SerializeField] private int _remainingUses;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake()
    {
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        _remainingUses = maxUses;

        if (_grabInteractable != null)
        {
            _grabInteractable.activated.AddListener(OnActivated);
        }
    }

    void OnDestroy()
    {
        if (_grabInteractable != null)
            _grabInteractable.activated.RemoveListener(OnActivated);
    }

    /// <summary>Called when the player pulls the trigger while holding the bottle.</summary>
    void OnActivated(ActivateEventArgs args)
    {
        if (IsEmpty)
        {
            Debug.Log("[SprayBottle] Empty — no uses remaining.");
            return;
        }

        PerformSpray();
    }

    void PerformSpray()
    {
        // Raycast from the bottle nozzle (forward direction)
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, sprayLayerMask))
        {
            var target = hit.collider.GetComponentInParent<TileBase>();
            if (target != null)
            {
                ApplyNot(target);
            }
            else
            {
                // Check for LogicBlock at hit point
                var block = hit.collider.GetComponentInParent<LogicBlock>();
                if (block != null)
                {
                    ApplyNotToBlock(block, hit.point);
                }
            }

            // Visual feedback at hit point
            if (sprayParticles != null)
            {
                sprayParticles.transform.position = hit.point;
                sprayParticles.Play();
            }
        }

        _remainingUses--;
        if (spraySound != null)
            spraySound.Play();

        Debug.Log($"[SprayBottle] Spray used. Remaining: {_remainingUses}");

        if (IsEmpty)
        {
            Debug.Log("[SprayBottle] Bottle is now empty!");
        }
    }

    void ApplyNot(TileBase tile)
    {
        if (tile is LogicTile logicTile)
        {
            logicTile.ApplyNot();
            Debug.Log($"[SprayBottle] NOT applied to LogicTile: {logicTile.CellState}");
        }
        else if (tile is ToggleTile toggle)
        {
            toggle.Toggle();
            Debug.Log($"[SprayBottle] NOT applied to ToggleTile: now {toggle.Value}");
        }
        else
        {
            // Tile_0 or Tile_1 cannot be flipped — they are immutable
            Debug.Log($"[SprayBottle] Cannot NOT-flip immutable tile: {tile.GetType().Name}");
        }
    }

    void ApplyNotToBlock(LogicBlock block, Vector3 hitPoint)
    {
        // Find the closest cell in the block
        LogicTile closest = null;
        float minDist = float.MaxValue;
        foreach (var cell in block.cells)
        {
            float d = Vector3.Distance(hitPoint, cell.transform.position);
            if (d < minDist) { minDist = d; closest = cell; }
        }
        if (closest != null)
            closest.ApplyNot();
    }

    /// <summary>Refill the bottle (for level restart or power-up).</summary>
    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[SprayBottle] Refilled with {uses} uses.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * sprayRange);
    }
#endif
}
