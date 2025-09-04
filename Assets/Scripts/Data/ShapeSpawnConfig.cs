using UnityEngine;

[System.Serializable]
public struct ClassWeights
{
    [Range(0f, 5f)] public float small;   // 1–3 cells
    [Range(0f, 5f)] public float medium;  // 4–5 cells
    [Range(0f, 5f)] public float large;   // 6+ cells
    [Range(0f, 5f)] public float line;    // 1xN or Nx1
    [Range(0f, 5f)] public float square;  // full 2x2, 3x3

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
    [Min(0)] public int requiredPlaceableSlots = 1;
    public bool enablePity = true;
    [Min(0)] public int pityRescueSlots = 1;

    // ---------- Bias: Line Chaser ----------
    [Header("Bias: Line Chaser")]
    [Range(0f, 1f)] public float forceLineClearChance = 0.2f;

    // ---------- DDA Base Weights ----------
    [Header("DDA Base Weights (before adjustments)")]
    public ClassWeights baseWeights = new ClassWeights
    {
        small = 0.25f,
        medium = 0.5f,
        large = 0.2f,
        line = 0.1f,
        square = 0.0f
    };

    // ---------- DDA Tight Board ----------
    [Header("DDA: Tight Board Adjustments")]
    [Range(0f, 1f)] public float tightBoardFreeThreshold = 0.35f;
    [Range(0.5f, 2f)] public float tightSmallBoost = 1.15f;
    [Range(0.5f, 2f)] public float tightLineBoost = 1.10f;
    [Range(0.3f, 1.2f)] public float tightLargePenalty = 0.85f;

    // ---------- DDA Ramp ----------
    [Header("DDA: Ramp over playtime (refill count)")]
    [Min(1)] public int rampRefillsToMax = 30;
    [Range(0.3f, 1f)] public float rampSmallPenalty = 0.7f;
    [Range(1f, 2f)] public float rampLargeBoost = 1.3f;

    // ---------- Scoring ----------
    [Header("Scoring (shape candidate scoring)")]
    public float wPlaceable = 2f;
    public float wPlacementCount = 0.5f;
    public int minPlacementsTarget = 2;
    public int maxPlacementsTarget = 8;
    public float wLineClear = 3f;
    public float wArea = 0.05f;

    // ---------- Optional Curves ----------
    [Header("Optional: Ramp Curves (alternative to linear ramp)")]
    public AnimationCurve smallMulCurve = AnimationCurve.Linear(0, 1, 1, 0.7f);
    public AnimationCurve largeMulCurve = AnimationCurve.Linear(0, 1, 1, 1.3f);

    // ---------- NEW: Triplet Bag ----------
    [Header("Triplet Bag")]
    [Tooltip("Use Triplet Bag (build ~N candidate bags, pick one randomly).")]
    public bool useTripletBag = true;

    [Tooltip("How many valid candidate bags to generate each refill (e.g. 50).")]
    [Min(1)] public int bagCandidateCount = 50;

    [Tooltip("Cap how many attempts when searching bag candidates (safety).")]
    [Min(10)] public int bagMaxBuildTrials = 400;

    [Tooltip("Avoid having duplicated shapes inside the same bag.")]
    public bool bagAvoidDuplicateShapes = true;

    [Tooltip("Require that the 3 shapes can be placed sequentially on a snapshot grid.")]
    public bool bagRequireSequentialPlaceability = true;

    [Tooltip("Additionally require that at least one of the 3 causes a line clear (optional).")]
    public bool bagRequireAtLeastOneLineClear = false;

    // ---------- NEW: Hole Filler ----------
    [Header("Hole Filler")]
    [Tooltip("Occasionally spawn a piece that perfectly fits a small empty cavity.")]
    public bool enableHoleFiller = true;

    [Tooltip("Chance per refill to inject ONE hole-filler into hand/bag.")]
    [Range(0f, 1f)] public float holeFillerChance = 0.25f;

    [Tooltip("Max cavity size (cells) to consider as a hole.")]
    [Min(1)] public int holeFillerMaxCells = 6;

    [Tooltip("How many random shapes to sample when searching hole-fits.")]
    [Min(1)] public int holeFitterSampleShapes = 220;

    [Tooltip("When using Bag mode, allow hole-filler to be injected into a bag.")]
    public bool holeFillerAffectsBag = true;

    private void OnValidate()
    {
        minPlacementsTarget = Mathf.Max(0, minPlacementsTarget);
        maxPlacementsTarget = Mathf.Max(minPlacementsTarget, maxPlacementsTarget);
        bagCandidateCount = Mathf.Max(1, bagCandidateCount);
    }
}
