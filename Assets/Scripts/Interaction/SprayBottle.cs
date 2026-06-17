// Scripts/Interaction/SprayBottle.cs
using UnityEngine;

/// <summary>
/// NOT-spray bottle tool. Uses Oculus ISDK Grabbable for hold detection
/// and OVRInput/Space for trigger. Raycasts forward, applies NOT to hit tiles.
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

    private Oculus.Interaction.Grabbable _grabbable;
    private bool _wasSpraying;
    private float _sprayCooldown;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake()
    {
        _grabbable = GetComponent<Oculus.Interaction.Grabbable>();
        _remainingUses = maxUses;
    }

    void Update()
    {
        if (IsEmpty) return;

        // Detect if bottle is held via Oculus ISDK grab
        bool isHeld = _grabbable != null && _grabbable.SelectingPointsCount > 0;

        // Trigger: Space (Editor) or Oculus Index Trigger (Quest)
        bool sprayTrigger = Input.GetKey(KeyCode.Space);
#if UNITY_ANDROID && !UNITY_EDITOR
        sprayTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
#endif

        bool isSpraying = isHeld && sprayTrigger;

        if (isSpraying && !_wasSpraying && _sprayCooldown <= 0f)
        {
            PerformSpray();
            _sprayCooldown = 0.3f;
        }

        if (!isSpraying)
            _sprayCooldown = Mathf.Max(0f, _sprayCooldown - Time.deltaTime);

        _wasSpraying = isSpraying;
    }

    void PerformSpray()
    {
        Vector3 origin = transform.position + transform.forward * 0.1f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, sprayRange, sprayLayerMask))
        {
            var target = hit.collider.GetComponentInParent<TileBase>();
            if (target != null)
            {
                if (target is LogicTile logicTile)
                {
                    logicTile.ApplyNot();
                    Debug.Log($"[SprayBottle] NOT: {logicTile.CellState}");
                }
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
        if (IsEmpty) Debug.Log("[SprayBottle] Empty!");
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
