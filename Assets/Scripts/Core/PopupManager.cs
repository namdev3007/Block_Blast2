using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [Header("Root (Canvas)")]
    public RectTransform root;

    [Header("Combo Popup")]
    public ComboPopup comboPopupPrefab;
    [Min(2)] public int minComboToShow = 2;

    [Header("Points Popup")]
    public PointsPopup pointsPopupPrefab;
    [Min(1)] public int minPointsToShow = 9;
    public float defaultPointsDelay = 0.05f;

    [Header("Fly-To-Score")]
    public bool enableFlyToScore = true;
    public RectTransform totalScoreAnchor;
    public Vector2 totalScoreOffset = Vector2.zero;

    [Header("Praise Popup")]
    public PraisePopup praisePopupPrefab;
    public Vector2 praiseOffset = new Vector2(0f, 64f);
    [Tooltip("Chỉ hiện khi ăn >= 2 lines.")]
    public int minLinesForPraise = 2;

    public void ShowComboAtScreenPoint(int combo, Vector2 screenPoint, Camera cam, Vector2 offset)
    {
        if (combo < minComboToShow || root == null || comboPopupPrefab == null) return;

        var p = Instantiate(comboPopupPrefab);
        string rich = $"<color=#FFFFFF>Combo</color> <color=#FFC107>{combo}</color>";
        p.ShowAtScreenPoint(rich, screenPoint, cam, root, offset);
    }

    public void ShowPointsAtScreenPoint(int points, Vector2 screenPoint, Camera cam, Vector2 offset, float? delayOverride = null)
    {
        if (points < minPointsToShow || root == null || pointsPopupPrefab == null) return;

        var p = Instantiate(pointsPopupPrefab);
        var flyTarget = (enableFlyToScore ? totalScoreAnchor : null);

        p.ShowAtScreenPoint(
            points, screenPoint, cam, root, offset,
            delayOverride ?? defaultPointsDelay,
            flyTarget, totalScoreOffset
        );
    }

    public void ShowComboThenPoints(
        int combo, int points,
        Vector2 screenPoint, Camera cam,
        Vector2 comboOffset, Vector2 pointsOffset,
        float? pointsDelayOverride = null)
    {
        ShowComboAtScreenPoint(combo, screenPoint, cam, comboOffset);
        ShowPointsAtScreenPoint(points, screenPoint, cam, pointsOffset, pointsDelayOverride);
    }

    // ===== NEW: Praise =====
    public void ShowPraiseForLines(int linesCleared, Vector2 screenPoint, Camera cam)
    {
        if (root == null || praisePopupPrefab == null) return;
        if (linesCleared < minLinesForPraise) return;

        string msg = linesCleared switch
        {
            2 => "Good!",
            3 => "Great!",
            4 => "Excellent!",
            5 => "Fantastic!",
            _ => "Legendary!" // 6+
        };

        var p = Instantiate(praisePopupPrefab);
        p.ShowAtScreenPoint(msg, screenPoint, cam, root, praiseOffset);
    }
}
