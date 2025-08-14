using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class ShapeDragItem : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public ShapePalette palette;
    public int slotIndex;
    public RectTransform dragRoot;   // RectTransform của Canvas (hoặc 1 lớp top-level)
    public GridView gridView;
    public BoardRuntime board;
    public GridInput gridInput;

    [Header("Ghost visuals")]
    public ShapeItemView ghostPrefab;
    public Vector2 ghostCellSize = new Vector2(64, 64);
    public Vector2 ghostSpacing = new Vector2(4, 4);

    [Header("Offsets")]
    public Vector2 ghostOffsetLocal = Vector2.zero; // offset trong hệ local của dragRoot
    public bool useGrabOffset = true;               // giữ điểm bấm ban đầu để ghost không "nhảy"

    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ShapeItemView _ghostView;
    private ShapeData _draggingData;
    private bool _isDragging;
    private Vector2 _grabOffsetLocal;
    private Camera _cam;                            // camera của Canvas (Screen Space - Camera)

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
    }

    private Vector2 TotalLocalOffset => ghostOffsetLocal + (useGrabOffset ? _grabOffsetLocal : Vector2.zero);

    private Vector2 LocalToScreenDelta(Vector2 localDelta)
    {
        // Chuyển một delta local (dragRoot) sang delta screen để bù cho raycast
        var w0 = dragRoot.TransformPoint(Vector3.zero);
        var w1 = dragRoot.TransformPoint((Vector3)localDelta);
        var s0 = RectTransformUtility.WorldToScreenPoint(_cam, w0);
        var s1 = RectTransformUtility.WorldToScreenPoint(_cam, w1);
        return s1 - s0;
    }

    // Ẩn slot ngay khi click
    public void OnPointerDown(PointerEventData eventData)
    {
        var data = palette.Peek(slotIndex);
        if (data == null) return;
        _cg.alpha = 0f;
    }

    // Nếu chỉ click thả (không kéo), hiện lại
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) _cg.alpha = 1f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _draggingData = palette.Peek(slotIndex);
        if (_draggingData == null) return;

        _isDragging = true;
        _cg.blocksRaycasts = false;
        _cam = eventData.pressEventCamera; // quan trọng với Screen Space - Camera (Overlay -> null cũng OK)

        // tạo ghost
        _ghostView = Instantiate(ghostPrefab, dragRoot);
        _ghostRT = _ghostView.GetComponent<RectTransform>();
        _ghostView.cellSize = ghostCellSize;
        _ghostView.spacing = ghostSpacing;
        _ghostView.Render(_draggingData);
        SetGraphicsRaycastTarget(_ghostView.gameObject, false);

        // đặt vị trí đầu theo điểm bấm + offset cấu hình
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var startLocal);
        _ghostRT.anchoredPosition = startLocal + ghostOffsetLocal;

        // grab offset để giữ điểm bấm (nếu bật)
        _grabOffsetLocal = _ghostRT.anchoredPosition - startLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT == null) return;

        // cập nhật vị trí ghost (local) + offset
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var local);
        _ghostRT.anchoredPosition = local + TotalLocalOffset;

        if (_draggingData == null) { board.ClearPreview(); return; }

        // dùng "con trỏ ảo" đã bù offset để raycast
        var screenPosWithOffset = eventData.position + LocalToScreenDelta(TotalLocalOffset);

        if (!gridInput.TryGetCell(screenPosWithOffset, out var targetCell))
        {
            board.ClearPreview();
            return;
        }

        var (minR, minC, _, _) = _draggingData.GetBounds();
        int anchorRow = targetCell.Row - minR;
        int anchorCol = targetCell.Col - minC;

        if (board.State.CanPlace(_draggingData, anchorRow, anchorCol))
        {
            var sprite = _draggingData.blockSprite != null ? _draggingData.blockSprite : board.placedSprite;
            board.ShowPreview(_draggingData, anchorRow, anchorCol, sprite);
        }
        else
        {
            board.ClearPreview();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;

        if (_ghostRT != null) Destroy(_ghostRT.gameObject);
        _ghostRT = null; _ghostView = null;

        // luôn clear preview khi thả
        board.ClearPreview();

        if (_draggingData == null) return;

        var screenPosWithOffset = eventData.position + LocalToScreenDelta(TotalLocalOffset);
        if (!gridInput.TryGetCell(screenPosWithOffset, out var targetCell))
        {
            _draggingData = null;
            return;
        }

        var (minR, minC, _, _) = _draggingData.GetBounds();
        int anchorRow = targetCell.Row - minR;
        int anchorCol = targetCell.Col - minC;

        if (board.State.CanPlace(_draggingData, anchorRow, anchorCol))
        {
            board.State.Place(_draggingData, anchorRow, anchorCol);
            var spriteToPaint = _draggingData.blockSprite != null ? _draggingData.blockSprite : null;
            board.PaintPlaced(_draggingData, anchorRow, anchorCol, spriteToPaint);
            palette.Consume(slotIndex);
        }

        _draggingData = null;
    }

    private static void SetGraphicsRaycastTarget(GameObject root, bool value)
    {
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = value;
    }
}
