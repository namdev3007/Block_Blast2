using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public Sprite placedSprite;          // sprite khi đặt xong
    [Range(0f, 1f)] public float previewAlpha = 0.35f;

    public BoardState State { get; private set; }

    // lưu các index đang preview để clear
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
            view.SetOccupied(true, sprite);
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
