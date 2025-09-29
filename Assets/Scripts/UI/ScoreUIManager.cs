using System.Collections;
using UnityEngine;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{
    [Header("Refs")]
    public GameScore gameScore;

    [Header("UI (TextMeshPro)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Animation")]
    [Tooltip("Thời gian tween đếm số điểm UI.")]
    [Range(0.05f, 1.0f)] public float scoreTweenTime = 0.25f;
    [Tooltip("Scale khi pulse high score.")]
    [Range(1f, 2.5f)] public float highPulseScale = 1.15f;
    [Range(0.05f, 0.35f)] public float highPulseTime = 0.12f;

    private int _displayedScore;
    private Coroutine _scoreTweenCo;
    private Coroutine _pulseCo;

    private void OnEnable()
    {
        if (gameScore != null)
        {
            gameScore.Scored += OnScored;
            gameScore.HighScoreChanged += OnHighScoreChanged;
        }
    }

    private void OnDisable()
    {
        if (gameScore != null)
        {
            gameScore.Scored -= OnScored;
            gameScore.HighScoreChanged -= OnHighScoreChanged;
        }
    }

    private void Start()
    {
        _displayedScore = gameScore != null ? gameScore.TotalScore : 0;
        SetText(scoreText, Format(_displayedScore));
        SetText(highScoreText, Format(gameScore != null ? gameScore.HighScore : 0));
    }

    private void OnScored(ScoreResult r)
    {
        if (gameScore == null) return;
        int target = gameScore.TotalScore;

        if (_scoreTweenCo != null) StopCoroutine(_scoreTweenCo);
        _scoreTweenCo = StartCoroutine(AnimateScore(_displayedScore, target, scoreTweenTime));
    }

    private void OnHighScoreChanged(int newHigh)
    {
        SetText(highScoreText, Format(newHigh));
        Pulse(highScoreText != null ? highScoreText.rectTransform : null);
    }

    // === Tween số điểm hiển thị ===
    private IEnumerator AnimateScore(int from, int to, float duration)
    {
        _displayedScore = from;

        if (duration <= 0f)
        {
            _displayedScore = to;
            SetText(scoreText, Format(_displayedScore));
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            _displayedScore = Mathf.RoundToInt(Mathf.Lerp(from, to, EaseOutQuad(k)));
            SetText(scoreText, Format(_displayedScore));
            yield return null;
        }

        _displayedScore = to;
        SetText(scoreText, Format(_displayedScore));
        _scoreTweenCo = null;
    }

    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);

    // === Pulse HighScore ===
    private void Pulse(RectTransform rt)
    {
        if (rt == null) return;
        if (_pulseCo != null) StopCoroutine(_pulseCo);
        _pulseCo = StartCoroutine(PulseCo(rt));
    }

    private IEnumerator PulseCo(RectTransform rt)
    {
        Vector3 baseScale = rt.localScale;
        Vector3 big = baseScale * highPulseScale;

        float t = 0f;
        while (t < highPulseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / highPulseTime);
            rt.localScale = Vector3.Lerp(baseScale, big, k);
            yield return null;
        }

        t = 0f;
        while (t < highPulseTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / highPulseTime);
            rt.localScale = Vector3.Lerp(big, baseScale, k);
            yield return null;
        }

        rt.localScale = baseScale;
        _pulseCo = null;
    }

    // === Helpers ===
    private static void SetText(TextMeshProUGUI t, string s) { if (t) t.text = s; }
    private static string Format(int value) => value.ToString("N0");

    // === UI Hook: gọi từ Button để reset High Score ===
    public void ResetHighScoreUI()
    {
        if (gameScore == null) return;
        gameScore.ResetHighScore(); // HighScoreChanged sẽ cập nhật UI + pulse
    }
}
