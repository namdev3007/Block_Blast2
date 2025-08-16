using UnityEngine;

[System.Serializable]
public struct ClassWeights
{
    [Range(0, 1)] public float small;   // 1–3 cells
    [Range(0, 1)] public float medium;  // 4–5 cells
    [Range(0, 1)] public float large;   // >=6 cells
    [Range(0, 1)] public float line;    // 1xN hoặc Nx1 (>=3)
    [Range(0, 1)] public float square;  // 2x2 full, 3x3 full...

    public void Normalize()
    {
        float s = small + medium + large + line + square;
        if (s <= 0f) { small = 1; medium = large = line = square = 0; return; }
        small /= s; medium /= s; large /= s; line /= s; square /= s;
    }
}

[System.Serializable]
public class ShapeSpawnConfig
{
    [Header("Sampling")]
    public int samplesPerSlot = 24;   // số ứng viên thử cho mỗi slot
    public int topK = 5;              // roulette trong top K

    [Header("Solvability")]
    [Tooltip("Số slot tối thiểu phải đặt được trong hand")]
    public int requiredPlaceableSlots = 1;
    public int maxHandBuildAttempts = 6;

    [Header("Weights (base)")]
    public ClassWeights baseWeights = new ClassWeights
    {
        small = 0.25f,
        medium = 0.45f,
        large = 0.20f,
        line = 0.07f,
        square = 0.03f
    };

    [Header("DDA (theo trạng thái bàn)")]
    [Tooltip("Khi freeRatio < ngưỡng -> bàn bí")]
    public float tightBoardFreeThreshold = 0.35f;
    [Tooltip("Booster khi bàn bí")]
    public float tightSmallBoost = 1.35f;
    public float tightLineBoost = 1.35f;
    public float tightLargePenalty = 0.65f;

    [Header("DDA (ramp theo thời gian chơi)")]
    [Tooltip("Sau bao nhiêu lần refill thì đạt khó tối đa ~1.0")]
    public int rampRefillsToMax = 20;
    [Tooltip("Khi ramp tăng -> tăng trọng số large, giảm small")]
    public float rampLargeBoost = 1.6f;
    public float rampSmallPenalty = 0.75f;

    [Header("Line-chaser bias")]
    [Range(0, 1)] public float forceLineClearChance = 0.30f;

    [Header("Scoring (điều chỉnh độ khó)")]
    public int minPlacementsTarget = 4;
    public int maxPlacementsTarget = 14;
    public float wPlaceable = 10f;
    public float wPlacementCount = 0.8f;
    public float wLineClear = 6f;
    public float wArea = -0.15f;

    [Header("Pity / Rescue")]
    public bool enablePity = true;
    public int pityRescueSlots = 1;
}
