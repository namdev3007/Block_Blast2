using System.Collections.Generic;
using UnityEngine;

public class BoardState
{
    public readonly int Width;
    public readonly int Height;

    private readonly bool[] _occupied;   // r*Width + c
    private readonly int[] _variant;    // song song với occupied; -1 nếu trống

    public BoardState(int width, int height)
    {
        Width = width;
        Height = height;
        _occupied = new bool[width * height];
        _variant = new int[width * height];
        for (int i = 0; i < _variant.Length; i++) _variant[i] = -1;
    }

    private int Idx(int r, int c) => r * Width + c;

    public bool IsOccupied(int r, int c)
    {
        if (r < 0 || r >= Height || c < 0 || c >= Width) return false;
        return _occupied[Idx(r, c)];
    }

    public int GetVariant(int r, int c)
    {
        if (r < 0 || r >= Height || c < 0 || c >= Width) return -1;
        return _variant[Idx(r, c)];
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

    /// <summary>Đặt và ghi variant cho từng ô.</summary>
    public void Place(ShapeData shape, int anchorRow, int anchorCol, int variantIndex)
    {
        foreach (var cell in shape.GetFilledCells())
        {
            int r = anchorRow + cell.x;
            int c = anchorCol + cell.y;
            int i = Idx(r, c);
            _occupied[i] = true;
            _variant[i] = variantIndex;
        }
    }

    // ====== Line checks ======
    public bool IsRowFull(int r)
    {
        for (int c = 0; c < Width; c++)
            if (!_occupied[Idx(r, c)]) return false;
        return true;
    }

    public bool IsColFull(int c)
    {
        for (int r = 0; r < Height; r++)
            if (!_occupied[Idx(r, c)]) return false;
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
        {
            int i = Idx(r, c);
            _occupied[i] = false;
            _variant[i] = -1;
        }
    }

    public void ClearCol(int c)
    {
        for (int r = 0; r < Height; r++)
        {
            int i = Idx(r, c);
            _occupied[i] = false;
            _variant[i] = -1;
        }
    }

    public void ClearLines(IEnumerable<int> fullRows, IEnumerable<int> fullCols)
    {
        if (fullRows != null)
            foreach (var r in fullRows) ClearRow(r);
        if (fullCols != null)
            foreach (var c in fullCols) ClearCol(c);
    }

    // === Seed & serialize helpers ===
    public void SetOccupied(int r, int c, bool value, int variantIndex = -1)
    {
        if (r < 0 || r >= Height || c < 0 || c >= Width) return;
        int i = Idx(r, c);
        _occupied[i] = value;
        _variant[i] = value ? variantIndex : -1;
    }

    public void CopyTo(out bool[] occ, out int[] varr)
    {
        int n = Width * Height;
        occ = new bool[n];
        varr = new int[n];
        _occupied.CopyTo(occ, 0);
        _variant.CopyTo(varr, 0);
    }

    public void LoadFrom(bool[] occ, int[] varr)
    {
        int n = Width * Height;
        if (occ == null || varr == null || occ.Length != n || varr.Length != n) return;
        occ.CopyTo(_occupied, 0);
        varr.CopyTo(_variant, 0);
    }
}
