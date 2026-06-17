// Scripts/Grid/GridNode.cs
using UnityEngine;

public class GridNode
{
	public GridIndex index;
	public Vector3 worldPos;
	public bool occupied;
	public TileBase placedTile;

	/// <summary>Logical state of this grid cell. Defaults to PureValue(0) when empty.</summary>
	public CellState cellState;

	public GridNode(GridIndex idx, Vector3 pos)
	{
		index = idx;
		worldPos = pos;
		occupied = false;
		placedTile = null;
		cellState = CellState.PureValue(0);
	}
}