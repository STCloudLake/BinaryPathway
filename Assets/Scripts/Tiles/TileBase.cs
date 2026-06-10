// Scripts/Tiles/TileBase.cs
using UnityEngine;

public abstract class TileBase : MonoBehaviour
{
	[Header("Tile Properties")]
	// 0/1 真值语义（可扩展为多态：阻塞、切换、逻辑门等）
	public abstract int Value { get; }

	// 放置后是否可被旋转/替换（可扩展玩法）
	public virtual bool LockAfterPlace => true;

	// 放置时回调（用于播放 SFX / 粒子 / Haptics）
	public virtual void OnPlaced(GridContainer container, GridIndex index) { }
	public virtual void OnRemoved(GridContainer container, GridIndex index) { }
}
