using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class ShapeDragItem : MonoBehaviour,
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
    public Vector2 ghostOffsetLocal = Vector2.zero;
    public bool useGrabOffset = true;

    [Header("Press Lift")]
    public float pressLiftY = 24f;

    [Header("Combo Popup")]
    public PopupManager Popup;
    public Vector2 comboOffset = new Vector2(0f, 36f);
    public Vector2 pointsOffset = new Vector2(0f, -6f);

    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ShapeItemView _ghostView;
    private ShapeData _draggingData;
    private bool _isDragging;
    private Vector2 _grabOffsetLocal;
    private Camera _cam;
    private int _variantIndex;
    private Vector2 _anchorFixLocal;
    private Vector2 _runtimeExtraOffset = Vector2.zero;
    private Vector2 _cursorOffsetScreen;
    private bool _gameOverTriggered = false;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
    }

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

    private void RecomputeCursorOffsetScreen()
    {
        _cursorOffsetScreen = LocalToScreenDelta(TotalLocalOffset + _anchorFixLocal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var data = palette.Peek(slotIndex);
        if (data == null || dragRoot == null || ghostPrefab == null) return;

        _draggingData = data;
        _cam = eventData.pressEventCamera;

        _variantIndex = palette.PeekVariant(slotIndex);
        var spriteForGhost = skinProvider ? skinProvider.GetTileSprite(_variantIndex) : board.placedSpriteFallback;

        _anchorFixLocal = ComputeTopLeftFixLocal(_draggingData);
        _runtimeExtraOffset = new Vector2(0f, pressLiftY);
        _grabOffsetLocal = ghostOffsetLocal;

        CreateGhostIfNeeded(spriteForGhost);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var startLocal);

        AudioManager.Instance?.PlayPickup();

        _ghostRT.anchoredPosition = startLocal + _grabOffsetLocal + _runtimeExtraOffset;
        RecomputeCursorOffsetScreen();
        _cg.alpha = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            if (_ghostRT) Destroy(_ghostRT.gameObject);
            _ghostRT = null;
            _ghostView = null;
            _draggingData = null;
            _runtimeExtraOffset = Vector2.zero;
            _cg.alpha = 1f;
        }
    }

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

        if (_ghostRT == null)
        {
            _anchorFixLocal = ComputeTopLeftFixLocal(_draggingData);
            _grabOffsetLocal = ghostOffsetLocal;
            var spriteForGhost = skinProvider ? skinProvider.GetTileSprite(_variantIndex) : board.placedSpriteFallback;
            CreateGhostIfNeeded(spriteForGhost);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragRoot, eventData.position, _cam, out var startLocal);
            _ghostRT.anchoredPosition = startLocal + _grabOffsetLocal + _runtimeExtraOffset;
        }

        RecomputeCursorOffsetScreen();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragRoot, eventData.position, _cam, out var local);

        _ghostRT.anchoredPosition = local + TotalLocalOffset;

        if (_draggingData == null) { board.ClearPreview(); return; }

        var screenPosWithOffset = eventData.position + _cursorOffsetScreen;

        if (!TryPickCellWithFallback(screenPosWithOffset, out var targetCell))
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
            TryGameOverAfterDrop();
            return;
        }

        var screenPosWithOffset = eventData.position + _cursorOffsetScreen;

        if (!TryPickCellWithFallback(screenPosWithOffset, out var targetCell))
        {
            CleanupAfterDrop();
            TryGameOverAfterDrop();
            return;
        }

        var bounds = _draggingData.GetBounds();
        int anchorRow = targetCell.Row - bounds.minR;
        int anchorCol = targetCell.Col - bounds.minC;

        if (!board.State.CanPlace(_draggingData, anchorRow, anchorCol))
        {
            CleanupAfterDrop();
            TryGameOverAfterDrop();
            return;
        }

        board.State.Place(_draggingData, anchorRow, anchorCol, _variantIndex);
        board.PaintPlacedVariant(_draggingData, anchorRow, anchorCol, _variantIndex);

        AudioManager.Instance?.PlayDrop();

        int linesCleared = board.ResolveAndClearFullLinesAfterPlacementVariantAndGetCount(
            _draggingData, anchorRow, anchorCol, _variantIndex);

        ScoreResult sr = default;
        if (board.score != null)
        {
            int blockCells = CountFilledCells(_draggingData);
            sr = board.score.OnPiecePlaced(blockCells, linesCleared);
        }

        Vector2 pos;
        if (!ComputePlacedCentroidScreen(_draggingData, anchorRow, anchorCol, _cam, out pos))
            pos = eventData.position;

        Vector2 comboOff = comboOffset;
        Vector2 pointsOff = pointsOffset;
        if (PlacementTouchesLeftEdge(_draggingData, anchorRow, anchorCol))
        {
            float pushIn = 200f;
            comboOff.x = pushIn;
            pointsOff.x += pushIn;
        }

        int comboThisTurn = Mathf.Max(1, sr.comboBefore);
        bool willShowCombo = (Popup != null && linesCleared > 0 && comboThisTurn >= Popup.minComboToShow);

        if (Popup != null && linesCleared > 0)
        {
            if (willShowCombo)
                Popup.ShowComboAtScreenPoint(comboThisTurn, pos, _cam, comboOff);

            if (sr.linePointsFinal > 0)
            {
                if (willShowCombo)
                    StartCoroutine(ShowPointsAndPraiseLater(sr.linePointsFinal, linesCleared, pos, _cam, pointsOff, 0.8f));
                else
                {
                    Popup.ShowPointsAtScreenPoint(sr.linePointsFinal, pos, _cam, pointsOff);
                    if (linesCleared >= 2)
                        Popup.ShowPraiseForLines(linesCleared, pos, _cam);
                }
            }
        }

        if (board != null && board.IsBoardCompletelyEmpty())
        {
            if (board.score != null)
                board.score.AwardBoardClearBonus();

            AudioManager.Instance?.PlayUnbelievable();

            float delayForBonus = willShowCombo ? 0.8f : 0f;
            if (Popup != null)
            {
                Popup.ShowUnbelievable(pos, _cam);
                Vector2 bonusPointsOffset = pointsOff + new Vector2(0f, -18f);
                Popup.ShowBoardClearBonus(pos, _cam, bonusPointsOffset, delayForBonus);
            }
        }

        palette.Consume(slotIndex);
        CleanupAfterDrop();
        TryGameOverAfterDrop();
        StartCoroutine(CoAutosaveNextFrame());
    }

    private IEnumerator CoAutosaveNextFrame()
    {
        yield return null;
        GameManager.Instance?.SaveSnapshotNow();
    }

    private IEnumerator ShowPointsAndPraiseLater(int points, int linesCleared, Vector2 pos, Camera cam, Vector2 offset, float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Popup != null)
        {
            Popup.ShowPointsAtScreenPoint(points, pos, cam, offset);
            if (linesCleared >= 2)
                Popup.ShowPraiseForLines(linesCleared, pos, cam);
        }
    }

    private void TryGameOverAfterDrop()
    {
        if (_gameOverTriggered || board == null || palette == null) return;
        StartCoroutine(CoCheckGameOverNextFrame());
    }

    private IEnumerator CoCheckGameOverNextFrame()
    {
        yield return null;

        int slotCount = 0;
        if (palette.slots != null) slotCount = palette.slots.Count;
        else slotCount = 3;

        bool anyMove = false;
        for (int i = 0; i < slotCount; i++)
        {
            var s = palette.Peek(i);
            if (s == null) continue;
            if (board.CanPlaceAnywhere(s))
            {
                anyMove = true;
                break;
            }
        }

        if (!anyMove)
        {
            _gameOverTriggered = true;
            GameManager.Instance?.OnNoMovesLeft();
        }
    }

    private bool TryPickCellWithFallback(Vector2 screenPos, out GridSquareView targetCell)
    {
        if (gridInput.TryGetCell(screenPos, out targetCell))
            return true;
        return gridView.TryGetNearestCellByScreenPoint(screenPos, _cam, out targetCell, out _, out _);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState s)
    {
        if (s == GameState.Playing)
            _gameOverTriggered = false;
    }

    private void CleanupAfterDrop()
    {
        if (_ghostRT) Destroy(_ghostRT.gameObject);
        _ghostRT = null; _ghostView = null;
        _draggingData = null;
        _runtimeExtraOffset = Vector2.zero;
        _cg.alpha = 1f;
    }

    private void CreateGhostIfNeeded(Sprite spriteForGhost)
    {
        if (_ghostRT != null) return;
        _ghostView = Instantiate(ghostPrefab, dragRoot);
        _ghostRT = _ghostView.GetComponent<RectTransform>();
        _ghostView.cellSize = ghostCellSize;
        _ghostView.spacing = ghostSpacing;
        _ghostView.Render(_draggingData, spriteForGhost);
        SetGraphicsRaycastTarget(_ghostView.gameObject, false);
    }

    private static void SetGraphicsRaycastTarget(GameObject root, bool value)
    {
        foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = value;
    }

    private bool ComputePlacedCentroidScreen(ShapeData s, int anchorRow, int anchorCol, Camera cam, out Vector2 screen)
    {
        screen = default;
        if (s == null || gridView == null) return false;

        Vector2 sum = Vector2.zero;
        int n = 0;

        foreach (var cell in s.GetFilledCells())
        {
            int rr = anchorRow + cell.x;
            int cc = anchorCol + cell.y;

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

    private bool PlacementTouchesLeftEdge(ShapeData s, int anchorRow, int anchorCol)
    {
        if (s == null) return false;
        foreach (var cell in s.GetFilledCells())
        {
            int cc = anchorCol + cell.y;
            if (cc == 0) return true;
        }
        return false;
    }

    private static int CountFilledCells(ShapeData s)
    {
        if (s == null || s.board == null) return 0;
        int n = 0;
        for (int r = 0; r < s.rows; r++)
            for (int c = 0; c < s.columns; c++)
                if (s.board[r].column[c]) n++;
        return n;
    }
}
