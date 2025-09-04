using System.Collections;
using UnityEngine;
using TMPro; // << IMPORTANT

public class ScoreUIManager : MonoBehaviour
{
    [Header("Refs")]
    public GameScore gameScore;

    [Header("UI (TextMeshPro)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Prefs / Keys")]
    [SerializeField] private string highScoreKey = "HighScore";

    [Header("Animation")]
    [Tooltip("Duration for score tween (UI count up).")]
    [Range(0.05f, 1.0f)] public float scoreTweenTime = 0.25f;
    [Tooltip("Scale for high score pulse.")]
    [Range(1f, 2.5f)] public float highPulseScale = 1.15f;
    [Range(0.05f, 0.35f)] public float highPulseTime = 0.12f;

    private int _displayedScore;
    private int _highScore;

    private Coroutine _scoreTweenCo;
    private Coroutine _pulseCo;

    private void OnEnable()
    {
        if (gameScore != null)
            gameScore.Scored += OnScored;
    }

    private void OnDisable()
    {
        if (gameScore != null)
            gameScore.Scored -= OnScored;
    }

    private void Start()
    {
        _highScore = PlayerPrefs.GetInt(highScoreKey, 0);

        _displayedScore = gameScore != null ? gameScore.TotalScore : 0;
        SetText(scoreText, Format(_displayedScore));
        SetText(highScoreText, Format(_highScore));
    }

    private void OnScored(ScoreResult r)
    {
        int target = gameScore.TotalScore;

        // tween score text
        if (_scoreTweenCo != null) StopCoroutine(_scoreTweenCo);
        _scoreTweenCo = StartCoroutine(AnimateScore(_displayedScore, target, scoreTweenTime));

        // high score
        if (target > _highScore)
        {
            _highScore = target;
            PlayerPrefs.SetInt(highScoreKey, _highScore);
            PlayerPrefs.Save();
            SetText(highScoreText, Format(_highScore));
            Pulse(highScoreText != null ? highScoreText.rectTransform : null);
        }
    }

    // tween số điểm hiển thị
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
    }

    private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);

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

    private static void SetText(TextMeshProUGUI t, string s) { if (t) t.text = s; }
    private static string Format(int value) => value.ToString("N0");

    public void ResetHighScore()
    {
        _highScore = 0;
        PlayerPrefs.SetInt(highScoreKey, 0);
        PlayerPrefs.Save();
        SetText(highScoreText, Format(_highScore));
    }
}
