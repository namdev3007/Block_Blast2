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
    public ShapeSpawnConfig config;

    [Header("Anti-repeat")]
    public int historyKeep = 6;

    [Header("Runtime Options")]
    public bool autoRefillOnStart = false;

    public GameScore score;

    private readonly List<ShapeData> _current = new();
    private readonly Queue<ShapeData> _history = new();
    private readonly HashSet<ShapeData> _historySet = new();
    private readonly List<int> _slotVariants = new();

    private System.Random _rng;
    private int _refillCount;

    public event System.Action SuddenGameOver;

    private class SuddenState
    {
        public bool active;
        public HashSet<ShapeData> remaining = new HashSet<ShapeData>();
        public bool requireClearEachStep;
    }
    private SuddenState _sudden = null;
    private int _refillsSinceSudden = 999;
    private int _nextSuddenAtMove = -1;

    public bool IsSuddenActive => _sudden != null && _sudden.active;

    private float RngValue() => (float)_rng.NextDouble();
    private int RngRange(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    private void Awake()
    {
        _rng = new System.Random();
        if (config == null) config = ScriptableObject.CreateInstance<ShapeSpawnConfig>();
    }

    private void Start()
    {
        if (autoRefillOnStart) Refill();
    }

    public void Refill()
    {
        if (library == null || slots == null || slots.Count == 0) return;

        List<ShapeData> hand = null;

        float r = RngValue();
        if (r < config.highLine6Chance) hand = TryBuildHighLineHandOrBag(slots.Count, 6);
        else if (r < config.highLine6Chance + config.highLine5Chance) hand = TryBuildHighLineHandOrBag(slots.Count, 5);

        if (hand == null)
        {
            bool shouldSudden = ShouldTriggerSuddenDeath();

            if (shouldSudden && slots.Count >= 3 && board != null && board.State != null)
            {
                hand = BuildSuddenDeathBag(slots.Count, out var triplet);
                if (hand != null && triplet != null)
                {
                    _sudden = new SuddenState
                    {
                        active = true,
                        requireClearEachStep = config.suddenRequireLineClearEachStep
                    };
                    foreach (var s in triplet) _sudden.remaining.Add(s);

                    _refillsSinceSudden = 0;
                    ScheduleNextSudden();
                }
            }
        }

        if (hand == null)
        {
            if (config.useTripletBag && slots.Count >= 3 && board != null && board.State != null)
            {
                hand = BuildTripletBagForCurrentBoard(slots.Count);
            }
            else
            {
                hand = BuildHandSmart(slots.Count);

                if (!config.useTripletBag && config.enableHoleFiller &&
                    (RngValue() < config.holeFillerChance) &&
                    board != null && board.State != null)
                {
                    var holeFits = CollectHoleFillerShapes(board.State, config.holeFillerMaxCells, config.holeFitterSampleShapes);
                    if (holeFits.Count > 0)
                    {
                        int slotPick = RngRange(0, Mathf.Min(slots.Count, hand.Count));
                        hand[slotPick] = holeFits[RngRange(0, holeFits.Count)];
                    }
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
        _refillsSinceSudden++;
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

        var consumed = _current[slotIndex];
        _history.Enqueue(consumed);
        _historySet.Add(consumed);
        while (_history.Count > historyKeep)
        {
            var removed = _history.Dequeue();
            _historySet.Remove(removed);
        }

        if (IsSuddenActive) _sudden.remaining.Remove(consumed);

        _current[slotIndex] = null;
        if (slotIndex < _slotVariants.Count) _slotVariants[slotIndex] = 0;

        if (slots[slotIndex] != null) slots[slotIndex].Clear();

        if (AllConsumed()) Refill();
    }

    public bool ValidateSuddenStateAfterPlacement(BoardState currentBoard)
    {
        if (!IsSuddenActive) return true;

        if (_sudden.remaining.Count == 0)
        {
            _sudden.active = false;
            return true;
        }

        var snap = new BoardSnapshot(currentBoard);
        var rest = new List<ShapeData>(_sudden.remaining).ToArray();
        bool ok = _sudden.requireClearEachStep
                  ? ExistsSequentialPlacementEachStepClears(snap, rest)
                  : ExistsSequentialPlacement(snap, rest, requireAnyLineClear: false);
        return ok;
    }

    public void ForceSuddenGameOver(string reason = "Wrong move in Sudden-Death!")
    {
        Debug.LogWarning($"GAME OVER: {reason}");
        SuddenGameOver?.Invoke();
        Time.timeScale = 0f;
    }

    private bool AllConsumed()
    {
        for (int i = 0; i < _current.Count; i++)
            if (_current[i] != null) return false;
        return true;
    }

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
                bool forceLine = RngValue() < config.forceLineClearChance;
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

    private List<ShapeData> BuildTripletBagForCurrentBoard(int needed)
    {
        var state = board.State;
        var weights = ComputeWeightsWithDDA();
        var candidates = new List<(ShapeData a, ShapeData b, ShapeData c)>();
        int trials = 0;

        List<ShapeData> holeFits = null;
        bool tryHoleFiller = config.enableHoleFiller && config.holeFillerAffectsBag &&
                             (RngValue() < config.holeFillerChance);
        if (tryHoleFiller)
        {
            holeFits = CollectHoleFillerShapes(state, config.holeFillerMaxCells, config.holeFitterSampleShapes);
            if (holeFits != null && holeFits.Count == 0) holeFits = null;
        }

        while (candidates.Count < config.bagCandidateCount && trials < config.bagMaxBuildTrials)
        {
            trials++;

            ShapeData s1, s2, s3;
            if (holeFits != null && holeFits.Count > 0 && RngRange(0, 3) == 0)
            {
                s1 = holeFits[RngRange(0, holeFits.Count)];
                s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, false);
                s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, false);
            }
            else if (holeFits != null && holeFits.Count > 0 && RngRange(0, 2) == 0)
            {
                s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, false);
                s2 = holeFits[RngRange(0, holeFits.Count)];
                s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, false);
            }
            else if (holeFits != null && holeFits.Count > 0)
            {
                s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, false);
                s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, false);
                s3 = holeFits[RngRange(0, holeFits.Count)];
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

            if (!ValidateBagOnSnapshot(state, s1, s2, s3)) continue;

            candidates.Add((s1, s2, s3));
        }

        if (candidates.Count == 0) return BuildHandSmart(needed);

        var pick = candidates[RngRange(0, candidates.Count)];
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
            if (!ExistsSequentialPlacement(snap, arr, requireAnyLineClear: true))
                return false;
        }
        return true;
    }

    private List<ShapeData> TryBuildHighLineHandOrBag(int needed, int minLines)
    {
        if (board == null || board.State == null) return null;

        if (config.useTripletBag && needed >= 3)
        {
            var bag = BuildTripletBagForCurrentBoard(needed);
            if (BagHasAnyMinLines(bag, minLines, board.State)) return bag;
        }

        for (int attempt = 0; attempt < config.maxHandBuildAttempts; attempt++)
        {
            var hand = BuildHandSmart(needed);
            if (BagHasAnyMinLines(hand, minLines, board.State)) return hand;
        }
        return null;
    }

    private bool BagHasAnyMinLines(List<ShapeData> hand, int minLines, BoardState state)
    {
        if (hand == null || state == null) return false;
        var snap = new BoardSnapshot(state);
        foreach (var s in hand)
            if (ExistsPlacementWithMinLines(snap, s, minLines)) return true;
        return false;
    }

    private bool ExistsPlacementWithMinLines(BoardSnapshot snap, ShapeData s, int minLines)
    {
        foreach (var pos in snap.AllAnchorsFor(s))
        {
            int clears = snap.CountLinesCompletedIfPlaced(s, pos.r, pos.c);
            if (clears >= minLines) return true;
        }
        return false;
    }

    private sealed class BoardSnapshot
    {
        public readonly int W, H;
        private readonly bool[] occ;
        private readonly int[] rowCnt;
        private readonly int[] colCnt;

        public BoardSnapshot(BoardState st)
        {
            W = st.Width; H = st.Height;
            occ = new bool[W * H];
            rowCnt = new int[H];
            colCnt = new int[W];

            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                {
                    bool o = st.IsOccupied(r, c);
                    occ[r * W + c] = o;
                    if (o) { rowCnt[r]++; colCnt[c]++; }
                }
        }

        private BoardSnapshot(int W, int H, bool[] occ, int[] rowCnt, int[] colCnt)
        {
            this.W = W; this.H = H;
            this.occ = occ;
            this.rowCnt = rowCnt;
            this.colCnt = colCnt;
        }

        public BoardSnapshot Clone()
        {
            var occ2 = new bool[occ.Length];
            Array.Copy(occ, occ2, occ.Length);
            var r2 = new int[rowCnt.Length];
            var c2 = new int[colCnt.Length];
            Array.Copy(rowCnt, r2, rowCnt.Length);
            Array.Copy(colCnt, c2, colCnt.Length);
            return new BoardSnapshot(W, H, occ2, r2, c2);
        }

        public bool IsInside(int r, int c) => (r >= 0 && r < H && c >= 0 && c < W);

        public bool CanPlace(ShapeData s, int anchorR, int anchorC)
        {
            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x, c = anchorC + cell.y;
                if (!IsInside(r, c) || occ[r * W + c]) return false;
            }
            return true;
        }

        public void Place(ShapeData s, int anchorR, int anchorC)
        {
            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x, c = anchorC + cell.y, idx = r * W + c;
                if (!occ[idx])
                {
                    occ[idx] = true;
                    rowCnt[r]++; colCnt[c]++;
                }
            }
        }

        public IEnumerable<(int r, int c)> AllAnchorsFor(ShapeData s)
        {
            var b = s.GetBounds();
            if (b.maxR < b.minR) yield break;

            int sh = b.maxR - b.minR + 1;
            int sw = b.maxC - b.minC + 1;

            for (int r = -b.minR; r <= H - sh; r++)
                for (int c = -b.minC; c <= W - sw; c++)
                    if (CanPlace(s, r, c)) yield return (r, c);
        }

        public int CountLinesCompletedIfPlaced(ShapeData s, int anchorR, int anchorC)
        {
            var next = Clone();
            next.Place(s, anchorR, anchorC);
            int clears = 0;
            for (int r = 0; r < H; r++) if (next.rowCnt[r] == W) clears++;
            for (int c = 0; c < W; c++) if (next.colCnt[c] == H) clears++;
            return clears;
        }

        public int CountNearFullLinesIfPlaced(ShapeData s, int anchorR, int anchorC, int missing = 1)
        {
            var rCnt = new int[rowCnt.Length];
            var cCnt = new int[colCnt.Length];
            Array.Copy(rowCnt, rCnt, rCnt.Length);
            Array.Copy(colCnt, cCnt, cCnt.Length);

            foreach (var cell in s.GetFilledCells())
            {
                int r = anchorR + cell.x, c = anchorC + cell.y;
                if (!IsInside(r, c)) continue;
                int idx = r * W + c;
                if (!occ[idx]) { rCnt[r]++; cCnt[c]++; }
            }

            int near = 0;
            for (int r = 0; r < H; r++) if (W - rCnt[r] == missing) near++;
            for (int c = 0; c < W; c++) if (H - cCnt[c] == missing) near++;
            return near;
        }
    }

    private bool ExistsSequentialPlacement(BoardSnapshot start, ShapeData[] shapes, bool requireAnyLineClear = false)
    {
        int[][] perms = {
            new[] {0,1,2}, new[] {0,2,1},
            new[] {1,0,2}, new[] {1,2,0},
            new[] {2,0,1}, new[] {2,1,0}
        };

        foreach (var p in perms)
            if (SearchChain(start, shapes, p, 0, requireAnyLineClear, anyClearSeen: false))
                return true;
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

    private bool ExistsSequentialPlacementEachStepClears(BoardSnapshot start, ShapeData[] shapes)
    {
        int[][] perms = {
            new[]{0,1,2}, new[]{0,2,1},
            new[]{1,0,2}, new[]{1,2,0},
            new[]{2,0,1}, new[]{2,1,0}
        };
        foreach (var p in perms)
            if (SearchChainRequireClearEach(start, shapes, p, 0)) return true;
        return false;
    }

    private bool SearchChainRequireClearEach(BoardSnapshot snap, ShapeData[] shapes, int[] order, int depth)
    {
        if (depth == order.Length) return true;

        var s = shapes[order[depth]];
        foreach (var pos in snap.AllAnchorsFor(s))
        {
            int clears = snap.CountLinesCompletedIfPlaced(s, pos.r, pos.c);
            if (clears <= 0) continue;

            var next = snap.Clone();
            next.Place(s, pos.r, pos.c);
            if (SearchChainRequireClearEach(next, shapes, order, depth + 1)) return true;
        }
        return false;
    }

    private bool ShouldTriggerSuddenDeath()
    {
        if (!config.suddenDeathEnabled) return false;
        if (board == null || board.State == null) return false;
        if (slots == null || slots.Count < 3) return false;
        if (_refillsSinceSudden < config.suddenCooldownRefills) return false;

        int moves = (score != null) ? score.MoveCount : _refillCount * 3;
        if (_nextSuddenAtMove < 0) ScheduleNextSudden();
        if (moves < _nextSuddenAtMove) return false;

        return RngValue() < config.suddenTriggerChance;
    }

    private void ScheduleNextSudden()
    {
        int moves = (score != null) ? score.MoveCount : _refillCount * 3;
        int extra = (config.suddenMaxExtraMoves <= 0) ? 0 : RngRange(0, config.suddenMaxExtraMoves + 1);
        _nextSuddenAtMove = moves + Mathf.Max(0, config.suddenMinMoves) + extra;
    }

    private List<ShapeData> BuildSuddenDeathBag(int needed, out ShapeData[] tripletOut)
    {
        tripletOut = null;
        var state = board.State;
        var weights = ComputeWeightsWithDDA();
        int trials = 0;

        while (trials++ < Mathf.Max(200, config.bagMaxBuildTrials / 2))
        {
            var s1 = PickOneShapeSmart(state, new List<ShapeData>(), weights, false, true);
            var s2 = PickOneShapeSmart(state, new List<ShapeData> { s1 }, weights, false, true);
            var s3 = PickOneShapeSmart(state, new List<ShapeData> { s1, s2 }, weights, false, true);

            if (config.bagAvoidDuplicateShapes &&
               (ReferenceEquals(s1, s2) || ReferenceEquals(s1, s3) || ReferenceEquals(s2, s3)))
                continue;

            var snap = new BoardSnapshot(state);
            var arr = new[] { s1, s2, s3 };

            bool seqOK = ExistsSequentialPlacement(snap, arr, requireAnyLineClear: false);
            if (!seqOK) continue;

            if (config.suddenRequireLineClearEachStep &&
                !ExistsSequentialPlacementEachStepClears(snap, arr))
                continue;

            var hand = new List<ShapeData>(needed);
            hand.Add(s1);
            if (needed > 1) hand.Add(s2);
            if (needed > 2) hand.Add(s3);
            while (hand.Count < needed) hand.Add(library.GetRandom());

            tripletOut = arr;
            return hand;
        }
        return null;
    }

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

        var top = new List<(ShapeData s, float score)>();

        for (int t = 0; t < Mathf.Max(1, config.samplesPerSlot); t++)
        {
            var cand = library.GetRandom();
            if (cand == null) continue;
            if (currentHand.Contains(cand)) continue;
            if (_historySet.Contains(cand)) continue;

            if (Classify(cand) != targetClass)
                if (RngValue() > 0.15f) continue;

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
        public int bestChainPotential;
        public bool anyPlaceable => placements > 0;
    }

    private Eval EvaluateShape(ShapeData s, BoardState state)
    {
        var e = new Eval { cells = CountCells(s), placements = 0, maxLineClears = 0, bestChainPotential = 0 };
        if (state == null) return e;

        var b = s.GetBounds();
        if (b.maxR < b.minR) return e;

        int W = state.Width, H = state.Height;
        var snap = new BoardSnapshot(state);

        for (int r = -b.minR; r <= H - (b.maxR - b.minR + 1); r++)
        {
            for (int c = -b.minC; c <= W - (b.maxC - b.minC + 1); c++)
            {
                if (!snap.CanPlace(s, r, c)) continue;
                e.placements++;

                int clears = snap.CountLinesCompletedIfPlaced(s, r, c);
                if (clears > e.maxLineClears) e.maxLineClears = clears;

                int near = snap.CountNearFullLinesIfPlaced(s, r, c, missing: 1);
                if (near > e.bestChainPotential) e.bestChainPotential = near;
            }
        }
        return e;
    }
    private float SquashWithGamma(int value, int minTarget, int maxTarget, float gamma)
    {
        int clamped = Mathf.Clamp(value, minTarget, maxTarget);
        float t = (maxTarget > minTarget) ? (clamped - minTarget) / (float)(maxTarget - minTarget) : 1f;
        return Mathf.Pow(Mathf.Clamp01(t), gamma);
    }

    private float GetBoardFree01()
    {
        if (board == null || board.State == null) return 1f;
        int H = board.State.Height, W = board.State.Width;
        int occ = 0;
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (board.State.IsOccupied(r, c)) occ++;
        return 1f - (float)occ / (W * H);
    }
    private float ScoreShape(Eval e, ShapeData s, List<ShapeData> currentHand)
    {
        float score = 0f;

        if (e.anyPlaceable)
            score += config.wPlaceable;

        float placementFactor = SquashWithGamma(
            e.placements,
            config.minPlacementsTarget,
            config.maxPlacementsTarget,
            config.placementCountGamma
        );
        score += config.wPlacementCount * placementFactor;

        score += config.wLineClear * e.maxLineClears;
        score += config.wArea * e.cells;
        score += config.wChainPotential * e.bestChainPotential;

        float boardFree01 = GetBoardFree01();
        if (boardFree01 >= config.tinyPenaltyFreeBoardMin && e.cells <= config.tinyCellThreshold)
            score -= config.tinyPenalty;

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

    public bool HasAnyMove(BoardState state)
    {
        if (state == null || slots == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            var s = Peek(i);
            if (s == null || s.board == null) continue;

            var b = s.GetBounds();
            if (b.maxR < b.minR) continue;

            int sh = b.maxR - b.minR + 1;
            int sw = b.maxC - b.minC + 1;

            for (int r = -b.minR; r <= state.Height - sh; r++)
                for (int c = -b.minC; c <= state.Width - sw; c++)
                    if (state.CanPlace(s, r, c)) return true;
        }
        return false;
    }

    public void RestoreFromSave(GameSaveV1 s)
    {
        if (library == null || slots == null || slots.Count == 0) return;

        int n = Mathf.Min(slots.Count, s.paletteSize);

        while (_current.Count < slots.Count) _current.Add(null);
        while (_slotVariants.Count < slots.Count) _slotVariants.Add(0);

        for (int i = 0; i < n; i++)
        {
            if (s.shapeIds != null && i < s.shapeIds.Length && s.shapeIds[i] >= 0)
            {
                var data = library.GetById(s.shapeIds[i]);
                int variant = (s.shapeVariants != null && i < s.shapeVariants.Length) ? s.shapeVariants[i] : 0;
                SetSlot(i, data, variant);
            }
            else
            {
                ClearSlot(i);
            }
        }

        for (int i = n; i < slots.Count; i++) ClearSlot(i);
    }

    public void SetSlot(int i, ShapeData data, int variant)
    {
        if (i < 0 || i >= slots.Count) return;

        _current[i] = data;
        if (i >= _slotVariants.Count) _slotVariants.Add(variant);
        else _slotVariants[i] = variant;

        Sprite displaySprite = (skinProvider != null)
            ? skinProvider.GetTileSprite(variant)
            : (board != null ? board.placedSpriteFallback : null);

        if (slots[i] != null)
        {
            if (data != null) slots[i].Render(data, displaySprite);
            else slots[i].Clear();

            var cg = slots[i].GetComponentInParent<CanvasGroup>();
            if (cg) cg.alpha = (data != null) ? 1f : 0.3f;
        }
    }

    public void ClearSlot(int i)
    {
        if (i < 0 || i >= slots.Count) return;
        _current[i] = null;
        if (i < _slotVariants.Count) _slotVariants[i] = 0;

        if (slots[i] != null)
        {
            slots[i].Clear();
            var cg = slots[i].GetComponentInParent<CanvasGroup>();
            if (cg) cg.alpha = 0.3f;
        }
    }
}
