// Scripts/Tiles/TileLabel.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    private TileBase _tile;
    private Text _label;
    private Canvas _canvas;
    private CellState _lastState;
    private Camera _cam;

    void Start()
    {
        _tile = GetComponent<TileBase>();
        _cam = Camera.main ?? FindFirstObjectByType<Camera>();

        var canvasGO = new GameObject("LabelCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0, 0.16f, 0); // Just above tile surface

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = _cam;
        var rect = _canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        _label = textGO.AddComponent<Text>();
        try { _label.font = Font.CreateDynamicFontFromOSFont("Arial", 14); } catch { }
        _label.fontSize = 24;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.rectTransform.sizeDelta = new Vector2(80, 30);
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;

        var initial = _tile.GetCellState();
        _lastState = initial;
        RefreshDisplay(initial);
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main ?? FindFirstObjectByType<Camera>();
        if (_canvas == null || _tile == null) return;

        // Billboard + distance scale
        if (_cam != null)
        {
            _canvas.worldCamera = _cam;
            var canvasT = _canvas.transform;
            canvasT.rotation = Quaternion.LookRotation(canvasT.position - _cam.transform.position);

            float dist = Vector3.Distance(canvasT.position, _cam.transform.position);
            float scale = Mathf.Clamp(dist * 0.004f, 0.002f, 0.008f);
            canvasT.localScale = Vector3.one * scale;
        }

        var currentState = _tile.GetCellState();
        if (!currentState.Equals(_lastState))
        {
            _lastState = currentState;
            RefreshDisplay(currentState);
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
                _label.color = new Color(1f, 0.6f, 0f);
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
