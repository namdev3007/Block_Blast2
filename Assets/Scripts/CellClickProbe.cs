using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class CellClickProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        img.enabled = true;               // ĐỪNG tắt nếu muốn nhận raycast
        img.raycastTarget = true;         // bắt raycast UI
        var c = img.color; if (c.a <= 0f) { c.a = 0.01f; img.color = c; } // ẩn mà vẫn nhận raycast
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[CellClickProbe] Down on {name} button={eventData.button}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[CellClickProbe] Up on {name} button={eventData.button}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[CellClickProbe] CLICK on {name}!");
    }
}
