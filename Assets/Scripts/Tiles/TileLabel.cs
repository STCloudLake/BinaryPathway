// Scripts/Tiles/TileLabel.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Dynamic world-space label showing tile's current CellState:
/// PureValue: "0" (red) or "1" (green)
/// ValueWithLogic: "v L" (e.g. "1 AND" in orange — waiting)
/// LogicOnly: "L" (e.g. "AND" in grey — waiting)
/// Hides during grab, auto-updates on CellState change.
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    [Header("Label Settings")]
    public float labelHeight = 0.12f;
    public float labelSize = 0.06f;

    private LogicTile _logicTile;
    private TileBase _tile;
    private TMPro.TextMeshPro _label;
    private GameObject _labelGO;
    private XRGrabInteractable _grab;

    // Track last state to avoid per-frame allocations
    private CellState _lastState;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        _logicTile = _tile as LogicTile;
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
        if (_labelGO == null || !_labelGO.activeSelf || _tile == null) return;

        var currentState = _tile.GetCellState();
        if (!currentState.Equals(_lastState))
        {
            _lastState = currentState;
            RefreshDisplay(currentState);
        }
    }

    void CreateLabel()
    {
        _labelGO = new GameObject("TileLabel");
        _labelGO.transform.SetParent(transform, false);
        _labelGO.transform.localPosition = Vector3.up * labelHeight;
        _labelGO.transform.localRotation = Quaternion.identity;

        _label = _labelGO.AddComponent<TMPro.TextMeshPro>();
        _label.fontSize = labelSize;
        _label.alignment = TMPro.TextAlignmentOptions.Center;

        var initial = _tile != null ? _tile.GetCellState() : CellState.PureValue(0);
        _lastState = initial;
        RefreshDisplay(initial);

        var scaler = _labelGO.AddComponent<FinalLabelScaler>();
        scaler.sizeOnScreen = 0.04f;
        scaler.faceCamera = true;
    }

    /// <summary>
    /// Update label text and color based on CellState.
    /// </summary>
    void RefreshDisplay(CellState state)
    {
        if (_label == null) return;

        switch (state.type)
        {
            case CellStateType.PureValue:
                _label.text = state.value.ToString();
                _label.color = state.value == 1 ? new Color(0.2f, 0.9f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
                break;

            case CellStateType.ValueWithLogic:
                _label.text = state.value + " " + LogicSymbol(state.logic);
                _label.color = new Color(1f, 0.6f, 0.1f); // orange — waiting
                break;

            case CellStateType.LogicOnly:
                _label.text = LogicSymbol(state.logic);
                _label.color = new Color(0.5f, 0.5f, 0.5f); // grey — waiting for value
                break;
        }
    }

    /// <summary>
    /// Convert LogicOp to a compact symbol string.
    /// </summary>
    static string LogicSymbol(LogicOp op) => op switch
    {
        LogicOp.AND  => "AND",
        LogicOp.NAND => "NAND",
        LogicOp.OR   => "OR",
        LogicOp.NOR  => "NOR",
        LogicOp.XOR  => "XOR",
        LogicOp.XNOR => "XNOR",
        _ => "?"
    };

    void HideLabel() { if (_labelGO != null) _labelGO.SetActive(false); }
    void ShowLabel() { if (_labelGO != null) _labelGO.SetActive(true); }
}
