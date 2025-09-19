using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class GridView : MonoBehaviour
{
    [Header("Grid size")]
    [Min(1)] public int columns = 8;
    [Min(1)] public int rows = 8;

    [Header("Layout (UI Grid)")]
    public Vector2 cellSize = new Vector2(96, 96);
    public Vector2 spacing = new Vector2(6, 6);
    public RectOffset padding;                 // có thể để trống → sẽ dùng (0,0,0,0)
    public bool centerGrid = true;

    [Header("Prefabs/Refs")]
    public GameObject gridSquarePrefab;
    public RectTransform contentRoot;          // để trống → dùng chính RectTransform của component

    // ===== Runtime data =====
    private readonly List<GridSquareView> _cells = new();
    private readonly Dictionary<GridRegion, List<GridSquareView>> _byRegion = new()
    {
        { GridRegion.TopLeft,     new List<GridSquareView>() },
        { GridRegion.TopRight,    new List<GridSquareView>() },
        { GridRegion.BottomLeft,  new List<GridSquareView>() },
        { GridRegion.BottomRight, new List<GridSquareView>() },
    };

    private GridLayoutGroup _layout;

    // Reuse list để tránh GC mỗi lần raycast
    private static readonly List<RaycastResult> s_RaycastResults = new(32);

    private RectTransform RootRT => contentRoot ? contentRoot : (RectTransform)transform;

    public IReadOnlyList<GridSquareView> Cells => _cells;
    public IReadOnlyDictionary<GridRegion, List<GridSquareView>> CellsByRegion => _byRegion;

    private void Awake()
    {
        EnsureLayout();
    }

    private void Start()
    {
        Build();
    }

    private void Reset()
    {
        EnsureLayout();
    }

    private void OnValidate()
    {
        // Clamp & cập nhật layout ngay trên Inspector
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        var root = RootRT;
        if (!_layout)
        {
            _layout = root.GetComponent<GridLayoutGroup>();
            if (!_layout) _layout = root.gameObject.AddComponent<GridLayoutGroup>();
        }

        _layout.cellSize = cellSize;
        _layout.spacing = spacing;
        _layout.padding = padding != null ? padding : new RectOffset(0, 0, 0, 0);
        _layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _layout.startCorner = GridLayoutGroup.Corner.UpperLeft;  // (0,0) ở góc trên trái
        _layout.childAlignment = centerGrid ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
        _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _layout.constraintCount = columns;
    }

    [ContextMenu("Rebuild")]
    public void Build()
    {
        EnsureLayout();

        if (!gridSquarePrefab)
        {
            Debug.LogError($"{name}: gridSquarePrefab chưa gán!");
            return;
        }

        // Clear cũ (PlayMode -> Destroy, Editor edit-time -> DestroyImmediate)
        var parent = RootRT;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(parent.GetChild(i).gameObject); }
            else { Destroy(parent.GetChild(i).gameObject); }
#else
            Destroy(parent.GetChild(i).gameObject);
#endif
        }

        _cells.Clear();
        foreach (var kv in _byRegion) kv.Value.Clear();

        _cells.Capacity = rows * columns;

        // Sinh mới
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var go = Instantiate(gridSquarePrefab, parent);
                go.name = $"Cell_{r}_{c}";

                // Đảm bảo có RectTransform
                if (!go.TryGetComponent<RectTransform>(out _))
                    go.AddComponent<RectTransform>();

                var view = go.GetComponent<GridSquareView>();
                if (!view) view = go.AddComponent<GridSquareView>();

                var region = ComputeRegion(r, c, rows, columns);
                view.Init(idx, r, c, region);

                _cells.Add(view);
                _byRegion[region].Add(view);

                idx++;
            }
        }
    }

    public GridSquareView GetCell(int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return _cells[row * columns + col];
    }

    public bool TryGetCellAtScreenPoint(
        Vector2 screenPoint, Camera uiCamera,
        out GridSquareView cell, out int row, out int col)
    {
        cell = null; row = col = -1;

        var raycaster = GetComponentInParent<GraphicRaycaster>();
        if (raycaster == null || EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = screenPoint };
        s_RaycastResults.Clear();
        raycaster.Raycast(ped, s_RaycastResults);

        for (int i = 0; i < s_RaycastResults.Count; i++)
        {
            var v = s_RaycastResults[i].gameObject.GetComponentInParent<GridSquareView>();
            if (v != null)
            {
                cell = v; row = v.Row; col = v.Col;
                return true;
            }
        }
        return false;
    }

    public bool TryGetSquareCenterWorld(int row, int col, out Vector3 world)
    {
        world = default;
        var cell = GetCell(row, col);
        if (cell == null) return false;

        var rt = cell.GetComponent<RectTransform>();
        if (rt == null) return false;

        world = rt.TransformPoint((Vector3)rt.rect.center);
        return true;
    }

    public bool TryGetSquareCenterScreen(int row, int col, Camera uiCamera, out Vector2 screen)
    {
        screen = default;
        if (!TryGetSquareCenterWorld(row, col, out var world)) return false;

        // World -> screen (uiCamera có thể null nếu Canvas Overlay)
        screen = RectTransformUtility.WorldToScreenPoint(uiCamera, world);
        return true;
    }

    public bool TryGetNearestCellByScreenPoint(
        Vector2 screenPoint, Camera uiCamera,
        out GridSquareView cell, out int row, out int col)
    {
        cell = null; row = col = -1;

        float bestSqr = float.PositiveInfinity;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (TryGetSquareCenterScreen(r, c, uiCamera, out var centerScreen))
                {
                    float d2 = (centerScreen - screenPoint).sqrMagnitude;
                    if (d2 < bestSqr)
                    {
                        bestSqr = d2;
                        cell = _cells[r * columns + c];
                        row = r; col = c;
                    }
                }
            }
        }
        return cell != null;
    }

    private static GridRegion ComputeRegion(int row, int col, int totalRows, int totalCols)
    {
        bool isTop = row < totalRows / 2;
        bool isLeft = col < totalCols / 2;

        if (isTop && isLeft) return GridRegion.TopLeft;
        if (isTop && !isLeft) return GridRegion.TopRight;
        if (!isTop && isLeft) return GridRegion.BottomLeft;
        return GridRegion.BottomRight;
    }
}
