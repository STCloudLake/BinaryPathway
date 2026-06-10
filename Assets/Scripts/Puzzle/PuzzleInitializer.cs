using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ƴͼ��ʼ����
/// �� GridContainer ���ɲ�ۺ��Զ���ʼ��һ��ƴͼ��
/// - �������һ������㵽�յ��·��
/// - ·���� pathTilePrefab ��䣨value=1��
/// - �����λ�� emptyTilePrefab ��䣨value=0��
/// - �����յ��� startMarker �� goalMarker ���
/// </summary>
public class PuzzleInitializer : MonoBehaviour
{
	[Header("����")]
	[Tooltip("ƴͼ������ GridContainer")]
	public GridContainer gridContainer;

	[Header("Tile Prefabs")]
	[Tooltip("·���ϵ�Tile��value=1��")]
	public GameObject pathTilePrefab;
	[Tooltip("�����λ��Tile��value=0��")]
	public GameObject emptyTilePrefab;

	[Header("������յ�")]
	[Tooltip("���������������·����ʼ��")]
	public GridIndex startIndex = new GridIndex(0, 0, 0);
	[Tooltip("�յ�������������·��ĩβ��")]
	public GridIndex goalIndex = new GridIndex(5, 5, 0);

	[Tooltip("���Ŀ��ӱ�ǣ���ѡ�������壩")]
	public GameObject startMarker;
	[Tooltip("�յ�Ŀ��ӱ�ǣ���ѡ�������壩")]
	public GameObject goalMarker;

	[Header("����ѡ��")]
	[Tooltip("·���㷨��0=ֱ�����·��, 1=���·��, 2=�Թ�ʽ")]
	[Range(0, 2)] public int pathAlgorithm = 1;

	[Tooltip("���Ϊtrue���Զ���GridContainer����ʱִ�г�ʼ��")]
	public bool autoInitializeOnStart = true;

	[Header("����")]
	public bool debugLogs = false;

	private List<GridIndex> _currentPath;

	private void Start()
	{
		if (autoInitializeOnStart && gridContainer != null)
		{
			InitializePuzzle();
		}
	}

	/// <summary>
	/// ��ʼ��ƴͼ��������
	/// </summary>
	public void InitializePuzzle()
	{
		if (gridContainer == null)
		{
			Debug.LogError("[PuzzleInitializer] GridContainer δָ��");
			return;
		}

		if (!gridContainer.InBounds(startIndex) || !gridContainer.InBounds(goalIndex))
		{
			Debug.LogError("[PuzzleInitializer] �����յ㳬������Χ");
			return;
		}

		if (pathTilePrefab == null || emptyTilePrefab == null)
		{
			Debug.LogError("[PuzzleInitializer] ·�����λTile prefabδָ��");
			return;
		}

		// 1) ����·��
		_currentPath = GeneratePath(startIndex, goalIndex);
		if (_currentPath == null || _currentPath.Count == 0)
		{
			Debug.LogError("[PuzzleInitializer] �޷����ɴ���㵽�յ��·��");
			return;
		}

		if (debugLogs)
			Debug.Log($"[PuzzleInitializer] ����·�������� {_currentPath.Count}");

		// 2) �����������·����pathTile��������emptyTile
		FillGrid(_currentPath);

		// 3) ���ñ��
		if (startMarker != null)
		{
			startMarker.transform.position = gridContainer.GetWorldPos(startIndex);
		}
		if (goalMarker != null)
		{
			goalMarker.transform.position = gridContainer.GetWorldPos(goalIndex);
		}

		if (debugLogs)
			Debug.Log("[PuzzleInitializer] ƴͼ��ʼ�����");
	}

	/// <summary>
	/// ����ѡ���㷨����·��
	/// </summary>
	private List<GridIndex> GeneratePath(GridIndex start, GridIndex goal)
	{
		return pathAlgorithm switch
		{
			0 => GenerateStraightPath(start, goal),
			1 => GenerateRandomPath(start, goal),
			2 => GenerateMazePath(start, goal),
			_ => GenerateStraightPath(start, goal),
		};
	}

