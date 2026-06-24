using UnityEngine;
using static GridIndex;

/// <summary>
/// ��ͨ�Կ��ӻ���
/// �������������壨�����壩�ϣ�ʵʱ��� GridContainer ��ָ����������ͨ�ԣ�
/// �����ݽ�����������ʼ��л����ṩ�Ӿ�������
/// </summary>
public class ConnectivityVisualizer : MonoBehaviour
{
	[Header("��������")]
	[Tooltip("Ҫ�����ͨ�Ե� GridContainer")]
	public GridContainer gridContainer;

	[Header("����")]
	[Tooltip("������������")]
	public GridIndex startIndex = new GridIndex(0, 0, 0);
	[Tooltip("�յ����������")]
	public GridIndex goalIndex = new GridIndex(5, 5, 0);

	[Header("����")]
	[Tooltip("��ͨʱʹ�õĲ��ʣ���ɫ������ȣ�")]
	public Material connectedMaterial;
	[Tooltip("����ͨʱʹ�õĲ��ʣ���ɫ�����õȣ�")]
	public Material disconnectedMaterial;

	[Header("����Ƶ��")]
	[Min(0.01f)]
	[Tooltip("ÿ����������һ����ͨ�ԣ�0 ��ʾÿ֡��⣩")]
	public float updateInterval = 0.1f;

	[Header("����")]
	public bool debugLogs = false;

	/// <summary>
	/// Fires when connectivity state changes. true = connected, false = disconnected.
	/// </summary>
	public event System.Action<bool> OnConnectivityChanged;

	private Renderer _renderer;
	private float _timeSinceLastCheck;
	private bool _lastConnectivityState = false;
	private bool _lastStateKnown = false;

	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
		if (_renderer == null)
		{
			Debug.LogError($"[{name}] ConnectivityVisualizer ��Ҫ�������� Renderer ��������");
		}

		if (gridContainer == null)
		{
			Debug.LogWarning($"[{name}] GridContainer δָ���������Զ�����...");
			gridContainer = FindFirstObjectByType<GridContainer>();
			if (gridContainer != null)
				Debug.Log($"[{name}] �Զ��ҵ� GridContainer: {gridContainer.name}");
		}
	}

	private void Update()
	{
		if (_renderer == null || gridContainer == null) return;

		// �����¼�����
		_timeSinceLastCheck += Time.deltaTime;
		if (_timeSinceLastCheck >= updateInterval)
		{
			_timeSinceLastCheck = 0f;
			CheckAndUpdateConnectivity();
		}
	}

	private void CheckAndUpdateConnectivity()
	{
		// ��������Ƿ���Ч
		if (!gridContainer.InBounds(startIndex))
		{
			Debug.LogWarning($"[{name}] ������� {startIndex} ������Χ");
			return;
		}
		if (!gridContainer.InBounds(goalIndex))
		{
			Debug.LogWarning($"[{name}] �յ����� {goalIndex} ������Χ");
			return;
		}

		// �����ͨ��
		bool isConnected = gridContainer.CheckConnectivity(startIndex, goalIndex);

		// ֻ��״̬�ı�ʱ���²��ʣ�����Ƶ������
		if (!_lastStateKnown || isConnected != _lastConnectivityState)
		{
			UpdateMaterial(isConnected);
			_lastConnectivityState = isConnected;
			_lastStateKnown = true;

			Debug.Log($"[Connectivity] {startIndex} -> {goalIndex} = {(isConnected ? "CONNECTED" : "BROKEN")}");
		}
	}

	private void UpdateMaterial(bool isConnected)
	{
		Material targetMat = isConnected ? connectedMaterial : disconnectedMaterial;
		if (targetMat != null && _renderer != null)
		{
			_renderer.material = targetMat;

			// Fire event for external listeners (e.g. GameManager)
			OnConnectivityChanged?.Invoke(isConnected);
		}
	}

	/// <summary>
	/// �ֶ����һ����ͨ�ԣ������ⲿ�ű����¼����ã�
	/// </summary>
	public bool ManualCheckConnectivity()
	{
		if (gridContainer == null) return false;
		if (!gridContainer.InBounds(startIndex) || !gridContainer.InBounds(goalIndex)) return false;

		bool result = gridContainer.CheckConnectivity(startIndex, goalIndex);
		if (debugLogs)
			Debug.Log($"[{name}] �ֶ����: {startIndex} -> {goalIndex} = {(result ? "��ͨ ?" : "�Ͽ� ?")}");

		return result;
	}

	/// <summary>
	/// �����µļ���
	/// </summary>
	public void SetCheckPoints(GridIndex newStart, GridIndex newGoal)
	{
		startIndex = newStart;
		goalIndex = newGoal;
		_lastStateKnown = false; // ����״̬��ǿ���´θ���
		if (debugLogs)
			Debug.Log($"[{name}] �����Ѹ���: {startIndex} -> {goalIndex}");
	}

#if UNITY_EDITOR
#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (gridContainer == null) return;

		// Avoid calling GetWorldPos during domain reload / grid rebuild
		if (!gridContainer.InBounds(startIndex) || !gridContainer.InBounds(goalIndex))
			return;

		Vector3 startPos = gridContainer.GetWorldPos(startIndex);
		Vector3 goalPos = gridContainer.GetWorldPos(goalIndex);

		// Check positions are valid (non-zero when bounds pass)
		if (startPos == Vector3.zero && goalPos == Vector3.zero) return;

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(startPos, 0.05f);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(goalPos, 0.05f);

		if (_lastStateKnown)
		{
			Gizmos.color = _lastConnectivityState ? Color.green : Color.red;
			Gizmos.DrawLine(startPos, goalPos);
		}
	}
#endif
#endif
}
