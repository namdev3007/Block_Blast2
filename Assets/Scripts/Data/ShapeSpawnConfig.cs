using UnityEngine;

[System.Serializable]
public struct ClassWeights
{
    [Range(0f, 5f)] public float small;
    [Range(0f, 5f)] public float medium;
    [Range(0f, 5f)] public float large;
    [Range(0f, 5f)] public float line;
    [Range(0f, 5f)] public float square;

    public void Normalize()
    {
        float sum = small + medium + large + line + square;
        if (sum <= 0f)
        {
            small = 1f; medium = 1f; large = 0.8f; line = 0.4f; square = 0.2f;
            sum = small + medium + large + line + square;
        }
        small /= sum; medium /= sum; large /= sum; line /= sum; square /= sum;
    }
}

[CreateAssetMenu(fileName = "ShapeSpawnConfig", menuName = "Config/Shape Spawn Config")]
public class ShapeSpawnConfig : ScriptableObject
{
    // ---------- Hand build / Sampling ----------
    [Header("Hand build / Sampling")]
    [Min(1)] public int maxHandBuildAttempts = 25;
    [Min(1)] public int samplesPerSlot = 30;
    [Min(1)] public int topK = 4;

    // ---------- Solvable Guard & Pity ----------
    [Header("Solvable Guard & Pity")]
    [Tooltip("Yêu cầu tối thiểu X slot có thể đặt ngay trong hand.")]
    [Min(0)] public int requiredPlaceableSlots = 1;

    [Tooltip("Bật rescue khi build hand thất bại nhiều lần.")]
    public bool enablePity = true;

    [Tooltip("Số slot đầu tiên cố lấy khối nhỏ & có thể đặt.")]
    [Min(0)] public int pityRescueSlots = 1;

    // ---------- Bias: Line Chaser ----------
    [Header("Bias: Line Chaser")]
    [Tooltip("Xác suất ép slot ưu tiên khối có khả năng clear line.")]
    [Range(0f, 1f)] public float forceLineClearChance = 0.2f;

    // ---------- DDA Base Weights (CỐ ĐỊNH) ----------
    [Header("DDA Base Weights (cố định, không ramp)")]
    public ClassWeights baseWeights = new ClassWeights
    {
        small = 0.25f,
        medium = 0.50f,
        large = 0.20f,
        line = 0.10f,
        square = 0.00f
    };

    // ---------- DDA: Tight Board ----------
    [Header("DDA: Điều chỉnh khi bàn CHẬT")]
    [Range(0f, 1f)] public float tightBoardFreeThreshold = 0.35f;
    [Range(0.5f, 2f)] public float tightSmallBoost = 1.15f;
    [Range(0.5f, 2f)] public float tightLineBoost = 1.10f;
    [Range(0.3f, 1.2f)] public float tightLargePenalty = 0.85f;

    // ---------- Scoring weights ----------
    [Header("Scoring (điểm đánh giá ứng viên)")]
    public float wPlaceable = 2f;

    // ↓ Giảm thiên vị khối nhỏ (ít “ăn” điểm vì có quá nhiều vị trí đặt)
    public float wPlacementCount = 0.2f;    // trước: 0.5
    public int minPlacementsTarget = 2;
    public int maxPlacementsTarget = 8;

    public float wLineClear = 3f;

    // ↑ Tăng giá trị diện tích để khối to cạnh tranh hơn
    public float wArea = 0.10f;             // trước: 0.05

    [Tooltip("Ưu tiên giữ chuỗi: số hàng/cột còn thiếu 1 ô sau khi giả lập đặt.")]
    public float wChainPotential = 1.0f;

    // ---------- Scoring: Anti-small bias ----------
    [Header("Scoring: anti-small bias")]
    [Tooltip("Phạt khi ô trống còn nhiều mà shape quá nhỏ.")]
    public float tinyPenalty = 0.8f;

    [Tooltip("Ngưỡng cellcount để xem là 'khối nhỏ' (<=).")]
    [Min(1)] public int tinyCellThreshold = 3;

    [Tooltip("Chỉ phạt khi bàn đang khá thoáng (>=).")]
    [Range(0f, 1f)] public float tinyPenaltyFreeBoardMin = 0.45f;

    // ---------- Diminishing returns cho placementCount ----------
    [Header("PlacementCount diminishing returns")]
    [Tooltip("Hệ số cong cho placementCount (1 = tuyến tính, <1 = giảm dần).")]
    [Range(0.25f, 1f)] public float placementCountGamma = 0.65f;

    // ---------- Triplet Bag ----------
    [Header("Triplet Bag")]
    public bool useTripletBag = true;
    [Min(1)] public int bagCandidateCount = 50;
    [Min(10)] public int bagMaxBuildTrials = 400;
    public bool bagAvoidDuplicateShapes = true;
    public bool bagRequireSequentialPlaceability = true;
    public bool bagRequireAtLeastOneLineClear = false;