	/// <summary>
	/// ֱ�����·���������پ��룩
	/// </summary>
	private List<GridIndex> GenerateStraightPath(GridIndex start, GridIndex goal)
	{
		var path = new List<GridIndex> { start };

		var current = start;
		while (!current.Equals(goal))
		{
			int dx = goal.x > current.x ? 1 : (goal.x < current.x ? -1 : 0);
			int dy = goal.y > current.y ? 1 : (goal.y < current.y ? -1 : 0);
			int dz = goal.z > current.z ? 1 : (goal.z < current.z ? -1 : 0);

			current = new GridIndex(current.x + dx, current.y + dy, current.z + dz);
			if (gridContainer.InBounds(current))
			{
				path.Add(current);
			}
			else
			{
				return null; // ·��Խ��
			}
		}

		return path;
	}

	/// <summary>
	/// �������·��
	/// ������������ֱ�������յ㣨���ܽϳ���
	/// </summary>
	private List<GridIndex> GenerateRandomPath(GridIndex start, GridIndex goal)
	{
		var path = new List<GridIndex> { start };
		var current = start;
		var visited = new HashSet<GridIndex> { start };

		int maxSteps = 1000; // ��ֹ��ѭ��
		int step = 0;

		while (!current.Equals(goal) && step < maxSteps)
		{
			// ��ȡ������Ч�ڽӵ�
			var neighbors = new List<GridIndex>();
			foreach (var nb in gridContainer.GetNeighbors(current))
			{
				neighbors.Add(nb);
			}

			if (neighbors.Count == 0) break;

			// ����ѡ����Ŀ����ڽӵ㣬����һ���������ƫ��
			GridIndex next = neighbors[Random.Range(0, neighbors.Count)];

			// 30% ����ѡ����Ŀ��ķ���������ڣ�
			if (Random.value < 0.3f)
			{
				var bestNeighbor = GetNeighborClosestToGoal(neighbors, goal);
				if (bestNeighbor.HasValue)
				{
					next = bestNeighbor.Value;
				}
			}

			if (!visited.Contains(next))
			{
				visited.Add(next);
				path.Add(next);
				current = next;
			}
			else
			{
				// ���ѡ�еĵ��ѷ��ʣ��������ѡ��
				next = neighbors[Random.Range(0, neighbors.Count)];
				if (!visited.Contains(next))
				{
					visited.Add(next);
					path.Add(next);
					current = next;
				}
			}

			step++;
		}

		return current.Equals(goal) ? path : null;
	}

	/// <summary>
	/// �Թ�ʽ·�����ݹ���ݣ�
	/// ����һ�����۵����Խ���·��
	/// </summary>
	private List<GridIndex> GenerateMazePath(GridIndex start, GridIndex goal)
	{
		var path = new List<GridIndex>();
		var visited = new HashSet<GridIndex>();

		if (DFSPath(start, goal, visited, path))
		{
			return path;
		}

		return null;
	}

	private bool DFSPath(GridIndex current, GridIndex goal, HashSet<GridIndex> visited, List<GridIndex> path)
	{
		visited.Add(current);
		path.Add(current);

		if (current.Equals(goal))
		{
			return true;
		}

		// ��������ڽӵ�˳�������Ӷ�����
		var neighbors = new List<GridIndex>(gridContainer.GetNeighbors(current));
		ShuffleList(neighbors);

		foreach (var neighbor in neighbors)
		{
			if (!visited.Contains(neighbor))
			{
				if (DFSPath(neighbor, goal, visited, path))
				{
					return true;
				}
			}
		}

		// ���ݣ��������·��ͨ���Ƴ���ǰ�ڵ�
		path.RemoveAt(path.Count - 1);
		return false;
	}

