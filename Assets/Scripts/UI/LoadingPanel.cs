using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private float tweenDuration = 0.2f;

    [Header("Behavior")]
    [SerializeField] private float minShowSeconds = 1f; // hiển thị tối thiểu

    private float _visualProgress = 0f;
    private Tweener _progressTweener;

    void Reset()
    {
        progressBar = GetComponentInChildren<Slider>();
        progressLabel = GetComponentInChildren<TMP_Text>();
    }

    void Awake()
    {
        gameObject.SetActive(false);
        SetImmediateProgress(0f);
    }

    public IEnumerator LoadBootAsync(
        string[] additiveScenes,
        string[] resourcePaths,
        Action onDone = null
    )
    {
        gameObject.SetActive(true);
        float t0 = Time.unscaledTime;

        var sceneList = additiveScenes != null ? additiveScenes : Array.Empty<string>();
        var resList = resourcePaths != null ? resourcePaths : Array.Empty<string>();

        const float sceneWeight = 0.6f;
        const float resWeight = 0.4f;

        float sceneChunk = sceneList.Length > 0 ? sceneWeight / sceneList.Length : 0f;
        float resChunk = resList.Length > 0 ? resWeight / resList.Length : 0f;

        float logicalProgress = 0f;
        UpdateUI(0f);

        // 1) Load scenes (additive)
        for (int i = 0; i < sceneList.Length; i++)
        {
            string sceneName = sceneList[i];
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            op.allowSceneActivation = true;

            float start = logicalProgress;
            while (!op.isDone)
            {
                float step = Mathf.Clamp01(op.progress / 0.9f);
                float target = start + step * sceneChunk;
                SmoothProgressTo(target);
                yield return null;
            }
            logicalProgress = start + sceneChunk;
            SmoothProgressTo(logicalProgress);
        }

        // 2) Load Resources
        var loadedAssets = new List<UnityEngine.Object>(resList.Length);
        for (int i = 0; i < resList.Length; i++)
        {
            string path = resList[i];
            ResourceRequest req = Resources.LoadAsync<UnityEngine.Object>(path);

            float start = logicalProgress;
            while (!req.isDone)
            {
                float target = start + req.progress * resChunk;
                SmoothProgressTo(target);
                yield return null;
            }
            loadedAssets.Add(req.asset);
            logicalProgress = start + resChunk;
            SmoothProgressTo(logicalProgress);
        }

        // 3) Đẩy lên 100%
        SmoothProgressTo(1f);
        yield return new WaitForSeconds(0.05f);
        KillProgressTween();
        SetImmediateProgress(1f);

        onDone?.Invoke();

        // 4) Đảm bảo tối thiểu
        float elapsed = Time.unscaledTime - t0;
        if (elapsed < minShowSeconds)
            yield return new WaitForSecondsRealtime(minShowSeconds - elapsed);

        gameObject.SetActive(false);
    }

    private void SmoothProgressTo(float target01)
    {
        target01 = Mathf.Clamp01(target01);
        KillProgressTween();
        _progressTweener = DOTween.To(
            () => _visualProgress,
            v => { _visualProgress = v; UpdateUI(_visualProgress); },
            target01,
            tweenDuration
        ).SetUpdate(true); // dùng unscaled time
    }

    private void SetImmediateProgress(float v)
    {
        KillProgressTween();
        _visualProgress = Mathf.Clamp01(v);
        UpdateUI(_visualProgress);
    }

    private void KillProgressTween()
    {
        if (_progressTweener != null && _progressTweener.IsActive())
            _progressTweener.Kill();
        _progressTweener = null;
    }

    private void UpdateUI(float v01)
    {
        if (progressBar) progressBar.value = v01;
        if (progressLabel) progressLabel.text = Mathf.RoundToInt(v01 * 100f) + "%";
    }
}
