using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class FallingBlockManager : MonoBehaviour
{
    public static FallingBlockManager Instance { get; private set; }

    [Header("Ayarlar")]
    [SerializeField] private float fallDuration = 0.2f;

    private List<List<Vector2Int>> _blockGroups = new List<List<Vector2Int>>();
    private HashSet<List<Vector2Int>> _fallingGroups = new HashSet<List<Vector2Int>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Invoke(nameof(RecalculateAllGroups), 0.2f);
    }

    public void OnBlockBroken(Vector2Int brokenBlockPos)
    {
        List<Vector2Int> groupToSplit = _blockGroups.FirstOrDefault(g => g.Contains(brokenBlockPos));

        if (groupToSplit != null)
        {
            groupToSplit.Remove(brokenBlockPos);
            _blockGroups.Remove(groupToSplit);
        }
        
        RecalculateAllGroups(); 
    }

    public void RecalculateAllGroups()
    {
        _blockGroups.Clear();
        bool[,] visited = new bool[GridManager.Instance.GridWidth, GridManager.Instance.GridHeight];

        for (int x = 0; x < GridManager.Instance.GridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.GridHeight; y++)
            {
                if (GridManager.Instance.GetCellType(x, y) == CellType.BreakableWall && !visited[x, y])
                {
                    List<Vector2Int> newGroup = new List<Vector2Int>();
                    FloodFill(new Vector2Int(x, y), visited, newGroup);
                    if (newGroup.Count > 0)
                    {
                        _blockGroups.Add(newGroup);
                    }
                }
            }
        }
        CheckAllGroupsGravity();
    }
    
    private void FloodFill(Vector2Int startPos, bool[,] visited, List<Vector2Int> group)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            group.Add(current);

            foreach (Vector2Int dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;

                if (nx >= 0 && nx < GridManager.Instance.GridWidth && ny >= 0 && ny < GridManager.Instance.GridHeight)
                {
                    if (!visited[nx, ny] && GridManager.Instance.GetCellType(nx, ny) == CellType.BreakableWall)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
    }

    public void CheckAllGroupsGravity()
    {
        foreach (var group in _blockGroups)
        {
            if (!_fallingGroups.Contains(group))
            {
                CheckGroupGravity(group);
            }
        }
    }

    private void CheckGroupGravity(List<Vector2Int> group)
    {
        if (group.Count == 0) return;

        bool canFall = true;
        foreach (Vector2Int pos in group)
        {
            Vector2Int posBelow = new Vector2Int(pos.x, pos.y - 1);

            if (group.Contains(posBelow)) continue;

            CellType typeBelow = GridManager.Instance.GetCellType(posBelow.x, posBelow.y);

            if (typeBelow != CellType.Empty && typeBelow != CellType.PlayerTail)
            {
                canFall = false;
                break;
            }
            
            if (typeBelow == CellType.PlayerTail)
            {
                 canFall = false;
                 break;
            }
        }

        if (canFall)
        {
            DropGroup(group);
        }
    }

    private void DropGroup(List<Vector2Int> group)
    {
        _fallingGroups.Add(group);

        foreach (Vector2Int pos in group)
        {
            GridManager.Instance.UpdateCellState(pos.x, pos.y, CellType.Empty);
        }

        int totalBlocks = group.Count;
        int completedAnimations = 0;
        var newGroupPositions = new List<Vector2Int>();

        for (int i = 0; i < group.Count; i++)
        {
            Vector2Int oldPos = group[i];
            Vector2Int newPos = new Vector2Int(oldPos.x, oldPos.y - 1);
            newGroupPositions.Add(newPos);

            GridManager.Instance.UpdateCellState(newPos.x, newPos.y, CellType.BreakableWall);
            GridManager.Instance.UpdateBreakableWallDictionary(oldPos, newPos);

            if (GridManager.Instance.BreakableWallObjects.TryGetValue(newPos, out GameObject blockObj))
            {
                Vector3 newWorldPos = GridManager.Instance.GetWorldPosition(newPos.x, newPos.y);
                blockObj.transform.DOMove(newWorldPos, fallDuration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    completedAnimations++;
                    if (completedAnimations == totalBlocks)
                    {
                        _fallingGroups.Remove(group);
                        group.Clear();
                        group.AddRange(newGroupPositions);
                        CheckGroupGravity(group);
                        
                        // KAZANMA KONTROLÜNÜ TETİKLE
                        GameManager.Instance.CheckWinCondition();
                    }
                });
            }
            else
            {
                completedAnimations++;
            }
        }
    }
}