// Scripts/Grid/GridContainer.cs
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


[ExecuteAlways]
public class GridContainer : MonoBehaviour
{
	[Header("�ߴ��벼��")]
	[Min(1)] public int width = 6;
	[Min(1)] public int height = 6;
	[Min(1)] public int layers = 1; // 1=2D, >1=3D
	[Min(0.05f)] public float cellSize = 0.3f;
	public Vector3 originOffset = Vector3.zero;
	public bool centerPivot = true;

	[Header("调试")]
	public bool debugConnectivityLogs = false;

	/// <summary>Fired after a tile is successfully placed on the grid.</summary>
	public event System.Action<TileBase, GridIndex> OnTilePlaced;
	/// <summary>Fired after a tile is successfully removed from the grid.</summary>
	public event System.Action<TileBase, GridIndex> OnTileRemoved;

	[Header("�ڽ������")]
	public bool allowDiagonals2D = false;
	public bool allowDiagonals3D = false; // Ĭ�� 6 �ڽ�

	[Header("Socket/ռλ����")]
	public GameObject nodeSocketPrefab; // ������� XR Socket Interactor ���Զ��� GridSocket
	public Transform socketsRoot;

	private GridNode[,,] _nodes;
	private readonly Vector3[] _dirs2D4 = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down };
	private readonly Vector3Int[] _dirs3D6 = new[] {
		new Vector3Int( 1, 0, 0), new Vector3Int(-1, 0, 0),
		new Vector3Int( 0, 1, 0), new Vector3Int( 0,-1, 0),
		new Vector3Int( 0, 0, 1), new Vector3Int( 0, 0,-1),
	};


	// ��ǣ��༭������������һ�����ؽ������� OnValidate �ڼ�����
	[HideInInspector] private bool _needsRebuild; // 非序列化，避免 OnValidate 与 delayCall 之间保存场景导致状态陈旧


	//[Header("�����ÿ��ӻ�")]
	//public GameObject nodeVisualPrefab;  
	//public bool autoGenerateAtStart = true;


	public bool Is3D => layers > 1;


	//private void Start()
	//{
	//	if (Application.isPlaying && autoGenerateAtStart)
	//	{
	//		Regenerate();
	//	}
	//}


	private void Start()
	{
		// ����ʱ��֤������
		if (Application.isPlaying)
		{
			RegenerateSafeRuntime();
		}
	}


	public void Regenerate()
	{
		AllocateNodes();
		BuildSockets();
		//BuildVisuals();   
	}

	private void OnValidate()
	{
		width = Mathf.Max(1, width);
		height = Mathf.Max(1, height);
		layers = Mathf.Max(1, layers);
		cellSize = Mathf.Max(0.05f, cellSize);
		// �ڱ༭�����Զ�Ԥ��
		//if (!Application.isPlaying) Regenerate();
		_needsRebuild = true;

		#if UNITY_EDITOR
			// �� delayCall ���ؽ��Ƴٵ���ȫʱ�����뿪 OnValidate �ĵ���ջ��
			EditorApplication.delayCall -= RebuildIfNeeded;
			EditorApplication.delayCall += RebuildIfNeeded;
		#endif
	}


#if UNITY_EDITOR
	private void RebuildIfNeeded()
	{
		if (this == null) return; // ����ѱ�ɾ
		if (!_needsRebuild) return;
		// ֻ�ڱ༭���ҷ� Play ģʽ���ñ༭����ȫ�ķ�ʽ�ؽ�
		if (!Application.isPlaying)
			RegenerateSafeEditor();
		else
			RegenerateSafeRuntime();

		_needsRebuild = false;
	}
#endif


	// ����ʱ��ȫ���ؽ���ȫ�� Destroy��
	private void RegenerateSafeRuntime()
	{
		AllocateNodes();
		BuildSocketsRuntime(); // �� Destroy()
		//BuildVisualsRuntime(); // �� Destroy()
	}

#if UNITY_EDITOR
	// �༭����ȫ���ؽ���ȫ�� DestroyImmediate �� Undo��
	private void RegenerateSafeEditor()
	{
		AllocateNodes();
		BuildSocketsEditor();  // �� DestroyImmediate/Undo
		//BuildVisualsEditor();  // �� DestroyImmediate/Undo
	}

	[ContextMenu("Regenerate Grid (Editor)")]
	private void ContextRegenerate()
	{
		RegenerateSafeEditor();
	}
