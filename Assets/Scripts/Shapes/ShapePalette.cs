using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeClass { Small, Medium, Large, Line, Square }

public class ShapePalette : MonoBehaviour
{
    [Header("Refs")]
    public ShapeLibrary library;
    public List<ShapeItemView> slots = new List<ShapeItemView>();
    public BoardRuntime board;
    public SkinProvider skinProvider;

    [Header("Spawn Config")]
    public ShapeSpawnConfig config; // assign via Inspector (không New() trực tiếp)

    [Header("Anti-repeat")]
    public int historyKeep = 6;

    private readonly List<ShapeData> _current = new();
    private readonly Queue<ShapeData> _history = new();
    private readonly List<int> _slotVariants = new();

    private System.Random _rng;
    private int _refillCount;

    private void Awake()
    {
        _rng = new System.Random();

        // An toàn nếu quên gán asset trong Inspector
        if (config == null)
        {
            Debug.LogWarning($"{name}: ShapeSpawnConfig is null. Creating a runtime instance (won't be saved).");
            config = ScriptableObject.CreateInstance<ShapeSpawnConfig>();
        }
    }

    private void Start() => Refill();

    public void Refill()
    {
        if (library == null || slots == null || slots.Count == 0) return;

        List<ShapeData> hand;

        if (config.useTripletBag && slots.Count >= 3 && board != null && board.State != null)
        {
            // NEW: dùng Triplet Bag
            hand = BuildTripletBagForCurrentBoard(slots.Count);
        }
        else
        {
            // Hand thông minh như cũ
            hand = BuildHandSmart(slots.Count);

            // NEW: inject 1 hole-filler ngẫu nhiên nếu không dùng bag
            if (!config.useTripletBag && config.enableHoleFiller && (_rng.NextDouble() < config.holeFillerChance)
                && board != null && board.State != null)
            {
                var holeFits = CollectHoleFillerShapes(board.State, config.holeFillerMaxCells, config.holeFitterSampleShapes);
                if (holeFits.Count > 0)
                {
                    int slotPick = _rng.Next(Mathf.Min(slots.Count, hand.Count));
                    hand[slotPick] = holeFits[_rng.Next(holeFits.Count)];
                }
            }
        }

        _current.Clear(); _current.AddRange(hand);

        _slotVariants.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            int variant = (skinProvider != null) ? skinProvider.RollVariant() : 0;
            _slotVariants.Add(variant);

            Sprite displaySprite = (skinProvider != null)
                ? skinProvider.GetTileSprite(variant)
                : (board != null ? board.placedSpriteFallback : null);

            slots[i].Render(_current[i], displaySprite);

            var cg = slots[i].GetComponentInParent<CanvasGroup>();
            if (cg) cg.alpha = 1f;
        }

