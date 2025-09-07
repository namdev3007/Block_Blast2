using TMPro;
using UnityEngine;
using System.Collections;

public class PointsPopup : MonoBehaviour
{
    public TextMeshProUGUI label;

    [Header("Appear Anim")]
    public float risePixels = 46f;
    public float appearDuration = 0.5f;
    public float popScale = 1.12f;

    [Header("Fly-To-Score")]
    [Tooltip("Thời gian bay về điểm tổng (nếu có flyTarget).")]
    public float flyTime = 0.45f;
    [Tooltip("Co nhỏ dần khi bay.")]
    public float flyEndScale = 0.6f;
    [Tooltip("Làm mờ khi bay về điểm tổng.")]
    public float flyEndAlpha = 0.0f;

    RectTransform _rt;
    CanvasGroup _cg;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = gameObject.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Hiện "+points" tại screenPoint. Sau phần appear (delay -> pop/rise),
    /// nếu flyTarget != null thì bay về flyTarget.
    /// </summary>
    public void ShowAtScreenPoint(
        int points,
        Vector2 screenPoint,
        Camera cam,
        RectTransform parent,
        Vector2 extraOffset,
        float delay,
        RectTransform flyTarget,         // null = không bay
        Vector2 flyTargetOffset
    )
    {
        // parent
        _rt.SetParent(parent, worldPositionStays: false);

        // place by screen point
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var local);
        _rt.anchoredPosition = local + extraOffset;

        // content
        if (label != null)
        {
            label.richText = false;
            label.text = $"+{points}";
        }

        StopAllCoroutines();
        StartCoroutine(PlayRoutine(delay, cam, parent, flyTarget, flyTargetOffset));
    }

    IEnumerator PlayRoutine(float delay, Camera cam, RectTransform parent, RectTransform flyTarget, Vector2 flyOffset)
    {
        // appear
        _cg.alpha = 0f;
        _rt.localScale = Vector3.one * 0.9f;

        if (delay > 0f)
        {
            float t0 = 0f;
            while (t0 < delay) { t0 += Time.unscaledDeltaTime; yield return null; }
        }

        Vector2 startPos = _rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, risePixels);

        float t = 0f;
        while (t < appearDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / appearDuration);

            // ease out quad
            float ease = 1f - (1f - k) * (1f - k);
            _rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, ease);

            // pop scale (vào 20% đầu -> về 1.0f)
            float s = (k < 0.2f) ? Mathf.Lerp(0.9f, popScale, k / 0.2f)
                                 : Mathf.Lerp(popScale, 1f, (k - 0.2f) / 0.8f);
            _rt.localScale = Vector3.one * s;

            // fade in nhanh rồi giữ
            _cg.alpha = Mathf.Clamp01(k / 0.1f);
            yield return null;
        }

        // Nếu không có target thì fade-out nhẹ rồi hủy
        if (flyTarget == null)
        {
            // fade + nhẹ lên thêm chút rồi biến mất
            float outT = 0f, outDur = 0.25f;
            Vector2 more = endPos + new Vector2(0f, 10f);
            while (outT < outDur)
            {
                outT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(outT / outDur);
                _rt.anchoredPosition = Vector2.Lerp(endPos, more, k);
                _cg.alpha = 1f - k;
                yield return null;
            }
            Destroy(gameObject);
            yield break;
        }

        // Có target: bay về điểm tổng
        // Tính local đích trong cùng 'parent'
        Vector2 flyDestLocal = _rt.anchoredPosition; // default fallback
        {
            // lấy tâm target theo screen -> local parent
            Vector3 world = flyTarget.TransformPoint((Vector3)flyTarget.rect.center);
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, targetScreen, cam, out var local);
            flyDestLocal = local + flyOffset;
        }

        // bay (move + scale + fade)
        Vector2 flyStartPos = _rt.anchoredPosition;
        Vector3 flyStartScale = _rt.localScale;
        float flyStartAlpha = _cg.alpha;

        float ft = 0f;
        while (ft < flyTime)
        {
            ft += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(ft / flyTime);
            // ease in-out (cubic)
            float e = k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) / 2f;

            _rt.anchoredPosition = Vector2.LerpUnclamped(flyStartPos, flyDestLocal, e);
            _rt.localScale = Vector3.LerpUnclamped(flyStartScale, Vector3.one * flyEndScale, e);
            _cg.alpha = Mathf.LerpUnclamped(flyStartAlpha, flyEndAlpha, e);
            yield return null;
        }

        Destroy(gameObject);
    }
}
