using UnityEngine;

[ExecuteAlways]
public class FinalLabelScaler : MonoBehaviour
{
    public Camera cam;
    public float sizeOnScreen = 0.05f;
    public bool faceCamera = true;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        // Fallback: find XR camera
        if (cam == null) cam = FindFirstObjectByType<Camera>();
    }

    void LateUpdate()
    {
        if (cam == null) { cam = Camera.main ?? FindFirstObjectByType<Camera>(); }
        if (!cam) return;

        if (faceCamera)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        float d = Vector3.Distance(transform.position, cam.transform.position);
        float h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float worldScale = h * sizeOnScreen;
        transform.localScale = Vector3.one * worldScale;
    }
}
