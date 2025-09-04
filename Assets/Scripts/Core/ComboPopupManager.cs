using UnityEngine;

public class ComboPopupManager : MonoBehaviour
{
    public RectTransform root;           // RectTransform trong Canvas
    public ComboPopup popupPrefab;       // Prefab popup
    [Min(2)] public int minComboToShow = 2;   // CHỈ hiện từ combo này trở lên

    public void ShowComboAtScreenPoint(int combo, Vector2 screenPoint, Camera cam, Vector2 offset)
    {
        // Không hiện nếu combo chưa đủ ngưỡng
        if (combo < minComboToShow || root == null || popupPrefab == null) return;

        var p = Instantiate(popupPrefab);
        string rich = $"<color=#FFFFFF>Combo</color> <color=#FFC107>{combo}</color>";
        p.ShowAtScreenPoint(rich, screenPoint, cam, root, offset);
    }
}
