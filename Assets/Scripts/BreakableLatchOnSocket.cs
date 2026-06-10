using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRSocketInteractor))]
public class BreakableLatchOnSocket : MonoBehaviour
{
	public float breakForce = 80f;
	public float breakTorque = 80f;
	public bool useConfigurable = true; // false时用FixedJoint
	public bool jointEnableCollision = false; // 连接后是否允许彼此碰撞
	public float projectionDistance = 0.005f;
	public float projectionAngle = 1f;

	XRSocketInteractor socket;
	XRInteractionManager im;

	void Awake()
	{
		socket = GetComponent<XRSocketInteractor>();
		im = FindObjectOfType<XRInteractionManager>();
		socket.selectEntered.AddListener(OnSelectEntered);
	}

	void OnDestroy()
	{
		socket.selectEntered.RemoveListener(OnSelectEntered);
	}

	void OnSelectEntered(SelectEnterEventArgs args)
	{
		// 仅处理可抓取物
		var interactable = args.interactableObject as XRGrabInteractable;
		if (!interactable) return;

		var inserted = (interactable as Component).transform;
		var host = socket.transform; // 父级，可按层级找到 host 的 Rigidbody
		var hostRb = host.GetComponentInParent<Rigidbody>();
		var insRb = inserted.GetComponent<Rigidbody>();
		if (!hostRb || !insRb) return;

		// 1) 先把被抓取物拉到 attachTransform（Socket 已帮你对齐）
		var at = socket.attachTransform;
		insRb.MovePosition(at.position);
		insRb.MoveRotation(at.rotation);

		// 2) 在被抓取物上创建可断裂连接。
		if (useConfigurable)
		{
			var j = inserted.gameObject.AddComponent<ConfigurableJoint>();
			j.connectedBody = hostRb;
			j.autoConfigureConnectedAnchor = true;
			j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
			j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Locked;
			j.breakForce = breakForce;
			j.breakTorque = breakTorque;
			j.enableCollision = jointEnableCollision;
			j.enablePreprocessing = true;
			j.projectionMode = JointProjectionMode.PositionAndRotation; // 关键：抗漂移
			j.projectionDistance = projectionDistance;
			j.projectionAngle = projectionAngle;
		}
		else
		{
			var j = inserted.gameObject.AddComponent<FixedJoint>();
			j.connectedBody = hostRb;
			j.breakForce = breakForce;
			j.breakTorque = breakTorque;
			j.enableCollision = jointEnableCollision;
			j.enablePreprocessing = true;
			// FixedJoint 没有显式 projection 参数；可通过其他方式补偿
		}

		// 3) 延迟释放 Socket 选择权，避免在 OnSelectEntered 回调内操作 XRI 状态机导致重入
		StartCoroutine(ExitSocketDeferred(interactable));

		// 4) 为稳定性提高求解器迭代次数（按需调整）
		BumpSolverIterations(insRb, 14, 14);
		BumpSolverIterations(hostRb, 14, 14);
	}

	IEnumerator ExitSocketDeferred(IXRSelectInteractable interactable)
	{
		yield return null; // 延迟一帧，确保 SelectEnter 事件处理完毕
		if (socket != null && socket.hasSelection)
			im.SelectExit((IXRSelectInteractor)socket, interactable);
	}

	static void BumpSolverIterations(Rigidbody rb, int it, int itVel)
	{
#if UNITY_2021_3_OR_NEWER
		if (!rb) return;
		rb.solverIterations = Mathf.Max(rb.solverIterations, it);
		rb.solverVelocityIterations = Mathf.Max(rb.solverVelocityIterations, itVel);
#endif
	}
}
