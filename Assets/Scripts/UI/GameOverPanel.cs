using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class GameOverPanel : MonoBehaviour
{
    public TextMeshProUGUI lastScoreText;
    public TextMeshProUGUI highScoreText;

    public Button btnRestart;

    private void Awake()
    {
        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
    }

    private void OnEnable()
    {
        RefreshScores();
    }

    public void RefreshScores()
    {
        var gm = GameManager.Instance;
        var score = (gm != null) ? gm.score : null;

        int last = (score != null) ? score.TotalScore : 0;
        int high = (score != null) ? score.HighScore : 0;

        AnimateScore(lastScoreText, last, 1f);
        SetText(highScoreText, Format(high));
    }

    private void AnimateScore(TextMeshProUGUI text, int targetValue, float duration)
    {
        if (text == null) return;
        text.text = "0";

        AudioManager.Instance?.PlayIncrease();

        DOTween.To(() => 0, x =>
        {
            text.text = x.ToString("N0");
        }, targetValue, duration)
        .SetEase(Ease.OutCubic);
    }

    private void OnClickRestart()
    {
        AudioManager.Instance?.PlayClick();
        GameManager.Instance?.Restart();
    }

    private static void SetText(TextMeshProUGUI t, string s)
    {
        if (t) t.text = s;
    }

    private static string Format(int value) => value.ToString("N0");
}
