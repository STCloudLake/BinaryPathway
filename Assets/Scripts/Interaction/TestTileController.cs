using TMPro;
using UnityEngine;

/// <summary>
/// Two-button control panel for a demo LogicTile.
/// - Logic button: cycles through LogicOp values (AND→OR→NAND→NOR→XOR→XNOR→None).
/// - Value button: cycles through values (0→1→None).
/// Both buttons and the tile stay in sync — external changes (spray, etc.) update buttons;
/// button presses update the tile.
/// </summary>
public class TestTileController : MonoBehaviour
{
    [Header("Target Tile")]
    public LogicTile testTile;

    [Header("Button Labels")]
    public TextMeshProUGUI logicButtonLabel;
    public TextMeshProUGUI valueButtonLabel;

    [Header("Logic Icons")]
    public GameObject logicIconParent;
    public Renderer[] valueIndicators; // 0 = value 0, 1 = value 1 (optional)

    // Cycle definitions
    private static readonly LogicOp[] _logicValues = {
        LogicOp.AND, LogicOp.OR, LogicOp.NAND, LogicOp.NOR, LogicOp.XOR, LogicOp.XNOR
    };
    private static readonly string[] _logicNames = {
        "AND", "OR", "NAND", "NOR", "XOR", "XNOR"
    };
    private static readonly int[] _valueValues = { 0, 1 };
    private static readonly string[] _valueNames = { "0", "1" };

    // Indices: 0 = None, 1..N = array element
    private int _logicIdx = 0;
    private int _valueIdx = 0;

    // External change detection
    private CellState _lastKnownState;

    void Start()
    {
        if (testTile == null) return;
        SyncFromTile();
        ApplyToTile();
    }

    void Update()
    {
        if (testTile == null) return;
        var current = testTile.GetCellState();
        if (!current.Equals(_lastKnownState))
        {
            // External change detected (spray, etc.) — sync buttons
            SyncFromTile();
        }
    }

    /// <summary>Called by Logic button onClick.</summary>
    public void CycleLogic()
    {
        _logicIdx = (_logicIdx + 1) % (_logicValues.Length + 1);
        ApplyToTile();
    }

    /// <summary>Called by Value button onClick.</summary>
    public void CycleValue()
    {
        _valueIdx = (_valueIdx + 1) % (_valueValues.Length + 1);
        ApplyToTile();
    }

    // ---------- Internal ----------

    void ApplyToTile()
    {
        if (testTile == null) return;

        LogicOp? logic = _logicIdx == 0 ? null : (LogicOp?)_logicValues[_logicIdx - 1];
        int? value = _valueIdx == 0 ? null : (int?)_valueValues[_valueIdx - 1];

        if (logic == null && value == null)
        {
            testTile.gameObject.SetActive(false);
        }
        else
        {
            testTile.gameObject.SetActive(true);

            if (logic == null)
            {
                testTile.CellState = CellState.PureValue(value.Value);
            }
            else if (value == null)
            {
                testTile.CellState = CellState.LogicOnly(logic.Value);
            }
            else
            {
                testTile.CellState = CellState.ValueWithLogic(value.Value, logic.Value);
            }
        }

        _lastKnownState = testTile.GetCellState();
        UpdateLabels();
    }

    void SyncFromTile()
    {
        if (testTile == null) return;

        if (!testTile.gameObject.activeSelf)
        {
            _logicIdx = 0;
            _valueIdx = 0;
            _lastKnownState = default;
            UpdateLabels();
            return;
        }

        var cs = testTile.GetCellState();

        // Map CellState back to indices
        switch (cs.type)
        {
            case CellStateType.PureValue:
                _logicIdx = 0;
                _valueIdx = cs.value == 0 ? 1 : 2;
                break;
            case CellStateType.LogicOnly:
                _logicIdx = IndexOfLogic(cs.logic) + 1;
                _valueIdx = 0;
                break;
            case CellStateType.ValueWithLogic:
                _logicIdx = IndexOfLogic(cs.logic) + 1;
                _valueIdx = cs.value == 0 ? 1 : 2;
                break;
        }

        _lastKnownState = cs;
        UpdateLabels();
    }

    int IndexOfLogic(LogicOp op)
    {
        for (int i = 0; i < _logicValues.Length; i++)
            if (_logicValues[i] == op) return i;
        return 0;
    }

    void UpdateLabels()
    {
        string logicText = _logicIdx == 0 ? "--" : _logicNames[_logicIdx - 1];
        string valueText = _valueIdx == 0 ? "--" : _valueNames[_valueIdx - 1];

        if (logicButtonLabel != null)
            logicButtonLabel.text = $"Logic: {logicText}";
        if (valueButtonLabel != null)
            valueButtonLabel.text = $"Value: {valueText}";
    }
}
