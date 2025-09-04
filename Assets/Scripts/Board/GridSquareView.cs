using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween cho flash viền & wave

public class GridSquareView : MonoBehaviour
{
    [Header("UI")]
    public Image hoverImage;          // overlay preview footprint (các ô của shape)
    public Image hoverPreviewImage;   // overlay preview HÀNG/CỘT hoàn thành
    public Image activeImage;         // khi đã đặt
    public Image normalImage;
    public Sprite defaultSprite;

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
    private Sequence _seq;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _initialScale = _rt.localScale;
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

        // intro wave overlay tắt mặc định
        if (introWaveImage != null)
        {
            var c2 = introWaveImage.color; c2.a = 0f;
            introWaveImage.color = c2;
            introWaveImage.enabled = false;
            introWaveImage.raycastTarget = false;
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

            IsHovered = false;
            PrepareGlow();
        }
        else
        {
            if (activeImage != null) activeImage.enabled = false;
            if (normalImage != null) { normalImage.enabled = true; normalImage.sprite = defaultSprite; }

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
            return;
        }

        if (on)
        {
            if (previewSprite != null) hoverPreviewImage.sprite = previewSprite;
            hoverPreviewImage.enabled = true;

            if (activeImage != null && activeImage.enabled) activeImage.enabled = false; // ẩn tạm
        }
        else
        {
            hoverPreviewImage.enabled = false;

            if (IsOccupied && activeImage != null) activeImage.enabled = true; // khôi phục
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

    public void ResetCell()
    {
        _glowTween?.Kill();
        if (_glow != null)
        {
            var c = _glow.effectColor; c.a = 0f;
            _glow.effectColor = c;
            _glow.enabled = false;
        }

        SetOccupied(false, null);
        SetHoverPreview(false, null, null);
        SetLinePreview(false, null, null);

        if (normalImage != null) { normalImage.sprite = defaultSprite; normalImage.enabled = true; }
        if (activeImage != null) activeImage.enabled = false;
        if (hoverImage != null) hoverImage.enabled = false;
        if (hoverPreviewImage != null) hoverPreviewImage.enabled = false;

        if (introWaveImage != null)
        {
            var c2 = introWaveImage.color; c2.a = 0f;
            introWaveImage.color = c2;
            introWaveImage.enabled = false;
        }
    }

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
            ResetCell();
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
                ResetCell();
            })
            .SetTarget(this);
    }

    public void PlayClearBump(float scaleUp = 1.2f, float upTime = 0.12f,
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
        DOTween.Kill(this);
    }

    public void PlayIntroWave(
        Sprite spriteForWave,
        Color? tint,
        float delay,
        float fadeIn = 0.18f,
        float hold = 0.06f,
        float fadeOut = 0.25f,
        float risePixels = 14f,
        Ease easeIn = Ease.OutSine,
        Ease easeOut = Ease.InSine
    )
    {
        if (IsOccupied) return;

        var img = introWaveImage != null ? introWaveImage
                 : (hoverPreviewImage != null ? hoverPreviewImage : normalImage);
        if (img == null) return;

        if (spriteForWave != null) img.sprite = spriteForWave;

        var baseColor = tint ?? Color.white;
        baseColor.a = 0f;

        img.enabled = true;
        img.color = baseColor;

        var rt = img.rectTransform;
        Vector2 basePos = rt.anchoredPosition;

        DOTween.Kill(img);
        DOTween.Kill(rt);

        DOTween.Sequence()
            .AppendInterval(delay)
            .AppendCallback(() =>
            {
                rt.anchoredPosition = basePos - new Vector2(0f, risePixels);
            })
            .Append(img.DOFade(1f, fadeIn).SetEase(easeIn))
            .Join(rt.DOAnchorPos(basePos, fadeIn).SetEase(easeIn))
            .AppendInterval(hold)
            .Append(img.DOFade(0f, fadeOut).SetEase(easeOut))
            .OnComplete(() =>
            {
                img.enabled = false;
                var c = img.color; c.a = 1f; img.color = c;
                rt.anchoredPosition = basePos;
            })
            .SetTarget(this);
    }
}
