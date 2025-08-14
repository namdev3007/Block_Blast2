using UnityEngine;
using UnityEngine.UI;

public class GridSquareView : MonoBehaviour
{
    [Header("UI")]
    public Image hooverImage;   // overlay khi preview
    public Image activeImage;   // ảnh khi đã đặt
    public Image normalImage;   // ảnh nền bình thường
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

    public void Init(int index, int row, int col, GridRegion region, Sprite overrideSprite = null)
    {
        Index = index; Row = row; Col = col; Region = region;

        IsOccupied = false;
        IsHovered = false;

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

    /// <summary>Preview bằng sprite (mờ). Chỉ hiện nếu chưa đặt.</summary>
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
        else
        {
            hooverImage.enabled = false;
        }
    }

    /// <summary>Giữ tương thích cũ (bật/tắt overlay không thay sprite).</summary>
    public void SetHover(bool on) => SetHoverPreview(on, null, null);

    private void ApplyHoverVisual()
    {
        if (hooverImage == null) return;
        hooverImage.enabled = !IsOccupied && IsHovered;
        if (hooverImage.enabled)
        {
            var c = hooverImage.color;
            c.a = hoverAlpha;
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
        SetOccupied(false, null);
        SetHoverPreview(false, null, null);
        if (normalImage != null)
        {
            normalImage.sprite = defaultSprite;
            normalImage.enabled = true;
        }
        if (activeImage != null) activeImage.enabled = false;
        if (hooverImage != null) hooverImage.enabled = false;
    }
}
