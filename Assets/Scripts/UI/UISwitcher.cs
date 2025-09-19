using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UISwitcher : MonoBehaviour, IPointerClickHandler
{
    public enum AudioSwitchTarget { None, Music, Sfx }

    [Header("Refs")]
    public Image backgroundImage;    // nền switch
    public RectTransform tip;        // nút tròn gạt

    [Header("Motion")]
    public float moveX = 60f;        // khoảng cách trượt theo trục X
    public float duration = 0.2f;    // thời gian animation

    [Header("Colors")]
    public Color offColor = Color.gray;
    public Color onColor = Color.green;

    [Header("Timing")]
    public bool useUnscaledTime = true; // chạy khi timeScale = 0 (pause)

    [Header("Persistence / Binding")]
    [Tooltip("Gán Music/Sfx để switch điều khiển AudioManager. Để None nếu chỉ là công tắc UI thường.")]
    public AudioSwitchTarget audioTarget = AudioSwitchTarget.Music;

    [Tooltip("Chỉ dùng khi audioTarget = None. Key lưu PlayerPrefs.")]
    public string prefsKey = "";

    [Tooltip("Tự nạp trạng thái khi start (None->PlayerPrefs; Music/Sfx->AudioManager).")]
    public bool loadOnStart = true;

    [Tooltip("Trạng thái mặc định (nếu chưa có dữ liệu lưu).")]
    public bool defaultOn = true;

    // runtime
    private bool isOn;
    private Vector2 offPos;
    private Vector2 onPos;
    private Tween tipTween;

    void Awake()
    {
        if (!backgroundImage) backgroundImage = GetComponent<Image>(); // fallback
    }

    void Start()
    {
        if (!tip)
        {
            Debug.LogWarning($"{name}: tip is null");
            return;
        }

        // Vị trí off/on theo local anchored
        offPos = tip.anchoredPosition;
        onPos = offPos + new Vector2(moveX, 0f);

        bool startOn = defaultOn;

        if (audioTarget != AudioSwitchTarget.None && AudioManager.Instance != null)
        {
            // Lấy từ AudioManager (nguồn sự thật)
            startOn = (audioTarget == AudioSwitchTarget.Music)
                    ? AudioManager.Instance.IsMusicEnabled()
                    : AudioManager.Instance.IsSfxEnabled();
        }
        else if (loadOnStart)
        {
            // Dùng PlayerPrefs khi không gắn audio
            if (string.IsNullOrEmpty(prefsKey))
                prefsKey = $"UISwitcher.{gameObject.scene.name}.{gameObject.name}";

            startOn = PlayerPrefs.GetInt(prefsKey, defaultOn ? 1 : 0) == 1;
        }

        SetState(startOn, instant: true, notify: false);
    }

    void OnEnable()
    {
        // Nếu switch gắn audio, khi bật lại (re-enable) đồng bộ UI từ AudioManager
        if (audioTarget != AudioSwitchTarget.None && AudioManager.Instance != null)
        {
            bool current = (audioTarget == AudioSwitchTarget.Music)
                ? AudioManager.Instance.IsMusicEnabled()
                : AudioManager.Instance.IsSfxEnabled();
            SetState(current, instant: true, notify: false);
        }
    }

    public void OnPointerClick(PointerEventData eventData) => Toggle();

    public void Toggle()
    {
        bool next = !isOn;

        if (audioTarget != AudioSwitchTarget.None && AudioManager.Instance != null)
        {
            // Cập nhật AudioManager trước, UI sau
            if (audioTarget == AudioSwitchTarget.Music)
                AudioManager.Instance.SetMusicEnabled(next);
            else
                AudioManager.Instance.SetSfxEnabled(next);

            SetState(next, instant: false, notify: false);
            AudioManager.Instance?.PlayClick();
        }
        else
        {
            // Công tắc UI thường -> tự lưu PlayerPrefs
            SetState(next, instant: false, notify: true);
            AudioManager.Instance?.PlayClick();
        }
    }

    public void Set(bool on, bool instant = false)
    {
        if (audioTarget != AudioSwitchTarget.None && AudioManager.Instance != null)
        {
            if (audioTarget == AudioSwitchTarget.Music)
                AudioManager.Instance.SetMusicEnabled(on);
            else
                AudioManager.Instance.SetSfxEnabled(on);

            SetState(on, instant, notify: false);
        }
        else
        {
            SetState(on, instant, notify: true);
        }
    }

    /// <summary>
    /// Đồng bộ UI theo AudioManager (nếu trạng thái âm thanh đổi ở nơi khác).
    /// </summary>
    public void SyncFromAudio()
    {
        if (audioTarget == AudioSwitchTarget.None || AudioManager.Instance == null) return;

        bool current = (audioTarget == AudioSwitchTarget.Music)
            ? AudioManager.Instance.IsMusicEnabled()
            : AudioManager.Instance.IsSfxEnabled();

        SetState(current, instant: true, notify: false);
    }

    private void SetState(bool on, bool instant, bool notify)
    {
        isOn = on;

        // Màu nền
        if (backgroundImage) backgroundImage.color = on ? onColor : offColor;

        // Motion tip
        if (tip)
        {
            tipTween?.Kill();
            if (instant)
                tip.anchoredPosition = on ? onPos : offPos;
            else
                tipTween = tip.DOAnchorPos(on ? onPos : offPos, duration)
                              .SetEase(Ease.OutQuad)
                              .SetUpdate(useUnscaledTime);
        }

        // Lưu prefs chỉ khi không gắn audio
        if (notify && audioTarget == AudioSwitchTarget.None)
        {
            if (string.IsNullOrEmpty(prefsKey))
                prefsKey = $"UISwitcher.{gameObject.scene.name}.{gameObject.name}";
            PlayerPrefs.SetInt(prefsKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // Cho script khác hỏi trạng thái hiện tại
    public bool IsOn() => isOn;
}
