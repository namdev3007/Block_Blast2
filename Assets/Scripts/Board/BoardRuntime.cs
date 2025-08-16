using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public SkinProvider skin;                 // NEW
    public Sprite placedSpriteFallback;       // fallback nếu chưa có skin
    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    [Range(0f, 1f)] public float linePreviewAlpha = 0.25f;
    public float clearFade = 0.15f;

    public BoardState State { get; private set; }

    private readonly List<int> _previewIdx = new();
    private readonly List<int> _linePreviewIdx = new();

    private void Awake()
    {
        if (gridView == null) gridView = GetComponent<GridView>();
        State = new BoardState(gridView.columns, gridView.rows);
    }

    private Sprite SpriteFromVariant(int variantIndex)
    {
        var s = skin ? skin.GetTileSprite(variantIndex) : null;
        return s != null ? s : placedSpriteFallback;
    }

    // ===== PREVIEW footprint (variant) =====
    public void ShowPreviewVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        ClearFootprintPreview();
        var sprite = SpriteFromVariant(variantIndex);
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            int idx = r * gridView.columns + c;
            var view = gridView.Cells[idx];
            view.SetHoverPreview(true, sprite, previewAlpha);
            _previewIdx.Add(idx);
        }
    }

    // ===== PREVIEW row/col completion (variant) =====
    public void ShowLineCompletionPreviewVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        ClearLinePreview();

        int W = gridView.columns, H = gridView.rows;
        bool[] proposed = new bool[W * H];
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r >= 0 && r < H && c >= 0 && c < W)
                proposed[r * W + c] = true;
        }

        var sprite = SpriteFromVariant(variantIndex);
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
                        gridView.Cells[idx].SetLinePreview(true, sprite, linePreviewAlpha);
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
                        gridView.Cells[idx].SetLinePreview(true, sprite, linePreviewAlpha);
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

    // ===== Đặt thật (variant) =====
    public void PaintPlacedVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        var sprite = SpriteFromVariant(variantIndex);
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            var view = gridView.Cells[r * gridView.columns + c];
            view.SetOccupied(true, sprite);
            view.PlayPlaceFlashOnce(); // nếu bạn đã có hàm này
        }
    }

    // ===== Clear các line full sau khi đặt (đổi sprite ngay rồi wave/fade) =====
    public void ResolveAndClearFullLinesAfterPlacementVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        int W = gridView.columns, H = gridView.rows;

        var rowsToCheck = new HashSet<int>();
        var colsToCheck = new HashSet<int>();
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r >= 0 && r < H) rowsToCheck.Add(r);
            if (c >= 0 && c < W) colsToCheck.Add(c);
        }

        var fullRows = new List<int>();
        foreach (var r in rowsToCheck) if (State.IsRowFull(r)) fullRows.Add(r);

        var fullCols = new List<int>();
        foreach (var c in colsToCheck) if (State.IsColFull(c)) fullCols.Add(c);

        if (fullRows.Count == 0 && fullCols.Count == 0) return;

        ClearPreview();

        var toClearIdx = new HashSet<int>();
        foreach (var r in fullRows) for (int c = 0; c < W; c++) toClearIdx.Add(r * W + c);
        foreach (var c in fullCols) for (int r = 0; r < H; r++) toClearIdx.Add(r * W + c);

        // Đổi NGAY sang sprite variant chọn
        var sprite = SpriteFromVariant(variantIndex);
        foreach (var idx in toClearIdx)
        {
            var view = gridView.Cells[idx];
            view.SetOccupied(true, sprite);
        }

        // Xóa trong STATE
        State.ClearLines(fullRows, fullCols);

        // Hiệu ứng clear (nếu bạn đã có Pop & Fade dạng wave)
        const float step = 0.03f;
        foreach (var idx in toClearIdx)
        {
            int r = idx / W;
            int c = idx % W;

            float dRow = fullRows.Contains(r) ? c * step : float.PositiveInfinity;
            float dCol = fullCols.Contains(c) ? r * step : float.PositiveInfinity;
            float delay = Mathf.Min(dRow, dCol);

            // Nếu bạn có PlayClearPopAndReset:
            gridView.Cells[idx].PlayClearPopAndReset(delay, 1.15f, 0.08f, 0.18f);
            // Nếu không có hàm trên, bạn có thể gọi ResetCell() sau một coroutine/tween tuỳ ý.
        }
    }
}
