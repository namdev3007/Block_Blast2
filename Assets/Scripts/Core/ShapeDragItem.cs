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
    public RectTransform dragRoot;   // RectTransform của Canvas/top-level
    public GridView gridView;
    public BoardRuntime board;
    public GridInput gridInput;
    public SkinProvider skinProvider;        // NEW: skin

    [Header("Ghost visuals")]
    public ShapeItemView ghostPrefab;
    public Vector2 ghostCellSize = new Vector2(64, 64);
    public Vector2 ghostSpacing = new Vector2(4, 4);

    [Header("Offsets")]
    public Vector2 ghostOffsetLocal = Vector2.zero;
    public bool useGrabOffset = true;

    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ShapeItemView _ghostView;
    private ShapeData _draggingData;
    private bool _isDragging;
    private Vector2 _grabOffsetLocal;
    private Camera _cam;

    // NEW: variant được chọn cho piece này
    private int _variantIndex;

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
        var w0 = dragRoot.TransformPoint(Vector3.zero);
        var w1 = dragRoot.TransformPoint((Vector3)localDelta);
        var s0 = RectTransformUtility.WorldToScreenPoint(_cam, w0);
        var s1 = RectTransformUtility.WorldToScreenPoint(_cam, w1);
        return s1 - s0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var data = palette.Peek(slotIndex);
        if (data == null) return;
        _cg.alpha = 0f; // ẩn slot gốc
    }

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
        _cam = eventData.pressEventCamera;

        // LẤY variant từ PALETTE để đồng bộ slot/ghost/placed
        _variantIndex = palette.PeekVariant(slotIndex);

        var spriteForGhost = (skinProvider != null)
            ? skinProvider.GetTileSprite(_variantIndex)
            : (board != null ? board.placedSpriteFallback : null);

        _ghostView = Instantiate(ghostPrefab, dragRoot);
        _ghostRT = _ghostView.GetComponent<RectTransform>();
        _ghostView.cellSize = ghostCellSize;
        _ghostView.spacing = ghostSpacing;
        _ghostView.Render(_draggingData, spriteForGhost);
        SetGraphicsRaycastTarget(_ghostView.gameObject, false);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var startLocal);
        _ghostRT.anchoredPosition = startLocal + ghostOffsetLocal;
        _grabOffsetLocal = _ghostRT.anchoredPosition - startLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var local);
        _ghostRT.anchoredPosition = local + TotalLocalOffset;

        if (_draggingData == null) { board.ClearPreview(); return; }

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
            board.ShowPreviewVariant(_draggingData, anchorRow, anchorCol, _variantIndex);
            board.ShowLineCompletionPreviewVariant(_draggingData, anchorRow, anchorCol, _variantIndex);
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
            board.PaintPlacedVariant(_draggingData, anchorRow, anchorCol, _variantIndex);
            board.ResolveAndClearFullLinesAfterPlacementVariant(_draggingData, anchorRow, anchorCol, _variantIndex);
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