        _refillCount++;
    }

    public void OnAllUsed() => Refill();

    public ShapeData Peek(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _current.Count) return null;
        return _current[slotIndex];
    }

    public int PeekVariant(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotVariants.Count) return 0;
        return _slotVariants[slotIndex];
    }

    public void Consume(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _current.Count) return;
        if (_current[slotIndex] == null) return;

        _history.Enqueue(_current[slotIndex]);
        while (_history.Count > historyKeep) _history.Dequeue();

        _current[slotIndex] = null;
        if (slotIndex < _slotVariants.Count) _slotVariants[slotIndex] = 0;

        if (slots[slotIndex] != null) slots[slotIndex].Clear();

        if (AllConsumed()) Refill();
    }

    private bool AllConsumed()
    {
        for (int i = 0; i < _current.Count; i++)
            if (_current[i] != null) return false;
        return true;
    }

    // ========================= NEW: Triplet Bag Builder =========================
    private List<ShapeData> BuildTripletBagForCurrentBoard(int needed)
    {
        var state = board.State;
        var weights = ComputeWeightsWithDDA();
        var candidates = new List<(ShapeData a, ShapeData b, ShapeData c)>();
        int trials = 0;

        // Chuẩn bị danh sách hole-filler nếu bật & trúng xác suất
        List<ShapeData> holeFits = null;
        bool tryHoleFiller = config.enableHoleFiller && config.holeFillerAffectsBag &&
                             (_rng.NextDouble() < config.holeFillerChance);
        if (tryHoleFiller)
        {
            holeFits = CollectHoleFillerShapes(state, config.holeFillerMaxCells, config.holeFitterSampleShapes);
            if (holeFits != null && holeFits.Count == 0) holeFits = null;
        }

        // Sinh đến khi đủ ~bagCandidateCount hoặc hết trials
        while (candidates.Count < config.bagCandidateCount && trials < config.bagMaxBuildTrials)
        {
            trials++;

            // pick 3 shapes; ưu tiên chèn hole-filler nếu có
            ShapeData s1, s2, s3;
            if (holeFits != null && holeFits.Count > 0 && _rng.Next(3) == 0)
            {
                s1 = holeFits[_rng.Next(holeFits.Count)];
                s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, false);
                s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, false);
            }
            else if (holeFits != null && holeFits.Count > 0 && _rng.Next(2) == 0)
            {
                s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, false);
                s2 = holeFits[_rng.Next(holeFits.Count)];
                s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, false);
            }
            else if (holeFits != null && holeFits.Count > 0)
            {
                s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, false);
                s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, false);
                s3 = holeFits[_rng.Next(holeFits.Count)];
            }
            else
            {
                s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, false);
                s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, false);
                s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, false);
            }

            if (config.bagAvoidDuplicateShapes &&
               (ReferenceEquals(s1, s2) || ReferenceEquals(s1, s3) || ReferenceEquals(s2, s3)))
                continue;

            // Validate bag
            if (ValidateBagOnSnapshot(state, s1, s2, s3))
                candidates.Add((s1, s2, s3));
        }

        if (candidates.Count == 0)
        {
            // fallback: dùng hand thông minh cũ
            return BuildHandSmart(needed);
        }

        // pick ngẫu nhiên 1 bag
        var pick = candidates[_rng.Next(candidates.Count)];
        var hand = new List<ShapeData>(needed);
        hand.Add(pick.a);
        if (needed > 1) hand.Add(pick.b);
        if (needed > 2) hand.Add(pick.c);
        while (hand.Count < needed) hand.Add(library.GetRandom());
        return hand;
    }

    private bool ValidateBagOnSnapshot(BoardState state, ShapeData s1, ShapeData s2, ShapeData s3)
    {
        if (state == null) return true;

        var snap = new BoardSnapshot(state);
        var arr = new[] { s1, s2, s3 };

        bool seqOK = true;
        if (config.bagRequireSequentialPlaceability)
            seqOK = ExistsSequentialPlacement(snap, arr);

        if (!seqOK) return false;

        if (config.bagRequireAtLeastOneLineClear)
        {
            // kiểm tra liệu có ít nhất một shape tạo line clear trong bất kỳ bước của chuỗi khả dụng
            if (!ExistsSequentialPlacement(snap, arr, requireAnyLineClear: true))
                return false;
        }

        return true;
    }

    // DFS nông tìm chuỗi đặt 3 shape theo một hoán vị và anchor bất kỳ
    private bool ExistsSequentialPlacement(BoardSnapshot start, ShapeData[] shapes, bool requireAnyLineClear = false)
    {
        // thử mọi hoán vị 3! = 6
        int[][] perms = {
            new[] {0,1,2}, new[] {0,2,1},
            new[] {1,0,2}, new[] {1,2,0},
            new[] {2,0,1}, new[] {2,1,0}
        };

        foreach (var p in perms)
        {
            if (SearchChain(start, shapes, p, 0, requireAnyLineClear, anyClearSeen: false))
                return true;
        }
        return false;
    }

    private bool SearchChain(BoardSnapshot snap, ShapeData[] shapes, int[] order, int depth, bool requireAnyLineClear, bool anyClearSeen)
    {
        if (depth == order.Length) return !requireAnyLineClear || anyClearSeen;

        var s = shapes[order[depth]];
        foreach (var pos in snap.AllAnchorsFor(s))
        {
            bool cleared = (snap.CountLinesCompletedIfPlaced(s, pos.r, pos.c) > 0);
            var next = snap.Clone();
            next.Place(s, pos.r, pos.c);

            if (SearchChain(next, shapes, order, depth + 1, requireAnyLineClear, anyClearSeen || cleared))
                return true;
        }
        return false;
    }

    // ========================= NEW: Hole Filler Helpers =========================
    private struct EmptyRegion
    {
        public int top, left, height, width, count;
        public bool[,] pattern;
    }

    private List<EmptyRegion> FindEmptyRegions(BoardState st, int maxCells)
    {
        int H = st.Height, W = st.Width;
        bool[,] occ = new bool[H, W];
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                occ[r, c] = st.IsOccupied(r, c);

        bool[,] vis = new bool[H, W];
        List<EmptyRegion> regions = new List<EmptyRegion>();
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int r0 = 0; r0 < H; r0++)
        {
            for (int c0 = 0; c0 < W; c0++)
            {
                if (occ[r0, c0] || vis[r0, c0]) continue;

                Queue<(int r, int c)> q = new Queue<(int r, int c)>();
                List<(int r, int c)> cells = new List<(int r, int c)>();
                q.Enqueue((r0, c0));
                vis[r0, c0] = true;

                int minR = r0, maxR = r0, minC = c0, maxC = c0;
                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    cells.Add(cur);
                    if (cur.r < minR) minR = cur.r;
                    if (cur.r > maxR) maxR = cur.r;
                    if (cur.c < minC) minC = cur.c;
                    if (cur.c > maxC) maxC = cur.c;

                    for (int k = 0; k < 4; k++)
                    {
                        int nr = cur.r + dr[k], nc = cur.c + dc[k];
                        if (nr < 0 || nr >= H || nc < 0 || nc >= W) continue;
                        if (occ[nr, nc] || vis[nr, nc]) continue;
                        vis[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }

                if (cells.Count <= 0 || cells.Count > maxCells) continue;

                int hh = maxR - minR + 1;
                int ww = maxC - minC + 1;
                bool[,] pattern = new bool[hh, ww];
                foreach (var p in cells)
                    pattern[p.r - minR, p.c - minC] = true;

                regions.Add(new EmptyRegion
                {
                    top = minR,
                    left = minC,
                    height = hh,
                    width = ww,
                    count = cells.Count,
                    pattern = pattern
                });
            }
        }
        return regions;
    }

    private bool ShapeExactlyMatchesRegion(ShapeData s, EmptyRegion reg)
    {
        if (s == null || s.board == null) return false;
        var b = s.GetBounds();
        if (b.maxR < b.minR) return false;

        int sh = b.maxR - b.minR + 1;
        int sw = b.maxC - b.minC + 1;
        if (sh != reg.height || sw != reg.width) return false;

        int filled = 0;
        for (int r = b.minR; r <= b.maxR; r++)
        {
            for (int c = b.minC; c <= b.maxC; c++)
            {
                bool v = s.board[r].column[c];
                bool want = reg.pattern[r - b.minR, c - b.minC];
                if (v != want) return false;
                if (v) filled++;
            }
        }
        return filled == reg.count;
    }

    private List<ShapeData> CollectHoleFillerShapes(BoardState st, int maxCells, int sampleCount)
    {
        var res = new List<ShapeData>();
        if (library == null) return res;

        var regions = FindEmptyRegions(st, maxCells);
        if (regions.Count == 0) return res;

        HashSet<ShapeData> seen = new HashSet<ShapeData>();
        for (int i = 0; i < sampleCount; i++)
        {
            var s = library.GetRandom();
            if (s == null || seen.Contains(s)) continue;

            bool fits = false;
            for (int k = 0; k < regions.Count; k++)
            {
                if (ShapeExactlyMatchesRegion(s, regions[k])) { fits = true; break; }
            }
            if (fits) { seen.Add(s); res.Add(s); }
        }
        return res;
    }

    // ========================= Snapshot simulator =========================
    private sealed class BoardSnapshot
    {
        public readonly int W, H;
        private readonly bool[] occ; // r*W + c

        public BoardSnapshot(BoardState st)
        {
            W = st.Width; H = st.Height;
            occ = new bool[W * H];
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                    occ[r * W + c] = st.IsOccupied(r, c);
        }

        private BoardSnapshot(int W, int H, bool[] occ)
        {
            this.W = W; this.H = H; this.occ = occ;
        }

        public BoardSnapshot Clone()
        {
            var copy = new bool[occ.Length];
            Array.Copy(occ, copy, occ.Length);
            return new BoardSnapshot(W, H, copy);
        }

        public bool IsInside(int r, int c) => (r >= 0 && r < H && c >= 0 && c < W);

        public bool CanPlace(ShapeData s, int anchorR, int anchorC)
        {
            var b = s.GetBounds();
            if (b.maxR < b.minR) return false;

            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x;
                int c = anchorC + cell.y;
                if (!IsInside(r, c)) return false;
                if (occ[r * W + c]) return false;
            }
            return true;
        }

        public void Place(ShapeData s, int anchorR, int anchorC)
        {
            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x;
                int c = anchorC + cell.y;
                occ[r * W + c] = true;
            }
        }

        public IEnumerable<(int r, int c)> AllAnchorsFor(ShapeData s)
        {
            var b = s.GetBounds();
            if (b.maxR < b.minR) yield break;

            int sh = b.maxR - b.minR + 1;
            int sw = b.maxC - b.minC + 1;

            for (int r = -b.minR; r <= H - sh; r++)
            {
                for (int c = -b.minC; c <= W - sw; c++)
                {
                    if (CanPlace(s, r, c)) yield return (r, c);
                }
            }
        }

        public int CountLinesCompletedIfPlaced(ShapeData s, int anchorR, int anchorC)
        {
            // apply into temp occupancy
            var tmp = new bool[occ.Length];
            Array.Copy(occ, tmp, occ.Length);

            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x;
                int c = anchorC + cell.y;
                if (IsInside(r, c)) tmp[r * W + c] = true;
            }

            int clears = 0;
            // rows
            for (int r = 0; r < H; r++)
            {
                bool full = true;
                for (int c = 0; c < W; c++)
                    if (!tmp[r * W + c]) { full = false; break; }
                if (full) clears++;
            }
            // cols
            for (int c = 0; c < W; c++)
            {
                bool full = true;
                for (int r = 0; r < H; r++)
                    if (!tmp[r * W + c]) { full = false; break; }
                if (full) clears++;
            }
            return clears;
        }
    }

    // ========================= DDA & Smart picking (giữ nguyên) =========================
    private ClassWeights ComputeWeightsWithDDA()
    {
        var W = config.baseWeights;
        W.Normalize();

        bool tight = false;
        if (board != null && board.State != null)
        {
            int H = board.State.Height, WW = board.State.Width;
            int occ = 0;
            for (int r = 0; r < H; r++)
                for (int c = 0; c < WW; c++)
                    if (board.State.IsOccupied(r, c)) occ++;

            float freeRatio = 1f - (float)occ / (WW * H);
            tight = freeRatio < config.tightBoardFreeThreshold;
        }

        if (tight)
        {
            W.small *= config.tightSmallBoost;
            W.line *= config.tightLineBoost;
            W.large *= config.tightLargePenalty;
        }

        float t = Mathf.Clamp01(config.rampRefillsToMax <= 0 ? 1f : (float)_refillCount / config.rampRefillsToMax);
        float smallMul = Mathf.Lerp(1f, config.rampSmallPenalty, t);
        float largeMul = Mathf.Lerp(1f, config.rampLargeBoost, t);
        W.small *= smallMul;
        W.large *= largeMul;

        W.Normalize();
        return W;
    }

    private List<ShapeData> BuildHandSmart(int count)
    {
        var state = board != null ? board.State : null;

        for (int attempt = 0; attempt < config.maxHandBuildAttempts; attempt++)
        {
            var weights = ComputeWeightsWithDDA();
            var hand = new List<ShapeData>(count);
            int placeableSlots = 0;

            for (int i = 0; i < count; i++)
            {
                bool forceLine = _rng.NextDouble() < config.forceLineClearChance;
                bool mustBePlaceable = (placeableSlots < config.requiredPlaceableSlots);

                var pick = PickOneShapeSmart(state, hand, weights, forceLine, mustBePlaceable);
                hand.Add(pick);

                if (state == null || CountPlacements(pick, state) > 0) placeableSlots++;
            }

            if (placeableSlots >= Mathf.Max(0, config.requiredPlaceableSlots))
                return hand;
        }

        if (!config.enablePity) return BuildFallbackRandom(count);
        return BuildPityHand(count, board != null ? board.State : null);
    }

    private List<ShapeData> BuildFallbackRandom(int count)
    {
        var hand = new List<ShapeData>(count);
        for (int i = 0; i < count; i++) hand.Add(library.GetRandom());
        return hand;
    }

    private List<ShapeData> BuildPityHand(int count, BoardState state)
    {
        var hand = new List<ShapeData>(count);
        int guaranteed = Mathf.Min(config.pityRescueSlots, count);
        for (int i = 0; i < guaranteed; i++) hand.Add(PickSmallAndPlaceable(state));

        var weights = ComputeWeightsWithDDA();
        for (int i = guaranteed; i < count; i++)
            hand.Add(PickOneShapeSmart(state, hand, weights, false, false));

        return hand;
    }

    private ShapeData PickSmallAndPlaceable(BoardState state)
    {
        for (int tries = 0; tries < 64; tries++)
        {
            var s = library.GetRandom();
            if (s == null) continue;
            if (Classify(s) == ShapeClass.Small && (state == null || CountPlacements(s, state) > 0))
                return s;
        }
        return library.GetRandom();
    }

    private ShapeData PickOneShapeSmart(BoardState state, List<ShapeData> currentHand,
                                        ClassWeights weights, bool forceLine, bool mustBePlaceable)
    {
        ShapeClass targetClass = forceLine ? ShapeClass.Line : RollClass(weights);

        ShapeData best = null;
        float bestScore = float.NegativeInfinity;
        var top = new List<(ShapeData s, float score)>();

        for (int t = 0; t < Mathf.Max(1, config.samplesPerSlot); t++)
        {
            var cand = library.GetRandom();
            if (cand == null) continue;
            if (currentHand.Contains(cand)) continue;
            if (_history.Contains(cand)) continue;

            if (Classify(cand) != targetClass)
                if (_rng.NextDouble() > 0.15) continue; // soft reject

            var eval = EvaluateShape(cand, state);
            if (mustBePlaceable && !eval.anyPlaceable) continue;
            if (forceLine && eval.maxLineClears <= 0) continue;

            float score = ScoreShape(eval, cand, currentHand);

            if (top.Count < config.topK) top.Add((cand, score));
            else
            {
                int idxMin = 0; float min = top[0].score;
                for (int i = 1; i < top.Count; i++)
                    if (top[i].score < min) { min = top[i].score; idxMin = i; }
                if (score > min) top[idxMin] = (cand, score);
            }
            if (score > bestScore) { bestScore = score; best = cand; }
        }

        if (top.Count == 0)
        {
            for (int i = 0; i < 50; i++)
            {
                var s = library.GetRandom();
                if (s == null) continue;
                if (mustBePlaceable && state != null && CountPlacements(s, state) == 0) continue;
                return s;
            }
            return library.GetRandom();
        }

        return RoulettePick(top);
    }

    private struct Eval
    {
        public int cells;
        public int placements;
        public int maxLineClears;
        public bool anyPlaceable => placements > 0;
    }

    private Eval EvaluateShape(ShapeData s, BoardState state)
    {
        var e = new Eval { cells = CountCells(s), placements = 0, maxLineClears = 0 };
        if (state == null) return e;

        var b = s.GetBounds();
        if (b.maxR < b.minR) return e;

        int W = state.Width, H = state.Height;
        for (int r = -b.minR; r <= H - (b.maxR - b.minR + 1); r++)
        {
            for (int c = -b.minC; c <= W - (b.maxC - b.minC + 1); c++)
            {
                if (!state.CanPlace(s, r, c)) continue;
                e.placements++;

                int clears = CountLinesCompletedIfPlaced_OnBoardState(state, s, r, c);
                if (clears > e.maxLineClears) e.maxLineClears = clears;
            }
        }
        return e;
    }

    private float ScoreShape(Eval e, ShapeData s, List<ShapeData> currentHand)
    {
        float score = 0f;
        if (e.anyPlaceable) score += config.wPlaceable;
        score += config.wPlacementCount * AdjustToTarget(e.placements, config.minPlacementsTarget, config.maxPlacementsTarget);
        score += config.wLineClear * e.maxLineClears;
        score += config.wArea * e.cells;

        int sameArea = 0;
        foreach (var x in currentHand) if (x != null && CountCells(x) == e.cells) sameArea++;
        score -= 0.5f * sameArea;

        score += (float)_rng.NextDouble() * 0.01f;
        return score;
    }

    private float AdjustToTarget(int n, int lo, int hi)
    {
        if (n <= 0) return 0;
        if (n >= lo && n <= hi) return n;
        int d = (n < lo) ? (lo - n) : (n - hi);
        return Mathf.Max(0, n - d);
    }

    private int CountCells(ShapeData s)
    {
        int n = 0;
        if (s == null || s.board == null) return 0;
        for (int r = 0; r < s.rows; r++)
            for (int c = 0; c < s.columns; c++)
                if (s.board[r].column[c]) n++;
        return n;
    }

    private int CountPlacements(ShapeData s, BoardState state)
    {
        if (state == null) return 1;
        var b = s.GetBounds();
        if (b.maxR < b.minR) return 0;
        int W = state.Width, H = state.Height, count = 0;

        for (int r = -b.minR; r <= H - (b.maxR - b.minR + 1); r++)
            for (int c = -b.minC; c <= W - (b.maxC - b.minC + 1); c++)
                if (state.CanPlace(s, r, c)) count++;
        return count;
    }

    private int CountLinesCompletedIfPlaced_OnBoardState(BoardState state, ShapeData s, int anchorRow, int anchorCol)
    {
        int W = state.Width, H = state.Height;
        var proposed = new bool[W * H];
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                proposed[r * W + c] = state.IsOccupied(r, c);

        foreach (var cell in s.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r >= 0 && r < H && c >= 0 && c < W) proposed[r * W + c] = true;
        }

        int clears = 0;
        for (int r = 0; r < H; r++)
        {
            bool full = true;
            for (int c = 0; c < W; c++)
                if (!proposed[r * W + c]) { full = false; break; }
            if (full) clears++;
        }
        for (int c = 0; c < W; c++)
        {
            bool full = true;
            for (int r = 0; r < H; r++)
                if (!proposed[r * W + c]) { full = false; break; }
            if (full) clears++;
        }
        return clears;
    }

    private ShapeClass Classify(ShapeData s)
    {
        if (s == null || s.board == null) return ShapeClass.Small;
        var b = s.GetBounds();
        int h = (b.maxR - b.minR + 1);
        int w = (b.maxC - b.minC + 1);
        int cells = CountCells(s);

        if ((h == 1 || w == 1) && cells >= 3) return ShapeClass.Line;
        if ((h == w) && (h == 2 || h == 3) && cells == h * w) return ShapeClass.Square;
        if (cells <= 3) return ShapeClass.Small;
        if (cells <= 5) return ShapeClass.Medium;
        return ShapeClass.Large;
    }

    private ShapeClass RollClass(ClassWeights w)
    {
        float r = (float)_rng.NextDouble();
        if (r < w.small) return ShapeClass.Small; r -= w.small;
        if (r < w.medium) return ShapeClass.Medium; r -= w.medium;
        if (r < w.large) return ShapeClass.Large; r -= w.large;
        if (r < w.line) return ShapeClass.Line;
        return ShapeClass.Square;
    }

    private ShapeData RoulettePick(List<(ShapeData s, float score)> top)
    {
        float sum = 0f;
        foreach (var it in top) sum += Mathf.Max(0.0001f, it.score);
        float r = (float)_rng.NextDouble() * sum, acc = 0f;
        foreach (var it in top)
        {
            acc += Mathf.Max(0.0001f, it.score);
            if (r <= acc) return it.s;
        }
        return top[0].s;
    }
}
