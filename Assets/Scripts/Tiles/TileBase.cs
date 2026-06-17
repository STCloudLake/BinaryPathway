// Scripts/Tiles/TileBase.cs
using UnityEngine;

public abstract class TileBase : MonoBehaviour
{
	[Header("Tile Properties")]
	public abstract int Value { get; }

	public virtual bool LockAfterPlace => true;

	/// <summary>
	/// Returns the CellState of this tile. Default: PureValue based on Value property.
	/// Override in LogicTile to return ValueWithLogic or LogicOnly states.
	/// </summary>
	public virtual CellState GetCellState() => CellState.PureValue(Value);

	public virtual void OnPlaced(GridContainer container, GridIndex index) { }
	public virtual void OnRemoved(GridContainer container, GridIndex index) { }
}
