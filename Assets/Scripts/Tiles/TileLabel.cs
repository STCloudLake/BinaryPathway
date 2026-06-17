// Scripts/Tiles/TileLabel.cs
using UnityEngine;

/// <summary>
/// Shows tile CellState above the tile using Unity TextMesh (not Pro).
/// Billboard-facing, fixed world scale.
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    private TileBase _tile;
    private TextMesh _label;
    private GameObject _labelGO;
    private CellState _lastState;
    private Camera _cam;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        _cam = Camera.main ?? FindFirstObjectByType<Camera>();
        CreateLabel();
    }

    void CreateLabel()
    {
        _labelGO = new GameObject("Label3D");
        _labelGO.transform.SetParent(transform, false);
        _labelGO.transform.localPosition = new Vector3(0, 0.18f, 0);
        _labelGO.transform.localRotation = Quaternion.identity;
        _labelGO.transform.localScale = Vector3.one * 0.02f;

        _label = _labelGO.AddComponent<TextMesh>();
        _label.fontSize = 50;
        _label.characterSize = 0.02f;
        _label.anchor = TextAnchor.MiddleCenter;
        _label.color = Color.white;

        var initial = _tile != null ? _tile.GetCellState() : CellState.PureValue(0);
        _lastState = initial;
        RefreshDisplay(initial);
    }

    void Update()
    {
        if (_label == null || _tile == null) return;

        // Billboard
        if (_cam != null)
            _labelGO.transform.rotation = Quaternion.LookRotation(
                _labelGO.transform.position - _cam.transform.position);

        // Poll cell state
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
                _label.color = state.value == 1 ? Color.green : Color.red;
                break;
            case CellStateType.ValueWithLogic:
                _label.text = state.value + " " + LogicSymbol(state.logic);
                _label.color = new Color(1f, 0.5f, 0f); // orange
                break;
            case CellStateType.LogicOnly:
                _label.text = LogicSymbol(state.logic);
                _label.color = Color.gray;
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
