using UnityEngine;

[System.Serializable]
public struct ClassWeights
{
    [Range(0f, 5f), Tooltip("Weight for small shapes (1–3 cells).")]
    public float small;
    [Range(0f, 5f), Tooltip("Weight for medium shapes (4–5 cells).")]
    public float medium;
    [Range(0f, 5f), Tooltip("Weight for large shapes (6+ cells).")]
    public float large;
    [Range(0f, 5f), Tooltip("Weight for line shapes (1xN or Nx1).")]
    public float line;
    [Range(0f, 5f), Tooltip("Weight for full squares (2x2, 3x3).")]
    public float square;

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
    [Header("Hand build / Sampling")]
    [Min(1)] public int maxHandBuildAttempts = 25;
    [Min(1)] public int samplesPerSlot = 30;
    [Min(1)] public int topK = 4;

    [Header("Solvable Guard & Pity")]
    [Min(0)] public int requiredPlaceableSlots = 1;
    public bool enablePity = true;
    [Min(0)] public int pityRescueSlots = 1;

    [Header("Bias: Line Chaser")]
    [Range(0f, 1f)] public float forceLineClearChance = 0.2f;

    [Header("DDA Base Weights (before adjustments)")]
    public ClassWeights baseWeights = new ClassWeights
    {
        small = 0.25f,
        medium = 0.5f,
        large = 0.2f,
        line = 0.1f,
        square = 0.0f
    };

    [Header("DDA: Tight Board Adjustments")]
    [Range(0f, 1f)] public float tightBoardFreeThreshold = 0.35f;
    [Range(0.5f, 2f)] public float tightSmallBoost = 1.15f;
    [Range(0.5f, 2f)] public float tightLineBoost = 1.10f;
    [Range(0.3f, 1.2f)] public float tightLargePenalty = 0.85f;

    [Header("DDA: Ramp over playtime (refill count)")]
    [Min(1)] public int rampRefillsToMax = 30;
    [Range(0.3f, 1f)] public float rampSmallPenalty = 0.7f;
    [Range(1f, 2f)] public float rampLargeBoost = 1.3f;

    [Header("Scoring (shape candidate scoring)")]
    public float wPlaceable = 2f;
    public float wPlacementCount = 0.5f;
    public int minPlacementsTarget = 2;
    public int maxPlacementsTarget = 8;
    public float wLineClear = 3f;
    public float wArea = 0.05f;

    [Header("Optional: Ramp Curves (alternative to linear ramp)")]
    public AnimationCurve smallMulCurve = AnimationCurve.Linear(0, 1, 1, 0.7f);
    public AnimationCurve largeMulCurve = AnimationCurve.Linear(0, 1, 1, 1.3f);

    private void OnValidate()
    {
        minPlacementsTarget = Mathf.Max(0, minPlacementsTarget);
        maxPlacementsTarget = Mathf.Max(minPlacementsTarget, maxPlacementsTarget);
    }
}
