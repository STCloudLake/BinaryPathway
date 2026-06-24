// Scripts/Interaction/DetachTool.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Handheld tool that breaks FixedJoints between connected tiles.
/// Visual feedback: brief red/green line on each use + console logging.
/// </summary>
public class DetachTool : MonoBehaviour
{
    [Min(1)] public int maxUses = 99;
    public float range = 5f;
    public LayerMask layerMask = ~0;

    [SerializeField] private int _remainingUses;

    private LineRenderer _lineRenderer;
    private XRGrabInteractable _grab;
    private Coroutine _lineCoroutine;

    public int RemainingUses => _remainingUses;
    public bool IsEmpty => _remainingUses <= 0;

    void Awake()
    {
        _remainingUses = maxUses;

        _grab = GetComponent<XRGrabInteractable>();

        // Create a LineRenderer for visual feedback
        var lrGO = new GameObject("DetachBeam");
        lrGO.transform.SetParent(transform, false);
        _lineRenderer = lrGO.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.008f;
        _lineRenderer.endWidth = 0.004f;
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    void Update()
    {
        if (IsEmpty) return;

        bool trigger = false;

        // Quest controller triggers
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        trigger |= OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        // Editor / PC fallbacks
        trigger |= Input.GetKeyDown(KeyCode.G);

        if (trigger)
        {
            Debug.Log($"[DetachTool] TRIGGER PRESSED! uses left={_remainingUses}, pos={transform.position}, fwd={transform.forward}");
            PerformDetach();
        }
    }

    void PerformDetach()
    {
        if (IsEmpty) return;

        Vector3 origin = transform.position + transform.forward * 0.1f;
        Vector3 dir = transform.forward;

        Debug.Log($"[DetachTool] SphereCast origin={origin} dir={dir} range={range}");

        bool didHit = Physics.SphereCast(origin, 0.08f, dir, out RaycastHit hitInfo, range, layerMask);

        // Fallback: ray from camera center
        if (!didHit)
        {
            var cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                Debug.Log($"[DetachTool] SphereCast MISS, trying camera fallback...");
                didHit = Physics.Raycast(cam.transform.position, cam.transform.forward, out hitInfo, range, layerMask);
                if (didHit) Debug.Log($"[DetachTool] Camera ray HIT: {hitInfo.collider.name}");
                else Debug.Log($"[DetachTool] Camera ray MISS too");
            }
            else
            {
                Debug.LogWarning("[DetachTool] No camera found for fallback!");
            }
        }

        if (didHit)
        {
            Debug.Log($"[DetachTool] HIT object: {hitInfo.collider.name}");
            ProcessHit(hitInfo);
        }

        // Visual feedback (always show, even on miss)
        ShowBeam(origin, didHit ? hitInfo.point : (origin + dir * range), didHit);
    }

    void ProcessHit(RaycastHit hit)
    {
        var rb = hit.collider.GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"[DetachTool] No Rigidbody found on {hit.collider.name} or its parents");
            return;
        }

        var joints = rb.GetComponents<Joint>();
        if (joints.Length == 0)
        {
            Debug.Log($"[DetachTool] No joints on {rb.name} — tile is not connected to anything");
            return;
        }

        // --- Phase 1: collect pairs + apply separation impulse ---
        var pairs = new System.Collections.Generic.List<(Rigidbody, Rigidbody, Vector3)>();
        foreach (var j in joints)
        {
            if (j == null || j.connectedBody == null) continue;
            // Direction from connected tile to this tile
            Vector3 sepDir = (rb.position - j.connectedBody.position).normalized;
            if (sepDir.sqrMagnitude < 0.001f) sepDir = Random.onUnitSphere; // paranoia
            pairs.Add((rb, j.connectedBody, sepDir));
        }

        // --- Phase 2: destroy all joints ---
        foreach (var j in joints)
        {
            if (j == null) continue;
            if (j.connectedBody != null)
            {
                // Also destroy reciprocal joints on the other tile
                foreach (var oj in j.connectedBody.GetComponents<Joint>())
                {
                    if (oj != null && oj.connectedBody == rb) Destroy(oj);
                }
            }
            Destroy(j);
        }

        // --- Phase 3: push apart + suppress re-connect ---
        float impulseMag = 0.6f; // enough to push tiles out of socket range (~0.3m)
        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (var (a, b, dir) in pairs)
        {
            a.AddForce(dir * impulseMag, ForceMode.Impulse);
            b.AddForce(-dir * impulseMag, ForceMode.Impulse);

            // Suppress face-socket re-connection for this pair
            BreakableLatchOnSocket.SuppressReconnect(a, b, 3f);

            // Log each unique pair
            int hash = a.GetInstanceID() ^ b.GetInstanceID();
            if (seen.Add(hash))
                Debug.Log($"[DetachTool] 💥 Separated: {a.name} ↔ {b.name}, impulse={impulseMag}");
        }

        if (pairs.Count > 0)
        {
            _remainingUses--;
            Debug.Log($"[DetachTool] ✅ BROKE {pairs.Count} connection(s)! {_remainingUses} uses left");
        }
    }

    /// <summary>Show a brief colored line: green=hit, red=miss.</summary>
    void ShowBeam(Vector3 start, Vector3 end, bool hit)
    {
        if (_lineRenderer == null) return;

        if (_lineCoroutine != null)
            StopCoroutine(_lineCoroutine);

        _lineCoroutine = StartCoroutine(FlashBeam(start, end, hit));
    }

    IEnumerator FlashBeam(Vector3 start, Vector3 end, bool hit)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);

        Color c = hit ? Color.green : Color.red;
        _lineRenderer.startColor = c;
        _lineRenderer.endColor = c;

        yield return new WaitForSeconds(0.3f);
        _lineRenderer.enabled = false;
        _lineCoroutine = null;
    }

    public void Refill(int uses)
    {
        _remainingUses = uses;
        Debug.Log($"[DetachTool] Refilled: {uses}");
    }

    void OnDestroy()
    {
        if (_lineRenderer != null)
            Destroy(_lineRenderer.gameObject);
    }
}

// GetPath extension is in SprayBottle.cs (TransformExtensions)