#endif


	private void AllocateNodes()
	{
		_nodes = new GridNode[width, height, layers];

		Vector3 origin = transform.position + originOffset;
		if (centerPivot)
		{
			var size = new Vector3((width - 1) * cellSize, (height - 1) * cellSize, (layers - 1) * cellSize);
			origin -= 0.5f * size;
		}

		for (int z = 0; z < layers; z++)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var idx = new GridIndex(x, y, z);
					var pos = origin + new Vector3(x * cellSize, y * cellSize, z * cellSize);
					_nodes[x, y, z] = new GridNode(idx, pos);
				}
			}
		}
	}

	private void BuildSockets()
	{
		if (socketsRoot == null)
		{
			var go = new GameObject("SocketsRoot");
			go.transform.SetParent(transform, false);
			socketsRoot = go.transform;
		}
		// �����ɵ�
		for (int i = socketsRoot.childCount - 1; i >= 0; i--)
		{
			if (Application.isPlaying) Destroy(socketsRoot.GetChild(i).gameObject);
			else DestroyImmediate(socketsRoot.GetChild(i).gameObject);
		}

		if (nodeSocketPrefab == null) return;

		// �����µ�
		for (int z = 0; z < layers; z++)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					var node = _nodes[x, y, z];
					var sok = Instantiate(nodeSocketPrefab, node.worldPos, Quaternion.identity, socketsRoot);
					// �������󶨵� socket�����ڷ��ûص���
					var gs = sok.GetComponent<GridSocket>();
					if (gs != null)
					{
						gs.Bind(this, node.index);
					}
				}
			}
		}
	}


	private void ClearChildrenRuntime(Transform root)
	{
		for (int i = root.childCount - 1; i >= 0; i--)
		{
			Destroy(root.GetChild(i).gameObject);
		}
	}

	#if UNITY_EDITOR
		private void ClearChildrenEditor(Transform root, bool recordUndo = false)
		{
			for (int i = root.childCount - 1; i >= 0; i--)
			{
				var child = root.GetChild(i).gameObject;
				if (recordUndo)
					Undo.DestroyObjectImmediate(child);
				else
					DestroyImmediate(child);
			}
		}
#endif


	// ������ȷ�� socketsRoot ����
	private void EnsureSocketsRoot()
	{
		if (socketsRoot == null)
		{
			var go = new GameObject("SocketsRoot");
			go.transform.SetParent(transform, false);
			socketsRoot = go.transform;
		}
	}

	// ����ʱ�����پɽڵ� �� ʵ�����½ڵ㣨Destroy��
	private void BuildSocketsRuntime()
	{
		EnsureSocketsRoot();
		ClearChildrenRuntime(socketsRoot);
		if (nodeSocketPrefab == null) return;

		for (int z = 0; z < layers; z++)
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					var node = _nodes[x, y, z];
					var sok = Instantiate(nodeSocketPrefab, node.worldPos, Quaternion.identity, socketsRoot);
					var gs = sok.GetComponent<GridSocket>();
					if (gs != null) gs.Bind(this, node.index);
				}
	}

#if UNITY_EDITOR
	// �༭�������پɽڵ� �� ʵ�����½ڵ㣨DestroyImmediate/Undo��
	private void BuildSocketsEditor()
	{
		EnsureSocketsRoot();
		ClearChildrenEditor(socketsRoot, recordUndo: true);
		if (nodeSocketPrefab == null) return;

		for (int z = 0; z < layers; z++)
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					var node = _nodes[x, y, z];
					var sok = (GameObject)PrefabUtility.InstantiatePrefab(nodeSocketPrefab, socketsRoot);
					sok.transform.position = node.worldPos;
					sok.transform.rotation = Quaternion.identity;

					var gs = sok.GetComponent<GridSocket>();
					if (gs != null) gs.Bind(this, node.index);
					Undo.RegisterCreatedObjectUndo(sok, "Create Grid Socket");
				}
	}