	/// <summary>
	/// �������·���ϵĽڵ���pathTile��������emptyTile
	/// </summary>
	private void FillGrid(List<GridIndex> path)
	{
		var pathSet = new HashSet<GridIndex>(path);

		// ������������ڵ�
		for (int x = 0; x < gridContainer.width; x++)
		{
			for (int y = 0; y < gridContainer.height; y++)
			{
				for (int z = 0; z < gridContainer.layers; z++)
				{
					var idx = new GridIndex(x, y, z);

					GameObject prefab = pathSet.Contains(idx) ? pathTilePrefab : emptyTilePrefab;

					// ������λ��ʵ����Tile
					var tileGo = Instantiate(prefab, gridContainer.GetWorldPos(idx), Quaternion.identity);
					var tile = tileGo.GetComponent<TileBase>();

					if (tile != null)
					{
						// 通过 GridContainer.Place 放置，检查返回值避免静默失败
						if (!gridContainer.Place(idx, tile))
						{
							Debug.LogWarning("[PuzzleInitializer] 无法在 " + idx + " 放置Tile，节点可能已被占用");
							if (Application.isPlaying)
								Destroy(tileGo);
							else
								DestroyImmediate(tileGo);
						}
					}
					else
					{
						Debug.LogWarning($"[PuzzleInitializer] Tile prefab {prefab.name} ��û�� TileBase ���");
					}
				}
			}
		}
	}

	/// <summary>
	/// ��ȡ�ڽӵ�����ӽ�Ŀ���һ��
	/// </summary>
	private GridIndex? GetNeighborClosestToGoal(List<GridIndex> neighbors, GridIndex goal)
	{
		if (neighbors.Count == 0) return null;

		GridIndex best = neighbors[0];
		float bestDist = Vector3.Distance(gridContainer.GetWorldPos(best), gridContainer.GetWorldPos(goal));

		foreach (var nb in neighbors)
		{
			float dist = Vector3.Distance(gridContainer.GetWorldPos(nb), gridContainer.GetWorldPos(goal));
			if (dist < bestDist)
			{
				bestDist = dist;
				best = nb;
			}
		}

		return best;
	}

	/// <summary>
	/// Fisher-Yates ϴ���㷨
	/// </summary>
	private void ShuffleList<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int randomIndex = Random.Range(0, i + 1);
			(list[i], list[randomIndex]) = (list[randomIndex], list[i]);
		}
	}

	/// <summary>
	/// �ⲿ���ã����³�ʼ��ƴͼ���������û���Ĳ������������ɣ�
	/// </summary>
	public void ReinitializePuzzle()
	{
		// �������Tile
		for (int x = 0; x < gridContainer.width; x++)
		{
			for (int y = 0; y < gridContainer.height; y++)
			{
				for (int z = 0; z < gridContainer.layers; z++)
				{
					var idx = new GridIndex(x, y, z);
					var node = gridContainer.GetNode(idx);
					if (node != null && node.placedTile != null)
					{
						if (Application.isPlaying)
							Destroy(node.placedTile.gameObject);
						else
							DestroyImmediate(node.placedTile.gameObject);
					}
					gridContainer.Remove(idx);
				}
			}
		}

		// ���³�ʼ��
		InitializePuzzle();
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (_currentPath == null || gridContainer == null) return;

		// ����·��
		Gizmos.color = Color.yellow;
		for (int i = 0; i < _currentPath.Count - 1; i++)
		{
			Vector3 p1 = gridContainer.GetWorldPos(_currentPath[i]);
			Vector3 p2 = gridContainer.GetWorldPos(_currentPath[i + 1]);
			Gizmos.DrawLine(p1, p2);
		}

		// ���������յ�
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(gridContainer.GetWorldPos(startIndex), 0.1f);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(gridContainer.GetWorldPos(goalIndex), 0.1f);
	}
#endif
}
