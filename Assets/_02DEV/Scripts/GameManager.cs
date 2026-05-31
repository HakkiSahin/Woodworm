using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Oyunun genel durumunu, kazanma koşulunu ve yeniden başlatma gibi temel mekanikleri yönetir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Verisi")]
    [SerializeField] private LevelData levelData;

    [Header("Olaylar")]
    public UnityEvent OnLevelComplete;

    private bool _isLevelFinished = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    /// <summary>
    /// Kazanma durumunu kontrol eder. Rölatif (göreceli) pozisyonları karşılaştırır.
    /// </summary>
    public void CheckWinCondition()
    {
        // GÖZDEN KAÇMASI İMKANSIZ TEST MESAJI
        Debug.LogAssertion("--- KAZANMA KONTROLÜ TETİKLENDİ ---");

        if (_isLevelFinished) return;

        // 1. Hedef şablonun rölatif koordinatlarını al.
        HashSet<Vector2Int> targetRelativePositions = GetRelativePositions(levelData.TargetShapeMatrix);

        // 2. Sahnedeki mevcut duvarların rölatif koordinatlarını al.
        HashSet<Vector2Int> wallRelativePositions = GetCurrentWallRelativePositions();
        
        // --- HATA AYIKLAMA (DEBUG) KODU ---
        PrintShapeForDebugging("Hedef Şekil Haritası", targetRelativePositions);
        PrintShapeForDebugging("Mevcut Duvar Haritası", wallRelativePositions);
        // --- HATA AYIKLAMA SONU ---

        if (targetRelativePositions.Count > 0 && targetRelativePositions.SetEquals(wallRelativePositions))
        {
            _isLevelFinished = true;
            Debug.Log("Level Complete! Şekil Başarıyla Eşleşti!");
            OnLevelComplete?.Invoke();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.enabled = false;
            }
        }
    }

    private HashSet<Vector2Int> GetRelativePositions(bool[,] matrix)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < matrix.GetLength(0); x++)
        {
            for (int y = 0; y < matrix.GetLength(1); y++)
            {
                if (matrix[x, y])
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (positions.Count == 0) return new HashSet<Vector2Int>();

        int minX = positions.Min(p => p.x);
        int minY = positions.Min(p => p.y);
        Vector2Int origin = new Vector2Int(minX, minY);

        return new HashSet<Vector2Int>(positions.Select(p => p - origin));
    }

    private HashSet<Vector2Int> GetCurrentWallRelativePositions()
    {
        var wallAbsolutePositions = GridManager.Instance.BreakableWallObjects.Keys;

        if (wallAbsolutePositions.Count == 0)
        {
            return new HashSet<Vector2Int>();
        }

        int minX = wallAbsolutePositions.Min(p => p.x);
        int minY = wallAbsolutePositions.Min(p => p.y);
        Vector2Int origin = new Vector2Int(minX, minY);

        return new HashSet<Vector2Int>(wallAbsolutePositions.Select(p => p - origin));
    }

    private void PrintShapeForDebugging(string title, HashSet<Vector2Int> positions)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- {title} ({positions.Count} blok) ---");
        
        var orderedPositions = positions.OrderBy(p => p.y).ThenBy(p => p.x);

        foreach (var pos in orderedPositions)
        {
            sb.Append($"({pos.x},{pos.y}) ");
        }
        Debug.Log(sb.ToString());
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
