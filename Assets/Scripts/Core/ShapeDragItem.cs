using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    public SkinProvider skinProvider;

    [Header("Ghost visuals")]
    public ShapeItemView ghostPrefab;
    public Vector2 ghostCellSize = new Vector2(64, 64);
    public Vector2 ghostSpacing = new Vector2(4, 4);

    [Header("Offsets")]
    public Vector2 ghostOffsetLocal = Vector2.zero; // offset cơ sở dưới con trỏ
    public bool useGrabOffset = true;

    [Header("Press Lift")]
    [Tooltip("Độ nâng UI ngay khi nhấn (pixel).")]
    public float pressLiftY = 24f;

    [Header("Combo Popup")]
    public ComboPopupManager comboPopup;                 // gán trong Inspector
    public Vector2 comboOffset = new Vector2(0f, 36f);

    // runtime
    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ShapeItemView _ghostView;
    private ShapeData _draggingData;
    private bool _isDragging;
    private Vector2 _grabOffsetLocal;       // CHỈ chứa ghostOffsetLocal
    private Camera _cam;
    private int _variantIndex;

    // offset bù raycast khi layout ở Middle Center
    private Vector2 _anchorFixLocal;

    // extra offset để “bay lên” khi nhấn
    private Vector2 _runtimeExtraOffset = Vector2.zero;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
    }

    // Tổng offset = grab (cơ sở) + lift (runtime). KHÔNG cộng ghostOffsetLocal lần 2.
    private Vector2 TotalLocalOffset =>
        (useGrabOffset ? _grabOffsetLocal : Vector2.zero) + _runtimeExtraOffset;

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

        _draggingData = data;
        _cam = eventData.pressEventCamera; // null nếu Canvas Overlay

        // chọn variant đúng với slot
        _variantIndex = palette.PeekVariant(slotIndex);
        var spriteForGhost = skinProvider ? skinProvider.GetTileSprite(_variantIndex) : board.placedSpriteFallback;

        // tính fix anchor & bật lift
        _anchorFixLocal = ComputeTopLeftFixLocal(_draggingData);
        _runtimeExtraOffset = new Vector2(0f, pressLiftY);

        // set grabOffset = ghostOffsetLocal (không cộng lift vào grab!)
        _grabOffsetLocal = ghostOffsetLocal;

        // tạo ghost NGAY lúc nhấn
        _ghostView = Instantiate(ghostPrefab, dragRoot);
        _ghostRT = _ghostView.GetComponent<RectTransform>();
        _ghostView.cellSize = ghostCellSize;
        _ghostView.spacing = ghostSpacing;
        _ghostView.Render(_draggingData, spriteForGhost);
        SetGraphicsRaycastTarget(_ghostView.gameObject, false);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var startLocal);

        // đặt ghost = vị trí con trỏ + grab + lift
        _ghostRT.anchoredPosition = startLocal + _grabOffsetLocal + _runtimeExtraOffset;

        // Ẩn slot gốc ngay khi nhấn (đã có ghost)
        _cg.alpha = 0f;
        // blocksRaycasts vẫn true, sẽ tắt khi BeginDrag
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            // chỉ click, không kéo -> huỷ ghost, trả lại slot
            if (_ghostRT) Destroy(_ghostRT.gameObject);
            _ghostRT = null;
            _ghostView = null;
            _draggingData = null;
            _runtimeExtraOffset = Vector2.zero;
            _cg.alpha = 1f;
        }
    }

    // Tính offset từ tâm layout -> tâm ô (minR,minC)
    private Vector2 ComputeTopLeftFixLocal(ShapeData s)
    {
        var (minR, minC, _, _) = s.GetBounds();

        float W = s.columns * ghostCellSize.x + (s.columns - 1) * ghostSpacing.x;
        float H = s.rows * ghostCellSize.y + (s.rows - 1) * ghostSpacing.y;

        float stepX = ghostCellSize.x + ghostSpacing.x;
        float stepY = ghostCellSize.y + ghostSpacing.y;

        float x = -W * 0.5f + (ghostCellSize.x * 0.5f) + minC * stepX;
        float y = H * 0.5f - (ghostCellSize.y * 0.5f) - minR * stepY;

        return new Vector2(x, y);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_draggingData == null) return;

        _isDragging = true;
        _cg.blocksRaycasts = false;

        // Edge case: nếu ghost chưa kịp tạo, tạo tại đây
        if (_ghostRT == null)
        {
            _anchorFixLocal = ComputeTopLeftFixLocal(_draggingData);
            _grabOffsetLocal = ghostOffsetLocal;

            _ghostView = Instantiate(ghostPrefab, dragRoot);
            _ghostRT = _ghostView.GetComponent<RectTransform>();
            _ghostView.cellSize = ghostCellSize;
            _ghostView.spacing = ghostSpacing;
            _ghostView.Render(_draggingData, skinProvider ? skinProvider.GetTileSprite(_variantIndex) : board.placedSpriteFallback);
            SetGraphicsRaycastTarget(_ghostView.gameObject, false);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragRoot, eventData.position, _cam, out var startLocal);
            _ghostRT.anchoredPosition = startLocal + _grabOffsetLocal + _runtimeExtraOffset;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var local);

        // kéo = local + (grab + lift)
        _ghostRT.anchoredPosition = local + TotalLocalOffset;

        if (_draggingData == null) { board.ClearPreview(); return; }

        // BÙ raycast bằng anchorFix (cùng TotalLocalOffset để không lệch)
        var screenPosWithOffset = eventData.position +
                                  LocalToScreenDelta(TotalLocalOffset + _anchorFixLocal);

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

        board.ClearPreview();
        if (_draggingData == null)
        {
            CleanupAfterDrop();
            return;
        }

        // Bù offset (grab + lift + anchorFix) cho raycast
        var screenPosWithOffset = eventData.position +
                                  LocalToScreenDelta(TotalLocalOffset + _anchorFixLocal);

        if (!gridInput.TryGetCell(screenPosWithOffset, out var targetCell))
        {
            CleanupAfterDrop();
            return;
        }

        var (minR, minC, _, _) = _draggingData.GetBounds();
        int anchorRow = targetCell.Row - minR;
        int anchorCol = targetCell.Col - minC;

        if (!board.State.CanPlace(_draggingData, anchorRow, anchorCol))
        {
            CleanupAfterDrop();
            return;
        }

        // 1) Place vào state + paint
        board.State.Place(_draggingData, anchorRow, anchorCol);
        board.PaintPlacedVariant(_draggingData, anchorRow, anchorCol, _variantIndex);

        // 2) Clear lines
        int linesCleared = board.ResolveAndClearFullLinesAfterPlacementVariantAndGetCount(
            _draggingData, anchorRow, anchorCol, _variantIndex);

        // 3) Score
        ScoreResult sr = default;
        if (board.score != null)
        {
            int blockCells = _draggingData.GetFilledCells().Count();
            sr = board.score.OnPiecePlaced(blockCells, linesCleared);
        }

        // 3b) Combo popup (có lọc min combo trong manager)
        if (linesCleared > 0 && comboPopup != null)
        {
            int comboThisTurn = Mathf.Max(1, sr.comboBefore);

            // offset mép trái: đẩy vào 150 px nếu chạm cột 0
            Vector2 off = comboOffset;
            if (PlacementTouchesLeftEdge(_draggingData, anchorRow, anchorCol))
                off.x = 200f;

            if (ComputePlacedCentroidScreen(_draggingData, anchorRow, anchorCol, _cam, out var centerScreen))
                comboPopup.ShowComboAtScreenPoint(comboThisTurn, centerScreen, _cam, off);
            else
                comboPopup.ShowComboAtScreenPoint(comboThisTurn, eventData.position, _cam, off);
        }

        // 4) Tiêu thụ slot & dọn
        palette.Consume(slotIndex);
        CleanupAfterDrop();
    }

    private void CleanupAfterDrop()
    {
        if (_ghostRT) Destroy(_ghostRT.gameObject);
        _ghostRT = null; _ghostView = null;
        _draggingData = null;
        _runtimeExtraOffset = Vector2.zero;
    }

    // === Helpers ===

    private static void SetGraphicsRaycastTarget(GameObject root, bool value)
    {
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = value;
    }

    // Tính tâm cụm ô vừa đặt -> ScreenPoint (GetFilledCells() trả Vector2Int)
    private bool ComputePlacedCentroidScreen(ShapeData s, int anchorRow, int anchorCol, Camera cam, out Vector2 screen)
    {
        screen = default;
        if (s == null || gridView == null) return false;

        Vector2 sum = Vector2.zero;
        int n = 0;

        foreach (var cell in s.GetFilledCells()) // IEnumerable<Vector2Int>
        {
            int rr = anchorRow + cell.x; // x = row
            int cc = anchorCol + cell.y; // y = col

            if (gridView.TryGetSquareCenterScreen(rr, cc, cam, out var sp))
            {
                sum += sp;
                n++;
            }
        }

        if (n <= 0) return false;
        screen = sum / n;
        return true;
    }

    // Có ô nào chạm cột ngoài cùng bên trái?
    private bool PlacementTouchesLeftEdge(ShapeData s, int anchorRow, int anchorCol)
    {
        if (s == null) return false;
        foreach (var cell in s.GetFilledCells()) // Vector2Int
        {
            int cc = anchorCol + cell.y;
            if (cc == 0) return true;
        }
        return false;
    }
}
