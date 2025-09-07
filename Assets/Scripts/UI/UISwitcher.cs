using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UISwitcher : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    public Image backgroundImage;          // nền switch (có thể để null → tự lấy Image trên cùng GO)
    public RectTransform tip;              // nút tròn trượt
    public float moveX = 60f;              // khoảng cách trượt
    public float duration = 0.2f;          // thời gian animation

    [Header("Colors")]
    public Color offColor = Color.gray;
    public Color onColor = Color.green;

    [Header("Options")]
    public bool useUnscaledTime = true; // chạy khi timeScale=0 (pause)
    public bool initialIsOn = true; // MẶC ĐỊNH BẬT

    // Sự kiện bắn ra khi đổi trạng thái (để binder lưu PlayerPrefs / gọi AudioManager)
    [System.Serializable] public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent onValueChanged = new BoolEvent();

    // Runtime
    private bool isOn;
    private Vector2 offPos;
    private Vector2 onPos;
    private Tween tipTween;

    private void Start()
    {
        if (!tip)
        {
            Debug.LogWarning($"{name}: UISwitcher.tip is null");
            return;
        }
        if (!backgroundImage) backgroundImage = GetComponent<Image>(); // fallback

        // Vị trí ban đầu của tip
        offPos = tip.anchoredPosition;
        onPos = offPos + new Vector2(moveX, 0f);

        // Khởi tạo theo initialIsOn (instant để không thấy nhảy)
        Set(initialIsOn, instant: true);
    }

    public void OnPointerClick(PointerEventData eventData) => Toggle();

    public void Toggle() => Set(!isOn);

    /// <summary>
    /// Đặt trạng thái switch (có thể instant), đồng bộ UI và bắn onValueChanged.
    /// </summary>
    public void Set(bool on, bool instant = false)
    {
        if (isOn == on && !instant) return;
        isOn = on;
        UpdateUI(isOn, instant);
        onValueChanged?.Invoke(isOn);
    }

    private void UpdateUI(bool on, bool instant = false)
    {
        if (backgroundImage) backgroundImage.color = on ? onColor : offColor;
        if (!tip) return;

        tipTween?.Kill();

        if (instant)
        {
            tip.anchoredPosition = on ? onPos : offPos;
        }
        else
        {
            tipTween = tip.DOAnchorPos(on ? onPos : offPos, duration)
                          .SetEase(Ease.OutQuad)
                          .SetUpdate(useUnscaledTime);
        }
    }

    public bool IsOn() => isOn;
}
