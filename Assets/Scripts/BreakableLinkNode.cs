using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BreakableLinkNode : MonoBehaviour
{
	// faceIndex -> Joint
	private readonly Dictionary<int, Joint> joints = new();

	public float reconnectCooldown = 0.2f;
	private float cooldown;

	void Update()
	{
		if (cooldown > 0f)
		{
			cooldown -= Time.deltaTime;
			return; // 冷却中跳过轮询
		}

		// 轮询检测已断裂的 Joint（替代已弃用的 OnJointBreak 回调）
		// OnJointBreak 仅在 Joint 所在 GameObject 上触发，而 Joint 可能在其他对象上创建
		var toRemove = new List<int>();
		foreach (var kv in joints)
		{
			if (!kv.Value) toRemove.Add(kv.Key);
		}
		if (toRemove.Count > 0)
		{
			foreach (var f in toRemove) joints.Remove(f);
			cooldown = reconnectCooldown;
		}
	}

	public bool TryRegisterJoint(int faceIndex, Joint j)
	{
		if (joints.ContainsKey(faceIndex) && joints[faceIndex]) return false;
		joints[faceIndex] = j;
		return true;
	}

	public bool IsCoolingDown() => cooldown > 0f;

	public void BreakAll()
	{
		foreach (var kv in joints)
		{
			if (kv.Value) Destroy(kv.Value);
		}
		joints.Clear();
		cooldown = reconnectCooldown;
	}
}
