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

    [Header("Visual")]
    public Sprite blockSprite;

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
}
