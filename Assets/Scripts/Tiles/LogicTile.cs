// Scripts/Tiles/LogicTile.cs
using UnityEngine;

/// <summary>
/// A single 1×1 cell within a logic block or standalone logic tile.
/// Carries a CellState that can be PureValue, ValueWithLogic, or LogicOnly.
/// Extends TileBase — compatible with GridContainer.Place() and GridSocket.
/// </summary>
public class LogicTile : TileBase
{
    [Header("Logic Tile State")]
    [SerializeField] private CellStateType _stateType = CellStateType.LogicOnly;
    [SerializeField] private int _value;
    [SerializeField] private LogicOp _logicOp = LogicOp.AND;

    /// <summary>Exposed CellState for external read/write (TileConnector, LogicBlock).</summary>
    public CellState CellState
    {
        get
        {
            return _stateType switch
            {
                CellStateType.PureValue      => CellState.PureValue(_value),
                CellStateType.ValueWithLogic => CellState.ValueWithLogic(_value, _logicOp),
                CellStateType.LogicOnly      => CellState.LogicOnly(_logicOp),
                _ => CellState.PureValue(0)
            };
        }
        set
        {
            _stateType = value.type;
            _value = value.value;
            _logicOp = value.logic;
            ApplyLook();
        }
    }

    public override int Value
    {
        get
        {
            // Only PureValue state has a "committed" BFS-relevant value.
            // ValueWithLogic returns 0 (treated as broken circuit by BFS).
            // LogicOnly returns 0.
            if (_stateType == CellStateType.PureValue)
                return _value;
            return 0;
        }
    }

    public override bool LockAfterPlace => false;

    public override CellState GetCellState() => CellState;

    [Header("Visuals")]
    public Renderer targetRenderer;
    public Material matZero;
    public Material matOne;
    public Material matLogicWaiting; // for ValueWithLogic or LogicOnly states

    protected virtual void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
        ApplyLook();
    }

    private void ApplyLook()
    {
        if (targetRenderer == null) return;
        Material target = _stateType switch
        {
            CellStateType.PureValue      => (_value == 1) ? matOne : matZero,
            CellStateType.ValueWithLogic => matLogicWaiting,
            CellStateType.LogicOnly      => matLogicWaiting,
            _ => matZero
        };
        if (target != null)
        {
            targetRenderer.material = target;
            // Set emission on the INSTANCE (not shared material)
            var matInstance = targetRenderer.material;
            Color emit = _stateType switch
            {
                CellStateType.PureValue => _value == 1 ? Color.green * 1.5f : Color.red * 1.5f,
                CellStateType.ValueWithLogic => new Color(1f, 0.4f, 0f) * 1.5f,
                CellStateType.LogicOnly => new Color(0.3f, 0.3f, 0.3f) * 1.5f,
                _ => Color.black
            };
            matInstance.EnableKeyword("_EMISSION");
            matInstance.SetColor("_EmissionColor", emit);
        }
    }

    /// <summary>
    /// Accept a value into a waiting logic cell. Returns the computed result, or -1 if invalid.
    /// If this cell is v L ___, computes v L incoming = result, sets this cell to PureValue(result).
    /// If this cell is L ___ (LogicOnly), captures the value: becomes incoming L ___.
    /// </summary>
    public int AcceptValue(int incomingValue)
    {
        if (_stateType == CellStateType.PureValue)
        {
            // Already pure value — cannot accept another value without logic
            return -1;
        }

        if (_stateType == CellStateType.ValueWithLogic)
        {
            int result = CellState.Compute(_value, _logicOp, incomingValue);
            CellState = CellState.PureValue(result);
            return result;
        }

        if (_stateType == CellStateType.LogicOnly)
        {
            CellState = CellState.ValueWithLogic(incomingValue, _logicOp);
            return -1; // not yet resolved, waiting for second value
        }

        return -1;
    }

    /// <summary>
    /// Accept a logic operation. Only valid when this cell is PureValue.
    /// </summary>
    public bool AcceptLogic(LogicOp L)
    {
        if (_stateType != CellStateType.PureValue)
            return false;
        CellState = CellState.ValueWithLogic(_value, L);
        return true;
    }

    /// <summary>
    /// Apply NOT spray to this cell.
    /// </summary>
    public void ApplyNot()
    {
        if (_stateType == CellStateType.PureValue)
        {
            CellState = CellState.PureValue(1 - _value);
        }
        else if (_stateType == CellStateType.ValueWithLogic)
        {
            CellState = CellState.ValueWithLogic(1 - _value, _logicOp);
        }
        else if (_stateType == CellStateType.LogicOnly)
        {
            CellState = CellState.LogicOnly(CellState.FlipLogic(_logicOp));
        }
    }
}
