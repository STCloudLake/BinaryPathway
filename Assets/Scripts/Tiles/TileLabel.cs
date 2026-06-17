// Scripts/Tiles/TileLabel.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space Canvas text label showing CellState above tile.
/// Uses UI/Text (not TMP) for maximum Quest compatibility.
/// </summary>
[RequireComponent(typeof(TileBase))]
public class TileLabel : MonoBehaviour
{
    private TileBase _tile;
    private Text _label;
    private Canvas _canvas;
    private CellState _lastState;

    void Start()
    {
        _tile = GetComponent<TileBase>();

        // Create world-space Canvas
        var canvasGO = new GameObject("LabelCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0, 0.25f, 0);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.003f; // Scale for readability (~3mm/unit)

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        var rect = _canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        _label = textGO.AddComponent<Text>();
        try { _label.font = Font.CreateDynamicFontFromOSFont("Arial", 14); }
        catch { /* use default */ }
        _label.fontSize = 28;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.rectTransform.sizeDelta = new Vector2(200, 50);
        _label.color = Color.white;
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;

        var initial = _tile.GetCellState();
        _lastState = initial;
        RefreshDisplay(initial);
    }

    void Update()
    {
        // Billboard
        if (_canvas != null)
        {
            var cam = _canvas.worldCamera;
            if (cam == null) cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                _canvas.worldCamera = cam;
                _canvas.transform.rotation = Quaternion.LookRotation(
                    _canvas.transform.position - cam.transform.position);
            }
        }

        // Poll state
        if (_tile == null) return;
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
                _label.color = new Color(1f, 0.5f, 0f);
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
