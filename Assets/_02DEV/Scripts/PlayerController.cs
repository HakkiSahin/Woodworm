using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Ayarlar")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float fallDuration = 0.15f;
    [SerializeField] private float breakDuration = 0.1f; 

    [Header("Görseller")]
    [SerializeField] private GameObject bodyPartPrefab;
    [SerializeField] private Transform bodyPartsParent;

    private Queue<Vector2Int> _wormPositions = new Queue<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> _bodyPartsDict = new Dictionary<Vector2Int, GameObject>();
    private bool _isProcessing = false;

    public IEnumerable<Vector2Int> WormPositions => _wormPositions;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Invoke(nameof(InitializeWorm), 0.1f);
    }

    private void InitializeWorm()
    {
        Vector2Int startPos = GridManager.Instance.PlayerStartPosition;
        for (int i = 2; i >= 0; i--)
        {
            Vector2Int pos = new Vector2Int(startPos.x - i, startPos.y);
            AddBodyPart(pos);
        }
        CheckGravity();
    }

    private void Update()
    {
        if (_isProcessing) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int currentHeadPos = _wormPositions.Last();
        Vector2Int targetPos = currentHeadPos + direction;

        CellType targetCellType = GridManager.Instance.GetCellType(targetPos.x, targetPos.y);

        if (targetCellType == CellType.Empty || targetCellType == CellType.PlayerStart)
        {
            MoveTo(targetPos);
            return;
        }

        if (targetCellType == CellType.BreakableWall)
        {
            BreakWallAndMove(targetPos);
            return;
        }
    }

    private void BreakWallAndMove(Vector2Int wallPos)
    {
        _isProcessing = true;

        if (GridManager.Instance.BreakableWallObjects.TryGetValue(wallPos, out GameObject wallObj))
        {
            wallObj.transform.DOScale(Vector3.zero, breakDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                Destroy(wallObj);
                GridManager.Instance.BreakableWallObjects.Remove(wallPos);
                GridManager.Instance.UpdateCellState(wallPos.x, wallPos.y, CellType.Empty);

                if (FallingBlockManager.Instance != null)
                {
                    FallingBlockManager.Instance.OnBlockBroken(wallPos);
                }

                MoveTo(wallPos);
            });
        }
        else
        {
            GridManager.Instance.UpdateCellState(wallPos.x, wallPos.y, CellType.Empty);
            MoveTo(wallPos);
        }
    }

    private void MoveTo(Vector2Int newHeadPos)
    {
        _isProcessing = true;
        AddBodyPart(newHeadPos, moveDuration);

        if (_wormPositions.Count > 3)
        {
            RemoveOldestBodyPart(moveDuration);
        }

        DOVirtual.DelayedCall(moveDuration, () =>
        {
            _isProcessing = false;
            CheckGravity();
            
            if (FallingBlockManager.Instance != null)
            {
                FallingBlockManager.Instance.CheckAllGroupsGravity();
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckWinCondition();
            }
            else
            {
                Debug.LogError("GameManager bulunamadı! Sahneye 'GameManager' adında bir obje ekleyip GameManager.cs scriptini atadığınızdan emin olun!");
            }
        });
    }

    private void AddBodyPart(Vector2Int gridPos, float animDuration = 0f)
    {
        _wormPositions.Enqueue(gridPos);
        GridManager.Instance.UpdateCellState(gridPos.x, gridPos.y, CellType.PlayerTail);

        Vector3 worldPos = GridManager.Instance.GetWorldPosition(gridPos.x, gridPos.y);
        GameObject newPart = Instantiate(bodyPartPrefab, worldPos, Quaternion.identity, bodyPartsParent);
        _bodyPartsDict.Add(gridPos, newPart);

        if (animDuration > 0f)
        {
            newPart.transform.localScale = Vector3.zero;
            newPart.transform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
        }
    }

    private void RemoveOldestBodyPart(float animDuration = 0f)
    {
        Vector2Int tailPos = _wormPositions.Dequeue();
        GridManager.Instance.UpdateCellState(tailPos.x, tailPos.y, CellType.Empty);

        if (_bodyPartsDict.TryGetValue(tailPos, out GameObject tailObj))
        {
            _bodyPartsDict.Remove(tailPos);
            if (animDuration > 0f)
            {
                tailObj.transform.DOScale(Vector3.zero, animDuration).SetEase(Ease.InBack).OnComplete(() => Destroy(tailObj));
            }
            else
            {
                Destroy(tailObj);
            }
        }
    }

    private void CheckGravity()
    {
        bool canFall = true;

        foreach (Vector2Int pos in _wormPositions)
        {
            Vector2Int[] directions = { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = pos + dir;
                CellType neighborType = GridManager.Instance.GetCellType(neighborPos.x, neighborPos.y);

                if (neighborType == CellType.SolidWall || neighborType == CellType.BreakableWall)
                {
                    canFall = false;
                    break;
                }
            }

            if (!canFall) break; 
        }

        if (canFall)
        {
            ApplyGravity();
        }
    }

    private void ApplyGravity()
    {
        _isProcessing = true;
        Queue<Vector2Int> tempQueue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, GameObject> newDict = new Dictionary<Vector2Int, GameObject>();

        foreach (Vector2Int pos in _wormPositions)
        {
            GridManager.Instance.UpdateCellState(pos.x, pos.y, CellType.Empty);
        }

        int totalParts = _wormPositions.Count;
        int completedAnimations = 0;

        while (_wormPositions.Count > 0)
        {
            Vector2Int oldPos = _wormPositions.Dequeue();
            Vector2Int newPos = new Vector2Int(oldPos.x, oldPos.y - 1);
            
            tempQueue.Enqueue(newPos);
            GridManager.Instance.UpdateCellState(newPos.x, newPos.y, CellType.PlayerTail);

            if (_bodyPartsDict.TryGetValue(oldPos, out GameObject partObj))
            {
                newDict.Add(newPos, partObj);
                Vector3 newWorldPos = GridManager.Instance.GetWorldPosition(newPos.x, newPos.y);
                partObj.transform.DOMove(newWorldPos, fallDuration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    completedAnimations++;
                    if (completedAnimations == totalParts)
                    {
                        _isProcessing = false;
                        CheckGravity(); 
                        
                        if (FallingBlockManager.Instance != null)
                        {
                            FallingBlockManager.Instance.CheckAllGroupsGravity();
                        }
                        
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.CheckWinCondition();
                        }
                    }
                });
            }
        }

        _wormPositions = tempQueue;
        _bodyPartsDict = newDict;
    }
}