    // ---------- Hole Filler ----------
    [Header("Hole Filler")]
    public bool enableHoleFiller = true;
    [Range(0f, 1f)] public float holeFillerChance = 0.25f;
    [Min(1)] public int holeFillerMaxCells = 6;
    [Min(1)] public int holeFitterSampleShapes = 220;
    public bool holeFillerAffectsBag = true;

    // ---------- High-Line Favor (≥5 / ≥6 lines) ----------
    [Header("Ưu tiên 'ăn nhiều hàng' (không bắt buộc)")]
    [Tooltip("Xác suất ưu tiên tạo ra hand/bag có ÍT NHẤT MỘT khối có thể ăn >= 6 lines (nếu đặt đúng).")]
    [Range(0f, 1f)] public float highLine6Chance = 0.03f;
    [Tooltip("Xác suất ưu tiên tạo ra hand/bag có ÍT NHẤT MỘT khối có thể ăn >= 5 lines.")]
    [Range(0f, 1f)] public float highLine5Chance = 0.07f;

    // ---------- Sudden-Death ----------
    [Header("Sudden-Death (sai 1 nước thua)")]
    public bool suddenDeathEnabled = true;
    [Min(0)] public int suddenMinMoves = 10;
    [Min(0)] public int suddenMaxExtraMoves = 10;
    [Range(0f, 1f)] public float suddenTriggerChance = 0.5f;
    public bool suddenRequireLineClearEachStep = false;
    [Min(0)] public int suddenCooldownRefills = 2;

    private void OnValidate()
    {
        maxHandBuildAttempts = Mathf.Max(1, maxHandBuildAttempts);
        samplesPerSlot = Mathf.Max(1, samplesPerSlot);
        topK = Mathf.Max(1, topK);

        requiredPlaceableSlots = Mathf.Max(0, requiredPlaceableSlots);
        pityRescueSlots = Mathf.Max(0, pityRescueSlots);

        bagCandidateCount = Mathf.Max(1, bagCandidateCount);
        bagMaxBuildTrials = Mathf.Max(10, bagMaxBuildTrials);

        holeFillerMaxCells = Mathf.Max(1, holeFillerMaxCells);
        holeFitterSampleShapes = Mathf.Max(1, holeFitterSampleShapes);

        tinyCellThreshold = Mathf.Max(1, tinyCellThreshold);
    }
}

/// <summary>
/// Thống kê đánh giá ứng viên (bạn ánh xạ lại theo hệ thống eval hiện có).
/// </summary>
public struct EvalStats
{
    public bool placeable;                       // có đặt được ít nhất 1 vị trí
    public int placementCount;                   // số vị trí có thể đặt
    public int maxLinesClearedIfBestPlacement;   // số line tối đa nếu đặt tốt nhất
    public int chainPotential;                   // số hàng/cột còn thiếu 1 ô sau giả lập
}

public static class SpawnScoring
{
    // Gamma squash: đưa x về [0..1], rồi lũy thừa gamma (<1) để “giảm dần lợi ích”.
    static float SquashWithGamma(int value, int minTarget, int maxTarget, float gamma)
    {
        int clamped = Mathf.Clamp(value, minTarget, maxTarget);
        float t = (maxTarget > minTarget) ? (clamped - minTarget) / (float)(maxTarget - minTarget) : 1f;
        return Mathf.Pow(Mathf.Clamp01(t), gamma);
    }

    /// <summary>
    /// Hàm chấm điểm ứng viên theo cấu hình hiện tại.
    /// - shapeCellCount: số ô của khối.
    /// - stats: kết quả eval (đặt được, số vị trí, max lines, chainPotential).
    /// - cfg: ShapeSpawnConfig (đã set tham số).
    /// - boardFree01: tỉ lệ ô trống hiện tại (0..1).
    /// </summary>
    public static float ScoreCandidate(
        int shapeCellCount,
        EvalStats stats,
        ShapeSpawnConfig cfg,
        float boardFree01)
    {
        float score = 0f;

        // 1) Có đặt được không → điểm nền cao
        if (stats.placeable)
            score += cfg.wPlaceable;

        // 2) Số vị trí đặt (giảm dần lợi ích để khối nhỏ bớt lợi thế)
        float placementFactor = SquashWithGamma(stats.placementCount,
                                                cfg.minPlacementsTarget,
                                                cfg.maxPlacementsTarget,
                                                cfg.placementCountGamma);
        score += cfg.wPlacementCount * placementFactor;

        // 3) Ăn line tối đa
        score += cfg.wLineClear * stats.maxLinesClearedIfBestPlacement;

        // 4) Diện tích khối (buff khối to)
        // Nếu muốn dịu hơn, có thể dùng sqrt:
        // score += cfg.wArea * Mathf.Sqrt(shapeCellCount);
        score += cfg.wArea * shapeCellCount;

        // 5) Tiềm năng chuỗi
        score += cfg.wChainPotential * stats.chainPotential;

        // 6) PHẠT KHỐI RẤT NHỎ KHI BÀN THOÁNG (anti-small bias)
        if (boardFree01 >= cfg.tinyPenaltyFreeBoardMin && shapeCellCount <= cfg.tinyCellThreshold)
            score -= cfg.tinyPenalty;

        return score;
    }
}
