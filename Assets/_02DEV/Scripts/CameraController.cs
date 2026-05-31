using UnityEngine;

/// <summary>
/// Oyun başladığında kamerayı, tüm level içeriğini ekrana sığdıracak şekilde otomatik olarak konumlandırır ve zoom yapar.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private LevelData levelData; // Seviyenin boyutlarını bilmek için

    [Header("Ayarlar")]
    [SerializeField] private float padding = 2.0f; // Kenarlarda bırakılacak ekstra boşluk
    [SerializeField] private float minOrthographicSize = 5.0f; // Kameranın çok fazla yakınlaşmasını engellemek için minimum boyut

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // GridManager'ın ve diğerlerinin dünyayı oluşturmasını beklemek için küçük bir gecikme
        Invoke(nameof(AdjustCamera), 0.1f);
    }

    /// <summary>
    /// Kamerayı seviye içeriğine göre ayarlar.
    /// </summary>
    public void AdjustCamera()
    {
        if (levelData == null)
        {
            Debug.LogError("CameraController'a LevelData atanmamış!", this);
            return;
        }

        // 1. Seviyenin kapladığı alanın sınırlarını hesapla
        Bounds levelBounds = CalculateLevelBounds();

        // 2. Kamerayı bu alanın merkezine taşı
        transform.position = new Vector3(levelBounds.center.x, levelBounds.center.y, -10); // 2D için Z=-10

        // 3. Kameranın görüş alanını (Orthographic Size) ayarla
        float verticalSize = levelBounds.size.y / 2f;
        float horizontalSize = (levelBounds.size.x / _cam.aspect) / 2f;

        // Genişlik mi yükseklik mi daha kısıtlayıcı ise onu baz al ve padding ekle
        float targetOrthographicSize = Mathf.Max(verticalSize, horizontalSize) + padding;

        // Minimum boyuttan daha küçük olmasını engelle
        _cam.orthographicSize = Mathf.Max(targetOrthographicSize, minOrthographicSize);
    }

    /// <summary>
    /// LevelData'ya göre seviyenin dünyadaki sınırlarını hesaplar.
    /// </summary>
    /// <returns>Seviyenin tamamını içeren bir Bounds nesnesi.</returns>
    private Bounds CalculateLevelBounds()
    {
        // Hedef şeklin en sağdaki noktasını bul
        int targetMaxX = -1;
        for (int x = 0; x < levelData.targetShapeWidth; x++)
        {
            for (int y = 0; y < levelData.targetShapeHeight; y++)
            {
                if (levelData.TargetShapeMatrix[x, y] && x > targetMaxX)
                {
                    targetMaxX = x;
                }
            }
        }
        if (targetMaxX == -1) targetMaxX = 0;

        // Kırılabilir duvar bloğunun boyutlarını hesapla
        int breakableBlockWidth = levelData.targetShapeWidth + levelData.wallPadding;
        
        // Oyuncu ve duvarın pozisyonlarını hesapla
        int playerStartX = targetMaxX + 4;
        int breakableBlockStartX = playerStartX + 2;

        // Seviyenin X eksenindeki başlangıç ve bitiş noktaları
        float minX = 0; // Seviye hedef şekil ile 0'dan başlar
        float maxX = breakableBlockStartX + breakableBlockWidth;

        // Seviyenin Y eksenindeki başlangıç ve bitiş noktaları
        float minY = 0;
        float maxY = 2 + (levelData.targetShapeHeight + levelData.wallPadding); // startY + blockHeight

        // Sınırları bir Bounds nesnesinde birleştir
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 1);

        return new Bounds(center, size);
    }
}
