using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastDebugger : MonoBehaviour
{
    [Header("Refs (optional)")]
    [Tooltip("Để trống sẽ tự tìm GraphicRaycaster gần nhất trong Canvas cha.")]
    public GraphicRaycaster raycaster;

    [Header("Controls")]
    public KeyCode logKey = KeyCode.Mouse0;   // chuột trái
    public bool logContinuouslyWhileHeld = false;

    void Awake()
    {
        if (!raycaster)
        {
            raycaster = GetComponent<GraphicRaycaster>();
            if (!raycaster) raycaster = GetComponentInParent<GraphicRaycaster>();
        }
    }

    void Update()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[UIRaycastDebugger] Không có EventSystem trong scene.");
            return;
        }

        bool fire = logContinuouslyWhileHeld ? Input.GetKey(logKey) : Input.GetKeyDown(logKey);
        if (!fire) return;

        DumpRaycast(Input.mousePosition);
    }

    void DumpRaycast(Vector2 screenPos)
    {
        if (!raycaster)
        {
            Debug.LogWarning("[UIRaycastDebugger] Chưa có GraphicRaycaster (kéo từ Canvas vào).");
            return;
        }

        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        var sb = new StringBuilder();
        sb.AppendLine("===== UI Raycast Debug =====");
        sb.AppendLine($"ScreenPos: {screenPos} | Hits: {results.Count}");

        if (results.Count == 0)
        {
            sb.AppendLine("Không hit gì. Khả năng: (1) Background chặn raycast, (2) Ô không phải UI/Image, (3) Image.enabled=false hoặc raycastTarget=OFF, (4) CanvasGroup.blocksRaycasts=false.");
            Debug.Log(sb.ToString());
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            var path = GetPath(go.transform);
            var img = go.GetComponent<Image>();
            var hasClickHandler = go.GetComponents<MonoBehaviour>().Any(m => m is IPointerClickHandler);

            // Kiểm tra CanvasGroup trên cha
            var cg = go.GetComponentsInParent<CanvasGroup>(true);
            bool anyBlockRay = cg.Any(g => g != null && g.blocksRaycasts == false);
            string cgNote = anyBlockRay ? " (Có CanvasGroup.blocksRaycasts=FALSE trên cha!)" : "";

            sb.AppendLine($"[{i}] {go.name}  path={path}");
            sb.AppendLine($"     Image? {(img ? "YES" : "NO")}  enabled={(img ? img.enabled.ToString() : "-")}  raycastTarget={(img ? img.raycastTarget.ToString() : "-")}  alpha={(img ? img.color.a.ToString("0.00") : "-")}");
            sb.AppendLine($"     Has IPointerClickHandler? {hasClickHandler}  SortingLayer={results[i].sortingLayer}; order={results[i].sortingOrder}{cgNote}");
        }

        Debug.Log(sb.ToString());
    }

    string GetPath(Transform t)
    {
        var stack = new List<string>();
        while (t != null)
        {
            stack.Add(t.name);
            t = t.parent;
        }
        stack.Reverse();
        return string.Join("/", stack);
    }
}
