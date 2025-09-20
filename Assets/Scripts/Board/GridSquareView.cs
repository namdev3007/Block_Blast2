using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween cho flash viền
using System.Collections;
using System.Collections.Generic;

public class GridSquareView : MonoBehaviour
{
    [Header("UI")]
    public Image hoverImage;          // overlay preview footprint (các ô của shape)
    public Image hoverPreviewImage;   // overlay preview HÀNG/CỘT hoàn thành
    public Image activeImage;         // khi đã đặt
    public Image normalImage;
    public Sprite defaultSprite;
    private bool _linePreviewOn;
    private bool _activeHiddenByLinePreview;

    // NEW: overlay riêng cho intro wave (kéo thả trong prefab)
    [Header("Intro Wave")]
    public Image introWaveImage;

    [Header("State")]
    [Range(0f, 1f)] public float hoverAlpha = 0.35f;
    public bool IsOccupied { get; private set; }
    public bool IsHovered { get; private set; }

    [Header("Info")]
    public int Index { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }
    public GridRegion Region { get; private set; }

    [Header("Flash Outline (trắng 1 lần)")]
    public float glowThickness = 4f;
    public bool addOutlineIfMissing = true;

    private Outline _glow;
    private Graphic _glowTarget;
    private Tween _glowTween;

    private RectTransform _rt;
    private Vector3 _initialScale;
    private Tween _clearTween;

    // NEW: tween cho intro/ghost
    private Tween _introTween;

    Sequence _seq;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _initialScale = _rt.localScale;

