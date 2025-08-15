using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween cho flash viền


public class GridSquareView : MonoBehaviour
{
    [Header("UI")]
    public Image hooverImage;   // overlay preview (trên cùng)
    public Image activeImage;   // khi đã đặt
    public Image normalImage;   // nền bình thường
    public Sprite defaultSprite;

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

        if (hooverImage != null)
        {
            var c = hooverImage.color; c.a = hoverAlpha;
            hooverImage.color = c;
            hooverImage.enabled = false;
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

    /// Flash viền màu trắng 1 lần: 0 -> 1 -> 0 rồi tắt.
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
            .OnComplete(() =>
            {
                _glow.enabled = false;
            })
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
            if (hooverImage != null) hooverImage.enabled = false;
            IsHovered = false;

            // đảm bảo flash vẽ trên graphic đang hiển thị
            PrepareGlow();
        }
        else
        {
            if (activeImage != null) activeImage.enabled = false;
            if (normalImage != null)
            {
                normalImage.enabled = true;
                normalImage.sprite = defaultSprite;
            }
            ApplyHoverVisual();
        }
    }

    /// Preview bằng sprite (mờ). Chỉ hiện nếu chưa đặt.
    public void SetHoverPreview(bool on, Sprite previewSprite = null, float? alpha = null)
    {
        if (hooverImage == null) return;

        if (IsOccupied)
        {
            hooverImage.enabled = false;
            IsHovered = false;
            return;
        }

        IsHovered = on;
        if (on)
        {
            if (previewSprite != null) hooverImage.sprite = previewSprite;
            var c = hooverImage.color;
            c.a = Mathf.Clamp01(alpha.HasValue ? alpha.Value : hoverAlpha);
            hooverImage.color = c;
            hooverImage.enabled = true;
        }
        else hooverImage.enabled = false;
    }

    public void SetHover(bool on) => SetHoverPreview(on, null, null);

    private void ApplyHoverVisual()
    {
        if (hooverImage == null) return;
        hooverImage.enabled = !IsOccupied && IsHovered;
        if (hooverImage.enabled)
        {
            var c = hooverImage.color; c.a = hoverAlpha;
            hooverImage.color = c;
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

        if (normalImage != null) { normalImage.sprite = defaultSprite; normalImage.enabled = true; }
        if (activeImage != null) activeImage.enabled = false;
        if (hooverImage != null) hooverImage.enabled = false;
    }
}
