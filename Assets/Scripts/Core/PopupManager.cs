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

    // ===== Board Clear Bonus =====
    [Header("Board Clear Bonus")]
    public int boardClearBonus = 240;
    public string unbelievableText = "Unbelievable!";
    public Vector2 unbelievableOffset = new Vector2(0f, 96f);

    public void ShowComboAtScreenPoint(int combo, Vector2 screenPoint, Camera cam, Vector2 offset)
    {
        if (combo < minComboToShow || root == null || comboPopupPrefab == null) return;

        // TẠM THỜI BỎ QUA clearComboSfxByTier -> không phát SFX combo theo tier

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
        // (Không phát SFX ở điểm vì yêu cầu chỉ “sau khi đạt điều kiện” praise/board clear)
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

    // ===== Praise (theo số line) =====
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

        // AUDIO: phát SFX praise theo số line
        AudioManager.Instance?.PlayPraiseForLines(linesCleared);
    }

    // ===== Board Clear =====
    public void ShowUnbelievable(Vector2 screenPoint, Camera cam)
    {
        if (root == null || praisePopupPrefab == null) return;

        var p = Instantiate(praisePopupPrefab);
        p.ShowAtScreenPoint(unbelievableText, screenPoint, cam, root, unbelievableOffset);

        //// AUDIO: phát SFX unbelievable
        //AudioManager.Instance?.PlayUnbelievable();
    }

    public void ShowBoardClearBonus(Vector2 screenPoint, Camera cam, Vector2 pointsOffset, float? delayOverride = null)
    {
        // Hiện chữ + SFX trước
        ShowUnbelievable(screenPoint, cam);

        // Sau đó hiện điểm thưởng (có thể delay nhẹ nếu muốn)
        ShowPointsAtScreenPoint(boardClearBonus, screenPoint, cam, pointsOffset, delayOverride);
    }
}
