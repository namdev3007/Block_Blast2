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
    public RectOffset padding;                  
    public bool centerGrid = true;           

    [Header("Prefabs/Refs")]
    public GameObject gridSquarePrefab;   
    public RectTransform contentRoot;       

    // Data
    private readonly List<GridSquareView> _cells = new();
    private readonly Dictionary<GridRegion, List<GridSquareView>> _byRegion = new()
    {
        { GridRegion.TopLeft,     new List<GridSquareView>() },
        { GridRegion.TopRight,    new List<GridSquareView>() },
        { GridRegion.BottomLeft,  new List<GridSquareView>() },
        { GridRegion.BottomRight, new List<GridSquareView>() },
    };

    private GridLayoutGroup _layout;
    private BoardModel _model;

    private void Start()
    {
        Build();
    }
    public IReadOnlyList<GridSquareView> Cells => _cells;
    public IReadOnlyDictionary<GridRegion, List<GridSquareView>> CellsByRegion => _byRegion;

    void Awake()
    {
        EnsureLayout();
    }

    void Reset()
    {
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        var root = contentRoot ? contentRoot : GetComponent<RectTransform>();
        if (!_layout)
        {
            _layout = root.gameObject.GetComponent<GridLayoutGroup>();
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

    public void Build()
    {
        EnsureLayout();

        _model = new BoardModel(columns, rows);

        // Clear cũ
        foreach (Transform child in (contentRoot ? contentRoot : transform))
            DestroyImmediate(child.gameObject);

        _cells.Clear();
        foreach (var kv in _byRegion) kv.Value.Clear();

        // Sinh mới
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                var parent = contentRoot ? contentRoot : transform as RectTransform;
                var go = Instantiate(gridSquarePrefab, parent);
                go.name = $"Cell_{r}_{c}";

                var view = go.GetComponent<GridSquareView>();
                if (!view)
                    view = go.AddComponent<GridSquareView>(); // đảm bảo có view

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
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        foreach (var r in results)
        {
            var v = r.gameObject.GetComponentInParent<GridSquareView>();
            if (v != null)
            {
                cell = v; row = v.Row; col = v.Col;
                return true;
            }
        }
        return false;
    }
    private static GridRegion ComputeRegion(int row, int col, int totalRows, int totalCols)
    {
        // Chia làm 2 nửa theo hàng/ cột
        bool isTop = row < totalRows / 2;
        bool isLeft = col < totalCols / 2;

        if (isTop && isLeft) return GridRegion.TopLeft;
        if (isTop && !isLeft) return GridRegion.TopRight;
        if (!isTop && isLeft) return GridRegion.BottomLeft;
        return GridRegion.BottomRight;
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

        var cell = GetCell(row, col);
        if (cell == null) return false;

        var rt = cell.GetComponent<RectTransform>();
        if (rt == null) return false;

        // Tâm local của rect
        Vector3 localCenter = (Vector3)rt.rect.center;
        // Đổi sang world
        Vector3 world = rt.TransformPoint(localCenter);
        // World -> screen (uiCamera có thể null nếu Canvas Overlay)
        screen = RectTransformUtility.WorldToScreenPoint(uiCamera, world);
        return true;
    }
}
