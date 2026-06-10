using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class GridSocket : MonoBehaviour
{
	private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor _socket;
	private GridContainer _container;
	private GridIndex _index;

	[Header("本地指示器（用于高亮/占用）")]
	[Tooltip("可见的小网格/环的Renderer；若为空，会在子物体中自动查找")]
	public Renderer indicatorRenderer;

	[Header("指示器材质（方案B：材质放在Socket，而不是GridContainer）")]
	public Material defaultMat;    // 默认
	public Material hoverMat;      // 吸附预览
	public Material occupiedMat;   // 已占用


	[Header("吸附手感参数")]
	[Tooltip("Hover 阶段吸附半径内的平滑靠拢速度（越大越“粘”）")]
	public float previewSnapSpeed = 18f;
	[Tooltip("Hover 阶段是否做持续平滑吸附")]
	public bool smoothPreview = true;
	[Tooltip("Hover 时仅当距离小于该阈值才开始吸附（建议 ~ 0.75 * 触发器半径）")]
	public float previewStartDistance = 0.2f;


	//——— 对外绑定（GridContainer在生成socket时调用） ———//
	public void Bind(GridContainer container, GridIndex index)
	{
		_container = container;
		_index = index;
	}

	private void Awake()
	{
		_socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
		if (indicatorRenderer == null)
			indicatorRenderer = GetComponentInChildren<Renderer>(true);

		_socket.hoverEntered.AddListener(OnHoverEntered);
		_socket.hoverExited.AddListener(OnHoverExited);
		_socket.selectEntered.AddListener(OnSelectEntered);

		SetIndicatorMat(defaultMat);
	}

	private void OnDestroy()
	{
		if (_socket != null)
		{
			_socket.hoverEntered.RemoveListener(OnHoverEntered);
			_socket.hoverExited.RemoveListener(OnHoverExited);
			_socket.selectEntered.RemoveListener(OnSelectEntered);
		}
	}

	private void OnHoverEntered(HoverEnterEventArgs args)
	{
		// Debug.Log($"[GridSocket] HoverEnter idx={_index}");
		var tile = args.interactableObject.transform.GetComponent<TileBase>();
		if (tile == null) return;

		// 吸附预览（如果你实现了视觉吸附，可在此对齐 attachTransform）
		WeakSnapOnce(tile);
		// 强制把 tile 瞬间对齐到格点（仅测试）tile.transform.position = _container.GetWorldPos(_index);
		SetIndicatorMat(hoverMat);
	}

	private void OnHoverExited(HoverExitEventArgs args)
	{
		// Debug.Log($"[GridSocket] HoverExit  idx={_index}");
		var tile = args.interactableObject.transform.GetComponent<TileBase>();
		if (tile == null) return;

		// 离开时：如果该格点已被占用 → 占用材质；否则 → 默认材质
		var n = _container?.GetNode(_index);
		SetIndicatorMat((n != null && n.occupied) ? occupiedMat : defaultMat);
	}

	private void OnSelectEntered(SelectEnterEventArgs args)
	{
		// Debug.Log($"[GridSocket] SelectEnter idx={_index}");
		var go = args.interactableObject.transform.gameObject;
		var tile = go.GetComponent<TileBase>();
		if (tile == null) { Reject(go); return; }

		if (_container != null && _container.CanPlace(_index, tile))
		{
			HardSnap(tile);


			// 关键：清零速度，避免松手后有残留动量（仅非 kinematic）
			var rb = tile.GetComponent<Rigidbody>();
			if (rb && !rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }


			// 真正落位（由容器完成：节点占用、位置对齐、Tile.OnPlaced）
			_container.Place(_index, tile);

			// 落位成功 → 占用材质
			SetIndicatorMat(occupiedMat);

			// 放下后是否允许再次抓取
			var grab = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
			if (grab && tile.LockAfterPlace) grab.enabled = true;
		}
		else
		{
			Reject(go);
		}
	}

	private void Reject(GameObject go)
	{
		if (_socket.hasSelection && _socket.isPerformingManualInteraction)
			_socket.EndManualInteraction();
		var rb = go.GetComponent<Rigidbody>();
		if (rb && !rb.isKinematic) rb.AddForce(Vector3.up * 0.5f, ForceMode.VelocityChange);
		// TODO: 可加红色闪烁/短震
	}

	private void SetIndicatorMat(Material m)
	{
		if (indicatorRenderer != null && m != null)
			indicatorRenderer.material = m;
	}

	// 如果你实现了“平滑吸附预览”，可以像下面这样在Update里插值靠拢 attachTransform
	private void Update()
	{
		if (!smoothPreview || _socket == null) return;
		if (_socket.interactablesHovered.Count == 0) return;

		// 简化：只对第一个悬停交互对象做处理
		var hovered = _socket.interactablesHovered[0];
		var tile = hovered.transform.GetComponent<TileBase>();
		if (tile == null) return;


		// 计算与格点的距离，太远则不吸
		Vector3 targetPos = _container.GetWorldPos(_index);
		Transform refT = GetAttachOrSelf(tile); // 以 AttachTransform 为参考更直观
		float dist = Vector3.Distance(refT.position, targetPos);
		if (dist > previewStartDistance) return;


		// 位置/旋转双向“阻尼”靠拢（不会一下跳上去）
		float k = Time.deltaTime * previewSnapSpeed;
		refT.position = Vector3.Lerp(refT.position, targetPos, k);
		refT.rotation = Quaternion.Slerp(refT.rotation, Quaternion.identity, k);


		// 目标对齐到网格世界坐标（你也可以对齐 attachTransform）
		//var targetPos = _container.GetWorldPos(_index);
		//var t = tile.transform;
		//t.position = Vector3.Lerp(t.position, targetPos, Time.deltaTime * previewSnapSpeed);
		// 如有方向性：再做旋转对齐
		// t.rotation = Quaternion.Slerp(t.rotation, Quaternion.identity, Time.deltaTime * previewSnapSpeed);
	}


	// —— 轻微瞬间贴近：让玩家“立刻感知到被吸” —— 
	private void WeakSnapOnce(TileBase tile)
	{
		if (_container == null) return;

		var refT = GetAttachOrSelf(tile);
		Vector3 targetPos = _container.GetWorldPos(_index);

		// 只移动一小步，避免突兀
		refT.position = Vector3.Lerp(refT.position, targetPos, 0.35f);
		refT.rotation = Quaternion.Slerp(refT.rotation, Quaternion.identity, 0.35f);
	}

	// —— 强力对齐：Select 时使用（瞬间对齐并清零速度） —— 
	private void HardSnap(TileBase tile)
	{
		if (_container == null) return;

		var refT = GetAttachOrSelf(tile);
		Vector3 targetPos = _container.GetWorldPos(_index);
		refT.position = targetPos;
		refT.rotation = Quaternion.identity;
	}


	// —— 取 AttachTransform（如未设置则回退 transform） —— 
	private Transform GetAttachOrSelf(TileBase tile)
	{
		var grab = tile.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
		if (grab && grab.attachTransform) return grab.attachTransform;
		return tile.transform;
	}


}