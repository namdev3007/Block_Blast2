using System.Collections.Generic;
using UnityEngine;

public static class ShapeDataExtensions
{
    // Giữ lại dạng IEnumerable để ai cần có thể foreach trực tiếp
    public static IEnumerable<Vector2Int> GetFilledCells(this ShapeData data)
    {
        if (data == null || data.board == null) yield break;

        for (int r = 0; r < data.rows; r++)
            for (int c = 0; c < data.columns; c++)
                if (data.board[r].column[c])
                    yield return new Vector2Int(r, c);
    }

    // Nếu bạn cần một List (để .Count là thuộc tính), dùng hàm này
    public static List<Vector2Int> GetFilledCellsList(this ShapeData data)
    {
        var list = new List<Vector2Int>();
        if (data == null || data.board == null) return list;

        for (int r = 0; r < data.rows; r++)
            for (int c = 0; c < data.columns; c++)
                if (data.board[r].column[c])
                    list.Add(new Vector2Int(r, c));

        return list;
    }

    public static int CountCells(this ShapeData data)
    {
        if (data == null || data.board == null) return 0;
        int n = 0;
        for (int r = 0; r < data.rows; r++)
            for (int c = 0; c < data.columns; c++)
                if (data.board[r].column[c]) n++;
        return n;
    }

    public static (int minR, int minC, int maxR, int maxC) GetBounds(this ShapeData data)
    {
        if (data == null || data.board == null)
            return (0, 0, -1, -1);

        int minR = int.MaxValue, minC = int.MaxValue;
        int maxR = int.MinValue, maxC = int.MinValue;
        bool found = false;

        for (int r = 0; r < data.rows; r++)
        {
            for (int c = 0; c < data.columns; c++)
            {
                if (!data.board[r].column[c]) continue;

                found = true;
                if (r < minR) minR = r;
                if (c < minC) minC = c;
                if (r > maxR) maxR = r;
                if (c > maxC) maxC = c;
            }
        }

        return found ? (minR, minC, maxR, maxC) : (0, 0, -1, -1);
    }
}
