using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Manages a world-space label on a Tile showing its current value (0/1).
/// Green = value 1 (conductive), Red = value 0 (blocked).
/// Hides during grab interaction, reappears after release.
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    [Header("Label Settings")]
    public float labelHeight = 0.12f;
    public float labelSize = 0.06f;

    private TileBase _tile;
    private TMPro.TextMeshPro _label;
    private GameObject _labelGO;
    private XRGrabInteractable _grab;
    private Renderer _labelRenderer;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        _grab = GetComponent<XRGrabInteractable>();

        CreateLabel();

        if (_grab != null)
        {
            _grab.selectEntered.AddListener(_ => HideLabel());
            _grab.selectExited.AddListener(_ => ShowLabel());
        }
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(_ => HideLabel());
            _grab.selectExited.RemoveListener(_ => ShowLabel());
        }
    }

    void Update()
    {
        if (_labelGO != null && _labelGO.activeSelf && _tile != null)
        {
            UpdateLabelText();
        }
    }

    void CreateLabel()
    {
        _labelGO = new GameObject("TileLabel");
        _labelGO.transform.SetParent(transform, false);
        _labelGO.transform.localPosition = Vector3.up * labelHeight;
        _labelGO.transform.localRotation = Quaternion.identity;

        _label = _labelGO.AddComponent<TMPro.TextMeshPro>();
        _label.text = _tile != null ? _tile.Value.ToString() : "?";
        _label.fontSize = labelSize;
        _label.alignment = TMPro.TextAlignmentOptions.Center;
        _label.color = (_tile != null && _tile.Value == 1) ? Color.green : Color.red;

        // Add adaptive scaling + billboard
        var scaler = _labelGO.AddComponent<FinalLabelScaler>();
        scaler.sizeOnScreen = 0.04f;
        scaler.faceCamera = true;
    }

    void UpdateLabelText()
    {
        if (_tile == null || _label == null) return;
        int val = _tile.Value;
        _label.text = val.ToString();
        _label.color = (val == 1) ? Color.green : Color.red;
    }

    void HideLabel()
    {
        if (_labelGO != null) _labelGO.SetActive(false);
    }

    void ShowLabel()
    {
        if (_labelGO != null) _labelGO.SetActive(true);
    }
}
