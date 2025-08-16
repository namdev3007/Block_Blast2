using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeClass { Small, Medium, Large, Line, Square }

public class ShapePalette : MonoBehaviour
{
    [Header("Refs")]
    public ShapeLibrary library;
    public List<ShapeItemView> slots = new List<ShapeItemView>();
    public BoardRuntime board;                  // để đọc BoardState

    [Header("Spawn Config")]
    public ShapeSpawnConfig config = new ShapeSpawnConfig();

    [Header("Anti-repeat")]
    public int historyKeep = 6;

    private readonly List<ShapeData> _current = new List<ShapeData>();
    private readonly Queue<ShapeData> _history = new Queue<ShapeData>();
    private System.Random _rng;
    private int _refillCount; // dùng cho ramp khó dần

    private void Awake()
    {
        _rng = new System.Random();
    }

    private void Start()
    {
        Refill();
    }

    // ================= API gốc =================
    public void Refill()
    {
        if (library == null || slots == null || slots.Count == 0) return;

        var hand = BuildHandSmart(slots.Count);
        _current.Clear();
        _current.AddRange(hand);

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Render(_current[i]);

            // đảm bảo slot hiện lại
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

    public void Consume(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _current.Count) return;
        if (_current[slotIndex] == null) return;

        // lưu lịch sử nhẹ (giảm lặp)
        _history.Enqueue(_current[slotIndex]);
        while (_history.Count > historyKeep) _history.Dequeue();

        _current[slotIndex] = null;
        if (slots[slotIndex] != null) slots[slotIndex].Clear();

        if (AllConsumed()) Refill();
    }

    private bool AllConsumed()
    {
        for (int i = 0; i < _current.Count; i++)
            if (_current[i] != null) return false;
        return true;
    }

    // =============== Core: hand thông minh ===============
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

            // Solvable guard
            if (placeableSlots >= Mathf.Max(0, config.requiredPlaceableSlots))
                return hand;
        }

        // Pity / Rescue
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

        for (int i = 0; i < guaranteed; i++)
            hand.Add(PickSmallAndPlaceable(state));

        var weights = ComputeWeightsWithDDA();
        for (int i = guaranteed; i < count; i++)
            hand.Add(PickOneShapeSmart(state, hand, weights, forceLine: false, mustBePlaceable: false));

        return hand;
    }

    // =============== DDA ===============
    private ClassWeights ComputeWeightsWithDDA()
    {
        var W = config.baseWeights;
        W.Normalize();

        // 1) Board “tight”?
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

        // 2) Ramp khó dần theo số lần refill
        float t = Mathf.Clamp01(config.rampRefillsToMax <= 0 ? 1f : (float)_refillCount / config.rampRefillsToMax);
        // Lerp multiplicative: small giảm dần, large tăng dần
        float smallMul = Mathf.Lerp(1f, config.rampSmallPenalty, t);
        float largeMul = Mathf.Lerp(1f, config.rampLargeBoost, t);
        W.small *= smallMul;
        W.large *= largeMul;

        W.Normalize();
        return W;
    }

    // =============== Pick 1 shape (smart) ===============
    private ShapeData PickOneShapeSmart(BoardState state, List<ShapeData> currentHand,
                                        ClassWeights weights, bool forceLine, bool mustBePlaceable)
    {
        // 1) quyết định class mục tiêu theo weights
        ShapeClass targetClass = RollClass(weights);

        // nếu ép line-clear thì ưu tiên class Line trước
        if (forceLine) targetClass = ShapeClass.Line;

        ShapeData best = null;
        float bestScore = float.NegativeInfinity;
        var top = new List<(ShapeData s, float score)>();

        for (int t = 0; t < Mathf.Max(1, config.samplesPerSlot); t++)
        {
            var cand = library.GetRandom();
            if (cand == null) continue;

            // Anti-repeat nhẹ
            if (currentHand.Contains(cand)) continue;
            if (_history.Contains(cand)) continue;

            // Lọc theo class bằng reject-sampling
            if (Classify(cand) != targetClass)
            {
                // Cho phép 1 phần trăm lệch class để tránh kẹt tuyệt đối
                if (_rng.NextDouble() > 0.15) continue;
            }

            // Chấm điểm
            var eval = EvaluateShape(cand, state);
            if (mustBePlaceable && !eval.anyPlaceable) continue;
            if (forceLine && eval.maxLineClears <= 0) continue;

            float score = ScoreShape(eval, cand, currentHand);

            if (top.Count < config.topK) top.Add((cand, score));
            else
            {
                // replace min
                int idxMin = 0; float min = top[0].score;
                for (int i = 1; i < top.Count; i++)
                    if (top[i].score < min) { min = top[i].score; idxMin = i; }
                if (score > min) top[idxMin] = (cand, score);
            }

            if (score > bestScore) { bestScore = score; best = cand; }
        }

        if (top.Count == 0)
        {
            // fallback: thả lỏng constraint để không kẹt
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

    // =============== Pity helper ===============
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

    // =============== Evaluation & scoring ===============
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

                int clears = CountLinesCompletedIfPlaced(state, s, r, c);
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

        // đa dạng nhẹ: phạt nếu đã có shape cùng area trong hand
        int sameArea = 0;
        foreach (var x in currentHand) if (x != null && CountCells(x) == e.cells) sameArea++;
        score -= 0.5f * sameArea;

        // tie-break
        score += (float)_rng.NextDouble() * 0.01f;
        return score;
    }

    private float AdjustToTarget(int n, int lo, int hi)
    {
        if (n <= 0) return 0;
        if (n >= lo && n <= hi) return n;       // thưởng đầy đủ trong vùng mục tiêu
        int d = (n < lo) ? (lo - n) : (n - hi); // phạt theo khoảng cách
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

    private int CountLinesCompletedIfPlaced(BoardState state, ShapeData s, int anchorRow, int anchorCol)
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
        // hàng
        for (int r = 0; r < H; r++)
        {
            bool full = true;
            for (int c = 0; c < W; c++)
                if (!proposed[r * W + c]) { full = false; break; }
            if (full) clears++;
        }
        // cột
        for (int c = 0; c < W; c++)
        {
            bool full = true;
            for (int r = 0; r < H; r++)
                if (!proposed[r * W + c]) { full = false; break; }
            if (full) clears++;
        }
        return clears;
    }

    // =============== Phân lớp shape & chọn class theo weight ===============
    private ShapeClass Classify(ShapeData s)
    {
        if (s == null || s.board == null) return ShapeClass.Small;
        var b = s.GetBounds();
        int h = (b.maxR - b.minR + 1);
        int w = (b.maxC - b.minC + 1);
        int cells = CountCells(s);

        // Line: 1xN hoặc Nx1 và cells == max(h,w)
        if ((h == 1 || w == 1) && cells >= 3) return ShapeClass.Line;

        // Square đầy: 2x2, 3x3 (mọi ô true)
        if ((h == w) && (h == 2 || h == 3) && cells == h * w) return ShapeClass.Square;

        if (cells <= 3) return ShapeClass.Small;
        if (cells <= 5) return ShapeClass.Medium;
        return ShapeClass.Large;
    }

    private ShapeClass RollClass(ClassWeights w)
    {
        // đã Normalize
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
