using System.Collections.Generic;
using UnityEngine;

public class BoardState
{
    public readonly int Width;
    public readonly int Height;
    private readonly bool[] _occupied; // r*Width + c

    public BoardState(int width, int height)
    {
        Width = width;
        Height = height;
        _occupied = new bool[width * height];
    }

    public bool IsOccupied(int r, int c)
    {
        if (r < 0 || r >= Height || c < 0 || c >= Width) return false;
        return _occupied[r * Width + c];
    }

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

    // ====== Line checks ======
    public bool IsRowFull(int r)
    {
        for (int c = 0; c < Width; c++)
            if (!_occupied[r * Width + c]) return false;
        return true;
    }

    public bool IsColFull(int c)
    {
        for (int r = 0; r < Height; r++)
            if (!_occupied[r * Width + c]) return false;
        return true;
    }

    public List<int> GetFullRows()
    {
        var rows = new List<int>();
        for (int r = 0; r < Height; r++)
            if (IsRowFull(r)) rows.Add(r);
        return rows;
    }

    public List<int> GetFullCols()
    {
        var cols = new List<int>();
        for (int c = 0; c < Width; c++)
            if (IsColFull(c)) cols.Add(c);
        return cols;
    }

    public void ClearRow(int r)
    {
        for (int c = 0; c < Width; c++)
            _occupied[r * Width + c] = false;
    }

    public void ClearCol(int c)
    {
        for (int r = 0; r < Height; r++)
            _occupied[r * Width + c] = false;
    }

    public void ClearLines(IEnumerable<int> fullRows, IEnumerable<int> fullCols)
    {
        if (fullRows != null)
            foreach (var r in fullRows) ClearRow(r);
        if (fullCols != null)
            foreach (var c in fullCols) ClearCol(c);
    }

    // ✅ API set hợp lệ cho seed
    public void SetOccupied(int r, int c, bool value)
    {
        if (r < 0 || r >= Height || c < 0 || c >= Width) return;
        _occupied[r * Width + c] = value;
    }
}
