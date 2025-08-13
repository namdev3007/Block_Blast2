using System;
using UnityEngine;

[Serializable]
public class BoardModel
{
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    public BoardModel(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public int GetRegionId(int row, int col)
    {
        bool top = row < Rows / 2;      // 0..3
        bool left = col < Columns / 2;  // 0..3
        if (top && left) return 0;      // TL
        if (top && !left) return 1;     // TR
        if (!top && left) return 2;     // BL
        return 3;                       // BR
    }

    public (int row, int col) IndexToRC(int index)
        => (index / Columns, index % Columns);

    public int RCToIndex(int row, int col)
        => row * Columns + col;
}
