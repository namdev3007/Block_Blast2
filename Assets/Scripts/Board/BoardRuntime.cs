using System.Collections.Generic;
using UnityEngine;

public class BoardRuntime : MonoBehaviour
{
    public GridView gridView;
    public SkinProvider skin;                 // lấy sprite/màu từ SkinProvider
    public Sprite placedSpriteFallback;       // fallback khi thiếu sprite

    [Range(0f, 1f)] public float previewAlpha = 0.35f;
    [Range(0f, 1f)] public float linePreviewAlpha = 0.25f;
    public float clearFade = 0.15f;

    [Header("Scoring")]
    public GameScore score;

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

    [Header("Spawn Colors (cho seed)")]
    [Range(2, 5)] public int minSpawnColors = 2;
    [Range(2, 5)] public int maxSpawnColors = 3;
    [Range(0f, 1f)] public float neighborSameColorBias = 0.7f;
    [Range(0.05f, 0.6f)] public float noiseScale = 0.22f;
    [Tooltip("Số biến thể sprite có thể có trong SkinProvider (để random).")]
    public int totalSkinVariants = 6;

    // ===== Intro Wave cấu hình =====
    [Header("Intro Wave")]
    public bool playIntroWaveOnStart = true;

    [Tooltip("Độ trễ theo mỗi hàng (từ dưới lên).")]
    [Range(0f, 0.2f)] public float waveRowStepDelay = 0.05f;

    [Tooltip("Dùng sprite từ SkinProvider cho sóng (mỗi ô 1 biến thể ngẫu nhiên).")]
    public bool waveUseSkinSprites = true;

    [Header("Intro Wave (double pass)")]
    [Tooltip("Chạy 2 lượt: dưới→trên rồi trên→dưới.")]
    public bool waveDoublePass = true;

    [Tooltip("Khoảng nghỉ giữa 2 lượt wave.")]
    [Range(0f, 0.6f)] public float wavePassGap = 0.18f;

    [Tooltip("Nếu KHÔNG dùng sprite, sẽ tô màu (màu lấy từ SkinProvider).")]
    public bool waveTintOnlyWhenNoSprite = true;

    // Cache palette màu trích từ SkinProvider khi cần tô màu cho sóng
    private List<Color> _skinWaveColors;

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

