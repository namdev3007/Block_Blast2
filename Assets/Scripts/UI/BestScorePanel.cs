using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class BestScorePanel : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI txtBestScore;
    public Button btnRestart;
    public Image cupImage;

    [Header("Particles")]
    public ParticleSystem particle1;
    public ParticleSystem particle2;

    [Header("Score Animation")]
    public float scoreAnimDuration = 1f;

    [Header("Cup Enter FX")]
    public Vector2 cupEnterOffset = new Vector2(0f, 800f);
    public float cupEnterDuration = 0.65f;
    public Ease cupEnterEase = Ease.OutBack;
    public float cupEnterScale = 1.08f;
    public float cupPulseDuration = 0.18f;
    public float cupIdlePulse = 1.03f;
    public float cupIdleDuration = 1.2f;

    private Coroutine animCoroutine;
    private RectTransform _cupRT;
    private Vector2 _cupHome;
    private Vector3 _cupHomeScale;
    private Sequence _cupSeq;

    private void Awake()
    {
        if (cupImage) _cupRT = cupImage.rectTransform;
        if (_cupRT)
        {
            _cupHome = _cupRT.anchoredPosition;
            _cupHomeScale = _cupRT.localScale;
        }
    }

    private void OnEnable()
    {
        PlayParticles();
        if (btnRestart) btnRestart.onClick.AddListener(OnRestartClicked);
        PlayCupEnterThenScore();
    }

    private void OnDisable()
    {
        if (btnRestart) btnRestart.onClick.RemoveListener(OnRestartClicked);
        KillCupTweens(true);
        if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
    }

    private void PlayParticles()
    {
        if (particle1) particle1.Play(true);
        if (particle2) particle2.Play(true);
    }

    private void PlayCupEnterThenScore()
    {
        var gm = GameManager.Instance;
        var score = gm != null ? gm.score : null;
        int targetScore = (score != null) ? score.TotalScore : 0;

        if (!_cupRT)
        {
            StartScoreAnim(targetScore);
            return;
        }

        KillCupTweens(false);
        _cupRT.localScale = _cupHomeScale;
        _cupRT.anchoredPosition = _cupHome + cupEnterOffset;

        _cupSeq = DOTween.Sequence()
            .Append(_cupRT.DOAnchorPos(_cupHome, cupEnterDuration).SetEase(cupEnterEase).SetUpdate(true))
            .Join(_cupRT.DOScale(_cupHomeScale * cupEnterScale, cupEnterDuration * 0.6f).SetEase(Ease.OutSine).SetUpdate(true))
            .Append(_cupRT.DOScale(_cupHomeScale, cupPulseDuration).SetEase(Ease.InOutSine).SetUpdate(true))
            .OnComplete(() =>
            {
                _cupRT.DOScale(_cupHomeScale * cupIdlePulse, cupIdleDuration)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(Ease.InOutSine)
                      .SetUpdate(true);
                StartScoreAnim(targetScore);
            });
    }

    private void StartScoreAnim(int targetScore)
    {
        if (txtBestScore == null) return;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateScore(targetScore));
    }

    private IEnumerator AnimateScore(int target)
    {
        float t = 0f;
        int start = 0;
        while (t < scoreAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / scoreAnimDuration);
            int cur = Mathf.RoundToInt(Mathf.Lerp(start, target, p));
            txtBestScore.text = cur.ToString("N0");
            yield return null;
        }
        txtBestScore.text = target.ToString("N0");
        AudioManager.Instance?.PlayIncrease();
        animCoroutine = null;
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        GameManager.Instance?.Restart();
    }

    private void KillCupTweens(bool resetScale)
    {
        _cupSeq?.Kill();
        _cupSeq = null;
        if (_cupRT)
        {
            _cupRT.DOKill();
            if (resetScale)
            {
                _cupRT.localScale = _cupHomeScale;
                _cupRT.anchoredPosition = _cupHome;
            }
        }
    }
}
