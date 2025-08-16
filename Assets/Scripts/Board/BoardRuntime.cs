using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public Sprite placedSprite;                 // fallback khi đặt
    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    [Range(0f, 1f)] public float linePreviewAlpha = 0.25f;

    public BoardState State { get; private set; }

    private readonly List<int> _previewIdx = new(); // footprint preview indices
    private readonly List<int> _linePreviewIdx = new(); // row/col preview indices

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
            view.PlayPlaceFlashOnce(); // flash 1 lần màu trắng
        }
    }

    // ==== PREVIEW (footprint) ====
    public void ShowPreview(ShapeData shape, int anchorRow, int anchorCol, Sprite previewSprite)
    {
        ClearFootprintPreview();
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

    // ==== PREVIEW (row/col completion) ====
    public void ShowLineCompletionPreview(ShapeData shape, int anchorRow, int anchorCol, Sprite previewSprite)
    {
        ClearLinePreview();

        int W = gridView.columns, H = gridView.rows;
        // đánh dấu các ô shape sẽ chiếm nếu đặt
        bool[] proposed = new bool[W * H];
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r >= 0 && r < H && c >= 0 && c < W)
                proposed[r * W + c] = true;
        }

        var added = new HashSet<int>();

        // hàng
        for (int r = 0; r < H; r++)
        {
            int count = 0;
            for (int c = 0; c < W; c++)
                if (State.IsOccupied(r, c) || proposed[r * W + c]) count++;

            if (count == W)
            {
                for (int c = 0; c < W; c++)
                {
                    int idx = r * W + c;
                    if (added.Add(idx))
                    {
                        gridView.Cells[idx].SetLinePreview(true, previewSprite, linePreviewAlpha);
                        _linePreviewIdx.Add(idx);
                    }
                }
            }
        }

        // cột
        for (int c = 0; c < W; c++)
        {
            int count = 0;
            for (int r = 0; r < H; r++)
                if (State.IsOccupied(r, c) || proposed[r * W + c]) count++;

            if (count == H)
            {
                for (int r = 0; r < H; r++)
                {
                    int idx = r * W + c;
                    if (added.Add(idx))
                    {
                        gridView.Cells[idx].SetLinePreview(true, previewSprite, linePreviewAlpha);
                        _linePreviewIdx.Add(idx);
                    }
                }
            }
        }
    }

    public void ClearPreview()
    {
        ClearFootprintPreview();
        ClearLinePreview();
    }

    private void ClearFootprintPreview()
    {
        if (_previewIdx.Count == 0) return;
        foreach (var idx in _previewIdx)
            gridView.Cells[idx].SetHoverPreview(false, null, null);
        _previewIdx.Clear();
    }

    private void ClearLinePreview()
    {
        if (_linePreviewIdx.Count == 0) return;
        foreach (var idx in _linePreviewIdx)
            gridView.Cells[idx].SetLinePreview(false, null, null);
        _linePreviewIdx.Clear();
    }
}
