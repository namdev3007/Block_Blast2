using TMPro;
using UnityEngine;
using System.Collections;

public class PraisePopup : MonoBehaviour
{
    public TextMeshProUGUI label;

    [Header("Anim")]
    public float risePixels = 56f;
    public float duration = 0.75f;
    public float popScale = 1.15f;

    [Header("Style Ranges")]
    [Tooltip("0.18–0.25 -> top lighten (V+)")]
    public Vector2 topLightenRange = new Vector2(0.18f, 0.25f);
    [Tooltip("0.12–0.18 -> bottom darken (V-)")]
    public Vector2 bottomDarkenRange = new Vector2(0.12f, 0.18f);

    [Tooltip("0.20–0.28 -> Outline width (TMP SDF units)")]
    public Vector2 outlineThicknessRange = new Vector2(0.20f, 0.28f);
    [Tooltip("0.3–0.4 -> tối outline so với base")]
    public Vector2 outlineDarkenRange = new Vector2(0.30f, 0.40f);

    [Tooltip("0.55–0.7 | 0.5–0.65 | 0.5–0.7")]
    public Vector2 underlayDilateRange = new Vector2(0.55f, 0.70f);
    public Vector2 underlaySoftRange = new Vector2(0.50f, 0.65f);
    public Vector2 underlayAlphaRange = new Vector2(0.50f, 0.70f);

    [Tooltip("0.04–0.08 -> chữ dày hơn")]
    public Vector2 faceDilateRange = new Vector2(0.04f, 0.08f);

    RectTransform _rt;
    CanvasGroup _cg;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = gameObject.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Hiện lời khen tại screenPoint. Màu chữ được random theo HSV rồi áp style.
    /// </summary>
    public void ShowAtScreenPoint(string text, Vector2 screenPoint, Camera cam, RectTransform parent, Vector2 extraOffset)
    {
        // parent & pos
        _rt.SetParent(parent, worldPositionStays: false);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var local);
        _rt.anchoredPosition = local + extraOffset;

        // content
        if (label != null)
        {
            label.richText = false;
            label.text = text;
            ApplyRandomStyle(label);
        }

        StopAllCoroutines();
        StartCoroutine(PlayAnim());
    }

    IEnumerator PlayAnim()
    {
        _cg.alpha = 0f;
        Vector2 start = _rt.anchoredPosition;
        Vector2 end = start + new Vector2(0f, risePixels);

        float t = 0f;
        _rt.localScale = Vector3.one * 0.9f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            // ease-up
            float e = 1f - Mathf.Pow(1f - k, 2f);
            _rt.anchoredPosition = Vector2.LerpUnclamped(start, end, e);

            float s = (k < 0.2f) ? Mathf.Lerp(0.9f, popScale, k / 0.2f)
                                 : Mathf.Lerp(popScale, 1f, (k - 0.2f) / 0.8f);
            _rt.localScale = Vector3.one * s;

            // fade in nhanh -> giữ -> fade out nhẹ
            _cg.alpha = (k < 0.85f) ? Mathf.Clamp01(k / 0.1f)
                                    : Mathf.Lerp(1f, 0f, (k - 0.85f) / 0.15f);

            yield return null;
        }

        Destroy(gameObject);
    }

    // ===== Style helpers =====
    void ApplyRandomStyle(TextMeshProUGUI tmp)
    {
        // Base random color (HSV)
        float h = Random.value;
        float s = Random.Range(0.65f, 0.95f);
        float v = Random.Range(0.75f, 0.95f);
        Color baseCol = Color.HSVToRGB(h, s, v);

        float topL = Random.Range(topLightenRange.x, topLightenRange.y);
        float botD = Random.Range(bottomDarkenRange.x, bottomDarkenRange.y);
        float oThick = Random.Range(outlineThicknessRange.x, outlineThicknessRange.y);
        float oDark = Random.Range(outlineDarkenRange.x, outlineDarkenRange.y);
        float uDil = Random.Range(underlayDilateRange.x, underlayDilateRange.y);
        float uSoft = Random.Range(underlaySoftRange.x, underlaySoftRange.y);
        float uAlpha = Random.Range(underlayAlphaRange.x, underlayAlphaRange.y);
        float fDil = Random.Range(faceDilateRange.x, faceDilateRange.y);

        // gradient: lighten top, darken bottom
        Color topC = AdjustV(baseCol, +topL);
        Color bottomC = AdjustV(baseCol, -botD);
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(topC, topC, bottomC, bottomC);

        // Material per-instance
        var mat = tmp.fontMaterial = new Material(tmp.fontMaterial);

        // Outline
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, oThick);
        Color outlineC = AdjustV(baseCol, -oDark);
        outlineC.a = 1f;
        mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineC);

        // Underlay (shadow)
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        Color underC = AdjustV(baseCol, -0.45f);
        underC.a = uAlpha;
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, underC);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, uSoft);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, uDil);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);

        // Face dilate
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, fDil);
    }

    static Color AdjustV(Color c, float deltaV)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        v = Mathf.Clamp01(v + deltaV);
        return Color.HSVToRGB(h, s, v);
    }
}
