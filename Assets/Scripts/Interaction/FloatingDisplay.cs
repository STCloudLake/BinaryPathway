using UnityEngine;

/// <summary>
/// Makes an object slowly rotate and gently float up/down.
/// Used for display/demo tiles to catch the player's attention.
/// </summary>
public class FloatingDisplay : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Degrees per second around Y axis.")]
    public float rotationSpeedY = 18f;

    [Tooltip("Tilt angle for gentle X-axis wobble.")]
    public float tiltAngleX = 3f;

    [Tooltip("Speed of the tilt oscillation.")]
    public float tiltSpeed = 0.7f;

    [Header("Float")]
    [Tooltip("Vertical float amplitude in meters.")]
    public float floatAmplitude = 0.06f;

    [Tooltip("Float speed (cycles per second).")]
    public float floatFrequency = 0.6f;

    private Vector3 _startPos;
    private Quaternion _startRot;
    private float _timeOffset;

    void Start()
    {
        _startPos = transform.position;
        _startRot = transform.rotation;
        _timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = Time.time + _timeOffset;

        // Vertical float (sine wave)
        float yOffset = Mathf.Sin(t * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
        transform.position = _startPos + Vector3.up * yOffset;

        // Y-axis spin
        float yAngle = (t * rotationSpeedY) % 360f;

        // X-axis gentle tilt
        float xTilt = Mathf.Sin(t * tiltSpeed * Mathf.PI * 2f) * tiltAngleX;

        transform.rotation = _startRot * Quaternion.Euler(xTilt, yAngle, 0f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? _startPos : transform.position;
        Gizmos.DrawWireCube(center + Vector3.up * floatAmplitude, Vector3.one * 0.1f);
        Gizmos.DrawWireCube(center - Vector3.up * floatAmplitude, Vector3.one * 0.1f);
    }
}
