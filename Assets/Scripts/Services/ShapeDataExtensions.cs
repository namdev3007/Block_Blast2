using System.Collections.Generic;
using System.Linq;            
using UnityEngine;

public static class ShapeDataExtensions
{
    public static IEnumerable<Vector2Int> GetFilledCells(this ShapeData data)
    {
        if (data == null || data.board == null) yield break;

        for (int r = 0; r < data.rows; r++)
            for (int c = 0; c < data.columns; c++)
                if (data.board[r].column[c]) yield return new Vector2Int(r, c);
    }

    public static (int minR, int minC, int maxR, int maxC) GetBounds(this ShapeData data)
    {
        var filled = data.GetFilledCells();
        if (!filled.Any()) return (0, 0, -1, -1);  // <- dùng Any() thay Count

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
