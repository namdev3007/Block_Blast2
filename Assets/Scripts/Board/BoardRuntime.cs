using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public Sprite placedSprite;                 // sprite fallback khi đặt
    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    [Range(0f, 1f)] public float linePreviewAlpha = 0.25f;

    [Header("Clear FX")]
    public float clearFade = 0.15f;            // thời gian fade khi xóa

    public BoardState State { get; private set; }

    private readonly List<int> _previewIdx = new();
    private readonly List<int> _linePreviewIdx = new();

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
            view.PlayPlaceFlashOnce(); // flash 1 lần màu trắng khi snap
        }
    }

    // ===== PREVIEW footprint =====
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

    // ===== PREVIEW row/col completion =====
    public void ShowLineCompletionPreview(ShapeData shape, int anchorRow, int anchorCol, Sprite previewSprite)
    {
        ClearLinePreview();

        int W = gridView.columns, H = gridView.rows;
        bool[] proposed = new bool[W * H]; // footprint shape nếu đặt
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

public void ResolveAndClearFullLinesAfterPlacement(ShapeData shape, int anchorRow, int anchorCol, Sprite chosenSprite)
{
    int W = gridView.columns, H = gridView.rows;

    // Chỉ check hàng/cột bị ảnh hưởng
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

    // Tắt preview trước khi chuyển sprite thật
    ClearPreview();

    // Gom mọi ô cần clear
    var toClearIdx = new HashSet<int>();
    foreach (var r in fullRows) for (int c = 0; c < W; c++) toClearIdx.Add(r * W + c);
    foreach (var c in fullCols) for (int r = 0; r < H; r++) toClearIdx.Add(r * W + c);

    // Đổi ngay sprite của tất cả ô thuộc line đầy (giữ nguyên yêu cầu của bạn)
    var sprite = chosenSprite != null ? chosenSprite : placedSprite;
    foreach (var idx in toClearIdx)
    {
        var view = gridView.Cells[idx];
        view.SetOccupied(true, sprite);
    }

    // Xóa trong STATE
    State.ClearLines(fullRows, fullCols);

    // Tính delay theo wave (row-first & col-first), lấy delay nhỏ nhất để wave trông đẹp
    const float step = 0.03f; // khoảng cách thời gian giữa 2 ô liên tiếp
    foreach (var idx in toClearIdx)
    {
        int r = idx / W;
        int c = idx % W;

        float dRow = float.PositiveInfinity;
        if (fullRows.Contains(r)) dRow = c * step;      // wave chạy theo cột trong hàng r

        float dCol = float.PositiveInfinity;
        if (fullCols.Contains(c)) dCol = r * step;      // wave chạy theo hàng trong cột c

        float delay = Mathf.Min(dRow, dCol);

        // Gọi hiệu ứng Pop + Fade (nổi bật) theo delay đã tính
        gridView.Cells[idx].PlayClearPopAndReset(delay, popScale: 1.15f, popIn: 0.08f, fadeOut: 0.18f);
    }
}


}
