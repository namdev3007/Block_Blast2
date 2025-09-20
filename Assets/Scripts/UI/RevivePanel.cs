using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RevivePanel : MonoBehaviour
{
    [Header("UI")]
    public Button btnRevive;                 // nút Revive duy nhất
    public Image ring;                       // Image type = Filled, Fill Method = Radial 360
    public TextMeshProUGUI txtSeconds;       // số đếm ngược

    [Header("Timing")]
    public float countdownSeconds = 5f;      // thời gian cho người chơi bấm

    [Header("Heartbeat FX")]
    [Tooltip("Scale tối đa cho nhịp mượt liên tục")]
    public float pulseScale = 1.08f;
    [Tooltip("Thời gian 1 nửa nhịp (to -> nhỏ hoặc nhỏ -> to)")]
    public float pulseHalfDuration = 0.35f;
    public Ease pulseEase = Ease.InOutSine;

    [Tooltip("Nhấn mạnh khi số đổi (mỗi giây)")]
    public float tickPunch = 0.18f;          // biên độ nhấn mạnh
    public float tickDuration = 0.18f;       // thời gian nhấn mạnh
    public int tickVibrato = 1;              // số lần rung trong punch
    public float tickElasticity = 0.5f;

    public event Action Accepted;
    public event Action TimedOut;

    Coroutine _cr;
    bool _running;

    // tween caches
    RectTransform _txtRT;
    Vector3 _txtInitialScale;
    Tween _pulseTween;       // nhịp mượt
    Tween _tickTween;        // nhấn mạnh mỗi giây

    void Awake()
    {
        if (btnRevive) btnRevive.onClick.AddListener(OnClickRevive);

        if (txtSeconds != null)
        {
            _txtRT = txtSeconds.rectTransform;
            _txtInitialScale = _txtRT.localScale;
        }
    }

    public void Show(float? secondsOverride = null)
    {
        gameObject.SetActive(true);
        float t = Mathf.Max(0.5f, secondsOverride ?? countdownSeconds);

        // reset & khởi động nhịp mượt
        StartContinuousPulse();

        StartCountdown(t);
    }

    public void Hide()
    {
        if (_cr != null) StopCoroutine(_cr);
        _cr = null;
        _running = false;

        KillTweens(resetScale: true);

        gameObject.SetActive(false);
    }

    void OnClickRevive()
    {
        if (!_running) return;
        _running = false;
        if (_cr != null) StopCoroutine(_cr);
        _cr = null;

        KillTweens(resetScale: true);

        Accepted?.Invoke();
    }

    void StartCountdown(float seconds)
    {
        if (_cr != null) StopCoroutine(_cr);
        _cr = StartCoroutine(CoCountdown(seconds));
    }

    IEnumerator CoCountdown(float seconds)
    {
        _running = true;

        // chuẩn hoá hiển thị vòng
        if (ring)
        {
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 1f; // bắt đầu đầy
        }

        float remain = seconds;

        // khởi tạo số ban đầu
        int lastShown = -1;
        if (txtSeconds)
        {
            lastShown = Mathf.CeilToInt(remain);
            txtSeconds.text = lastShown.ToString();
        }

        while (remain > 0f)
        {
            // unscaled để không bị ảnh hưởng bởi timeScale
            remain -= Time.unscaledDeltaTime;

            // cập nhật vòng (1 → 0)
            if (ring) ring.fillAmount = Mathf.Clamp01(remain / seconds);

            // cập nhật số + giật nhịp khi đổi số
            if (txtSeconds)
            {
                int show = Mathf.CeilToInt(Mathf.Max(0f, remain));
                if (show != lastShown)
                {
                    txtSeconds.text = show.ToString();
                    lastShown = show;
                    BeatOnce(); // nhấn mạnh mỗi giây
                }
            }

            yield return null;
        }

        _running = false;
        _cr = null;

        KillTweens(resetScale: true);

        TimedOut?.Invoke();
    }

    // ========== Heartbeat helpers ==========
    void StartContinuousPulse()
    {
        if (_txtRT == null) return;

        // reset scale trước khi vào vòng nhịp
        _txtRT.localScale = _txtInitialScale;

        // nhịp mượt lặp vô hạn (unscaled)
        _pulseTween?.Kill();
        _pulseTween = _txtRT
            .DOScale(_txtInitialScale * pulseScale, pulseHalfDuration)
            .SetEase(pulseEase)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true); // unscaled time
    }

    void BeatOnce()
    {
        if (_txtRT == null) return;

        // chồng thêm punch ngắn mỗi khi số đổi
        _tickTween?.Kill();
        _tickTween = _txtRT
            .DOPunchScale(Vector3.one * tickPunch, tickDuration, tickVibrato, tickElasticity)
            .SetUpdate(true);
    }

    void KillTweens(bool resetScale)
    {
        _tickTween?.Kill();
        _pulseTween?.Kill();
        _tickTween = null;
        _pulseTween = null;

        if (resetScale && _txtRT != null)
            _txtRT.localScale = _txtInitialScale;
    }

    void OnDisable()
    {
        // đảm bảo thu dọn tween khi panel bị tắt
        KillTweens(resetScale: true);
    }
}
