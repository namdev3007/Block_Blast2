using System.Collections.Generic;
using UnityEngine;

public static class ShapeDataExtensions
{
    public static List<Vector2Int> GetFilledCells(this ShapeData data)
    {
        var list = new List<Vector2Int>();
        if (data == null || data.board == null) return list;

        for (int r = 0; r < data.rows; r++)
            for (int c = 0; c < data.columns; c++)
                if (data.board[r].column[c]) list.Add(new Vector2Int(r, c));
        return list;
    }

    // Bounding box nhỏ nhất bao quanh các ô = true
    public static (int minR, int minC, int maxR, int maxC) GetBounds(this ShapeData data)
    {
        var filled = data.GetFilledCells();
        if (filled.Count == 0) return (0, 0, -1, -1);
        int minR = int.MaxValue, minC = int.MaxValue, maxR = int.MinValue, maxC = int.MinValue;
        foreach (var v in filled)
        {
            if (v.x < minR) minR = v.x;
            if (v.y < minC) minC = v.y;
            if (v.x > maxR) maxR = v.x;
            if (v.y > maxC) maxC = v.y;
        }
        return (minR, minC, maxR, maxC);
    }
}
