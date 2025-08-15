using UnityEngine;

public class BoardState
{
    public readonly int Width;
    public readonly int Height;
    private readonly bool[] _occupied;

    public BoardState(int width, int height)
    {
        Width = width; Height = height;
        _occupied = new bool[width * height];
    }

    public bool IsOccupied(int r, int c) => _occupied[r * Width + c];

    public bool CanPlace(ShapeData shape, int anchorRow, int anchorCol)
    {
        if (shape == null) return false;
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            if (r < 0 || c < 0 || r >= Height || c >= Width) return false;
            if (IsOccupied(r, c)) return false;
        }
        return true;
    }

    public void Place(ShapeData shape, int anchorRow, int anchorCol)
    {
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            _occupied[r * Width + c] = true;
        }
    }
}
