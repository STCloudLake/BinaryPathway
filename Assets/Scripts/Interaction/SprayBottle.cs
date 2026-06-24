// Scripts/Interaction/SprayBottle.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum SprayMode { NotLogic, Detach }

/// <summary>
/// Trigger: OVRInput index trigger + editor fallbacks (F key, right-click).
/// Mode: reads nozzle Z rotation from WaterSprayNozzleTransformer.
///
/// Does NOT touch ISDK WaterSpray — it runs independently for visual effect.
/// </summary>
public class SprayBottle : MonoBehaviour
{
    [Min(1)] public int maxUses = 99;
    public float sprayRange = 5f;
    public LayerMask sprayLayerMask = ~0;

    [SerializeField] private int _remainingUses;
    [SerializeField] private SprayMode _currentMode = SprayMode.NotLogic;

    private Transform _nozzle;
    private bool _actionInProgress;
    private TMPro.TextMeshProUGUI _labelText;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;
    public SprayMode CurrentMode => _currentMode;

    void Awake()
    {
        _remainingUses = maxUses;
    }

    void Start()
    {
        _nozzle = GetComponentInChildren<Oculus.Interaction.Demo.WaterSprayNozzleTransformer>()?.transform;

        // Find the label TMP for dynamic use counter
        var labelComp = GetComponentInChildren<Oculus.Interaction.Samples.InteractableObjectLabel>();
        if (labelComp != null)
        {
            _labelText = labelComp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        }
        UpdateLabel();

        // Disable ISDK WaterSpray stamping
        var waterSpray = GetComponent<Oculus.Interaction.Demo.WaterSpray>();
        if (waterSpray != null)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var sprayHitsField = typeof(Oculus.Interaction.Demo.WaterSpray).GetField("_sprayHits", flags);
            if (sprayHitsField != null) sprayHitsField.SetValue(waterSpray, 0);
            Debug.Log("[SprayBottle] ISDK WaterSpray stamps disabled");
        }
    }

    void Update()
    {
        if (IsEmpty) return;

        // Read nozzle Z rotation for mode (same logic as ISDK GetNozzleMode)
        if (_nozzle != null)
        {
            float angle = _nozzle.localEulerAngles.z;
            int rotations = (int)(angle + 45f) / 90;
            SprayMode nozzleMode = (rotations % 2 == 0) ? SprayMode.NotLogic : SprayMode.Detach;
            if (nozzleMode != _currentMode)
            {
                _currentMode = nozzleMode;
                Debug.Log($"[SprayBottle] Mode → {_currentMode}");
            }
        }

        // Trigger detection
        bool trigger = false;
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        trigger |= Input.GetKeyDown(KeyCode.F);
        trigger |= Input.GetMouseButtonDown(1);

        if (trigger)
        {
            Debug.Log($"[SprayBottle] TRIGGER mode={_currentMode} uses={_remainingUses}");
            PerformSpray();
        }
    }

    // ============================================================
    // SPRAY
    // ============================================================

    public void PerformSpray()
    {
        if (IsEmpty || _actionInProgress) return;
        _actionInProgress = true;

        try
        {
            // Raycast: try from bottle nozzle first, then camera fallback
            Vector3 origin = transform.position + transform.forward * 0.1f;
            Vector3 dir = transform.forward;

            bool hit = Physics.SphereCast(origin, 0.05f, dir, out RaycastHit hitInfo, sprayRange, sprayLayerMask);

            if (!hit)
            {
                var cam = Camera.main ?? FindFirstObjectByType<Camera>();
                if (cam != null)
                    hit = Physics.Raycast(cam.transform.position, cam.transform.forward, out hitInfo, sprayRange, sprayLayerMask);
            }

            if (!hit) return;

            Debug.Log($"[SprayBottle] Hit: {hitInfo.collider.name}, mode={_currentMode}");

            bool didSomething = false;
            switch (_currentMode)
            {
                case SprayMode.NotLogic:
                    didSomething = ApplyNot(hitInfo);
                    break;
                case SprayMode.Detach:
                    didSomething = ApplyDetach(hitInfo);
                    break;
            }

            if (didSomething)
            {
                _remainingUses--;
                Debug.Log($"[SprayBottle] Use consumed, {_remainingUses} left");
                UpdateLabel();
            }
        }
        finally
        {
            _actionInProgress = false;
        }
    }

    // ============================================================
    // NOT LOGIC
    // ============================================================

    bool ApplyNot(RaycastHit hit)
    {
        var tile = hit.collider.GetComponentInParent<TileBase>();
        if (tile is LogicTile lt)
        {
            lt.ApplyNot();
            Debug.Log($"[SprayBottle] NOT → {lt.CellState}");
            return true;
        }
        return false;
    }

    // ============================================================
    // DETACH
    // ============================================================

    bool ApplyDetach(RaycastHit hit)
    {
        var rb = hit.collider.GetComponentInParent<Rigidbody>();
        if (rb == null) return false;

        var joints = new List<Joint>(rb.GetComponents<Joint>());
        if (joints.Count == 0) return false;

        var pairs = new List<(Rigidbody, Rigidbody, Vector3)>();
        foreach (var j in joints)
        {
            if (j == null || j.connectedBody == null) continue;
            Vector3 sepDir = (rb.position - j.connectedBody.position).normalized;
            if (sepDir.sqrMagnitude < 0.001f) sepDir = Random.onUnitSphere;
            pairs.Add((rb, j.connectedBody, sepDir));
        }

        foreach (var j in joints)
        {
            if (j == null) continue;
            if (j.connectedBody != null)
            {
                foreach (var oj in j.connectedBody.GetComponents<Joint>())
                    if (oj != null && oj.connectedBody == rb) Destroy(oj);
            }
            Destroy(j);
        }

        foreach (var (a, b, sepDir) in pairs)
        {
            a.AddForce(sepDir * 0.6f, ForceMode.Impulse);
            b.AddForce(-sepDir * 0.6f, ForceMode.Impulse);
            BreakableLatchOnSocket.SuppressReconnect(a, b, 3f);
        }

        return true;
    }

    // ============================================================
    // UTILITY
    // ============================================================

    public void Refill(int uses)
    {
        _remainingUses = uses;
        UpdateLabel();
        Debug.Log($"[SprayBottle] Refilled: {uses}");
    }

    void UpdateLabel()
    {
        if (_labelText != null)
            _labelText.text = $"{_remainingUses}";
    }
}
