using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// LevelData'yı okuyarak oyun dünyasını ve grid yapısını runtime'da oluşturan ve yöneten ana yönetici.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Level Verisi")]
    [SerializeField] private LevelData levelData;

    [Header("Prefabs")]
    [SerializeField] private GameObject solidWallPrefab;
    [SerializeField] private GameObject breakableWallPrefab;
    [SerializeField] private GameObject targetGuidePrefab; // Altın renkli hedef şekil parçası

    [Header("Hiyerarşi Organizasyonu")]
    [SerializeField] private Transform gridParent; // Duvarlar bu obje altına oluşturulacak
    [SerializeField] private Transform targetParent; // Hedef şekil parçaları bu obje altına oluşturulacak

    // --- Runtime Verileri ---
    private CellType[,] _currentGridState;
    public Vector2Int PlayerStartPosition { get; private set; }

    // Kırılabilir blokların GameObject referanslarını tutan sözlük
    public Dictionary<Vector2Int, GameObject> BreakableWallObjects { get; private set; } = new Dictionary<Vector2Int, GameObject>();

    public int GridWidth => levelData.mainGridWidth;
    public int GridHeight => levelData.mainGridHeight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (levelData == null)
        {
            Debug.LogError("GridManager'a LevelData atanmamış!");
            return;
        }

        InitializeGrids();
        BuildWorld();
    }

    private void InitializeGrids()
    {
        _currentGridState = (CellType[,])levelData.InitialGridMatrix.Clone();
    }

    private void BuildWorld()
    {
        for (int x = 0; x < levelData.mainGridWidth; x++)
        {
            for (int y = 0; y < levelData.mainGridHeight; y++)
            {
                CellType cellType = _currentGridState[x, y];
                Vector3 worldPos = GetWorldPosition(x, y);

                switch (cellType)
                {
                    case CellType.SolidWall:
                        Instantiate(solidWallPrefab, worldPos, Quaternion.identity, gridParent);
                        break;
                    case CellType.BreakableWall:
                        GameObject bwObj = Instantiate(breakableWallPrefab, worldPos, Quaternion.identity, gridParent);
                        BreakableWallObjects.Add(new Vector2Int(x, y), bwObj);
                        break;
                    case CellType.PlayerStart:
                        PlayerStartPosition = new Vector2Int(x, y);
                        _currentGridState[x, y] = CellType.Empty; 
                        break;
                }
            }
        }

        // Hedef silüetini oluştur
        for (int x = 0; x < levelData.targetShapeWidth; x++)
        {
            for (int y = 0; y < levelData.targetShapeHeight; y++)
            {
                if (levelData.TargetShapeMatrix[x, y])
                {
                    Vector3 worldPos = GetWorldPosition(x, y);
                    Instantiate(targetGuidePrefab, worldPos, Quaternion.identity, targetParent);
                }
            }
        }
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y, 0) + transform.position;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < levelData.mainGridWidth && y >= 0 && y < levelData.mainGridHeight;
    }

    public bool IsCellEmpty(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return _currentGridState[x, y] == CellType.Empty;
    }

    public CellType GetCellType(int x, int y)
    {
        if (!IsInBounds(x, y)) return CellType.SolidWall;
        return _currentGridState[x, y];
    }

    public void UpdateCellState(int x, int y, CellType newType)
    {
        if (IsInBounds(x, y))
        {
            _currentGridState[x, y] = newType;
        }
    }

    public void UpdateBreakableWallDictionary(Vector2Int oldPos, Vector2Int newPos)
    {
        if (BreakableWallObjects.TryGetValue(oldPos, out GameObject obj))
        {
            BreakableWallObjects.Remove(oldPos);
            BreakableWallObjects.Add(newPos, obj);
        }
    }
}
