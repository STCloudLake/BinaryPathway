// Scripts/Tiles/TileToggle.cs
using UnityEngine;

public class ToggleTile : TileBase
{
	[SerializeField] private int _value = 0; // ��ֵ
	public override int Value => _value;
	public override bool LockAfterPlace => false; // ���º��Կɲ���

	[Header("���")]
	public Material matZero;
	public Material matOne;
	public Renderer targetRenderer;

	public override void OnPlaced(GridContainer container, GridIndex index)
	{
		ApplyLook();
	}

	public void Toggle()
	{
		_value = 1 - _value;
		ApplyLook();
	}

	private void ApplyLook()
	{
		if (!targetRenderer) return;
		var mats = targetRenderer.sharedMaterials;
		for (int i = 0; i < mats.Length; ++i)
			mats[i] = (_value == 1 ? matOne : matZero);
		targetRenderer.sharedMaterials = mats;
	}
}