// Scripts/Tiles/TileLabel.cs
using UnityEngine;

/// <summary>
/// Dynamic label showing tile's CellState above the tile.
/// Creates own TextMeshPro child at startup.
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    public float labelHeight = 0.15f;
    public float labelSize = 0.06f;

    private TileBase _tile;
    private TMPro.TextMeshPro _label;
    private GameObject _labelGO;
    private CellState _lastState;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        CreateLabel();
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
        _label.rectTransform.sizeDelta = new Vector2(0.3f, 0.1f);

        var initial = _tile != null ? _tile.GetCellState() : CellState.PureValue(0);
        _lastState = initial;
        RefreshDisplay(initial);

        var scaler = _labelGO.AddComponent<FinalLabelScaler>();
        scaler.sizeOnScreen = 0.04f;
        scaler.faceCamera = true;
    }

    void Update()
    {
        if (_label == null || _tile == null) return;
        var currentState = _tile.GetCellState();
        if (!currentState.Equals(_lastState))
        {
            _lastState = currentState;
            RefreshDisplay(currentState);
            Debug.Log($"[TileLabel] {name}: {currentState}");
        }
    }

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
                _label.color = new Color(1f, 0.6f, 0.1f);
                break;
            case CellStateType.LogicOnly:
                _label.text = LogicSymbol(state.logic);
                _label.color = new Color(0.5f, 0.5f, 0.5f);
                break;
        }
    }

    static string LogicSymbol(LogicOp op) => op switch
    {
        LogicOp.AND => "AND", LogicOp.NAND => "NAND",
        LogicOp.OR => "OR", LogicOp.NOR => "NOR",
        LogicOp.XOR => "XOR", LogicOp.XNOR => "XNOR",
        _ => "?"
    };
}
