using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI lastScoreText;
    public TextMeshProUGUI highScoreText;


    [Header("Buttons (optional)")]
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

        SetText(lastScoreText, Format(last));
        SetText(highScoreText, Format(high));

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
