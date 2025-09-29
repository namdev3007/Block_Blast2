// ShapeData.cs (bổ sung vào class hiện tại)
using UnityEngine;

[CreateAssetMenu(fileName = "NewShapeData", menuName = "Data/Shape Data")]
public class ShapeData : ScriptableObject
{
    public int shapeId = -1;

    [System.Serializable]
    public class Row
    {
        public bool[] column;
        public Row() { }
        public Row(int size) { CreateRow(size); }
        public void CreateRow(int size) { column = new bool[size]; ClearRow(); }
        public void ClearRow() { for (int i = 0; i < column.Length; i++) column[i] = false; }
    }

    // ====== NEW: Marker grid ======
    public enum CellTag : sbyte
    {
        None = 0,          // không có gì
        MustPlace = 1      // ô đánh dấu vị trí bắt buộc đặt (anchor)
    }

    [System.Serializable]
    public class TagRow
    {
        public sbyte[] tags; // lưu giá trị CellTag (serialize được)
        public TagRow() { }
        public TagRow(int size) { Create(size); }
        public void Create(int size)
        {
            tags = new sbyte[size];
            Clear();
        }
        public void Clear() { for (int i = 0; i < tags.Length; i++) tags[i] = (sbyte)CellTag.None; }
    }

    [Header("Grid")]
    public int columns = 0;
    public int rows = 0;
    public Row[] board;

    [Header("Markers (anchors that DO NOT occupy cells)")]
    public TagRow[] markers; // cùng kích thước với board

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

        // NEW: sync markers
        markers = new TagRow[rows];
        for (int i = 0; i < rows; i++) markers[i] = new TagRow(columns);
    }

    // NEW: chỉ reset marker
    public void ClearMarkers()
    {
        if (markers == null || markers.Length != rows)
        {
            markers = new TagRow[rows];
            for (int i = 0; i < rows; i++) markers[i] = new TagRow(columns);
            return;
        }
        for (int i = 0; i < rows; i++)
            if (markers[i] != null && markers[i].tags != null) markers[i].Clear();
    }

    // NEW: helpers
    public void SetMarker(int r, int c, CellTag tag)
    {
        if (markers == null || r < 0 || c < 0 || r >= rows || c >= columns) return;
        if (markers[r] == null || markers[r].tags == null) return;
        markers[r].tags[c] = (sbyte)tag;
    }

    public CellTag GetMarker(int r, int c)
    {
        if (markers == null || r < 0 || c < 0 || r >= rows || c >= columns) return CellTag.None;
        if (markers[r] == null || markers[r].tags == null) return CellTag.None;
        return (CellTag)markers[r].tags[c];
    }

    public System.Collections.Generic.IEnumerable<Vector2Int> GetMustPlaceCells()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (GetMarker(r, c) == CellTag.MustPlace)
                    yield return new Vector2Int(r, c);
    }

    // (giữ nguyên các hàm GetFilledCells, GetBounds...)
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
