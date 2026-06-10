// Scripts/Grid/GridNode.cs
using UnityEngine;

public class GridNode
{
	public GridIndex index;
	public Vector3 worldPos;
	public bool occupied;
	public TileBase placedTile;

	public GridNode(GridIndex idx, Vector3 pos)
	{
		index = idx;
		worldPos = pos;
		occupied = false;
		placedTile = null;
	}
}