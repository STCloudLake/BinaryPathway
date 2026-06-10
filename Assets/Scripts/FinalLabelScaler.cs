using UnityEngine;

[ExecuteAlways]
public class FinalLabelScaler : MonoBehaviour
{
	public Camera cam;
	public float sizeOnScreen = 0.05f;  // 屏幕高度占比（0.05 = 屏幕高度 5%）
	public bool faceCamera = true;

	void Start()
	{
		if (cam == null)
			cam = Camera.main;
	}

	void LateUpdate()
	{
		if (!cam) return;

		// 1) Billboard（始终面向相机）
		if (faceCamera)
			transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

		// 2) 根据距离对缩放做补偿，让屏幕尺寸恒定
		float d = Vector3.Distance(transform.position, cam.transform.position);

		// 透视摄像机下：世界尺寸 = 距离 × 视角因子
		float h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
		float worldScale = (h * sizeOnScreen);

		transform.localScale = Vector3.one * worldScale;
	}
}