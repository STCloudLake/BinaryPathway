// Scripts/Interaction/SprayBottle.cs
using UnityEngine;

/// <summary>
/// NOT-spray bottle tool. Space key sprays forward from bottle position.
/// Applies NOT to hit LogicTiles. Limited uses per level.
/// </summary>
public class SprayBottle : MonoBehaviour
{
    [Header("Spray Config")]
    [Min(1)] public int maxUses = 3;
    public float sprayRange = 3f;
    public LayerMask sprayLayerMask = ~0;

    [Header("Effects")]
    public ParticleSystem sprayParticles;
    public AudioSource spraySound;

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

        // Space key to spray (Editor test)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PerformSpray();
        }
    }

    public void PerformSpray()
    {
        if (IsEmpty) return;

        Vector3 origin = transform.position + transform.forward * 0.1f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, sprayLayerMask))
        {
            var target = hit.collider.GetComponentInParent<TileBase>();
            if (target is LogicTile logicTile)
            {
                logicTile.ApplyNot();
                Debug.Log($"[SprayBottle] NOT: {logicTile.CellState}");
            }

            if (sprayParticles != null)
            {
                sprayParticles.transform.position = hit.point;
                sprayParticles.Play();
            }
        }

        _remainingUses--;
        if (spraySound != null) spraySound.Play();
        Debug.Log($"[SprayBottle] Used. Remaining: {_remainingUses}");
    }

    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[SprayBottle] Refilled: {uses}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + transform.forward * 0.1f, transform.forward * sprayRange);
    }
#endif
}
