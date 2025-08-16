using UnityEngine;

[CreateAssetMenu(fileName = "NewShapeData", menuName = "Data/Shape Data")]
public class ShapeData : ScriptableObject
{
    [System.Serializable]
    public class Row
    {
        public bool[] column;
        public Row() { }
        public Row(int size) { CreateRow(size); }
        public void CreateRow(int size) { column = new bool[size]; ClearRow(); }
        public void ClearRow() { for (int i = 0; i < column.Length; i++) column[i] = false; }
    }

    [Header("Grid")]
    public int columns = 0;
    public int rows = 0;
    public Row[] board;

    public void Clear()
    {
        if (board == null || board.Length != rows) { CreateNewBoard(); return; }
        for (int i = 0; i < rows; i++)
            if (board[i] != null && board[i].column != null) board[i].ClearRow();
    }

    public void CreateNewBoard()
    {
        board = new Row[rows];
        for (int i = 0; i < rows; i++) board[i] = new Row(columns);
    }

    public System.Collections.Generic.IEnumerable<Vector2Int> GetFilledCells()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (board[r].column[c]) yield return new Vector2Int(r, c);
    }
    public (int minR, int minC, int maxR, int maxC) GetBounds()
    {
        int minR = int.MaxValue, minC = int.MaxValue, maxR = int.MinValue, maxC = int.MinValue;
        bool any = false;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (board[r].column[c])
                {
                    any = true;
                    if (r < minR) minR = r;
                    if (c < minC) minC = c;
                    if (r > maxR) maxR = r;
                    if (c > maxC) maxC = c;
                }
        if (!any) return (0, 0, -1, -1);
        return (minR, minC, maxR, maxC);
    }
}