#endif


	//private void BuildVisuals()
	//{
	//	if (nodeVisualPrefab == null) return;

	//	// ��һ�����ڵ�
	//	var visRoot = transform.Find("NodeVisuals");
	//	if (visRoot != null)
	//	{
	//		if (Application.isPlaying) Destroy(visRoot.gameObject);
	//		else DestroyImmediate(visRoot.gameObject);
	//	}
	//	var go = new GameObject("NodeVisuals");
	//	go.transform.SetParent(transform, false);
	//	visRoot = go.transform;

	//	for (int z = 0; z < layers; z++)
	//	{
	//		for (int y = 0; y < height; y++)
	//		{
	//			for (int x = 0; x < width; x++)
	//			{
	//				var node = _nodes[x, y, z];
	//				var v = Instantiate(nodeVisualPrefab, node.worldPos, Quaternion.identity, visRoot);
	//				v.name = $"Node_{x}_{y}_{z}";
	//			}
	//		}
	//	}
	//}



	public bool InBounds(GridIndex i) =>
		i.x >= 0 && i.x < width && i.y >= 0 && i.y < height && i.z >= 0 && i.z < layers;

public GridNode GetNode(GridIndex i)
	{
		if (_nodes == null) return null;
		return InBounds(i) ? _nodes[i.x, i.y, i.z] : null;
	}

public Vector3 GetWorldPos(GridIndex i)
	{
		if (_nodes == null) return Vector3.zero;
		return GetNode(i)?.worldPos ?? Vector3.zero;
	}

	public bool CanPlace(GridIndex i, TileBase tile)
	{
		var n = GetNode(i);
		if (n == null) return false;
		if (n.occupied) return false;
		// ������ԼӸ�������磺������ĳ�����ڡ��㼶���Ƶ�
		return tile != null;
	}

	public bool Place(GridIndex i, TileBase tile)
	{
		if (!CanPlace(i, tile)) return false;
		var n = GetNode(i);
		n.occupied = true;
		n.placedTile = tile;
		n.cellState = tile.GetCellState(); // Sync CellState from tile
		tile.transform.position = n.worldPos;
		tile.transform.rotation = Quaternion.identity; // Align to grid
		tile.OnPlaced(this, i);
			OnTilePlaced?.Invoke(tile, i);
		return true;
	}

	public bool Remove(GridIndex i)
	{
		var n = GetNode(i);
		if (n == null || !n.occupied) return false;
		var tile = n.placedTile;
		n.occupied = false;
		n.placedTile = null;
		n.cellState = CellState.PureValue(0); // Reset cell state on remove
		if (tile != null) tile.OnRemoved(this, i);
			OnTileRemoved?.Invoke(tile, i);

		//SetNodeMaterial(i, defaultMat);
		return true;
	}


	// 缓存的 BFS 容器，避免每次分配
	private readonly Queue<GridIndex> _bfsQueue = new Queue<GridIndex>();
	private readonly HashSet<GridIndex> _bfsVisited = new HashSet<GridIndex>();
	// ��ͨ�ԣ����� value==1 �ĸ�����Ϊ��ͨ�нڵ㣨�ɰ�����չ��
