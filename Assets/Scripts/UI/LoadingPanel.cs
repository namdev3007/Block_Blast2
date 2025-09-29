using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressLabel;

    [Header("Options")]
    [Range(0.1f, 2f)] public float smooth = 0.25f;
    [Min(0f)] public float minVisibleSeconds = 1f;

    private float _target01 = 0f;
    private float _shownTRealtime;
    private Coroutine _pendingHideCo;

    void Update()
    {
        if (!progressBar) return;
        progressBar.value = Mathf.Lerp(
            progressBar.value, _target01,
            1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, smooth))
        );
    }

    public void SetProgress01(float v)
    {
        _target01 = Mathf.Clamp01(v);
        if (progressLabel) progressLabel.text = $"{Mathf.RoundToInt(_target01 * 100f)}%";
    }

    public void Show(bool on)
    {
        if (on)
        {
            if (_pendingHideCo != null) { StopCoroutine(_pendingHideCo); _pendingHideCo = null; }
            gameObject.SetActive(true);
            if (progressBar) progressBar.value = 0f;
            SetProgress01(0f);
            _shownTRealtime = Time.realtimeSinceStartup;
            return;
        }

        float elapsed = Time.realtimeSinceStartup - _shownTRealtime;
        float wait = Mathf.Max(0f, minVisibleSeconds - elapsed);

        if (_pendingHideCo != null) StopCoroutine(_pendingHideCo);
        _pendingHideCo = StartCoroutine(CoHideAfter(wait));
    }

    private IEnumerator CoHideAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        gameObject.SetActive(false);
        _pendingHideCo = null;
    }
}
