using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class LoadingPanel : MonoBehaviour
{
    [Header("Progress UI")]
    public Slider progressBar;
    public TextMeshProUGUI percentText;

    [Header("Tile Prefab & Root")]
    public RectTransform tilesRoot;          // để trống: sẽ tự tạo child "TilesRoot"
    public GameObject tilePrefab;

    [Header("Tile Layout")]
    public float cell = 64f;
    public float animTime = 0.6f;
    public float holdTime = 0.25f;
    public float bounce = 1.08f;

    [Header("Options")]
    public bool playOnEnable = true;
    public bool useUnscaledTime = true;

    [Header("Auto Close at 100%")]
    public bool autoCloseAt100 = true;
    public float closeDelay = 0.2f;
    public bool fadeOut = true;
    public float fadeDuration = 0.25f;
    [Tooltip("Để trống nếu bạn muốn dùng onCompleted tự gọi UI/GameManager")]
    public GameObject homePanel;
    public UnityEvent onCompleted;

    // runtime
    private RectTransform[] tiles = new RectTransform[4];
    private RectTransform _tilesContainer;    // NEW: container riêng
    private Sequence _seq;
    private float _target, _display;
    private CanvasGroup _cg;
    private bool _closeFired;                 // NEW: guard thay cho autoCloseAt100

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;

        if (!tilesRoot) tilesRoot = transform as RectTransform;

        // Tạo container riêng cho tiles nếu chưa có
        var t = tilesRoot.Find("TilesRoot") as RectTransform;
        if (!t)
        {
            var go = new GameObject("TilesRoot", typeof(RectTransform));
            _tilesContainer = go.GetComponent<RectTransform>();
            _tilesContainer.SetParent(tilesRoot, false);
            _tilesContainer.anchorMin = _tilesContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _tilesContainer.pivot = new Vector2(0.5f, 0.5f);
            _tilesContainer.anchoredPosition = Vector2.zero;
        }
        else _tilesContainer = t;
    }

    void OnEnable()
    {
        _display = _target = 0f;
        _cg.alpha = 1f;
        _closeFired = false;                 // NEW: reset guard mỗi lần bật
        UpdateProgressUI();
        EnsureTiles();
        if (playOnEnable) StartTiles();
    }

    void OnDisable()
    {
        _seq?.Kill();
        _seq = null;
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; // NEW
        // smoothing 60fps-independent
        _display = Mathf.Lerp(_display, _target, 1f - Mathf.Pow(1f - 0.18f, dt * 60f));
        UpdateProgressUI();

        if (!_closeFired && autoCloseAt100 && _target >= 1f && _display >= 0.999f)
        {
            _closeFired = true;              // NEW: chỉ bắn 1 lần, không đổi option
            CompleteAndOpenHome();
        }
    }

    public void Show(bool on)
    {
        gameObject.SetActive(on);
        if (on)
        {
            EnsureTiles();
            if (_seq == null) StartTiles();
        }
        else
        {
            _seq?.Kill(); _seq = null;
        }
    }

    public void SetProgress01(float value01) => _target = Mathf.Clamp01(value01);

    public void ForceComplete()
    {
        _target = 1f;
        _display = 1f;
        UpdateProgressUI();
        if (!_closeFired) { _closeFired = true; CompleteAndOpenHome(); }
    }

    public void SetTileSprite(Sprite sp)
    {
        if (sp == null) return;
        for (int i = 0; i < tiles.Length; i++)
        {
            if (!tiles[i]) continue;
            var img = tiles[i].GetComponent<Image>();
            if (img) img.sprite = sp;
        }
    }

    void UpdateProgressUI()
    {
        if (progressBar) progressBar.value = _display;
        if (percentText) percentText.text = Mathf.RoundToInt(_display * 100f) + "%";
    }

    void EnsureTiles()
    {
        if (!_tilesContainer) return;
        // Chỉ xoá các tile cũ bên trong TilesRoot (không đụng các UI khác)
        for (int i = _tilesContainer.childCount - 1; i >= 0; i--)
            Destroy(_tilesContainer.GetChild(i).gameObject);

        if (!tilePrefab)
        {
            Debug.LogWarning($"{name}: tilePrefab chưa gán!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            var go = Instantiate(tilePrefab, _tilesContainer);
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            tiles[i] = rt;
        }
    }

    void StartTiles()
    {
        if (!tiles[0] || !tiles[1] || !tiles[2] || !tiles[3]) return;

        Vector2 L0 = new Vector2(0f, cell);
        Vector2 L1 = new Vector2(0f, 0f);
        Vector2 L2 = new Vector2(0f, -cell);
        Vector2 L3 = new Vector2(cell, -cell);

        Vector2 Q0 = new Vector2(-cell * 0.5f, cell * 0.5f);
        Vector2 Q1 = new Vector2(cell * 0.5f, cell * 0.5f);
        Vector2 Q2 = new Vector2(-cell * 0.5f, -cell * 0.5f);
        Vector2 Q3 = new Vector2(cell * 0.5f, -cell * 0.5f);

        tiles[0].anchoredPosition = L0; tiles[1].anchoredPosition = L1;
        tiles[2].anchoredPosition = L2; tiles[3].anchoredPosition = L3;
        tiles[0].localScale = tiles[1].localScale = tiles[2].localScale = tiles[3].localScale = Vector3.one;

        _seq?.Kill();
        _seq = DOTween.Sequence().SetAutoKill(false).SetLoops(-1, LoopType.Restart)
                                 .SetUpdate(useUnscaledTime);

        _seq.Append(MorphTiles((L0, L1, L2, L3), (Q0, Q1, Q2, Q3), animTime));
        _seq.Append(Bounce(holdTime));
        _seq.Append(MorphTiles((Q0, Q1, Q2, Q3), (L0, L1, L2, L3), animTime));
        _seq.Append(Bounce(holdTime));
    }

    Tween MorphTiles((Vector2 a0, Vector2 a1, Vector2 a2, Vector2 a3) from,
                     (Vector2 b0, Vector2 b1, Vector2 b2, Vector2 b3) to,
                     float t)
    {
        var s = DOTween.Sequence();
        s.Join(tiles[0].DOAnchorPos(to.b0, t).SetEase(Ease.InOutSine));
        s.Join(tiles[1].DOAnchorPos(to.b1, t).SetEase(Ease.InOutSine));
        s.Join(tiles[2].DOAnchorPos(to.b2, t).SetEase(Ease.InOutSine));
        s.Join(tiles[3].DOAnchorPos(to.b3, t).SetEase(Ease.InOutSine));
        return s;
    }

    Tween Bounce(float hold)
    {
        var s = DOTween.Sequence();
        s.Append(tiles[0].DOScale(bounce, 0.12f).SetEase(Ease.OutBack));
        s.Join(tiles[1].DOScale(bounce, 0.12f).SetEase(Ease.OutBack));
        s.Join(tiles[2].DOScale(bounce, 0.12f).SetEase(Ease.OutBack));
        s.Join(tiles[3].DOScale(bounce, 0.12f).SetEase(Ease.OutBack));
        s.AppendInterval(hold);
        s.Append(tiles[0].DOScale(1f, 0.10f));
        s.Join(tiles[1].DOScale(1f, 0.10f));
        s.Join(tiles[2].DOScale(1f, 0.10f));
        s.Join(tiles[3].DOScale(1f, 0.10f));
        return s;
    }

    private void CompleteAndOpenHome()
    {
        _seq?.Kill(); _seq = null;

        DOVirtual.DelayedCall(closeDelay, () =>
        {
            if (fadeOut && _cg)
            {
                _cg.DOFade(0f, fadeDuration)
                   .SetUpdate(useUnscaledTime)
                   .OnComplete(FinalizeClose);
            }
            else FinalizeClose();
        }).SetUpdate(useUnscaledTime);
    }

    private void FinalizeClose()
    {
        gameObject.SetActive(false);

        // Chỉ bật home nếu có gán; khuyến nghị để trống và dùng onCompleted để gọi GameManager.
        if (homePanel) homePanel.SetActive(true);

        onCompleted?.Invoke();

        if (_cg) _cg.alpha = 1f;
    }
}
    