using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShapeItemView : MonoBehaviour
{
    [Header("UI")]
    public GameObject cellPrefab;
    public Vector2 cellSize = new Vector2(64, 64);
    public Vector2 spacing = new Vector2(4, 4);
    public ShapeData Current { get; private set; }

    private GridLayoutGroup _layout;
    private readonly List<Image> _cells = new();

    private void Awake()
    {
        _layout = GetComponent<GridLayoutGroup>();
        if (_layout == null) _layout = gameObject.AddComponent<GridLayoutGroup>();
        _layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    }

    // NEW: có tham số displaySprite (tùy chọn)
    public void Render(ShapeData data, Sprite displaySprite = null)
    {
        Current = data;
        Clear();
        if (data == null || data.board == null) return;

        _layout.cellSize = cellSize;
        _layout.spacing = spacing;
        _layout.constraintCount = data.columns;

        int total = data.columns * data.rows;
        for (int i = 0; i < total; i++)
        {
            var go = Instantiate(cellPrefab, transform);
            var img = go.GetComponent<Image>();
            _cells.Add(img);
        }

        for (int r = 0; r < data.rows; r++)
        {
            for (int c = 0; c < data.columns; c++)
            {
                int idx = r * data.columns + c;
                bool filled = data.board[r].column[c];
                var img = _cells[idx];
                img.enabled = filled;
                if (filled && displaySprite != null)
                    img.sprite = displaySprite;
            }
        }

        gameObject.name = $"ShapeItem_{data.rows}x{data.columns}";
    }

    public void Clear()
    {
        Current = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        _cells.Clear();
    }
}
