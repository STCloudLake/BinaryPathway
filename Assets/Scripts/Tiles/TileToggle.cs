// Scripts/Tiles/TileToggle.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ToggleTile : TileBase
{
	[SerializeField] private int _value = 0;
	public override int Value => _value;
	public override bool LockAfterPlace => false;

	[Header("素材")]
	public Material matZero;
	public Material matOne;
	public Renderer targetRenderer;

	[Header("Toggle Interaction")]
	[Tooltip("Trigger 子物体的碰撞体缩放倍数")]
	public float triggerScale = 1.5f;

	private XRSimpleInteractable _toggleInteractable;
	private GameObject _toggleTrigger;
	private XRInteractionManager _interactionManager;

	void Awake()
	{
		_interactionManager = FindFirstObjectByType<XRInteractionManager>();
	}

	public override void OnPlaced(GridContainer container, GridIndex index)
	{
		ApplyLook();

		// 创建 toggle trigger 子物体（首次放置时）
		if (_toggleTrigger == null)
		{
			_toggleTrigger = new GameObject("ToggleTrigger");
			_toggleTrigger.transform.SetParent(transform, false);
			_toggleTrigger.transform.localPosition = Vector3.zero;
			_toggleTrigger.transform.localScale = Vector3.one * triggerScale;
			_toggleTrigger.layer = gameObject.layer;

			var col = _toggleTrigger.AddComponent<BoxCollider>();
			col.isTrigger = true;

			_toggleInteractable = _toggleTrigger.AddComponent<XRSimpleInteractable>();
			_toggleInteractable.interactionManager = _interactionManager;
			_toggleInteractable.selectMode = InteractableSelectMode.Single;
			_toggleInteractable.selectEntered.AddListener(_ => Toggle());
		}

		_toggleTrigger.SetActive(true);
		if (_interactionManager != null && _toggleInteractable != null)
			_interactionManager.RegisterInteractable((IXRInteractable)_toggleInteractable);
	}

	public override void OnRemoved(GridContainer container, GridIndex index)
	{
		if (_toggleTrigger != null)
			_toggleTrigger.SetActive(false);
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
