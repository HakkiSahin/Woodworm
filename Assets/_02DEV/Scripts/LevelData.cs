using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Linq;

public enum CellType
{
    [LabelText("Boş Alan")] Empty,
    [LabelText("Sabit Duvar")] [GUIColor(0.5f, 0.5f, 0.5f)] SolidWall,
    [LabelText("Kırılabilir Duvar")] [GUIColor(0.8f, 0.6f, 0.2f)] BreakableWall,
    [LabelText("Oyuncu Başlangıç")] [GUIColor(0.2f, 0.6f, 1f)] PlayerStart,
    [HideInInspector] PlayerTail
}

[CreateAssetMenu(fileName = "LevelData_01", menuName = "Woodworm/Level Data")]
public class LevelData : SerializedScriptableObject
{
    [Title("1. Adım: Hedef Şekli ve Boyutunu Belirleyin")]
    [InfoBox("Hedef şeklin boyutlarını ayarlayın ve ardından aşağıdaki alanda şekli çizin.")]
    [BoxGroup("Hedef Şekil Editörü", ShowLabel = false)]
    [OnValueChanged("ResizeTargetShapeMatrix")]
    [Range(3, 10)]
    [LabelText("Hedef Genişlik")]
    public int targetShapeWidth = 5;

    [BoxGroup("Hedef Şekil Editörü")]
    [OnValueChanged("ResizeTargetShapeMatrix")]
    [Range(3, 10)]
    [LabelText("Hedef Yükseklik")]
    public int targetShapeHeight = 5;

    [BoxGroup("Hedef Şekil Editörü")]
    [TableMatrix(SquareCells = true, ResizableColumns = false, RowHeight = 35, DrawElementMethod = "DrawColoredCell")]
    public bool[,] TargetShapeMatrix;

    [Title("Oyun Alanı Ayarları")]
    [Range(0, 5)]
    [LabelText("Duvar Kesme Payı (Padding)")]
    [InfoBox("Oluşturulacak kırılabilir duvar bloğu, hedef şeklin boyutundan bu kadar birim daha büyük olur (Rahat kesim için).")]
    public int wallPadding = 1;

#if UNITY_EDITOR
    private bool DrawColoredCell(Rect rect, bool value)
    {
        Rect innerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            value = !value;
            GUI.changed = true;
            Event.current.Use();
        }
        Color cellColor = value ? new Color(1f, 0.84f, 0f) : new Color(0, 0, 0, 0.2f);
        UnityEditor.EditorGUI.DrawRect(innerRect, cellColor);
        return value;
    }
#endif

    private void ResizeTargetShapeMatrix()
    {
        TargetShapeMatrix = new bool[targetShapeWidth, targetShapeHeight];
    }

    [Title("2. Adım: Seviyeyi Otomatik Oluşturun")]
    [Button("Generate Level Layout", ButtonSizes.Large)]
    private void GenerateLevelLayout()
    {
        // Grid'i temizle
        InitialGridMatrix = new CellType[mainGridWidth, mainGridHeight];

        // Hedef şeklin en sağdaki dolu hücresini bul
        int targetMaxX = -1;
        for (int x = 0; x < targetShapeWidth; x++)
        {
            for (int y = 0; y < targetShapeHeight; y++)
            {
                if (TargetShapeMatrix[x, y] && x > targetMaxX)
                {
                    targetMaxX = x;
                }
            }
        }

        if (targetMaxX == -1)
        {
            Debug.LogWarning("Hedef şekil boş! Lütfen önce bir şekil çizin.");
            return;
        }

        // Kırılacak olan duvar bloğunun boyutlarını hesapla (Kesme Payı dahil)
        int breakableBlockWidth = targetShapeWidth + wallPadding;
        int breakableBlockHeight = targetShapeHeight + wallPadding;

        // YERLEŞİM MANTIĞI: Hedef -> 4 birim -> Oyuncu -> 2 birim -> Duvar Bloğu
        int startY = 2; // Her şeyi tabana yakın (y=2) başlat
        int playerStartX = targetMaxX + 4;
        int breakableBlockStartX = playerStartX + 2;

        // Sınır kontrolü
        if (breakableBlockStartX + breakableBlockWidth >= mainGridWidth || startY + breakableBlockHeight >= mainGridHeight)
        {
            Debug.LogError($"Otomatik yerleşim ana grid'in dışına taşıyor! Lütfen 'Ana Grid' boyutlarını artırın.");
            return;
        }

        // Oyuncu başlangıç noktasını yerleştir
        InitialGridMatrix[playerStartX, startY] = CellType.PlayerStart;

        // Kırılabilir duvar bloğunu (kalıp halinde) yerleştir
        for (int x = 0; x < breakableBlockWidth; x++)
        {
            for (int y = 0; y < breakableBlockHeight; y++)
            {
                InitialGridMatrix[breakableBlockStartX + x, startY + y] = CellType.BreakableWall;
            }
        }

        Debug.Log($"Seviye oluşturuldu! Oyuncu: ({playerStartX}, {startY}), Duvar Bloğu: {breakableBlockWidth}x{breakableBlockHeight} boyutunda, ({breakableBlockStartX}, {startY}) noktasında.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [Title("Ana Grid Ayarları")]
    [BoxGroup("Ana Grid")]
    [OnValueChanged("ResizeMainGrid")]
    [Range(20, 50)]
    [LabelText("Genişlik")]
    public int mainGridWidth = 30;

    [BoxGroup("Ana Grid")]
    [OnValueChanged("ResizeMainGrid")]
    [Range(20, 50)]
    [LabelText("Yükseklik")]
    public int mainGridHeight = 20;

    [Title("Oyun Alanı Önizlemesi (Sadece Okunur)")]
    [TableMatrix(SquareCells = true, ResizableColumns = false, RowHeight = 20, IsReadOnly = true)]
    public CellType[,] InitialGridMatrix;

    private void ResizeMainGrid()
    {
        InitialGridMatrix = new CellType[mainGridWidth, mainGridHeight];
    }

    [OnInspectorInit]
    private void InitializeGridsIfNeeded()
    {
        if (TargetShapeMatrix == null || TargetShapeMatrix.GetLength(0) != targetShapeWidth || TargetShapeMatrix.GetLength(1) != targetShapeHeight)
        {
            ResizeTargetShapeMatrix();
        }

        if (InitialGridMatrix == null ||
            InitialGridMatrix.GetLength(0) != mainGridWidth || InitialGridMatrix.GetLength(1) != mainGridHeight)
        {
            ResizeMainGrid();
        }
    }
}