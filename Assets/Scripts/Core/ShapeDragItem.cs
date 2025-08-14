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
    public RectTransform dragRoot;
    public GridView gridView;
    public BoardRuntime board;
    public GridInput gridInput;

    [Header("Ghost visuals")]
    public ShapeItemView ghostPrefab;
    public Vector2 ghostCellSize = new Vector2(106, 106);
    public Vector2 ghostSpacing = new Vector2(0, 0);

    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ShapeItemView _ghostView;
    private ShapeData _draggingData;
    private bool _isDragging;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
    }

    // Ẩn ngay khi click chọn (kể cả chưa kéo)
    public void OnPointerDown(PointerEventData eventData)
    {
        var data = palette.Peek(slotIndex);
        if (data == null) return;      // slot rỗng thì bỏ qua
        _cg.alpha = 0f;                // ẩn slot gốc
    }

    // Nếu chỉ click rồi thả (không kéo), hiện lại
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging)
            _cg.alpha = 1f;            // hiện lại nếu không có thao tác kéo
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _draggingData = palette.Peek(slotIndex);
        if (_draggingData == null) return;

        _isDragging = true;
        _cg.blocksRaycasts = false;    // cho raycast xuyên qua slot gốc

        // tạo ghost
        _ghostView = Instantiate(ghostPrefab, dragRoot);
        _ghostRT = _ghostView.GetComponent<RectTransform>();
        _ghostView.cellSize = ghostCellSize;
        _ghostView.spacing = ghostSpacing;
        _ghostView.Render(_draggingData);
        SetGraphicsRaycastTarget(_ghostView.gameObject, false); // ghost không chặn raycast
        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT == null) return;
        UpdateGhostPosition(eventData);

        if (_draggingData == null) { board.ClearPreview(); return; }

        // Ray vào grid
        if (!gridInput.TryGetCell(eventData.position, out var targetCell))
        {
            board.ClearPreview();
            return;
        }

        // neo theo bounds min
        var (minR, minC, _, _) = _draggingData.GetBounds();
        int anchorRow = targetCell.Row - minR;
        int anchorCol = targetCell.Col - minC;

        // chỉ highlight khi là nước đi hợp lệ
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

        // hiện lại slot gốc
        _cg.blocksRaycasts = true;
        _cg.alpha = 1f;

        if (_ghostRT != null) Destroy(_ghostRT.gameObject);
        _ghostRT = null; _ghostView = null;

        // luôn clear preview khi thả
        board.ClearPreview();

        if (_draggingData == null) return;

        if (!gridInput.TryGetCell(eventData.position, out var targetCell))
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


    private void UpdateGhostPosition(PointerEventData ev)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, ev.position, ev.pressEventCamera, out var local);
        _ghostRT.anchoredPosition = local;
    }

    private static void SetGraphicsRaycastTarget(GameObject root, bool value)
    {
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = value;
    }
}
