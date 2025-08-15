using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public Sprite placedSprite;                 // sprite fallback khi đặt
    [Range(0f, 1f)] public float previewAlpha = 0.35f;

    public BoardState State { get; private set; }

    private readonly List<int> _previewIdx = new();

    private void Awake()
    {
        if (gridView == null) gridView = GetComponent<GridView>();
        State = new BoardState(gridView.columns, gridView.rows);
    }

    public void PaintPlaced(ShapeData shape, int anchorRow, int anchorCol, Sprite overrideSprite = null)
    {
        var sprite = overrideSprite != null ? overrideSprite : placedSprite;

        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            var view = gridView.Cells[r * gridView.columns + c];

            // đặt block
            view.SetOccupied(true, sprite);

            // flash viền trắng 1 lần
            view.PlayPlaceFlashOnce(); // mặc định 0.12s in, 0.18s out
        }
    }

    public void ShowPreview(ShapeData shape, int anchorRow, int anchorCol, Sprite previewSprite)
    {
        ClearPreview();
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            int idx = r * gridView.columns + c;
            var view = gridView.Cells[idx];
            view.SetHoverPreview(true, previewSprite, previewAlpha);
            _previewIdx.Add(idx);
        }
    }

    public void ClearPreview()
    {
        if (_previewIdx.Count == 0) return;
        foreach (var idx in _previewIdx)
        {
            var view = gridView.Cells[idx];
            view.SetHoverPreview(false, null, null);
        }
        _previewIdx.Clear();
    }
}