public bool CheckConnectivity(GridIndex start, GridIndex goal)
	{
		if (!InBounds(start) || !InBounds(goal)) return false;
		var s = GetNode(start);
		var g = GetNode(goal);
		if (s == null || g == null) return false;
		if (!IsOne(s) || !IsOne(g)) return false;

		_bfsQueue.Clear();
		_bfsVisited.Clear();

		_bfsVisited.Add(start);
		_bfsQueue.Enqueue(start);

		while (_bfsQueue.Count > 0)
		{
			var cur = _bfsQueue.Dequeue();
			if (cur.Equals(goal)) return true;

			foreach (var nb in GetNeighbors(cur))
			{
				if (!_bfsVisited.Contains(nb) && IsOne(GetNode(nb)))
				{
					_bfsVisited.Add(nb);
					_bfsQueue.Enqueue(nb);
				}
			}
		}
		return false;
	}

	private bool IsOne(GridNode node)
	{
		if (node == null || !node.occupied) return false;
		// Use CellState for BFS — only PureValue(1) is conductive
		return node.cellState.IsConductive();
	}

	/// <summary>
	/// Place a LogicTile cell onto a grid node. Handles CellState-based logic
	/// interaction with the existing grid state. Does NOT check bounds or conflicts
	/// (caller is responsible). Used by LogicBlock.PlaceOnGrid().
	/// </summary>
	public void PlaceLogicCell(GridIndex i, LogicTile logicCell)
	{
		var node = GetNode(i);
		if (node == null) return;

		CellState incoming = logicCell.CellState;
		CellState existing = node.cellState;

		if (incoming.type == CellStateType.ValueWithLogic)
		{
			if (existing.type == CellStateType.PureValue)
			{
				// Compute: incoming.v L existing.v = result
				int result = CellState.Compute(incoming.value, incoming.logic, existing.value);
				node.cellState = CellState.PureValue(result);
				if (debugConnectivityLogs)
					Debug.Log($"[GridContainer] Logic merge at {i}: {incoming.value} {incoming.logic} {existing.value} = {result}");
			}
			else
			{
				// Existing is also waiting or logic-only — just overwrite
				node.cellState = incoming;
			}
		}
		else if (incoming.type == CellStateType.LogicOnly)
		{
			if (existing.type == CellStateType.PureValue)
			{
				// Capture: grid value becomes value for the logic
				node.cellState = CellState.ValueWithLogic(existing.value, incoming.logic);
			}
			else
			{
				node.cellState = incoming;
			}
		}
		else // PureValue
		{
			if (existing.type == CellStateType.PureValue)
			{
				// Both pure values — keep existing (incoming just attaches physically)
			}
			else
			{
				// Overwrite waiting/logic-only with pure value
				node.cellState = incoming;
			}
		}

		// Mark as occupied
		node.occupied = true;
		node.placedTile = logicCell;

		// Position the LogicTile at the grid cell
		logicCell.transform.position = node.worldPos;
		logicCell.transform.rotation = Quaternion.identity;
		logicCell.OnPlaced(this, i);
		OnTilePlaced?.Invoke(logicCell, i);
	}

	public IEnumerable<GridIndex> GetNeighbors(GridIndex i)
	{
		if (!InBounds(i)) yield break;

		if (!Is3D)
		{
			// 2D��4 �ڽӣ���ѡ�Խǣ�
			var dirs = new Vector2Int[] {
				new Vector2Int( 1, 0), new Vector2Int(-1, 0),
				new Vector2Int( 0, 1), new Vector2Int( 0,-1),
			};
			foreach (var d in dirs)
			{
				var nb = new GridIndex(i.x + d.x, i.y + d.y, i.z);
				if (InBounds(nb)) yield return nb;
			}
			if (allowDiagonals2D)
			{
				var diag = new Vector2Int[] {
					new Vector2Int( 1, 1), new Vector2Int( 1,-1),
					new Vector2Int(-1, 1), new Vector2Int(-1,-1),
				};
				foreach (var d in diag)
				{
					var nb = new GridIndex(i.x + d.x, i.y + d.y, i.z);
					if (InBounds(nb)) yield return nb;
				}
			}
		}
		else
		{
			// 3D��6 �ڽӣ���ѡ 26 �ڽ���չ��
			foreach (var d in _dirs3D6)
			{
				var nb = new GridIndex(i.x + d.x, i.y + d.y, i.z + d.z);
				if (InBounds(nb)) yield return nb;
			}
			// ���� 26 �ڽӣ��ڴ���չ allowDiagonals3D
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (_nodes == null) return;
		// 使用数组实际尺寸遍历，避免因字段值已变更但数组尚未重建导致的 IndexOutOfRangeException
		int maxX = _nodes.GetLength(0);
		int maxY = _nodes.GetLength(1);
		int maxZ = _nodes.GetLength(2);
		Gizmos.color = Color.gray;
		for (int z = 0; z < maxZ; z++)
		{
			for (int y = 0; y < maxY; y++)
			{
				for (int x = 0; x < maxX; x++)
				{
					var n = _nodes[x, y, z];
					if (n == null) continue;
					var c = n.occupied ? (IsOne(n) ? Color.green : Color.blue) : Color.gray;
					Gizmos.color = c;
					Gizmos.DrawWireCube(n.worldPos, Vector3.one * (cellSize * 0.92f));
				}
			}
		}
	}
#endif
}
