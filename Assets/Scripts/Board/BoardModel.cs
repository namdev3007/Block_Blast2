using System.Collections.Generic;

public class BoardModel
{
    public int Width { get; }
    public int Height { get; }

    public BoardModel(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public IEnumerable<int> AllIndices()
    {
        for (int i = 0; i < Width * Height; i++)
            yield return i;
    }

    public IEnumerable<int> RowIndices(int row)
    {
        int start = row * Width;
        for (int c = 0; c < Width; c++) yield return start + c;
    }

    public IEnumerable<int> ColIndices(int col)
    {
        for (int r = 0; r < Height; r++) yield return r * Width + col;
    }

    // Chia 8x8 thành 4 khối 4x4 (2x2)
    public IEnumerable<int> Block4x4Indices(int blockRow, int blockCol)
    {
        int startRow = blockRow * 4;
        int startCol = blockCol * 4;
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                yield return (startRow + r) * Width + (startCol + c);
    }
}
