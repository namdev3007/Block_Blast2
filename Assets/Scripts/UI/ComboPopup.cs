using TMPro;
using UnityEngine;
using System.Collections;

public class ComboPopup : MonoBehaviour
{
    public TextMeshProUGUI label;

    [Header("FX")]
    public RectTransform numberFxPrefab;   // Prefab UI để spawn tại tâm chữ số
    public Vector2 fxOffset = Vector2.zero; // bù thêm nếu muốn

    [Header("Anim")]
    public float risePixels = 60f;
    public float duration = 0.8f;
    public float popScale = 1.15f;

    RectTransform _rt;
    CanvasGroup _cg;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = gameObject.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void ShowAtScreenPoint(string richText, Vector2 screenPoint, Camera cam, RectTransform parent, Vector2 extraOffset)
    {
        // set parent
        _rt.SetParent(parent, worldPositionStays: false);

        // place by screen point
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var local);
        _rt.anchoredPosition = local + extraOffset;

        // content
        label.richText = true;
        label.text = richText;

        // spawn FX tại tâm chữ số
        SpawnFxAtDigitsCenter();

        // anim
        StopAllCoroutines();
        StartCoroutine(PlayAnim());
    }

    IEnumerator PlayAnim()
    {
        _cg.alpha = 1f;
        Vector3 start = _rt.anchoredPosition;
        Vector3 end = start + new Vector3(0f, risePixels, 0f);

        float t = 0f;
        _rt.localScale = Vector3.one * 0.9f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            // ease
            float easeUp = 1f - Mathf.Pow(1f - k, 2f);

            _rt.anchoredPosition = Vector3.Lerp(start, end, easeUp);

            // pop scale vào nửa đầu
            float s = (k < 0.2f) ? Mathf.Lerp(0.9f, popScale, k / 0.2f)
                                  : Mathf.Lerp(popScale, 1f, (k - 0.2f) / 0.8f);
            _rt.localScale = Vector3.one * s;

            // fade ra dần
            _cg.alpha = 1f - k;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnFxAtDigitsCenter()
    {
        if (numberFxPrefab == null || label == null) return;

        // cập nhật mesh để có textInfo chính xác
        label.ForceMeshUpdate();
        var ti = label.textInfo;
        if (ti == null || ti.characterCount == 0) return;

        bool found = false;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < ti.characterCount; i++)
        {
            var ch = ti.characterInfo[i];
            if (!ch.isVisible) continue;
            if (!char.IsDigit(ch.character)) continue;

            found = true;
            // các toạ độ này ở LOCAL SPACE của label
            Vector2 bl = ch.bottomLeft;
            Vector2 tr = ch.topRight;

            if (bl.x < min.x) min.x = bl.x;
            if (bl.y < min.y) min.y = bl.y;
            if (tr.x > max.x) max.x = tr.x;
            if (tr.y > max.y) max.y = tr.y;
        }

        if (!found) return;

        // tâm của cụm chữ số trong LOCAL của label
        Vector2 centerLocalInLabel = (min + max) * 0.5f;

        // -> WORLD
        Vector3 world = label.rectTransform.TransformPoint(centerLocalInLabel);

        // -> LOCAL của popup (_rt)
        Vector2 centerLocalInPopup;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt,
            RectTransformUtility.WorldToScreenPoint(null, world),
            null,
            out centerLocalInPopup
        );

        // spawn prefab làm con của popup để đi cùng popup
        var fx = Instantiate(numberFxPrefab, _rt);
        fx.anchoredPosition = centerLocalInPopup + fxOffset;
        fx.localScale = Vector3.one;
        fx.SetAsLastSibling(); // đảm bảo ở trên label nếu cần
    }
}