        if (playIntroWaveOnStart)
            PlayIntroWaveOverEmpties();
    }

    private void EnsureGridBuilt()
    {
        if (gridView == null) return;
        int expected = gridView.columns * gridView.rows;
        if (gridView.Cells == null || gridView.Cells.Count != expected)
            gridView.Build();
    }

    private Sprite SpriteFromVariant(int variantIndex)
    {
        var s = skin ? skin.GetTileSprite(variantIndex) : null;
        return s != null ? s : placedSpriteFallback;
    }

    // =================== PREVIEW footprint (variant) ===================
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

        // rows
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

        // cols
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

    // =================== Đặt thật (variant) ===================
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
            view.PlayPlaceFlashOnce(); // sáng viền nhẹ khi đặt
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
            view.SetOccupied(true, sprite); // để thấy “ăn” bằng block đó
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

    // =================== SEED ngẫu nhiên 35–45 ô (màu gần nhau) ===================
    public void SeedRandomOccupied(int minCount, int maxCount, bool avoidFullLines)
    {
        if (gridView == null || gridView.Cells == null || gridView.Cells.Count == 0) return;
        if (State == null) State = new BoardState(gridView.columns, gridView.rows);

        int W = gridView.columns;
        int H = gridView.rows;

        // Reset
        State = new BoardState(W, H);
        ClearPreview();
        for (int i = 0; i < gridView.Cells.Count; i++)
            gridView.Cells[i].SetOccupied(false, null);

        // Target
        int lo = Mathf.Clamp(minCount, 0, W * H);
        int hi = Mathf.Clamp(maxCount, lo, W * H);
        int target = Random.Range(lo, hi + 1);

        // Chọn 2..3 biến thể (màu) cho seed (cluster nhẹ)
        int k = Random.Range(Mathf.Min(2, minSpawnColors), Mathf.Max(2, maxSpawnColors) + 1);
        k = Mathf.Clamp(k, 2, Mathf.Max(2, totalSkinVariants));
        var palette = PickDistinctVariants(k);

        // Shuffle cells
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

        // Perlin offset
        float ox = Random.Range(-1000f, 1000f);
        float oy = Random.Range(-1000f, 1000f);

        int ChooseVariant(int r, int c)
        {
            var neigh = new List<int>(4);
            void TryAdd(int rr, int cc)
            {
                if (rr < 0 || rr >= H || cc < 0 || cc >= W) return;
                int idx = rr * W + cc;
                int v = placedVariant[idx];
                if (v >= 0 && !neigh.Contains(v)) neigh.Add(v);
            }
            TryAdd(r - 1, c);
            TryAdd(r + 1, c);
            TryAdd(r, c - 1);
            TryAdd(r, c + 1);

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

        // Nếu chưa đủ target, nới lỏng luật
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

    // =================== INTRO WAVE: 2 lượt (dưới→trên, rồi trên→dưới) ===================
    public void PlayIntroWaveOverEmpties()
    {
        if (gridView == null || gridView.Cells == null || gridView.Cells.Count == 0) return;

        int W = gridView.columns;
        int H = gridView.rows;

        // Chuẩn bị palette màu nếu cần (khi không dùng sprite)
        if (!waveUseSkinSprites)
            _skinWaveColors = BuildSkinWaveColors();

        // Thống nhất thông số anim (dùng cho cả 2 pass)
        const float waveFadeIn = 0.18f;
        const float waveHold = 0.06f;
        const float waveFadeOut = 0.25f;
        const float waveRise = 14f;   // pass 1
        const float waveRise2 = -14f;  // pass 2: âm để tạo cảm giác “đổ xuống”
        float animTotal = waveFadeIn + waveHold + waveFadeOut;

        // ===== PASS 1: dưới -> trên =====
        float lastRowDelayPass1 = 0f;
        for (int r = H - 1; r >= 0; r--)
        {
            float rowDelay = (H - 1 - r) * waveRowStepDelay;
            lastRowDelayPass1 = rowDelay;

            for (int c = 0; c < W; c++)
            {
                var view = gridView.GetCell(r, c);
                if (view == null || view.IsOccupied) continue;

                // (Tuỳ chọn) jitter nhẹ theo cột cho đẹp hơn
                float colJitter = (c % 2 == 0 ? 0f : 0.012f);

                Sprite sp = null; Color? tint = null;
                if (waveUseSkinSprites && skin != null)
                {
                    int v = skin.RollVariant();
                    sp = skin.GetTileSprite(v);
                    tint = Color.white;
                }
                else
                {
                    var palette = _skinWaveColors;
                    tint = (palette == null || palette.Count == 0)
                        ? (Color?)Color.white
                        : palette[Random.Range(0, palette.Count)];
                    sp = view.defaultSprite;
                }

                view.PlayIntroWave(
                    spriteForWave: sp,
                    tint: tint,
                    delay: rowDelay + colJitter,
                    fadeIn: waveFadeIn,
                    hold: waveHold,
                    fadeOut: waveFadeOut,
                    risePixels: waveRise
                );
            }
        }

        if (!waveDoublePass) return;

        // ===== PASS 2: trên -> dưới (bắt đầu sau khi pass 1 hoàn tất) =====
        float baseDelay = lastRowDelayPass1 + animTotal + wavePassGap;

        for (int r = 0; r < H; r++)
        {
            float rowDelay = baseDelay + (r * waveRowStepDelay);

            for (int c = 0; c < W; c++)
            {
                var view = gridView.GetCell(r, c);
                if (view == null || view.IsOccupied) continue;

                float colJitter = (c % 2 == 0 ? 0f : 0.012f);

                Sprite sp = null; Color? tint = null;
                if (waveUseSkinSprites && skin != null)
                {
                    int v = skin.RollVariant();
                    sp = skin.GetTileSprite(v);
                    tint = Color.white;
                }
                else
                {
                    var palette = _skinWaveColors;
                    tint = (palette == null || palette.Count == 0)
                        ? (Color?)Color.white
                        : palette[Random.Range(0, palette.Count)];
                    sp = view.defaultSprite;
                }

                view.PlayIntroWave(
                    spriteForWave: sp,
                    tint: tint,
                    delay: rowDelay + colJitter,
                    fadeIn: waveFadeIn,
                    hold: waveHold,
                    fadeOut: waveFadeOut,
                    risePixels: waveRise2   // âm để “đổ xuống”
                );
            }
        }
    }

    // Thu màu trung bình từ sprite các biến thể trong SkinProvider
    private List<Color> BuildSkinWaveColors()
    {
        var colors = new List<Color>();
        if (skin == null) { colors.Add(Color.white); return colors; }

        int variantsToProbe = Mathf.Max(1, totalSkinVariants);
        var seen = new HashSet<int>();
        int safety = variantsToProbe * 4;

        while (colors.Count < variantsToProbe && safety-- > 0)
        {
            int v = skin.RollVariant();
            if (!seen.Add(v)) continue;

            var sp = skin.GetTileSprite(v);
            if (sp == null || sp.texture == null) continue;

            var avg = AverageSpriteColor(sp);
            if (avg.a < 0.1f) continue;
            if (avg.maxColorComponent < 0.1f) continue;

            colors.Add(avg);
        }

        if (colors.Count == 0) colors.Add(Color.white);
        return colors;
    }

    // Tính màu trung bình từ sprite (mẫu thưa để rẻ)
    private Color AverageSpriteColor(Sprite s)
    {
        try
        {
            var tex = s.texture;
            var r = s.textureRect; // pixel rect
            int x = Mathf.RoundToInt(r.x);
            int y = Mathf.RoundToInt(r.y);
            int w = Mathf.RoundToInt(r.width);
            int h = Mathf.RoundToInt(r.height);

            int stride = Mathf.Max(1, Mathf.Min(w, h) / 24); // adaptive & nhẹ
            float rAcc = 0, gAcc = 0, bAcc = 0, aAcc = 0;
            int n = 0;

            for (int yy = y; yy < y + h; yy += stride)
            {
                for (int xx = x; xx < x + w; xx += stride)
                {
                    var c = tex.GetPixel(xx, yy);
                    if (c.a < 0.2f) continue;
                    rAcc += c.r; gAcc += c.g; bAcc += c.b; aAcc += c.a;
                    n++;
                }
            }

            if (n == 0) return Color.white;
            return new Color(rAcc / n, gAcc / n, bAcc / n, aAcc / n);
        }
        catch
        {
            return Color.white;
        }
    }

    // Chọn k biến thể sprite khác nhau cho seed
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

        // shuffle
        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        for (int i = 0; i < k && i < all.Count; i++) list.Add(all[i]);
        return list;
    }
}