        // đảm bảo introWaveImage init đúng
        if (introWaveImage)
        {
            var c = introWaveImage.color; c.a = 0f;
            introWaveImage.color = c;
            introWaveImage.raycastTarget = false;
            introWaveImage.enabled = true; // để sẵn, alpha = 0
            introWaveImage.preserveAspect = true;
        }
    }

    public void Init(int index, int row, int col, GridRegion region, Sprite overrideSprite = null)
    {
        Index = index; Row = row; Col = col; Region = region;

        IsOccupied = false; IsHovered = false;

        if (normalImage != null)
        {
            normalImage.sprite = overrideSprite != null ? overrideSprite : defaultSprite;
            normalImage.enabled = true;
        }
        if (activeImage != null) activeImage.enabled = false;

        if (hoverImage != null)
        {
            var c = hoverImage.color; c.a = hoverAlpha;
            hoverImage.color = c;
            hoverImage.enabled = false;
            hoverImage.raycastTarget = false;
        }

        if (hoverPreviewImage != null)
        {
            hoverPreviewImage.enabled = false;
            hoverPreviewImage.raycastTarget = false;
        }

        // reset intro/ghost overlay
        if (introWaveImage)
        {
            var cc = introWaveImage.color; cc.a = 0f;
            introWaveImage.color = cc;
            introWaveImage.enabled = true;
            introWaveImage.preserveAspect = true;
        }

        PrepareGlow();
    }

    private void PrepareGlow()
    {
        _glowTarget = activeImage != null ? (Graphic)activeImage : normalImage;
        if (_glowTarget == null) return;

        _glow = _glowTarget.GetComponent<Outline>();
        if (_glow == null && addOutlineIfMissing)
            _glow = _glowTarget.gameObject.AddComponent<Outline>();

        if (_glow != null)
        {
            _glow.effectDistance = new Vector2(glowThickness, -glowThickness);
            var c = _glow.effectColor; c.a = 0f;
            _glow.effectColor = c;
            _glow.useGraphicAlpha = true;
            _glow.enabled = false;
        }
    }

    public void PlayPlaceFlashOnce(float fadeIn = 0.12f, float fadeOut = 0.18f)
    {
        if (_glowTarget == null) PrepareGlow();
        if (_glow == null) return;

        _glowTween?.Kill();
        _glow.enabled = true;

        var start = new Color(1f, 1f, 1f, 0f);
        var full = new Color(1f, 1f, 1f, 1f);

        _glow.effectColor = start;

        _glowTween = DOTween.Sequence()
            .Append(DOTween.To(() => _glow.effectColor, x => _glow.effectColor = x, full, fadeIn))
            .Append(DOTween.To(() => _glow.effectColor, x => _glow.effectColor = x, start, fadeOut))
            .OnComplete(() => _glow.enabled = false)
            .SetTarget(this);
    }

    public void SetOccupied(bool occupied, Sprite placedSprite = null)
    {
        IsOccupied = occupied;

        if (occupied)
        {
            if (activeImage != null)
            {
                if (placedSprite != null) activeImage.sprite = placedSprite;
                activeImage.enabled = true;
            }
            if (normalImage != null) normalImage.enabled = false;
            if (hoverImage != null) hoverImage.enabled = false;
            if (hoverPreviewImage != null) hoverPreviewImage.enabled = false;

            _linePreviewOn = false;
            _activeHiddenByLinePreview = false;

            IsHovered = false;
            PrepareGlow();
        }
        else
        {
            if (activeImage != null) activeImage.enabled = false;
            if (normalImage != null) { normalImage.enabled = true; normalImage.sprite = defaultSprite; }

            _linePreviewOn = false;
            _activeHiddenByLinePreview = false;

            ApplyHoverVisual();
        }
    }

    public void SetHoverPreview(bool on, Sprite previewSprite = null, float? alpha = null)
    {
        if (hoverImage == null) return;

        if (IsOccupied)
        {
            hoverImage.enabled = false;
            IsHovered = false;
            return;
        }

        IsHovered = on;
        if (on)
        {
            if (previewSprite != null) hoverImage.sprite = previewSprite;
            var c = hoverImage.color; c.a = Mathf.Clamp01(alpha ?? hoverAlpha);
            hoverImage.color = c;
            hoverImage.enabled = true;

            if (hoverPreviewImage != null) hoverPreviewImage.enabled = false;
            RestoreActiveIfHiddenByLinePreview();
        }
        else
        {
            hoverImage.enabled = false;
        }
    }

    public void SetLinePreview(bool on, Sprite previewSprite = null, float? alpha = null)
    {
        if (hoverPreviewImage == null) return;

        if (hoverImage != null && hoverImage.enabled)
        {
            hoverPreviewImage.enabled = false;
            RestoreActiveIfHiddenByLinePreview();
            return;
        }

        if (on)
        {
            if (previewSprite != null) hoverPreviewImage.sprite = previewSprite;
            hoverPreviewImage.enabled = true;
            HideActiveForLinePreview();
        }
        else
        {
            hoverPreviewImage.enabled = false;
            RestoreActiveIfHiddenByLinePreview();
        }
    }

    public void SetHover(bool on) => SetHoverPreview(on, null, null);

    private void ApplyHoverVisual()
    {
        if (hoverImage == null) return;
        hoverImage.enabled = !IsOccupied && IsHovered;
        if (hoverImage.enabled)
        {
            var c = hoverImage.color; c.a = hoverAlpha;
            hoverImage.color = c;
        }
    }

    public void SetSprite(Sprite s)
    {
        if (normalImage == null) return;
        normalImage.sprite = s != null ? s : defaultSprite;
    }

    /// <summary>
    /// Reset cell. Nếu muốn giữ ghost sau game over, truyền resetGhost = false.
    /// </summary>
    public void ResetCell(bool resetGhost = true)
    {
        _glowTween?.Kill();
        if (_glow != null)
        {
            var c = _glow.effectColor; c.a = 0f;
            _glow.effectColor = c;
            _glow.enabled = false;
        }

        _linePreviewOn = false;
        _activeHiddenByLinePreview = false;

        SetOccupied(false, null);
        SetHoverPreview(false, null, null);
        SetLinePreview(false, null, null);

        if (normalImage != null) { normalImage.sprite = defaultSprite; normalImage.enabled = true; }
        if (activeImage != null) activeImage.enabled = false;
        if (hoverImage != null) hoverImage.enabled = false;
        if (hoverPreviewImage != null) hoverPreviewImage.enabled = false;

        // reset intro/ghost layer (nếu được yêu cầu)
        if (introWaveImage && resetGhost)
        {
            _introTween?.Kill();
            var cc = introWaveImage.color; cc.a = 0f;
            introWaveImage.color = cc;
        }
    }

    /// <summary>
    /// Clear + pop (đặt thật) rồi reset cell.
    /// </summary>
    public void PlayClearPopAndReset(
        float delay = 0f,
        float popScale = 1.15f,
        float popIn = 0.08f,
        float fadeOut = 0.18f,
        Ease popEaseIn = Ease.OutBack,
        Ease popEaseOut = Ease.InBack
    )
    {
        if (activeImage == null || !activeImage.enabled)
        {
            ResetCell(true);
            return;
        }

        var col = activeImage.color; col.a = 1f; activeImage.color = col;
        _clearTween?.Kill();

        _clearTween = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(_rt.DOScale(_initialScale * popScale, popIn).SetEase(popEaseIn))
            .Append(
                DOTween.Sequence()
                    .Join(_rt.DOScale(_initialScale, fadeOut).SetEase(popEaseOut))
                    .Join(activeImage.DOFade(0f, fadeOut))
            )
            .OnComplete(() =>
            {
                var c2 = activeImage.color; c2.a = 1f; activeImage.color = c2;
                _rt.localScale = _initialScale;
                ResetCell(true);
            })
            .SetTarget(this);
    }

    /// <summary>
    /// Bản bounce đơn giản (đổi tên để tránh trùng overload mơ hồ).
    /// </summary>
    public void PlayClearPopBounce(
        float scaleUp = 1.2f, float upTime = 0.12f,
        float downTime = 0.10f, Ease upEase = Ease.OutBack, Ease downEase = Ease.InSine)
    {
        _seq?.Kill();
        transform.localScale = Vector3.one;

        _seq = DOTween.Sequence()
            .SetAutoKill(true)
            .OnKill(() => _seq = null);

        _seq.Append(transform.DOScale(scaleUp, upTime).SetEase(upEase));
        _seq.Append(transform.DOScale(1f, downTime).SetEase(downEase));
    }

    void OnDisable()
    {
        _seq?.Kill();
        _clearTween?.Kill();
        _glowTween?.Kill();
        _introTween?.Kill();
    }

    private void HideActiveForLinePreview()
    {
        if (activeImage != null && activeImage.enabled)
        {
            activeImage.enabled = false;
            _activeHiddenByLinePreview = true;
        }
        _linePreviewOn = true;
    }

    private void RestoreActiveIfHiddenByLinePreview()
    {
        if (_linePreviewOn && _activeHiddenByLinePreview && IsOccupied && activeImage != null)
            activeImage.enabled = true;

        _linePreviewOn = false;
        _activeHiddenByLinePreview = false;
    }

    // ================= Intro bằng SKIN SPRITE =================

    /// <summary>
    /// Flash intro nhưng dùng sprite (thường lấy từ SkinProvider). Không chiếm ô.
    /// </summary>
    public void PlayIntroFlashWithSprite(Sprite skinSprite, float delay, float fadeIn = 0.10f, float fadeOut = 0.20f, float maxAlpha = 1f)
    {
        if (!introWaveImage) return;

        _introTween?.Kill();

        introWaveImage.sprite = skinSprite != null ? skinSprite : defaultSprite;
        introWaveImage.preserveAspect = true;

        var c0 = introWaveImage.color; c0.a = 0f;
        introWaveImage.color = c0;
        introWaveImage.enabled = true;

        float a = Mathf.Clamp01(maxAlpha);

        _introTween = DOTween.Sequence()
            .AppendInterval(Mathf.Max(0f, delay))
            .Append(introWaveImage.DOFade(a, fadeIn))
            .Append(introWaveImage.DOFade(0f, fadeOut))
            .SetTarget(this);
    }

    public void PlayIntroFlash(float delay, float fadeIn = 0.10f, float fadeOut = 0.20f, float maxAlpha = 0.8f)
    {
        PlayIntroFlashWithSprite(defaultSprite, delay, fadeIn, fadeOut, maxAlpha);
    }

    public void PlayIntroColorPour(Sprite skinSprite, float delay, float fadeIn = 0.14f, float hold = 0.05f, float fadeOut = 0.28f)
    {
        if (!introWaveImage) return;

        _introTween?.Kill();

        introWaveImage.sprite = skinSprite != null ? skinSprite : defaultSprite;
        introWaveImage.preserveAspect = true;

        var c0 = introWaveImage.color; c0.a = 0f;
        introWaveImage.color = c0;
        introWaveImage.enabled = true;

        _introTween = DOTween.Sequence()
            .AppendInterval(Mathf.Max(0f, delay))
            .Append(introWaveImage.DOFade(1f, fadeIn))
            .AppendInterval(Mathf.Max(0f, hold))
            .Append(introWaveImage.DOFade(0f, fadeOut))
            .SetTarget(this);
    }

    public Sprite CurrentSprite
    {
        get
        {
            if (activeImage != null && activeImage.enabled && activeImage.sprite != null)
                return activeImage.sprite;
            if (normalImage != null && normalImage.sprite != null)
                return normalImage.sprite;
            return defaultSprite;
        }
    }

    // === GHOST OVERLAY (giữ nguyên sau wave) ===
    public void ShowIntroGhost(Sprite s, float alpha = 0.85f)
    {
        if (!introWaveImage) return;
        _introTween?.Kill();

        introWaveImage.sprite = s != null ? s : defaultSprite;
        introWaveImage.preserveAspect = true;

        var c = introWaveImage.color;
        c.a = alpha;
        introWaveImage.color = c;
        introWaveImage.enabled = true;
    }

    public void HideIntroGhost(bool instant = false, float fadeOut = 0.15f)
    {
        if (!introWaveImage) return;
        _introTween?.Kill();

        if (instant || fadeOut <= 0f)
        {
            var c = introWaveImage.color; c.a = 0f;
            introWaveImage.color = c;
            introWaveImage.enabled = false;
            return;
        }

        _introTween = introWaveImage.DOFade(0f, fadeOut)
            .OnComplete(() => introWaveImage.enabled = false)
            .SetTarget(this);
    }

    public void PlayGameOverGhost(Sprite s, float delay, float fadeIn = 0.12f, float targetAlpha = 0.85f)
    {
        if (!introWaveImage) return;

        _introTween?.Kill();

        introWaveImage.sprite = s != null ? s : defaultSprite;
        introWaveImage.preserveAspect = true;

        var c0 = introWaveImage.color; c0.a = 0f;
        introWaveImage.color = c0;
        introWaveImage.enabled = true;

        float a = Mathf.Clamp01(targetAlpha);

        _introTween = DOTween.Sequence()
            .AppendInterval(Mathf.Max(0f, delay))
            .Append(introWaveImage.DOFade(a, fadeIn))
            .SetTarget(this);
    }
}
