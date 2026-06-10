// Scripts/Grid/GridIndex.cs
using UnityEngine;

[System.Serializable]
public struct GridIndex
{
	public int x, y, z;
	public GridIndex(int x, int y, int z = 0) { this.x = x; this.y = y; this.z = z; }
	public override string ToString() => $"({x},{y},{z})";
	public static readonly GridIndex Invalid = new GridIndex(-1, -1, -1);
	public override int GetHashCode() => (x, y, z).GetHashCode();
	public override bool Equals(object obj) => obj is GridIndex other && x == other.x && y == other.y && z == other.z;
}