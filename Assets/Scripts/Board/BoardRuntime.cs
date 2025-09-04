using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public SkinProvider skin;
    public Sprite placedSpriteFallback;

    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    [Range(0f, 1f)] public float linePreviewAlpha = 0.25f;
    public float clearFade = 0.15f;

    [Header("Scoring")]
    public GameScore score;   // gán trong Inspector

    public BoardState State { get; private set; }

    private readonly List<int> _previewIdx = new();
    private readonly List<int> _linePreviewIdx = new();

    // ===== Seed cấu hình =====
    [Header("Initial Seed")]
    public bool seedAtStart = true;
    [Min(0)] public int initialMinOccupied = 35;
    [Min(0)] public int initialMaxOccupied = 45;
    [Tooltip("Tránh tạo hàng/cột full ngay khi bắt đầu.")]
    public bool avoidFullRowsCols = true;

    [Header("Spawn Colors")]
    [Range(2, 5)] public int minSpawnColors = 2;
    [Range(2, 5)] public int maxSpawnColors = 3;

    [Tooltip("Xác suất chọn cùng màu với láng giềng đã có (0..1).")]
    [Range(0f, 1f)] public float neighborSameColorBias = 0.7f;

    [Tooltip("Tần số Perlin noise. Nhỏ -> mảng màu liền mạch hơn.")]
    [Range(0.05f, 0.6f)] public float noiseScale = 0.22f;

    [Tooltip("Tổng số biến thể skin mà SkinProvider hỗ trợ (điền đúng theo SkinProvider).")]
    public int totalSkinVariants = 6;

    // ===== Intro waves config =====
    [Header("Intro Waves")]
    [Tooltip("Khoảng delay giữa các hàng cho sóng (giây).")]
    [Range(0.01f, 0.12f)] public float waveRowStep = 0.04f;
    [Tooltip("Jitter nhỏ giữa các cột (giây).")]
    [Range(0f, 0.05f)] public float waveColJitter = 0.01f;

    private void Awake()
    {
        if (gridView == null) gridView = GetComponent<GridView>();
        State = new BoardState(gridView.columns, gridView.rows);
    }

    private void Start()
    {
        EnsureGridBuilt();

        if (seedAtStart)
            SeedRandomOccupied(initialMinOccupied, initialMaxOccupied, avoidFullRowsCols);

        // Chạy intro waves ngay sau seed
        StartCoroutine(PlayIntroWavesForEmpties());
    }

    private void EnsureGridBuilt()
    {
        if (gridView == null) return;
        int expected = gridView.columns * gridView.rows;
        if (gridView.Cells == null || gridView.Cells.Count != expected)
        {
            gridView.Build();
        }
    }

    private Sprite SpriteFromVariant(int variantIndex)
    {
        var s = skin ? skin.GetTileSprite(variantIndex) : null;
        return s != null ? s : placedSpriteFallback;
    }

    // ===== PREVIEW footprint (giữ nguyên) =====
    public void ShowPreviewVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        ClearFootprintPreview();
        var sprite = SpriteFromVariant(variantIndex);

        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r < 0 || r >= gridView.rows || c < 0 || c >= gridView.columns) continue;

            int idx = r * gridView.columns + c;
            var view = gridView.Cells[idx];
            view.SetHoverPreview(true, sprite, previewAlpha);
            _previewIdx.Add(idx);
        }
    }

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

    // ===== Đặt thật (giữ nguyên) =====
    public void PaintPlacedVariant(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        var sprite = SpriteFromVariant(variantIndex);
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r < 0 || r >= gridView.rows || c < 0 || c >= gridView.columns) continue;

            var view = gridView.Cells[r * gridView.columns + c];
            view.SetOccupied(true, sprite);
            view.PlayPlaceFlashOnce();
        }
    }

    public int ResolveAndClearFullLinesAfterPlacementVariantAndGetCount(
        ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
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

        int totalLines = fullRows.Count + fullCols.Count;
        if (totalLines == 0) return 0;

        ClearPreview();

        var toClearIdx = new HashSet<int>();
        foreach (var r in fullRows) for (int c = 0; c < W; c++) toClearIdx.Add(r * W + c);
        foreach (var c in fullCols) for (int r = 0; r < H; r++) toClearIdx.Add(r * W + c);

        var sprite = SpriteFromVariant(variantIndex);
        foreach (var idx in toClearIdx)
        {
            var view = gridView.Cells[idx];
            view.SetOccupied(true, sprite);
        }

        State.ClearLines(fullRows, fullCols);

        const float step = 0.03f;
        foreach (var idx in toClearIdx)
        {
            int r = idx / W;
            int c = idx % W;

            float dRow = fullRows.Contains(r) ? c * step : float.PositiveInfinity;
            float dCol = fullCols.Contains(c) ? r * step : float.PositiveInfinity;
            float delay = Mathf.Min(dRow, dCol);

            gridView.Cells[idx].PlayClearPopAndReset(delay, 1.15f, 0.08f, 0.18f);
        }

        return totalLines;
    }

    public void ResolveAndClearFullLinesAfterPlacementVariant(
        ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        _ = ResolveAndClearFullLinesAfterPlacementVariantAndGetCount(shape, anchorRow, anchorCol, variantIndex);
    }

    // ====== INTRO WAVES ======
    private IEnumerator PlayIntroWavesForEmpties()
    {
        if (gridView == null || gridView.Cells == null) yield break;

        int W = gridView.columns;
        int H = gridView.rows;

        // Thu list ô trống sau khi seed
        var empty = new List<(int idx, int r, int c)>();
        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                if (!State.IsOccupied(r, c))
                    empty.Add((r * W + c, r, c));
            }
        }

        // Chuẩn bị sprite từ SkinProvider cho từng ô trống (giữ nhất quán cả 3 wave)
        var waveSprite = new Dictionary<int, Sprite>(empty.Count);
        foreach (var e in empty)
        {
            int variant = skin != null ? skin.RollVariant() : 0;
            var sprite = skin != null ? skin.GetTileSprite(variant) : placedSpriteFallback;
            waveSprite[e.idx] = sprite;
        }

        // --- Wave 1: từ dưới lên (flash bằng sprite)
        foreach (var e in empty)
        {
            float delay = (H - 1 - e.r) * waveRowStep + Random.Range(0f, waveColJitter);
            gridView.Cells[e.idx].PlayIntroFlashWithSprite(waveSprite[e.idx], delay, 0.10f, 0.18f, 1f);
        }
        float total1 = H * waveRowStep + 0.30f;
        yield return new WaitForSeconds(total1);

        // --- Wave 2: từ trên xuống (flash bằng sprite)
        foreach (var e in empty)
        {
            float delay = (e.r) * waveRowStep + Random.Range(0f, waveColJitter);
            gridView.Cells[e.idx].PlayIntroFlashWithSprite(waveSprite[e.idx], delay, 0.10f, 0.18f, 0.9f);
        }
    }

    // ====== (giữ nguyên) – Seed ngẫu nhiên 35–45 ô với 2–3 màu “gần nhau” ======
    public void SeedRandomOccupied(int minCount, int maxCount, bool avoidFullLines)
    {
        if (gridView == null || gridView.Cells == null || gridView.Cells.Count == 0) return;
        if (State == null) State = new BoardState(gridView.columns, gridView.rows);

        int W = gridView.columns;
        int H = gridView.rows;

        // Reset state + UI
        State = new BoardState(W, H);
        ClearPreview();
        for (int i = 0; i < gridView.Cells.Count; i++)
            gridView.Cells[i].SetOccupied(false, null);

        int lo = Mathf.Clamp(minCount, 0, W * H);
        int hi = Mathf.Clamp(maxCount, lo, W * H);
        int target = Random.Range(lo, hi + 1);

        int k = Random.Range(Mathf.Min(2, minSpawnColors), Mathf.Max(2, maxSpawnColors) + 1);
        k = Mathf.Clamp(k, 2, Mathf.Max(2, totalSkinVariants));
        var palette = PickDistinctVariants(k);

        // ô xáo trộn
        var all = new List<(int r, int c)>(W * H);
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                all.Add((r, c));
        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        int[] rowCount = new int[H];
        int[] colCount = new int[W];
        int[] placedVariant = new int[W * H];
        for (int i = 0; i < placedVariant.Length; i++) placedVariant[i] = -1;

        float ox = Random.Range(-1000f, 1000f);
        float oy = Random.Range(-1000f, 1000f);

        int ChooseVariant(int r, int c)
        {
            var neigh = new System.Collections.Generic.List<int>(4);
            void TryAdd(int rr, int cc)
            {
                if (rr < 0 || rr >= H || cc < 0 || cc >= W) return;
                int idx = rr * W + cc;
                int v = placedVariant[idx];
                if (v >= 0 && !neigh.Contains(v)) neigh.Add(v);
            }
            TryAdd(r - 1, c); TryAdd(r + 1, c); TryAdd(r, c - 1); TryAdd(r, c + 1);

            float t = Mathf.PerlinNoise((r + ox) * noiseScale, (c + oy) * noiseScale);
            int noiseIdx = Mathf.Clamp(Mathf.FloorToInt(t * k), 0, k - 1);
            int noiseVariant = palette[noiseIdx];

            if (neigh.Count > 0 && Random.value < neighborSameColorBias)
                return neigh[Random.Range(0, neigh.Count)];

            return noiseVariant;
        }

        int placed = 0;
        int safety = W * H * 3;

        foreach (var (r, c) in all)
        {
            if (placed >= target) break;
            if (safety-- <= 0) break;

            if (avoidFullLines)
            {
                if (rowCount[r] >= W - 1) continue;
                if (colCount[c] >= H - 1) continue;
            }

            State.SetOccupied(r, c, true);

            int variant = ChooseVariant(r, c);
            placedVariant[r * W + c] = variant;

            var sprite = (skin != null) ? skin.GetTileSprite(variant) : placedSpriteFallback;
            gridView.Cells[r * W + c].SetOccupied(true, sprite);

            rowCount[r]++; colCount[c]++; placed++;
        }

        if (placed < target)
        {
            foreach (var (r, c) in all)
            {
                if (placed >= target) break;
                if (State.IsOccupied(r, c)) continue;

                State.SetOccupied(r, c, true);

                int variant = ChooseVariant(r, c);
                placedVariant[r * W + c] = variant;

                var sprite = (skin != null) ? skin.GetTileSprite(variant) : placedSpriteFallback;
                gridView.Cells[r * W + c].SetOccupied(true, sprite);

                rowCount[r]++; colCount[c]++; placed++;
            }
        }
    }

    private List<int> PickDistinctVariants(int k)
    {
        var list = new List<int>(k);
        if (totalSkinVariants <= 0)
        {
            list.Add(0);
            if (k > 1) list.Add(1);
            return list;
        }

        var all = new List<int>(totalSkinVariants);
        for (int i = 0; i < totalSkinVariants; i++) all.Add(i);

        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        for (int i = 0; i < k && i < all.Count; i++) list.Add(all[i]);
        return list;
    }
}
